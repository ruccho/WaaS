#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    internal class FlattenedLoweringPusher : LoweringPusherBase
    {
        private static readonly Stack<FlattenedLoweringPusher> Pool = new();
        private int destinationCursor;
        private StackValueItem[]? rentDestination;
        private Memory<StackValueItem> Destination { get; set; }

        public static FlattenedLoweringPusher Get(int flattenedCount, out Memory<StackValueItem> destination)
        {
            if (!Pool.TryPop(out var pooled)) pooled = new FlattenedLoweringPusher();
            var dest = ArrayPool<StackValueItem>.Shared.Rent(flattenedCount);
            // for safety
            Array.Clear(dest, 0, dest.Length);

            destination = pooled.Destination =
                (pooled.rentDestination = dest)
                .AsMemory().Slice(0, flattenedCount);
            pooled.destinationCursor = 0;
            return pooled;
        }

        private static FlattenedLoweringPusher Get(Memory<StackValueItem> destination)
        {
            if (!Pool.TryPop(out var pooled)) pooled = new FlattenedLoweringPusher();
            pooled.Destination = destination;
            pooled.destinationCursor = 0;
            pooled.rentDestination = default;
            return pooled;
        }

        protected override void Dispose(bool reuse)
        {
            if (rentDestination != null) ArrayPool<StackValueItem>.Shared.Return(rentDestination);
            rentDestination = null;

            Destination = default;
            destinationCursor = 0;

            if (reuse) Pool.Push(this);
        }

        protected override void PushU8Core(byte value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushU16Core(ushort value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushU32Core(uint value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushU64Core(ulong value)
        {
            Destination.Span[destinationCursor] = new StackValueItem(value);
            destinationCursor++;
            MoveNextType();
        }

        protected override void PushS8Core(sbyte value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();;
        }

        protected override void PushS16Core(short value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushS32Core(int value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType == ValueType.I64
                ? new StackValueItem((ulong)value)
                : new StackValueItem(value);

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushS64Core(long value) => PushU64Core(unchecked((ulong)value));

        protected override void PushF32Core(float value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType switch
            {
                ValueType.I32 => new StackValueItem(Unsafe.As<float, uint>(ref value)),
                ValueType.I64 => new StackValueItem((ulong)Unsafe.As<float, uint>(ref value)),
                _ => new StackValueItem(value)
            };

            destinationCursor++;
            MoveNextType();
        }

        protected override void PushF64Core(double value)
        {
            ref var t = ref Destination.Span[destinationCursor];
            t = t.valueType switch
            {
                ValueType.I64 => new StackValueItem(Unsafe.As<double, ulong>(ref value)),
                _ => new StackValueItem(value)
            };

            destinationCursor++;
            MoveNextType();
        }

        public override void PushString(uint ptr, uint length)
        {
            if (MoveNextType<IPrimitiveValueType>() is not { Kind: PrimitiveValueTypeKind.String })
                throw new InvalidOperationException();

            var dest = Destination.Slice(destinationCursor, 2).Span;
            destinationCursor += 2;
            dest[0] = new StackValueItem(ptr);
            dest[1] = new StackValueItem(length);
        }

        public override ValuePusher PushRecord()
        {
            var recordType = MoveNextType<IRecordType>();

            var count = checked((int)recordType.FlattenedCount);
            var dest = Destination.Slice(destinationCursor, count);
            destinationCursor += count;
            return Get(dest).Init(Context ?? throw new InvalidOperationException(),
                ElementTypeSelector.FromRecord(recordType)).Wrap();
        }

        public override ValuePusher PushVariant(int caseIndex)
        {
            var variantType = MoveNextType<IVariantType>();
            var caseType = variantType.Cases.Span[caseIndex].Type;

            Destination.Span[destinationCursor] = new StackValueItem(caseIndex);
            destinationCursor++;

            var flattenedCaseCount = checked((int)variantType.FlattenedCount) - 1;

            var dest = Destination.Slice(destinationCursor, flattenedCaseCount);
            destinationCursor += flattenedCaseCount;

            // pre-join type
            // NOTE: is it better to use pooled arrays for large variants?

            Span<ValueType> prejoinedTypes = stackalloc ValueType[flattenedCaseCount + 1];
            variantType.Flatten(prejoinedTypes);
            prejoinedTypes = prejoinedTypes[1..];

            for (var i = 0; i < flattenedCaseCount; i++)
                if ((byte)dest.Span[i].valueType == 0) // not fixed yet
                    dest.Span[i] = new StackValueItem(prejoinedTypes[i]);
            
            return Get(dest).Init(Context ?? throw new InvalidOperationException(),
                ElementTypeSelector.FromSingle(caseType?.Despecialize())).Wrap();
        }
    }
}