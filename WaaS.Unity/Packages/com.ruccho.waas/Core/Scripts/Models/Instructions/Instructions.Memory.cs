using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    #region Load

    public abstract partial class Load<TValue, TValueType, TReadValue> : Instruction
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
        where TReadValue : unmanaged
    {
        [Operand(0)] public uint Align { get; }
        [Operand(1)] public uint Offset { get; }

        protected abstract TValue Convert(in TReadValue value);

        public override void Execute(WasmStackFrame current)
        {
            try
            {
                var readSize = Unsafe.SizeOf<TReadValue>();

                var i = current.Pop().ExpectValueI32();
                var ea = checked((int)(Offset + i));

                var memory = current.Instance.GetMemory(0);

                if (ea + readSize > memory.Length) current.Context.Trap();

                var slice = memory[ea..];
                ref var asRef = ref MemoryMarshal.GetReference(slice);

                var result = Convert(Unsafe.ReadUnaligned<TReadValue>(ref asRef));

                var ti = default(TValueType);
                ti.Push(current, result);
            }
            catch (OverflowException)
            {
                current.Context.Trap();
            }
        }

        public override void Validate(in ValidationContext context)
        {
            var readSize = Unsafe.SizeOf<TReadValue>();
            var readSizeRank = 0;
            while (readSize != 0)
            {
                readSize >>= 1;
                readSizeRank++;
            }

            if (Align >= readSizeRank)
                throw new InvalidModuleException(
                    $"Alignment must be larger than natural. expected: <{readSizeRank}, actual: {Align}");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.I32);
            stackState.Push(ValueTypeRegistry.GetFor<TValue>().Value);
        }
    }

    public abstract partial class Load<TValue, TValueType> : Load<TValue, TValueType, TValue>
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override TValue Convert(in TValue value)
        {
            return value;
        }
    }

    [OpCode(0x28)]
    public partial class LoadI32 : Load<uint, ValueTypeI32>
    {
    }

    [OpCode(0x29)]
    public partial class LoadI64 : Load<ulong, ValueTypeI64>
    {
    }

    [OpCode(0x2A)]
    public partial class LoadF32 : Load<float, ValueTypeF32>
    {
    }

    [OpCode(0x2B)]
    public partial class LoadF64 : Load<double, ValueTypeF64>
    {
    }

    [OpCode(0x2C)]
    public partial class LoadI8AsI32 : Load<uint, ValueTypeI32, sbyte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override uint Convert(in sbyte value)
        {
            return unchecked((uint)value);
        }
    }

    [OpCode(0x2D)]
    public partial class LoadU8AsI32 : Load<uint, ValueTypeI32, byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override uint Convert(in byte value)
        {
            return value;
        }
    }

    [OpCode(0x2E)]
    public partial class LoadI16AsI32 : Load<uint, ValueTypeI32, short>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override uint Convert(in short value)
        {
            return unchecked((uint)value);
        }
    }

    [OpCode(0x2F)]
    public partial class LoadU16AsI32 : Load<uint, ValueTypeI32, ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override uint Convert(in ushort value)
        {
            return value;
        }
    }

    [OpCode(0x30)]
    public partial class LoadI8AsI64 : Load<ulong, ValueTypeI64, sbyte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in sbyte value)
        {
            return unchecked((ulong)value);
        }
    }

    [OpCode(0x31)]
    public partial class LoadU8AsI64 : Load<ulong, ValueTypeI64, byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in byte value)
        {
            return value;
        }
    }

    [OpCode(0x32)]
    public partial class LoadI16AsI64 : Load<ulong, ValueTypeI64, short>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in short value)
        {
            return unchecked((ulong)value);
        }
    }

    [OpCode(0x33)]
    public partial class LoadU16AsI64 : Load<ulong, ValueTypeI64, ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in ushort value)
        {
            return value;
        }
    }

    [OpCode(0x34)]
    public partial class LoadI32AsI64 : Load<ulong, ValueTypeI64, int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in int value)
        {
            return unchecked((ulong)value);
        }
    }

    [OpCode(0x35)]
    public partial class LoadU32AsI64 : Load<ulong, ValueTypeI64, uint>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ulong Convert(in uint value)
        {
            return value;
        }
    }

    #endregion

    #region Store

    public abstract partial class Store<TValue, TValueType, TWriteValue> : Instruction
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
        where TWriteValue : unmanaged
    {
        [Operand(0)] public uint Align { get; }
        [Operand(1)] public uint Offset { get; }

        protected abstract TWriteValue Convert(in TValue value);

        public override void Execute(WasmStackFrame current)
        {
            var writeSize = Unsafe.SizeOf<TWriteValue>();

            var t = default(TValueType);
            var c = t.Pop(current);

            var i = current.Pop().ExpectValueI32();
            var ea = (int)(i + Offset);

            var memory = current.Instance.GetMemory(0);

            if (ea + writeSize > memory.Length) current.Context.Trap();

            var writeValue = Convert(c);

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(memory[ea..]), writeValue);
        }

        public override void Validate(in ValidationContext context)
        {
            var writeSize = Unsafe.SizeOf<TWriteValue>();
            var writeSizeRank = 0;
            while (writeSize != 0)
            {
                writeSize >>= 1;
                writeSizeRank++;
            }

            if (Align >= writeSizeRank)
                throw new InvalidModuleException(
                    $"Alignment must be larger than natural. expected: <{writeSizeRank}, actual: {Align}");
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            var type = ValueTypeRegistry.GetFor<TValue>().Value;
            stackState.Pop(type);
            stackState.Pop(ValueType.I32);
        }
    }


    public abstract partial class Store<TValue, TValueType> : Store<TValue, TValueType, TValue>
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override TValue Convert(in TValue value)
        {
            return value;
        }
    }

    [OpCode(0x36)]
    public partial class StoreI32 : Store<uint, ValueTypeI32>
    {
    }

    [OpCode(0x37)]
    public partial class StoreI64 : Store<ulong, ValueTypeI64>
    {
    }

    [OpCode(0x38)]
    public partial class StoreF32 : Store<float, ValueTypeF32>
    {
    }

    [OpCode(0x39)]
    public partial class StoreF64 : Store<double, ValueTypeF64>
    {
    }

    [OpCode(0x3A)]
    public partial class StoreI32AsU8 : Store<uint, ValueTypeI32, byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte Convert(in uint value)
        {
            return unchecked((byte)value);
        }
    }

    [OpCode(0x3B)]
    public partial class StoreI32AsU16 : Store<uint, ValueTypeI32, ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ushort Convert(in uint value)
        {
            return unchecked((ushort)value);
        }
    }

    [OpCode(0x3C)]
    public partial class StoreI64AsU8 : Store<ulong, ValueTypeI64, byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override byte Convert(in ulong value)
        {
            return unchecked((byte)value);
        }
    }

    [OpCode(0x3D)]
    public partial class StoreI64AsU16 : Store<ulong, ValueTypeI64, ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override ushort Convert(in ulong value)
        {
            return unchecked((ushort)value);
        }
    }

    [OpCode(0x3E)]
    public partial class StoreI64AsU32 : Store<ulong, ValueTypeI64, uint>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override uint Convert(in ulong value)
        {
            return unchecked((uint)value);
        }
    }

    #endregion

    #region Memory

    [OpCode(0x3F)]
    public partial class MemorySize : Instruction
    {
        [Operand(0)] public byte Reserved { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.Push(checked((uint)(current.Instance.GetMemory(0).Length >> Memory.PageSizeRank)));
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (Reserved != 0) throw new InvalidModuleException();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Push(ValueType.I32);
        }
    }

    [OpCode(0x40)]
    public partial class MemoryGrow : Instruction
    {
        [Operand(0)] public byte Reserved { get; }

        public override void Execute(WasmStackFrame current)
        {
            var n = current.Pop().ExpectValueI32();
            if (current.Instance.TryGrowMemory(0, checked((int)n), out var oldNumPages))
                current.Push(checked((uint)oldNumPages));
            else
                current.Push(uint.MaxValue);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (Reserved != 0) throw new InvalidModuleException();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.I32);
            stackState.Push(ValueType.I32);
        }
    }


    [OpCode(0xFC, 0x0A)]
    public partial class MemoryCopy : Instruction
    {
        [Operand(0)] public byte Reserved0 { get; }
        [Operand(1)] public byte Reserved1 { get; }

        public override void Execute(WasmStackFrame current)
        {
            var memory = current.Instance.GetMemory(0);

            var n = current.Pop().ExpectValueI32();
            var src = current.Pop().ExpectValueI32();
            var dest = current.Pop().ExpectValueI32();

            checked
            {
                if (src + n > memory.Length || dest + n > memory.Length) throw new TrapException();
                if (n == 0) return;

                var srcSpan = memory.Slice((int)src, (int)n);
                var destSpan = memory.Slice((int)dest, (int)n);

                srcSpan.CopyTo(destSpan);
            }
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (Reserved0 != 0) throw new InvalidModuleException();
            if (Reserved1 != 0) throw new InvalidModuleException();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
        }
    }


    [OpCode(0xFC, 0x0B)]
    public partial class MemoryFill : Instruction
    {
        [Operand(0)] public byte Reserved { get; }

        public override void Execute(WasmStackFrame current)
        {
            var memory = current.Instance.GetMemory(0);

            var n = current.Pop().ExpectValueI32();
            var val = unchecked((byte)current.Pop().ExpectValueI32());
            var dest = current.Pop().ExpectValueI32();

            checked
            {
                if (dest + n > memory.Length) throw new TrapException();
                if (n == 0) return;

                var destSpan = memory.Slice((int)dest, (int)n);

                destSpan.Fill(val);
            }
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            if (Reserved != 0) throw new InvalidModuleException();
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.I32);
        }
    }

    #endregion
}