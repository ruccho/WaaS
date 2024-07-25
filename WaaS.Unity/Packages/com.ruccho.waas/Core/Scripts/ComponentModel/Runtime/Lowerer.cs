#nullable enable

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WaaS.Runtime;
using ExecutionContext = WaaS.Runtime.ExecutionContext;

namespace WaaS.ComponentModel.Runtime
{
    public struct ComponentInvocationContext : IDisposable
    {
        public struct Async : IAsyncDisposable
        {
            private ComponentInvocationContext inner;

            internal Async(in ComponentInvocationContext inner)
            {
                this.inner = inner;
            }

            public async ValueTask DisposeAsync()
            {
                var task = inner.Context.InvokeAsync(inner.Function.CoreFunction, inner.arguments);

                if (inner.arguments != null) ArrayPool<StackValueItem>.Shared.Return(inner.arguments);

                inner.arguments = null;

                await task;
            }
        }

        private IFunction Function { get; }
        private ExecutionContext Context { get; }
        public Memory? Memory => Function.MemoryToRealloc;
        private StackValueItem[]? arguments;
        private readonly int argumentCount;

        internal uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
        {
            Span<StackValueItem> args = stackalloc StackValueItem[5];
            args[0] = new StackValueItem(originalPtr);
            args[1] = new StackValueItem(originalSize);
            args[2] = new StackValueItem(alignment);
            args[3] = new StackValueItem(newSize);

            Context.InterruptFrame(Function.ReallocFunction, args[..4], args[4..]);
            return args[4].ExpectValueI32();
        }

        internal ComponentInvocationContext(ExecutionContext executionContext, IFunction function,
            out ArgumentLowerer lowerer)
        {
            Function = function;
            Context = executionContext;

            uint count = 0;
            foreach (var parameter in function.Type.Parameters.Span)
            {
                if (parameter.Type is not IDespecializedValueType despecialized)
                    despecialized = parameter.Type.Despecialize();

                count += despecialized.FlattenedCount;
            }

            var flatten = count <= 16 /* MAX_FLAT_PARAMS */;

            var type = function.Type.ParameterType;

            if (flatten)
            {
                argumentCount = checked((int)count);
                arguments = ArrayPool<StackValueItem>.Shared.Rent(argumentCount);
                lowerer = new ArgumentLowerer(this, type, arguments);
            }
            else
            {
                var size = type.ElementSize;
                argumentCount = 1;
                arguments = ArrayPool<StackValueItem>.Shared.Rent(1);
                var ptr = Realloc(0, 0, checked((uint)(1 << type.AlignmentRank)), size);
                arguments[0] = new StackValueItem(ptr);
                lowerer = new ArgumentLowerer(this, type, Memory!.Span.Slice(checked((int)ptr), size));
            }
        }

        public void Dispose()
        {
            // execute
            Context.Invoke(Function.CoreFunction, arguments.AsSpan()[..argumentCount]);

            if (arguments != null) ArrayPool<StackValueItem>.Shared.Return(arguments);

            arguments = null;
        }

        public Async ToAsync()
        {
            return new Async(this);
        }
    }


    public ref struct ArgumentLowerer
    {
        private ComponentInvocationContext cx;
        private bool Flattened { get; }
        private Span<StackValueItem> DestFlattened { get; set; }
        private Span<byte> DestStore { get; }

        private IDespecializedValueType Type { get; }
        private int typeCursor;
        private uint serializedCursor;

        private static class ElementTypeSelector
        {
            private static readonly IElementTypeSelector<IPrimitiveValueType>
                Primitive = new ValueTypeCursorPrimitiveValueType();

            private static readonly IElementTypeSelector<IRecordType> Record = new ValueTypeCursorRecordType();
            private static readonly IElementTypeSelector<IListType> List = new ValueTypeCursorListType();

            public static IValueType GetNextType(IDespecializedValueType type, int index)
            {
                return type switch
                {
                    IPrimitiveValueType typedType => Primitive.GetNextType(typedType, index),
                    IRecordType typedType => Record.GetNextType(typedType, index),
                    IListType typedType => List.GetNextType(typedType, index),
                    _ => null
                } ?? throw new ArgumentOutOfRangeException(nameof(type));
            }
        }


        public ArgumentLowerer(ComponentInvocationContext cx, IDespecializedValueType type, Span<StackValueItem> dest)
        {
            // flattened
            this.cx = cx;
            Type = type;
            DestFlattened = dest;
            DestStore = default;

            Flattened = true;
            serializedCursor = 0;
            typeCursor = 0;
        }

        public ArgumentLowerer(ComponentInvocationContext cx, IDespecializedValueType type, Span<byte> destStore)
        {
            // serialized
            this.cx = cx;
            Type = type;
            DestFlattened = default;
            DestStore = destStore;

            Flattened = false;
            serializedCursor = 0;
            typeCursor = 0;
        }

        public IValueType GetNextType()
        {
            return ElementTypeSelector.GetNextType(Type, typeCursor);
        }

        private void PushU8Core(byte value)
        {
            if (Flattened)
            {
                ref var t = ref DestFlattened[0];
                t = t.valueType == ValueType.I64
                    ? new StackValueItem((ulong)value)
                    : new StackValueItem(value);
                DestFlattened = DestFlattened[1..];
            }
            else
            {
                DestStore[checked((int)serializedCursor++)] = value;
            }

            typeCursor++;
        }

        private void PushU16Core(ushort value)
        {
            if (Flattened)
            {
                ref var t = ref DestFlattened[0];
                t = t.valueType == ValueType.I64
                    ? new StackValueItem((ulong)value)
                    : new StackValueItem(value);
                DestFlattened = DestFlattened[1..];
            }
            else
            {
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, 1);
                Unsafe.As<byte, ushort>(ref DestStore[checked((int)serializedCursor)]) = value;
            }

            typeCursor++;
        }

