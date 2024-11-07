#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Text;
using WaaS.ComponentModel.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    internal abstract class LoweringPusherBase : IValuePusherCore
    {
        private int typeCursor;

        protected ICanonContext? Context { get; private set; }
        private ElementTypeSelector RootType { get; set; }

        public ushort Version { get; private set; }

        public void Dispose(ushort version)
        {
            if (version != Version) return;
            var reuse = ++Version != ushort.MaxValue;
            typeCursor = default;
            Dispose(reuse);
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

        public void PushChar32(uint value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Char })
                throw new InvalidOperationException();
            PushU32Core(value);
        }

        public ValuePusher PushList(int length)
        {
            var listType = MoveNextType<IListType>();

            var despecialized = listType.Despecialize();
            var size = checked((uint)(despecialized.ElementSize * length));
            var ptr = Realloc(0, 0,
                checked((uint)(1 << despecialized.AlignmentRank)),
                size);

            PushU32Core(ptr);
            PushU32Core(checked((uint)length));

            return SerializedLoweringPusher.Get(
                    Context!.Options.MemoryToRealloc!.AsMemory().Slice(checked((int)ptr), checked((int)size)))
                .Init(Context, ElementTypeSelector.FromList(listType, length))
                .Wrap();
        }

        public void PushFlags(uint flagValue)
        {
            if (GetNextType() is not IFlagsType flagsType) throw new InvalidOperationException();
            switch (flagsType.ElementSize)
            {
                case 1:
                    PushU8Core(checked((byte)flagValue)); // NOTE: should we uncheck?
                    break;
                case 2:
                    PushU16Core(checked((ushort)flagValue));
                    break;
                case 4:
                    PushU32Core(flagValue);
                    break;
            }
        }

        public void PushOwned(Owned handle)
        {
            if (GetNextType() is not IOwnedType owned) throw new InvalidOperationException();
            if (owned.Type != handle.Type) throw new InvalidOperationException();
            PushU32Core(handle.GetValue());
        }

        public void PushBorrowed(Borrowed handle)
        {
            if (GetNextType() is not IBorrowedType borrowed) throw new InvalidOperationException();
            if (borrowed.Type != handle.Type) throw new InvalidOperationException();
            PushU32Core(handle.GetValue());
        }

        public void Push(ReadOnlySpan<char> value)
        {
            if (GetNextType() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.String })
                throw new InvalidOperationException();
            // MoveNextType();

            var destEncoding = Context!.Options.StringEncoding;
            uint ptr;
            uint length;
            switch (destEncoding)
            {
                case CanonOptionStringEncodingKind.Utf8:
                {
                    var encoding = Encoding.UTF8;
                    length = (uint)encoding.GetByteCount(value);
                    ptr = Realloc(0, 0, 1, length);
                    encoding.GetBytes(value,
                        Context.Options.MemoryToRealloc!.Span.Slice(checked((int)ptr), (int)length));
                    break;
                }
                case CanonOptionStringEncodingKind.Utf16:
                case CanonOptionStringEncodingKind.Latin1Utf16: // TODO: latin1
                {
                    // store_string_copy()
                    var span = MemoryMarshal.AsBytes(value);
                    ptr = Realloc(0, 0, 2, checked((uint)span.Length));
                    if ((ptr & 1) != 0) throw new TrapException();
                    span.CopyTo(Context.Options.MemoryToRealloc!.Span.Slice(checked((int)ptr), span.Length));
                    length = (uint)value.Length;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (destEncoding == CanonOptionStringEncodingKind.Latin1Utf16) length |= (uint)1 << 31;

            PushString(ptr, length);
        }

        public abstract ValuePusher PushRecord();
        public abstract ValuePusher PushVariant(int caseIndex);
        public abstract void PushString(uint ptr, uint length);

        public IValueType GetNextType()
        {
            return RootType.GetNextType(typeCursor);
        }

        public static LoweringPusherBase GetRoot(ICanonContext context, bool isArgument, int flattenThreshold,
            out StackValueItems values)
        {
            uint count = 0;
            if (isArgument)
                foreach (var parameter in context.ComponentFunction.Type.Parameters.Span)
                {
                    if (parameter.Type is not IDespecializedValueType despecialized)
                        despecialized = parameter.Type.Despecialize();

                    count += despecialized.FlattenedCount;
                }
            else
                count = context.ComponentFunction.Type.Result?.Despecialize().FlattenedCount ?? 0;

            var flatten = count <= flattenThreshold;
            var resultType = context.ComponentFunction.Type.Result?.Despecialize();
            var type = isArgument ? context.ComponentFunction.Type.ParameterType : resultType;

            LoweringPusherBase result;
            if (flatten)
            {
                result = FlattenedLoweringPusher.Get(checked((int)count), out var dest);
                values = new StackValueItems(dest, result.Wrap());
            }
            else
            {
                var ptr = context.Realloc(0, 0, checked((uint)(1 << type!.AlignmentRank)),
                    type.ElementSize);
                result = SerializedLoweringPusher.Get(
                    context.Options.MemoryToRealloc!.AsMemory().Slice(checked((int)ptr), type.ElementSize));
                values = new StackValueItems(new StackValueItem(ptr));
            }

            var selector = isArgument
                ? ElementTypeSelector.FromRecord(context.ComponentFunction.Type.ParameterType)
                : ElementTypeSelector.FromSingle(resultType);
            result.Init(context, selector);

            return result;
        }

        protected LoweringPusherBase Init(ICanonContext context, ElementTypeSelector? rootType)
        {
            Context = context;
            RootType = rootType ?? ElementTypeSelector.FromRecord(context.ComponentFunction.Type.ParameterType);
            return this;
        }

        protected T MoveNextType<T>() where T : IValueType
        {
            var next = GetNextType();
            if (next is not T typed)
            {
                next = next.Despecialize();
                if (next is not T typed1) throw new InvalidOperationException();
                typed = typed1;
            }

            typeCursor++;
            return typed;
        }

        protected IValueType MoveNextType()
        {
            var result = GetNextType();
            typeCursor++;
            return result;
        }

        private uint Realloc(uint originalPtr, uint originalSize, uint alignment, uint newSize)
        {
            return Context!.Realloc(originalPtr, originalSize, alignment, newSize);
        }

        protected abstract void Dispose(bool reuse);

        protected abstract void PushU8Core(byte value);

        protected abstract void PushU16Core(ushort value);

        protected abstract void PushU32Core(uint value);

        protected abstract void PushU64Core(ulong value);

        protected abstract void PushF32Core(float value);

        protected abstract void PushF64Core(double value);
    }
}