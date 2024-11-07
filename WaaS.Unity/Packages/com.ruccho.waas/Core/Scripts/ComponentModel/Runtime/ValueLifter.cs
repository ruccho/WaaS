#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Text;
using WaaS.ComponentModel.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    public ref struct ValueLifter
    {
        private ICanonContext Context { get; }
        private ElementTypeSelector RootType { get; }
        private ReadOnlySpan<StackValueItem> SourceFlattened { get; set; }
        private readonly ReadOnlySpan<byte> source;
        private bool Flattened { get; }
        private int typeCursor;
        private uint serializedCursor;

        public IValueType GetNextType()
        {
            return RootType.GetNextType(typeCursor);
        }

        // flattened
        internal ValueLifter(ICanonContext context, ElementTypeSelector type,
            ReadOnlySpan<StackValueItem> sourceFlattened)
        {
            Context = context;
            RootType = type;
            SourceFlattened = sourceFlattened;
            source = default;
            Flattened = true;
            typeCursor = 0;
            serializedCursor = 0;
        }

        // serialized
        internal ValueLifter(ICanonContext context, ElementTypeSelector type, ReadOnlySpan<byte> source)
        {
            Context = context;
            RootType = type;
            this.source = source;
            SourceFlattened = default;
            Flattened = false;
            typeCursor = 0;
            serializedCursor = 0;
        }

        private StackValueItem NextFlattened()
        {
            var item = SourceFlattened[0];
            SourceFlattened = SourceFlattened[1..];
            return item;
        }

        private float NextFlattenedF32()
        {
            static float Convert(ulong value)
            {
                var a = unchecked((uint)value);
                return Unsafe.As<uint, float>(ref a);
            }

            var result = NextFlattened();
            return result.valueType switch
            {
                ValueType.F32 => result.valueF32,
                // for variant (types must be verified)
                ValueType.I32 => Unsafe.As<uint, float>(ref result.valueI32),
                ValueType.I64 => Convert(result.valueI64),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private double NextFlattenedF64()
        {
            var result = NextFlattened();
            return result.valueType switch
            {
                ValueType.F64 => result.valueF64,
                // for variant (types must be verified)
                ValueType.I64 => Unsafe.As<ulong, double>(ref result.valueI64),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private uint NextFlattenedI32()
        {
            var result = NextFlattened();
            return result.valueType switch
            {
                ValueType.I32 => result.valueI32,
                // for variant (types must be verified)
                ValueType.I64 => unchecked((uint)result.valueI64),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private unsafe T LoadSerialized<T>() where T : unmanaged
        {
            serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, sizeof(T) switch
            {
                1 => 0,
                2 => 1,
                4 => 2,
                8 => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(T))
            });

            var result =
                Unsafe.As<byte, T>(ref Unsafe.AsRef(in source.Slice(checked((int)serializedCursor), sizeof(T))[0]));
            serializedCursor = checked((uint)(serializedCursor + sizeof(T)));
            return result;
        }

        public bool PullBool()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Bool })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return NextFlattenedI32() != 0;
                else
                    return LoadSerialized<byte>() != 0;
            }
            finally
            {
                typeCursor++;
            }
        }

        public byte PullU8()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U8 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((byte)NextFlattenedI32());
                else
                    return LoadSerialized<byte>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public ushort PullU16()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U16 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((ushort)NextFlattenedI32());
                else
                    return LoadSerialized<ushort>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public uint PullU32()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U32 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return NextFlattenedI32();
                else
                    return LoadSerialized<uint>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public ulong PullU64()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.U64 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return NextFlattened().ExpectValueI64();
                else
                    return LoadSerialized<ulong>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public sbyte PullS8()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S8 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((sbyte)(byte)NextFlattenedI32());
                else
                    return LoadSerialized<sbyte>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public short PullS16()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S16 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((short)(ushort)NextFlattenedI32());
                else
                    return LoadSerialized<short>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public int PullS32()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S32 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((int)NextFlattenedI32());
                else
                    return LoadSerialized<int>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public long PullS64()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.S64 })
                throw new InvalidOperationException();
            try
            {
                if (Flattened)
                    return unchecked((long)NextFlattened().ExpectValueI64());
                else
                    return LoadSerialized<long>();
            }
            finally
            {
                typeCursor++;
            }
        }

        public float PullF32()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.F32 })
                throw new InvalidOperationException();
            try
            {
                float value;
                if (Flattened)
                    value = NextFlattenedF32();
                else
                    value = LoadSerialized<float>();

                if (float.IsNaN(value)) Unsafe.As<float, uint>(ref value) = 0x7fc00000;

                return value;
            }
            finally
            {
                typeCursor++;
            }
        }

        public double PullF64()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.F64 })
                throw new InvalidOperationException();
            try
            {
                double value;
                if (Flattened)
                    value = NextFlattenedF64();
                else
                    value = LoadSerialized<float>();

                if (double.IsNaN(value)) Unsafe.As<double, ulong>(ref value) = 0x7ff8000000000000;

                return value;
            }
            finally
            {
                typeCursor++;
            }
        }

        public uint PullChar()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.Char })
                throw new InvalidOperationException();
            try
            {
                uint value;
                if (Flattened)
                    value = NextFlattenedI32();
                else
                    value = LoadSerialized<uint>();

                if (value is >= 0x110000 or (>= 0xD800 and <= 0xDFFF))
                    throw new InvalidOperationException();

                return value;
            }
            finally
            {
                typeCursor++;
            }
        }

        public readonly struct StringInfo
        {
            public readonly uint begin;
            public readonly byte alignment;
            public readonly uint byteLength;
            public readonly Encoding encoding;

            public StringInfo(uint begin, byte alignment, uint byteLength, Encoding encoding)
            {
                this.begin = begin;
                this.alignment = alignment;
                this.byteLength = byteLength;
                this.encoding = encoding;
            }
        }

        public StringInfo PullStringInfo()
        {
            if (GetNextType().Despecialize() is not IPrimitiveValueType { Kind: PrimitiveValueTypeKind.String })
                throw new InvalidOperationException();

            const uint UTF16_TAG = unchecked((uint)(1 << 31));

            uint begin;
            byte alignment;
            uint byteLength;
            Encoding encoding;

            uint taggedCodeUints;
            if (Flattened)
            {
                begin = NextFlattenedI32();
                taggedCodeUints = NextFlattenedI32();
            }
            else
            {
                begin = LoadSerialized<uint>();
                taggedCodeUints = LoadSerialized<uint>();
            }

            (alignment, byteLength, encoding) = Context.Options.StringEncoding switch
            {
                CanonOptionStringEncodingKind.Utf8 => ((byte)1, taggedCodeUints, Encoding.UTF8),
                CanonOptionStringEncodingKind.Utf16 => ((byte)2, taggedCodeUints * 2, Encoding.Unicode),
                CanonOptionStringEncodingKind.Latin1Utf16 => (taggedCodeUints & UTF16_TAG) != 0
                    ? ((byte)2, (taggedCodeUints ^ UTF16_TAG) * 2, Encoding.Unicode)
                    : ((byte)2, taggedCodeUints, Encoding.GetEncoding(28591 /* latin-1 */)),
                _ => throw new ArgumentOutOfRangeException(nameof(Context.Options.StringEncoding))
            };

            return new StringInfo(begin, alignment, byteLength, encoding);
        }

        public int PollStringCharCount(in StringInfo info)
        {
            if ((info.begin & (info.alignment - 1)) != 0) throw new InvalidOperationException();

            var bytes = Context.Options.MemoryToRealloc!.Span.Slice(checked((int)info.begin),
                checked((int)info.byteLength));
            return info.encoding.GetCharCount(bytes);
        }

        public int GetStringMaxCharCount(in StringInfo info)
        {
            if ((info.begin & (info.alignment - 1)) != 0) throw new InvalidOperationException();

            var bytes = Context.Options.MemoryToRealloc!.Span.Slice(checked((int)info.begin),
                checked((int)info.byteLength));
            return info.encoding.GetMaxCharCount(bytes.Length);
        }

        public string PullString(in StringInfo info)
        {
            try
            {
                if ((info.begin & (info.alignment - 1)) != 0) throw new InvalidOperationException();

                var bytes = Context.Options.MemoryToRealloc!.Span.Slice(checked((int)info.begin),
                    checked((int)info.byteLength));

                return info.encoding.GetString(bytes);
            }
            finally
            {
                typeCursor++;
            }
        }

        public int GetString(in StringInfo info, /* scoped */Span<char> result)
        {
            try
            {
                if ((info.begin & (info.alignment - 1)) != 0) throw new InvalidOperationException();

                var bytes = Context.Options.MemoryToRealloc!.Span.Slice(checked((int)info.begin),
                    checked((int)info.byteLength));

                return info.encoding.GetChars(bytes, result);
            }
            finally
            {
                typeCursor++;
            }
        }

        public ValueLifter PullList(out uint length)
        {
            if (GetNextType().Despecialize() is not IListType listType)
                throw new InvalidOperationException();

            try
            {
                uint begin;
                if (Flattened)
                {
                    begin = NextFlattenedI32();
                    length = NextFlattenedI32();
                }
                else
                {
                    begin = LoadSerialized<uint>();
                    length = LoadSerialized<uint>();
                }

                var elementType = listType.ElementType.Despecialize();

                if ((begin & ((1 << elementType.AlignmentRank) - 1)) != 0) throw new InvalidOperationException();

                return new ValueLifter(Context, ElementTypeSelector.FromList(listType, checked((int)length)),
                    Context.Options.MemoryToRealloc!.Span.Slice(checked((int)begin),
                        checked((int)(length * elementType.ElementSize))));
            }
            finally
            {
                typeCursor++;
            }
        }

        public ValueLifter PullRecord()
        {
            if (GetNextType().Despecialize() is not IRecordType recordType)
                throw new InvalidOperationException();

            try
            {
                if (Flattened)
                {
                    var count = checked((int)recordType.FlattenedCount);
                    var newSource = SourceFlattened[..count];
                    SourceFlattened = SourceFlattened[count..];
                    return new ValueLifter(Context, ElementTypeSelector.FromRecord(recordType), newSource);
                }
                else
                {
                    serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, recordType.AlignmentRank);
                    var length = recordType.ElementSize;
                    var newSource = source.Slice(checked((int)serializedCursor), length);
                    serializedCursor = checked(serializedCursor + length);
                    return new ValueLifter(Context, ElementTypeSelector.FromRecord(recordType), newSource);
                }
            }
            finally
            {
                typeCursor++;
            }
        }

        public ValueLifter PullVariant(out uint caseIndex)
        {
            if (GetNextType().Despecialize() is not IVariantType variantType)
                throw new InvalidOperationException();

            try
            {
                if (Flattened)
                {
                    // stack value type is verified by context
                    caseIndex = NextFlattenedI32();
                    var caseType = variantType.Cases.Span[checked((int)caseIndex)].Type?.Despecialize();
                    if (caseType == null) return default;

                    var count = checked((int)caseType.FlattenedCount);
                    var newSource = SourceFlattened[..count];
                    SourceFlattened = SourceFlattened[count..];
                    return new ValueLifter(Context, ElementTypeSelector.FromSingle(caseType), newSource);
                }
                else
                {
                    caseIndex = LoadSerialized<uint>();
                    var caseType = variantType.Cases.Span[checked((int)caseIndex)].Type?.Despecialize();
                    if (caseType == null) return default;

                    serializedCursor = Utils.ElementSizeAlignTo(serializedCursor, caseType.AlignmentRank);
                    var length = caseType.ElementSize;
                    var newSource = source.Slice(checked((int)serializedCursor), length);
                    serializedCursor = checked(serializedCursor + length);
                    return new ValueLifter(Context, ElementTypeSelector.FromSingle(caseType), newSource);
                }
            }
            finally
            {
                typeCursor++;
            }
        }

        public uint PullFlags()
        {
            if (GetNextType().Despecialize() is not IFlagsType flagsType)
                throw new InvalidOperationException();

            try
            {
                if (Flattened)
                    return NextFlattenedI32();
                else
                    switch (flagsType.ElementSize)
                    {
                        case 1:
                            return LoadSerialized<byte>();
                        case 2:
                            return LoadSerialized<ushort>();
                        case 4:
                            return LoadSerialized<uint>();
                        default:
                            throw new InvalidOperationException();
                    }
            }
            finally
            {
                typeCursor++;
            }
        }

        public Owned PullOwned()
        {
            if (GetNextType().Despecialize() is not IOwnedType { Type: { } resourceType })
                throw new InvalidOperationException();

            try
            {
                var value = Flattened ? NextFlattenedI32() : LoadSerialized<uint>();

                return Ownership.GetHandle(resourceType, value);
            }
            finally
            {
                typeCursor++;
            }
        }

        public Borrowed PullBorrowed()
        {
            if (GetNextType().Despecialize() is not IBorrowedType { Type: { } resourceType })
                throw new InvalidOperationException();

            try
            {
                var value = Flattened ? NextFlattenedI32() : LoadSerialized<uint>();

                var owned = Ownership.GetHandle(resourceType, value);
                //owned.// TODO: lifetime
                return owned.Borrow();
            }
            finally
            {
                typeCursor++;
            }
        }
    }
}