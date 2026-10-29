using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    #region Vector Memory Templates

    /// <summary>
    ///     Base class for vector instructions which access the memory.
    /// </summary>
    public abstract partial class V128MemoryInstruction : Instruction
    {
        [Operand(0)] public uint Align { get; }
        [Operand(1)] public new uint Offset { get; }

        /// <summary>
        ///     Number of bytes the instruction reads or writes.
        /// </summary>
        protected abstract int AccessSize { get; }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);

            var accessSize = AccessSize;
            var maxAlign = 0;
            while ((accessSize >>= 1) != 0) maxAlign++;

            if (Align > maxAlign)
                throw new InvalidModuleException(
                    $"Alignment must not be larger than natural. expected: <={maxAlign}, actual: {Align}");
        }

        /// <summary>
        ///     Gets the reference to the memory at the effective address. Traps if the access is out of bounds.
        /// </summary>
        protected ref byte GetEffectiveAddress(WasmStackFrame current, uint address)
        {
            var memory = current.Instance.GetMemory(0);
            var ea = (ulong)address + Offset;
            if (ea + (ulong)AccessSize > (ulong)memory.Length)
                throw new TrapException("out of bounds memory access");
            return ref Unsafe.Add(ref MemoryMarshal.GetReference(memory), (int)ea);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which load a v128 from the memory.
    /// </summary>
    public abstract partial class V128LoadInstruction : V128MemoryInstruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var address = current.Pop().ExpectValueI32();
            current.Push(Read(ref GetEffectiveAddress(current, address)));
        }

        protected abstract V128 Read(ref byte source);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.I32);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which load 64 bits and widen each lane.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128LoadExtendInstruction<TLane, TResultLane> : V128LoadInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        protected sealed override int AccessSize => 8;

        protected sealed override V128 Read(ref byte source)
        {
            var loaded = new V128(Unsafe.ReadUnaligned<ulong>(ref source), 0);
            var result = default(V128);
            var count = VectorLanes.Count<TResultLane>();
            for (var i = 0; i < count; i++)
                V128.Lane<TResultLane>(ref result, i) = Extend(V128.Lane<TLane>(ref loaded, i));
            return result;
        }

        protected abstract TResultLane Extend(TLane value);
    }

    /// <summary>
    ///     Base class for vector instructions which load a single lane and fill all lanes with it.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LoadSplatInstruction<TLane> : V128LoadInstruction
        where TLane : unmanaged
    {
        protected sealed override int AccessSize => Unsafe.SizeOf<TLane>();

        protected sealed override V128 Read(ref byte source)
        {
            var lane = Unsafe.ReadUnaligned<TLane>(ref source);
            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++) V128.Lane<TLane>(ref result, i) = lane;
            return result;
        }
    }

    /// <summary>
    ///     Base class for vector instructions which load a single lane into lane 0 and zero the other lanes.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LoadZeroInstruction<TLane> : V128LoadInstruction
        where TLane : unmanaged
    {
        protected sealed override int AccessSize => Unsafe.SizeOf<TLane>();

        protected sealed override V128 Read(ref byte source)
        {
            var result = default(V128);
            V128.Lane<TLane>(ref result, 0) = Unsafe.ReadUnaligned<TLane>(ref source);
            return result;
        }
    }

    /// <summary>
    ///     Base class for vector instructions which load a single lane into the specified lane of a v128.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LoadLaneInstruction<TLane> : V128MemoryInstruction
        where TLane : unmanaged
    {
        [Operand(2)] public byte LaneIndex { get; }

        protected sealed override int AccessSize => Unsafe.SizeOf<TLane>();

        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            var address = current.Pop().ExpectValueI32();
            V128.Lane<TLane>(ref value, LaneIndex) =
                Unsafe.ReadUnaligned<TLane>(ref GetEffectiveAddress(current, address));
            current.Push(value);
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            VectorLanes.ValidateLaneIndex<TLane>(LaneIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.I32);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which store the specified lane of a v128 into the memory.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128StoreLaneInstruction<TLane> : V128MemoryInstruction
        where TLane : unmanaged
    {
        [Operand(2)] public byte LaneIndex { get; }

        protected sealed override int AccessSize => Unsafe.SizeOf<TLane>();

        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            var address = current.Pop().ExpectValueI32();
            Unsafe.WriteUnaligned(ref GetEffectiveAddress(current, address), VectorLanes.Get<TLane>(value, LaneIndex));
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            VectorLanes.ValidateLaneIndex<TLane>(LaneIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.I32);
        }
    }

    #endregion

    #region Vector Load

    /// <summary>
    ///     Represents "v128.load" instruction.
    /// </summary>
    [OpCode(0xFD, 0x00)]
    public partial class LoadV128 : V128LoadInstruction
    {
        protected override int AccessSize => 16;

        protected override V128 Read(ref byte source)
        {
            return Unsafe.ReadUnaligned<V128>(ref source);
        }
    }

    /// <summary>
    ///     Represents "v128.load8x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x01)]
    public partial class LoadI8x8AsI16x8 : V128LoadExtendInstruction<sbyte, short>
    {
        protected override short Extend(sbyte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load8x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x02)]
    public partial class LoadU8x8AsI16x8 : V128LoadExtendInstruction<byte, ushort>
    {
        protected override ushort Extend(byte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load16x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x03)]
    public partial class LoadI16x4AsI32x4 : V128LoadExtendInstruction<short, int>
    {
        protected override int Extend(short value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load16x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x04)]
    public partial class LoadU16x4AsI32x4 : V128LoadExtendInstruction<ushort, uint>
    {
        protected override uint Extend(ushort value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load32x2_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x05)]
    public partial class LoadI32x2AsI64x2 : V128LoadExtendInstruction<int, long>
    {
        protected override long Extend(int value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load32x2_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x06)]
    public partial class LoadU32x2AsI64x2 : V128LoadExtendInstruction<uint, ulong>
    {
        protected override ulong Extend(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "v128.load8_splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x07)]
    public partial class Load8SplatV128 : V128LoadSplatInstruction<byte>
    {
    }

    /// <summary>
    ///     Represents "v128.load16_splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x08)]
    public partial class Load16SplatV128 : V128LoadSplatInstruction<ushort>
    {
    }

    /// <summary>
    ///     Represents "v128.load32_splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x09)]
    public partial class Load32SplatV128 : V128LoadSplatInstruction<uint>
    {
    }

    /// <summary>
    ///     Represents "v128.load64_splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0A)]
    public partial class Load64SplatV128 : V128LoadSplatInstruction<ulong>
    {
    }

    /// <summary>
    ///     Represents "v128.load32_zero" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5C)]
    public partial class Load32ZeroV128 : V128LoadZeroInstruction<uint>
    {
    }

    /// <summary>
    ///     Represents "v128.load64_zero" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5D)]
    public partial class Load64ZeroV128 : V128LoadZeroInstruction<ulong>
    {
    }

    /// <summary>
    ///     Represents "v128.load8_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x54)]
    public partial class Load8LaneV128 : V128LoadLaneInstruction<byte>
    {
    }

    /// <summary>
    ///     Represents "v128.load16_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x55)]
    public partial class Load16LaneV128 : V128LoadLaneInstruction<ushort>
    {
    }

    /// <summary>
    ///     Represents "v128.load32_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x56)]
    public partial class Load32LaneV128 : V128LoadLaneInstruction<uint>
    {
    }

    /// <summary>
    ///     Represents "v128.load64_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x57)]
    public partial class Load64LaneV128 : V128LoadLaneInstruction<ulong>
    {
    }

    #endregion

    #region Vector Store

    /// <summary>
    ///     Represents "v128.store" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0B)]
    public partial class StoreV128 : V128MemoryInstruction
    {
        protected override int AccessSize => 16;

        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            var address = current.Pop().ExpectValueI32();
            Unsafe.WriteUnaligned(ref GetEffectiveAddress(current, address), value);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 0);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            context.GetMemoryType(0);
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.I32);
        }
    }

    /// <summary>
    ///     Represents "v128.store8_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x58)]
    public partial class Store8LaneV128 : V128StoreLaneInstruction<byte>
    {
    }

    /// <summary>
    ///     Represents "v128.store16_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x59)]
    public partial class Store16LaneV128 : V128StoreLaneInstruction<ushort>
    {
    }

    /// <summary>
    ///     Represents "v128.store32_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5A)]
    public partial class Store32LaneV128 : V128StoreLaneInstruction<uint>
    {
    }

    /// <summary>
    ///     Represents "v128.store64_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5B)]
    public partial class Store64LaneV128 : V128StoreLaneInstruction<ulong>
    {
    }

    #endregion
}
