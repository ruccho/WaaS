#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WaaS.ComponentModel.Runtime
{
    internal class SerializedLoweringPusher : LoweringPusherBase
    {
        private static readonly Stack<SerializedLoweringPusher> Pool = new();
        private uint destinationCursor;

        // The destination is held as an offset into the linear memory instead of a slice of its buffer,
        // because the buffer is replaced when the memory grows (e.g. by realloc while lowering a string).
        private WaaS.Runtime.Memory? destinationMemory;
        private int destinationOffset;
        private int destinationLength;

        private Span<byte> DestinationSpan =>
            destinationMemory!.Span.Slice(destinationOffset, destinationLength);

        public static SerializedLoweringPusher Get(WaaS.Runtime.Memory memory, int offset, int length)
        {
            if (!Pool.TryPop(out var pooled)) pooled = new SerializedLoweringPusher();
            pooled.destinationMemory = memory;
            pooled.destinationOffset = offset;
            pooled.destinationLength = length;
            pooled.destinationCursor = 0;
            return pooled;
        }

        protected override void Dispose(bool reuse)
        {
            destinationMemory = null;
            if (reuse) Pool.Push(this);
        }

        protected override void PushU8Core(byte value)
        {
            DestinationSpan[checked((int)destinationCursor++)] = value;
            MoveNextType();
        }

        protected override void PushU16Core(ushort value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 1);
            Unsafe.As<byte, ushort>(ref DestinationSpan[checked((int)destinationCursor)]) = value;
            destinationCursor += 2;
            MoveNextType();
        }

        protected override void PushU32Core(uint value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)destinationCursor)]) = value;
            destinationCursor += 4;
            MoveNextType();
        }

        protected override void PushListDataWithoutMoving(uint ptr, uint length)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)destinationCursor)]) = ptr;
            destinationCursor += 4;
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)destinationCursor)]) = length;
            destinationCursor += 4;
        }

        protected override void PushU64Core(ulong value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 3);
            Unsafe.As<byte, ulong>(ref DestinationSpan[checked((int)destinationCursor)]) = value;
            destinationCursor += 8;
            MoveNextType();
        }

        protected override void PushS8Core(sbyte value)
        {
            PushU8Core(unchecked((byte)value));
        }

        protected override void PushS16Core(short value)
        {
            PushU16Core(unchecked((ushort)value));
        }

        protected override void PushS32Core(int value)
        {
            PushU32Core(unchecked((uint)value));
        }

        protected override void PushS64Core(long value)
        {
            PushU64Core(unchecked((ulong)value));
        }

        protected override void PushF32Core(float value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, float>(ref DestinationSpan[checked((int)destinationCursor)]) = value;
            destinationCursor += 4;
            MoveNextType();
        }

        protected override void PushF64Core(double value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 3);
            Unsafe.As<byte, double>(ref DestinationSpan[checked((int)destinationCursor)]) = value;
            destinationCursor += 8;
            MoveNextType();
        }

        public override void PushString(uint ptr, uint length)
        {
            if (MoveNextType<IPrimitiveValueType>() is not { Kind: PrimitiveValueTypeKind.String })
                throw new InvalidOperationException();

            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)destinationCursor)]) = ptr;
            destinationCursor += 4;
            Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)destinationCursor)]) = length;
            destinationCursor += 4;
        }

        public override ValuePusher PushRecord()
        {
            var recordType = MoveNextType<IRecordType>();

            var size = recordType.ElementSize;
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, recordType.AlignmentRank);
            var dest = Get(destinationMemory!, destinationOffset + checked((int)destinationCursor), size);
            destinationCursor += size;
            return dest.Init(Context ?? throw new InvalidOperationException(),
                ElementTypeSelector.FromRecord(recordType)).Wrap();
        }

        public override ValuePusher PushVariant(int caseIndex)
        {
            var variantType = MoveNextType<IVariantType>();
            var caseType = variantType.Cases.Span[caseIndex].Type;

            // a variant occupies its whole size regardless of the case
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, variantType.AlignmentRank);
            var start = destinationCursor;

            var discriminantTypeSizeRank = variantType.DiscriminantTypeSizeRank;
            switch (discriminantTypeSizeRank)
            {
                case 0:
                    DestinationSpan[checked((int)start)] = checked((byte)caseIndex);
                    break;
                case 1:
                    Unsafe.As<byte, ushort>(ref DestinationSpan[checked((int)start)]) = checked((ushort)caseIndex);
                    break;
                case 2:
                    Unsafe.As<byte, uint>(ref DestinationSpan[checked((int)start)]) = checked((uint)caseIndex);
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            // the payload is aligned to the maximum alignment of the cases
            var payloadOffset = Utils.ElementSizeAlignTo(start + (1u << discriminantTypeSizeRank),
                variantType.MaxCaseAlignmentRank);
            destinationCursor = start + variantType.ElementSize;

            if (caseType == null) return default;

            var caseTypeDespecialized = caseType.Despecialize();
            var dest = Get(destinationMemory!, destinationOffset + checked((int)payloadOffset),
                caseTypeDespecialized.ElementSize);
            return dest.Init(Context ?? throw new InvalidOperationException(),
                ElementTypeSelector.FromSingle(caseTypeDespecialized)).Wrap();
        }
    }
}