        private void PushU32Core(uint value)
        {
            if (Flattened)
            {
                ref var t = ref DestFlattened[0];
                t = t.valueType == ValueType.I64
                    ? new StackValueItem((ulong)value)
                    : new StackValueItem(value);
                DestFlattened = DestFlattened[1..];
            }
            else
            {
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, 2);
                Unsafe.As<byte, uint>(ref DestStore[checked((int)serializedCursor)]) = value;
            }

            typeCursor++;
        }

        private void PushU64Core(ulong value)
        {
            if (Flattened)
            {
                DestFlattened[0] = new StackValueItem(value);
                DestFlattened = DestFlattened[1..];
            }
            else
            {
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, 3);
                Unsafe.As<byte, ulong>(ref DestStore[checked((int)serializedCursor)]) = value;
            }

            typeCursor++;
        }

        private void PushF32Core(float value)
        {
            if (Flattened)
            {
                ref var t = ref DestFlattened[0];
                t = t.valueType switch
                {
                    ValueType.I32 => new StackValueItem(Unsafe.As<float, uint>(ref value)),
                    ValueType.I64 => new StackValueItem((ulong)Unsafe.As<float, uint>(ref value)),
                    _ => new StackValueItem(value)
                };

                DestFlattened = DestFlattened[1..];
            }
            else
            {
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, 2);
                Unsafe.As<byte, float>(ref DestStore[checked((int)serializedCursor)]) = value;
            }

            typeCursor++;
        }

        private void PushF64Core(double value)
        {
            if (Flattened)
            {
                ref var t = ref DestFlattened[0];
                t = t.valueType switch
                {
                    ValueType.I64 => new StackValueItem(Unsafe.As<double, ulong>(ref value)),
                    _ => new StackValueItem(value)
                };

                DestFlattened = DestFlattened[1..];
            }
            else
            {
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, 3);
                Unsafe.As<byte, double>(ref DestStore[checked((int)serializedCursor)]) = value;
            }

            typeCursor++;
        }

        public ArgumentLowerer PushRecord()
        {
            if (GetNextType().Despecialize() is not IRecordType recordType)
                throw new InvalidOperationException();
            typeCursor++;
            if (Flattened)
            {
                var count = checked((int)recordType.FlattenedCount);
                var dest = DestFlattened[..count];
                DestFlattened = DestFlattened[count..];
                return new ArgumentLowerer(cx, recordType, dest);
            }
            else
            {
                var size = recordType.ElementSize;
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, recordType.AlignmentRank);
                var dest = DestStore.Slice(checked((int)serializedCursor), size);
                serializedCursor += size;
                return new ArgumentLowerer(cx, recordType, dest);
            }
        }

        public ArgumentLowerer PushVariant(int caseIndex)
        {
            if (GetNextType().Despecialize() is not IVariantType variantType)
                throw new InvalidOperationException();
            typeCursor++;

            var caseType = variantType.Cases.Span[caseIndex].Type;

            if (Flattened)
            {
                DestFlattened[0] = new StackValueItem(caseIndex);
                DestFlattened = DestFlattened[1..];

                if (caseType == null) return default;

                var count = checked((int)caseType.Despecialize().FlattenedCount);
                var dest = DestFlattened[..count];
                DestFlattened = DestFlattened[count..];

                // pre-join type
                // NOTE: is managed array better for large variant?

                Span<ValueType> prejoinedTypes = stackalloc ValueType[checked((int)variantType.FlattenedCount)];
                variantType.Flatten(prejoinedTypes);
                prejoinedTypes = prejoinedTypes[1..];

                for (var i = 0; i < count; i++)
                    if ((byte)dest[i].valueType == 0) // not fixed yet
                        dest[i] = new StackValueItem(prejoinedTypes[i]);

                return new ArgumentLowerer(cx, caseType.Despecialize(), dest);
            }
            else
            {
                var discriminantTypeSizeRank = variantType.DiscriminantTypeSizeRank;
                switch (discriminantTypeSizeRank)
                {
                    case 1:
                        PushU8Core(checked((byte)caseIndex));
                        break;
                    case 2:
                        PushU16Core(checked((ushort)caseIndex));
                        break;
                    case 4:
                        PushU32Core(checked((uint)caseIndex));
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                }

                if (caseType == null) return default;

                var caseTypeDespecialized = caseType.Despecialize();
                var size = caseTypeDespecialized.ElementSize;
                serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, caseTypeDespecialized.AlignmentRank);
                var dest = DestStore.Slice(checked((int)serializedCursor), size);
                serializedCursor += size;
                return new ArgumentLowerer(cx, caseTypeDespecialized, dest);
            }
        }

        public ArgumentLowerer PushList(int length)
        {
            if (GetNextType().Despecialize() is not IListType listType)
                throw new InvalidOperationException();
            typeCursor++;

            var despecialized = listType.Despecialize();
            var size = checked((uint)(despecialized.ElementSize * length));
            var ptr = cx.Realloc(0, 0,
                checked((uint)(1 << despecialized.AlignmentRank)),
                size);

            PushU32Core(ptr);
            PushU32Core(checked((uint)length));

            return new ArgumentLowerer(cx, listType, cx.Memory!.Span.Slice(checked((int)ptr), checked((int)size)));
        }

        public ArgumentLowerer PushTuple()
        {
            if (GetNextType() is not ITupleType) throw new InvalidOperationException();
            return PushRecord();
        }

        public ArgumentLowerer PushEnum(int labelIndex)
        {
            if (GetNextType() is not IEnumType) throw new InvalidOperationException();
            return PushVariant(labelIndex);
        }

        public ArgumentLowerer PushOptionNone()
        {
            if (GetNextType() is not IOptionType) throw new InvalidOperationException();
            return PushVariant(0);
        }

        public ArgumentLowerer PushOptionSome()
        {
            if (GetNextType() is not IOptionType) throw new InvalidOperationException();
            return PushVariant(1);
        }

        public ArgumentLowerer PushResultOk()
        {
            if (GetNextType() is not IResultType) throw new InvalidOperationException();
            return PushVariant(0);
        }

        public ArgumentLowerer PushResultError()
        {
            if (GetNextType() is not IResultType) throw new InvalidOperationException();
            return PushVariant(1);
        }

        public void Push(bool value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Bool })
                throw new InvalidOperationException();
            PushU8Core(value ? (byte)1 : (byte)0);
        }

        public void Push(byte value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U8 })
                throw new InvalidOperationException();
            PushU8Core(value);
        }

        public void Push(sbyte value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S8 })
                throw new InvalidOperationException();
            PushU8Core(unchecked((byte)value));
        }

        public void Push(ushort value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U16 })
                throw new InvalidOperationException();
            PushU16Core(value);
        }

        public void Push(short value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S16 })
                throw new InvalidOperationException();
            PushU16Core(unchecked((ushort)value));
        }

        public void Push(uint value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U32 })
                throw new InvalidOperationException();
            PushU32Core(value);
        }

        public void Push(int value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S32 })
                throw new InvalidOperationException();
            PushU32Core(unchecked((uint)value));
        }

        public void Push(ulong value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U64 })
                throw new InvalidOperationException();
            PushU64Core(value);
        }

        public void Push(long value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S64 })
                throw new InvalidOperationException();
            PushU64Core(unchecked((ulong)value));
        }

        public void Push(float value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.F32 })
                throw new InvalidOperationException();
            PushF32Core(value);
        }

        public void Push(double value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.F64 })
                throw new InvalidOperationException();
            PushF64Core(value);
        }

        public void Push(char value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Char })
                throw new InvalidOperationException();
            PushU32Core(value);
        }

        public void PushChar32(uint value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Char })
                throw new InvalidOperationException();
            PushU32Core(value);
        }
    }

    internal interface IElementTypeSelector<in T> where T : IDespecializedValueType
    {
        IValueType GetNextType(T type, int index);
    }

    internal class ValueTypeCursorRecordType : IElementTypeSelector<IRecordType>
    {
        public IValueType GetNextType(IRecordType type, int index)
        {
            return type.Fields.Span[index].Type;
        }
    }

    internal class ValueTypeCursorListType : IElementTypeSelector<IListType>
    {
        public IValueType GetNextType(IListType type, int index)
        {
            return type.ElementType;
        }
    }

    internal class ValueTypeCursorPrimitiveValueType : IElementTypeSelector<IPrimitiveValueType>
    {
        public IValueType GetNextType(IPrimitiveValueType type, int index)
        {
            if (index == 0) return type;
            throw new InvalidOperationException();
        }
    }
}