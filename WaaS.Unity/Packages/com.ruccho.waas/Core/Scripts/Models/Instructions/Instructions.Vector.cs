using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    #region Vector Helpers

    /// <summary>
    ///     Helpers to access the lanes of <see cref="V128" />.
    /// </summary>
    internal static class VectorLanes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(in V128 vector, int index) where T : unmanaged
        {
            return ref Unsafe.Add(ref Unsafe.As<V128, T>(ref Unsafe.AsRef(in vector)), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<T>() where T : unmanaged
        {
            return 16 / Unsafe.SizeOf<T>();
        }

        /// <summary>
        ///     Sets all bits of the lane.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAllOnes<T>(ref V128 vector, int index) where T : unmanaged
        {
            switch (Unsafe.SizeOf<T>())
            {
                case 1:
                    V128.Lane<byte>(ref vector, index) = byte.MaxValue;
                    break;
                case 2:
                    V128.Lane<ushort>(ref vector, index) = ushort.MaxValue;
                    break;
                case 4:
                    V128.Lane<uint>(ref vector, index) = uint.MaxValue;
                    break;
                default:
                    V128.Lane<ulong>(ref vector, index) = ulong.MaxValue;
                    break;
            }
        }

        public static void ValidateLaneIndex<T>(byte laneIndex) where T : unmanaged
        {
            if (laneIndex >= Count<T>()) throw new InvalidCodeException("invalid lane index");
        }
    }

    /// <summary>
    ///     Saturating conversions used by vector instructions.
    /// </summary>
    internal static class VectorSaturation
    {
        public static sbyte ToSByte(int value)
        {
            if (value < sbyte.MinValue) return sbyte.MinValue;
            if (value > sbyte.MaxValue) return sbyte.MaxValue;
            return (sbyte)value;
        }

        public static byte ToByte(int value)
        {
            if (value < byte.MinValue) return byte.MinValue;
            if (value > byte.MaxValue) return byte.MaxValue;
            return (byte)value;
        }

        public static short ToInt16(int value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)value;
        }

        public static ushort ToUInt16(int value)
        {
            if (value < ushort.MinValue) return ushort.MinValue;
            if (value > ushort.MaxValue) return ushort.MaxValue;
            return (ushort)value;
        }
    }

    #endregion

    #region Vector Templates

    /// <summary>
    ///     Base class for vector instructions which take one v128 and produce one v128.
    /// </summary>
    public abstract partial class V128UnaryInstruction : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            current.Push(Operate(value));
        }

        protected abstract V128 Operate(in V128 value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which take two v128 and produce one v128.
    /// </summary>
    public abstract partial class V128BinaryInstruction : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var rhs = current.Pop().ExpectValueV128();
            var lhs = current.Pop().ExpectValueV128();
            current.Push(Operate(lhs, rhs));
        }

        protected abstract V128 Operate(in V128 lhs, in V128 rhs);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which take one v128 and produce one i32.
    /// </summary>
    public abstract partial class V128TestInstruction : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            current.Push(Operate(value));
        }

        protected abstract uint Operate(in V128 value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.I32);
        }
    }

    /// <summary>
    ///     Base class for lane-wise vector instructions which take one v128 and produce one v128.
    ///     Both of the lane types must have the same size.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128LanewiseUnaryInstruction<TLane, TResultLane> : V128UnaryInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        protected sealed override V128 Operate(in V128 value)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++)
                V128.Lane<TResultLane>(ref result, i) = OperateLane(VectorLanes.Get<TLane>(value, i));
            return result;
        }

        protected abstract TResultLane OperateLane(TLane value);
    }

    /// <summary>
    ///     Base class for lane-wise vector instructions which take one v128 and produce one v128 of the same lane type.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LanewiseUnaryInstruction<TLane> : V128LanewiseUnaryInstruction<TLane, TLane>
        where TLane : unmanaged
    {
    }

    /// <summary>
    ///     Base class for lane-wise vector instructions which take two v128 and produce one v128.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LanewiseBinaryInstruction<TLane> : V128BinaryInstruction
        where TLane : unmanaged
    {
        protected sealed override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++)
                V128.Lane<TLane>(ref result, i) =
                    OperateLane(VectorLanes.Get<TLane>(lhs, i), VectorLanes.Get<TLane>(rhs, i));
            return result;
        }

        protected abstract TLane OperateLane(TLane lhs, TLane rhs);
    }

    /// <summary>
    ///     Base class for lane-wise vector comparison instructions.
    ///     Each lane of the result has all bits set if the comparison holds, otherwise all bits cleared.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128LanewiseCompareInstruction<TLane> : V128BinaryInstruction
        where TLane : unmanaged
    {
        protected sealed override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++)
                if (OperateLane(VectorLanes.Get<TLane>(lhs, i), VectorLanes.Get<TLane>(rhs, i)))
                    VectorLanes.SetAllOnes<TLane>(ref result, i);
            return result;
        }

        protected abstract bool OperateLane(TLane lhs, TLane rhs);
    }

    /// <summary>
    ///     Base class for vector shift instructions. The shift count is taken modulo the lane width.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    public abstract partial class V128ShiftInstruction<TLane> : Instruction
        where TLane : unmanaged
    {
        public override void Execute(WasmStackFrame current)
        {
            var shift = (int)(current.Pop().ExpectValueI32() & (uint)(Unsafe.SizeOf<TLane>() * 8 - 1));
            var value = current.Pop().ExpectValueV128();

            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++)
                V128.Lane<TLane>(ref result, i) = OperateLane(VectorLanes.Get<TLane>(value, i), shift);

            current.Push(result);
        }

        protected abstract TLane OperateLane(TLane value, int shift);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.I32);
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which widen either the lower or the upper half of the lanes.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128ExtendInstruction<TLane, TResultLane> : V128UnaryInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        /// <summary>
        ///     Whether the instruction takes the upper half of the lanes.
        /// </summary>
        protected abstract bool IsHigh { get; }

        protected sealed override V128 Operate(in V128 value)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TResultLane>();
            var offset = IsHigh ? count : 0;
            for (var i = 0; i < count; i++)
                V128.Lane<TResultLane>(ref result, i) = OperateLane(VectorLanes.Get<TLane>(value, offset + i));
            return result;
        }

        protected abstract TResultLane OperateLane(TLane value);
    }

    /// <summary>
    ///     Base class for vector instructions which multiply either the lower or the upper half of the lanes into
    ///     wider lanes.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128ExtMulInstruction<TLane, TResultLane> : V128BinaryInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        /// <summary>
        ///     Whether the instruction takes the upper half of the lanes.
        /// </summary>
        protected abstract bool IsHigh { get; }

        protected sealed override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TResultLane>();
            var offset = IsHigh ? count : 0;
            for (var i = 0; i < count; i++)
                V128.Lane<TResultLane>(ref result, i) = OperateLane(
                    VectorLanes.Get<TLane>(lhs, offset + i),
                    VectorLanes.Get<TLane>(rhs, offset + i));
            return result;
        }

        protected abstract TResultLane OperateLane(TLane lhs, TLane rhs);
    }

    /// <summary>
    ///     Base class for vector instructions which add the adjacent pairs of the lanes into wider lanes.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128ExtAddPairwiseInstruction<TLane, TResultLane> : V128UnaryInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        protected sealed override V128 Operate(in V128 value)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TResultLane>();
            for (var i = 0; i < count; i++)
                V128.Lane<TResultLane>(ref result, i) = OperateLane(
                    VectorLanes.Get<TLane>(value, i * 2),
                    VectorLanes.Get<TLane>(value, i * 2 + 1));
            return result;
        }

        protected abstract TResultLane OperateLane(TLane lhs, TLane rhs);
    }

    /// <summary>
    ///     Base class for vector instructions which narrow the lanes of two v128 into one v128 with saturation.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TResultLane"></typeparam>
    public abstract partial class V128NarrowInstruction<TLane, TResultLane> : V128BinaryInstruction
        where TLane : unmanaged where TResultLane : unmanaged
    {
        protected sealed override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++)
            {
                V128.Lane<TResultLane>(ref result, i) = OperateLane(VectorLanes.Get<TLane>(lhs, i));
                V128.Lane<TResultLane>(ref result, count + i) = OperateLane(VectorLanes.Get<TLane>(rhs, i));
            }

            return result;
        }

        protected abstract TResultLane OperateLane(TLane value);
    }

    /// <summary>
    ///     Base class for vector instructions which fill all lanes with a scalar value.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class V128SplatInstruction<TLane, TValue, TValueType> : Instruction
        where TLane : unmanaged
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            var lane = Convert(t.Pop(current));

            var result = default(V128);
            var count = VectorLanes.Count<TLane>();
            for (var i = 0; i < count; i++) V128.Lane<TLane>(ref result, i) = lane;

            current.Push(result);
        }

        protected abstract TLane Convert(TValue value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException());
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Base class for vector instructions which extract a lane as a scalar value.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class V128ExtractLaneInstruction<TLane, TValue, TValueType> : Instruction
        where TLane : unmanaged
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
    {
        [Operand(0)] public byte LaneIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var value = current.Pop().ExpectValueV128();
            var t = default(TValueType);
            t.Push(current, Convert(VectorLanes.Get<TLane>(value, LaneIndex)));
        }

        protected abstract TValue Convert(TLane lane);

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            VectorLanes.ValidateLaneIndex<TLane>(LaneIndex);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException());
        }
    }

    /// <summary>
    ///     Base class for vector instructions which replace a lane with a scalar value.
    /// </summary>
    /// <typeparam name="TLane"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class V128ReplaceLaneInstruction<TLane, TValue, TValueType> : Instruction
        where TLane : unmanaged
        where TValue : unmanaged
        where TValueType : struct, IValueType<TValue>
    {
        [Operand(0)] public byte LaneIndex { get; }

        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            var lane = Convert(t.Pop(current));
            var value = current.Pop().ExpectValueV128();
            V128.Lane<TLane>(ref value, LaneIndex) = lane;
            current.Push(value);
        }

        protected abstract TLane Convert(TValue value);

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
            stackState.Pop(ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException());
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.V128);
        }
    }

    #endregion

    #region Vector Constant and Lane Operations

    /// <summary>
    ///     Represents "v128.const" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0C)]
    public partial class ConstV128 : Instruction
    {
        [Operand(0)] public V128 Value { get; }

        public override void Execute(WasmStackFrame current)
        {
            current.Push(Value);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Represents "i8x16.shuffle" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0D)]
    public partial class ShuffleI8x16 : V128BinaryInstruction
    {
        /// <summary>
        ///     Indices of the lanes to select from the concatenation of the two operands, one per byte.
        /// </summary>
        [Operand(0)] public V128 LaneIndices { get; }

        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            var indices = LaneIndices;
            for (var i = 0; i < 16; i++)
            {
                var index = V128.Lane<byte>(ref indices, i);
                V128.Lane<byte>(ref result, i) = index < 16
                    ? VectorLanes.Get<byte>(lhs, index)
                    : VectorLanes.Get<byte>(rhs, index - 16);
            }

            return result;
        }

        public override void Validate(in ValidationContext context)
        {
            base.Validate(context);
            var indices = LaneIndices;
            for (var i = 0; i < 16; i++)
                if (V128.Lane<byte>(ref indices, i) >= 32)
                    throw new InvalidCodeException("invalid lane index");
        }
    }

    /// <summary>
    ///     Represents "i8x16.swizzle" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0E)]
    public partial class SwizzleI8x16 : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            for (var i = 0; i < 16; i++)
            {
                var index = VectorLanes.Get<byte>(rhs, i);
                V128.Lane<byte>(ref result, i) = index < 16 ? VectorLanes.Get<byte>(lhs, index) : (byte)0;
            }

            return result;
        }
    }

    #endregion

    #region Splat

    /// <summary>
    ///     Represents "i8x16.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x0F)]
    public partial class SplatI8x16 : V128SplatInstruction<byte, uint, ValueTypeI32>
    {
        protected override byte Convert(uint value)
        {
            return (byte)value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x10)]
    public partial class SplatI16x8 : V128SplatInstruction<ushort, uint, ValueTypeI32>
    {
        protected override ushort Convert(uint value)
        {
            return (ushort)value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x11)]
    public partial class SplatI32x4 : V128SplatInstruction<uint, uint, ValueTypeI32>
    {
        protected override uint Convert(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x12)]
    public partial class SplatI64x2 : V128SplatInstruction<ulong, ulong, ValueTypeI64>
    {
        protected override ulong Convert(ulong value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32x4.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x13)]
    public partial class SplatF32x4 : V128SplatInstruction<float, float, ValueTypeF32>
    {
        protected override float Convert(float value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f64x2.splat" instruction.
    /// </summary>
    [OpCode(0xFD, 0x14)]
    public partial class SplatF64x2 : V128SplatInstruction<double, double, ValueTypeF64>
    {
        protected override double Convert(double value)
        {
            return value;
        }
    }

    #endregion

    #region Lane

    /// <summary>
    ///     Represents "i8x16.extract_lane_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x15)]
    public partial class ExtractLaneI8x16S : V128ExtractLaneInstruction<sbyte, uint, ValueTypeI32>
    {
        protected override uint Convert(sbyte lane)
        {
            return unchecked((uint)lane);
        }
    }

    /// <summary>
    ///     Represents "i8x16.extract_lane_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x16)]
    public partial class ExtractLaneI8x16U : V128ExtractLaneInstruction<byte, uint, ValueTypeI32>
    {
        protected override uint Convert(byte lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "i8x16.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x17)]
    public partial class ReplaceLaneI8x16 : V128ReplaceLaneInstruction<byte, uint, ValueTypeI32>
    {
        protected override byte Convert(uint value)
        {
            return (byte)value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.extract_lane_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x18)]
    public partial class ExtractLaneI16x8S : V128ExtractLaneInstruction<short, uint, ValueTypeI32>
    {
        protected override uint Convert(short lane)
        {
            return unchecked((uint)lane);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extract_lane_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x19)]
    public partial class ExtractLaneI16x8U : V128ExtractLaneInstruction<ushort, uint, ValueTypeI32>
    {
        protected override uint Convert(ushort lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "i16x8.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1A)]
    public partial class ReplaceLaneI16x8 : V128ReplaceLaneInstruction<ushort, uint, ValueTypeI32>
    {
        protected override ushort Convert(uint value)
        {
            return (ushort)value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extract_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1B)]
    public partial class ExtractLaneI32x4 : V128ExtractLaneInstruction<uint, uint, ValueTypeI32>
    {
        protected override uint Convert(uint lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "i32x4.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1C)]
    public partial class ReplaceLaneI32x4 : V128ReplaceLaneInstruction<uint, uint, ValueTypeI32>
    {
        protected override uint Convert(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extract_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1D)]
    public partial class ExtractLaneI64x2 : V128ExtractLaneInstruction<ulong, ulong, ValueTypeI64>
    {
        protected override ulong Convert(ulong lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "i64x2.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1E)]
    public partial class ReplaceLaneI64x2 : V128ReplaceLaneInstruction<ulong, ulong, ValueTypeI64>
    {
        protected override ulong Convert(ulong value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32x4.extract_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x1F)]
    public partial class ExtractLaneF32x4 : V128ExtractLaneInstruction<float, float, ValueTypeF32>
    {
        protected override float Convert(float lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "f32x4.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x20)]
    public partial class ReplaceLaneF32x4 : V128ReplaceLaneInstruction<float, float, ValueTypeF32>
    {
        protected override float Convert(float value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f64x2.extract_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x21)]
    public partial class ExtractLaneF64x2 : V128ExtractLaneInstruction<double, double, ValueTypeF64>
    {
        protected override double Convert(double lane)
        {
            return lane;
        }
    }

    /// <summary>
    ///     Represents "f64x2.replace_lane" instruction.
    /// </summary>
    [OpCode(0xFD, 0x22)]
    public partial class ReplaceLaneF64x2 : V128ReplaceLaneInstruction<double, double, ValueTypeF64>
    {
        protected override double Convert(double value)
        {
            return value;
        }
    }

    #endregion

    #region Bitwise

    /// <summary>
    ///     Represents "v128.not" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4D)]
    public partial class NotV128 : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            return new V128(~value.Lower, ~value.Upper);
        }
    }

    /// <summary>
    ///     Represents "v128.and" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4E)]
    public partial class AndV128 : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            return new V128(lhs.Lower & rhs.Lower, lhs.Upper & rhs.Upper);
        }
    }

    /// <summary>
    ///     Represents "v128.andnot" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4F)]
    public partial class AndNotV128 : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            return new V128(lhs.Lower & ~rhs.Lower, lhs.Upper & ~rhs.Upper);
        }
    }

    /// <summary>
    ///     Represents "v128.or" instruction.
    /// </summary>
    [OpCode(0xFD, 0x50)]
    public partial class OrV128 : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            return new V128(lhs.Lower | rhs.Lower, lhs.Upper | rhs.Upper);
        }
    }

    /// <summary>
    ///     Represents "v128.xor" instruction.
    /// </summary>
    [OpCode(0xFD, 0x51)]
    public partial class XorV128 : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            return new V128(lhs.Lower ^ rhs.Lower, lhs.Upper ^ rhs.Upper);
        }
    }

    /// <summary>
    ///     Represents "v128.bitselect" instruction.
    /// </summary>
    [OpCode(0xFD, 0x52)]
    public partial class BitSelectV128 : Instruction
    {
        public override void Execute(WasmStackFrame current)
        {
            var mask = current.Pop().ExpectValueV128();
            var rhs = current.Pop().ExpectValueV128();
            var lhs = current.Pop().ExpectValueV128();
            current.Push(new V128(
                (lhs.Lower & mask.Lower) | (rhs.Lower & ~mask.Lower),
                (lhs.Upper & mask.Upper) | (rhs.Upper & ~mask.Upper)));
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (3, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.V128);
            stackState.Pop(ValueType.V128);
            stackState.Push(ValueType.V128);
        }
    }

    /// <summary>
    ///     Represents "v128.any_true" instruction.
    /// </summary>
    [OpCode(0xFD, 0x53)]
    public partial class AnyTrueV128 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return (value.Lower | value.Upper) != 0 ? 1u : 0u;
        }
    }

    #endregion
}
