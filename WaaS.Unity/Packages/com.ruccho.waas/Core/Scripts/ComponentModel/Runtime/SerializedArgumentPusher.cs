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
        private Memory<byte> Destination { get; set; }

        public static SerializedLoweringPusher Get(Memory<byte> destination)
        {
            if (!Pool.TryPop(out var pooled)) pooled = new SerializedLoweringPusher();
            pooled.Destination = destination;
            pooled.destinationCursor = 0;
            return pooled;
        }

        protected override void Dispose(bool reuse)
        {
            if(reuse) Pool.Push(this);
        }

        protected override void PushU8Core(byte value)
        {
            Destination.Span[checked((int)destinationCursor++)] = value;
            MoveNextType();
        }

        protected override void PushU16Core(ushort value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 1);
            Unsafe.As<byte, ushort>(ref Destination.Span[checked((int)destinationCursor)]) = value;
            MoveNextType();
        }

        protected override void PushU32Core(uint value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, uint>(ref Destination.Span[checked((int)destinationCursor)]) = value;
            MoveNextType();
        }

        protected override void PushU64Core(ulong value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 3);
            Unsafe.As<byte, ulong>(ref Destination.Span[checked((int)destinationCursor)]) = value;
            MoveNextType();
        }

        protected override void PushF32Core(float value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            Unsafe.As<byte, float>(ref Destination.Span[checked((int)destinationCursor)]) = value;
            MoveNextType();
        }

        protected override void PushF64Core(double value)
        {
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 3);
            Unsafe.As<byte, double>(ref Destination.Span[checked((int)destinationCursor)]) = value;
            MoveNextType();
        }

        public override void PushString(uint ptr, uint length)
        {
            if (MoveNextType<IPrimitiveValueType>() is not { Kind: PrimitiveValueTypeKind.String })
                throw new InvalidOperationException();

            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, 2);
            var dest = Destination.Slice(checked((int)destinationCursor), 8);
            Unsafe.As<byte, uint>(ref Destination.Span[checked((int)destinationCursor)]) = ptr;
            destinationCursor += 4;
            Unsafe.As<byte, uint>(ref Destination.Span[checked((int)destinationCursor)]) = length;
            destinationCursor += 4;
        }

        public override ValuePusher PushRecord()
        {
            var recordType = MoveNextType<IRecordType>();

            var size = recordType.ElementSize;
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, recordType.AlignmentRank);
            var dest = Destination.Slice(checked((int)destinationCursor), size);
            destinationCursor += size;
            return Get(dest).Init(Context, ElementTypeSelector.FromRecord(recordType)).Wrap();
        }

        public override ValuePusher PushVariant(int caseIndex)
        {
            var variantType = MoveNextType<IVariantType>();
            var caseType = variantType.Cases.Span[caseIndex].Type;

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
            destinationCursor = Utils.ElementSizeAlignTo(destinationCursor, caseTypeDespecialized.AlignmentRank);
            var dest = Destination.Slice(checked((int)destinationCursor), size);
            destinationCursor += size;
            return Get(dest).Init(Context, ElementTypeSelector.FromSingle(caseTypeDespecialized)).Wrap();
        }
    }
}