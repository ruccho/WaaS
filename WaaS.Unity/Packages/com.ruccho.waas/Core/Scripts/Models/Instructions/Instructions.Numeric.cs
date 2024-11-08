using System;
using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    #region Const

    /// <summary>
    ///     Base class for constant instructions.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class Const<TValue, TValueType> : Instruction
        where TValue : unmanaged where TValueType : struct, IValueType<TValue>
    {
        [Operand(0, Signed = true)] public TValue Value { get; }

        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            t.Push(current, Value);
        }

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (0, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException();
            stackState.Push(type);
        }
    }

    /// <summary>
    ///     Represents "i32.const" instruction.
    /// </summary>
    [OpCode(0x41)]
    public partial class ConstI32 : Const<uint, ValueTypeI32>
    {
    }

    /// <summary>
    ///     Represents "i64.const" instruction.
    /// </summary>
    [OpCode(0x42)]
    public partial class ConstI64 : Const<ulong, ValueTypeI64>
    {
    }

    /// <summary>
    ///     Represents "f32.const" instruction.
    /// </summary>
    [OpCode(0x43)]
    public partial class ConstF32 : Const<float, ValueTypeF32>
    {
    }

    /// <summary>
    ///     Represents "f64.const" instruction.
    /// </summary>
    [OpCode(0x44)]
    public partial class ConstF64 : Const<double, ValueTypeF64>
    {
    }

    #endregion

    #region Operator Templates

    /// <summary>
    ///     Base class for unary operator instructions with the same input and output types.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class UnaryInstruction<TValue, TValueType> : Instruction
        where TValue : unmanaged where TValueType : struct, IValueType<TValue>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            t.Push(current, Operate(t.Pop(current)));
        }

        protected abstract TValue Operate(TValue value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException();
            stackState.Pop(type);
            stackState.Push(type);
        }
    }

    /// <summary>
    ///     Base class for unary operator instructions with different input and output types.
    /// </summary>
    /// <typeparam name="TValue1"></typeparam>
    /// <typeparam name="TValueType1"></typeparam>
    /// <typeparam name="TValue2"></typeparam>
    /// <typeparam name="TValueType2"></typeparam>
    public abstract partial class UnaryInstruction<TValue1, TValueType1, TValue2, TValueType2> : Instruction
        where TValue1 : unmanaged
        where TValueType1 : struct, IValueType<TValue1>
        where TValue2 : unmanaged
        where TValueType2 : struct, IValueType<TValue2>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t1 = default(TValueType1);
            var t2 = default(TValueType2);
            t2.Push(current, Operate(t1.Pop(current)));
        }

        protected abstract TValue2 Operate(TValue1 value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type1 = ValueTypeRegistry.GetFor<TValue1>() ?? throw new InvalidModuleException();
            var type2 = ValueTypeRegistry.GetFor<TValue2>() ?? throw new InvalidModuleException();
            stackState.Pop(type1);
            stackState.Push(type2);
        }
    }

    /// <summary>
    ///     Base class for binary operator instructions with the same input and output types.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class BinaryInstruction<TValue, TValueType> : Instruction
        where TValue : unmanaged where TValueType : struct, IValueType<TValue>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            var rhs = t.Pop(current);
            var lhs = t.Pop(current);
            t.Push(current, Operate(lhs, rhs));
        }

        protected abstract TValue Operate(TValue lhs, TValue rhs);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException();
            stackState.Pop(type);
            stackState.Pop(type);
            stackState.Push(type);
        }
    }

    /// <summary>
    ///     Base class for unary operator instructions with boolean output.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class UnaryBoolInstruction<TValue, TValueType> : Instruction
        where TValue : unmanaged where TValueType : struct, IValueType<TValue>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            current.Push((uint)(Operate(t.Pop(current)) ? 1 : 0));
        }

        protected abstract bool Operate(TValue value);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (1, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException();
            stackState.Pop(type);
            stackState.Push(ValueType.I32);
        }
    }

    /// <summary>
    ///     Base class for binary operator instructions with boolean output.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TValueType"></typeparam>
    public abstract partial class BinaryBoolInstruction<TValue, TValueType> : Instruction
        where TValue : unmanaged where TValueType : struct, IValueType<TValue>
    {
        public override void Execute(WasmStackFrame current)
        {
            var t = default(TValueType);
            var rhs = t.Pop(current);
            var lhs = t.Pop(current);
            current.Push((uint)(Operate(lhs, rhs) ? 1 : 0));
        }

        protected abstract bool Operate(TValue lhs, TValue rhs);

        public override (uint popCount, uint pushCount) PreValidateStackState(in ValidationContext context)
        {
            return (2, 1);
        }

        public override void ValidateStackState(in ValidationContext context, ref ValidationBlockStackState stackState)
        {
            var type = ValueTypeRegistry.GetFor<TValue>() ?? throw new InvalidModuleException();
            stackState.Pop(type);
            stackState.Pop(type);
            stackState.Push(ValueType.I32);
        }
    }

    #endregion

    #region Operators (i32)

    /// <summary>
    ///     Represents "i32.eqz" instruction.
    /// </summary>
    [OpCode(0x45)]
    public partial class EqzI32 : UnaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint value)
        {
            return value == 0;
        }
    }

    /// <summary>
    ///     Represents "i32.eq" instruction.
    /// </summary>
    [OpCode(0x46)]
    public partial class EqI32 : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.ne" instruction.
    /// </summary>
    [OpCode(0x47)]
    public partial class NeI32 : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.lt_s" instruction.
    /// </summary>
    [OpCode(0x48)]
    public partial class LtI32S : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return unchecked((int)lhs < (int)rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.lt_u" instruction.
    /// </summary>
    [OpCode(0x49)]
    public partial class LtI32U : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.gt_s" instruction.
    /// </summary>
    [OpCode(0x4A)]
    public partial class GtI32S : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return unchecked((int)lhs > (int)rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.gt_u" instruction.
    /// </summary>
    [OpCode(0x4B)]
    public partial class GtI32U : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.le_s" instruction.
    /// </summary>
    [OpCode(0x4C)]
    public partial class LeI32S : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return unchecked((int)lhs <= (int)rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.le_u" instruction.
    /// </summary>
    [OpCode(0x4D)]
    public partial class LeI32U : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs <= rhs;
        }
    }
    /// <summary>
    ///     Represents "i32.ge_s" instruction.
    /// </summary>

    [OpCode(0x4E)]
    public partial class GeI32S : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return unchecked((int)lhs >= (int)rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.ge_u" instruction.
    /// </summary>
    [OpCode(0x4F)]
    public partial class GeI32U : BinaryBoolInstruction<uint, ValueTypeI32>
    {
        protected override bool Operate(uint lhs, uint rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Operators (I64)

    /// <summary>
    ///     Represents "i64.eqz" instruction.
    /// </summary>
    [OpCode(0x50)]
    public partial class EqzI64 : UnaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong value)
        {
            return value == 0;
        }
    }

    /// <summary>
    ///     Represents "i64.eq" instruction.
    /// </summary>
    [OpCode(0x51)]
    public partial class EqI64 : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.ne" instruction.
    /// </summary>
    [OpCode(0x52)]
    public partial class NeI64 : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.lt_s" instruction.
    /// </summary>
    [OpCode(0x53)]
    public partial class LtI64S : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return unchecked((long)lhs < (long)rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.lt_u" instruction.
    /// </summary>
    [OpCode(0x54)]
    public partial class LtI64U : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.gt_s" instruction.
    /// </summary>
    [OpCode(0x55)]
    public partial class GtI64S : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return unchecked((long)lhs > (long)rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.gt_u" instruction.
    /// </summary>
    [OpCode(0x56)]
    public partial class GtI64U : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.le_s" instruction.
    /// </summary>
    [OpCode(0x57)]
    public partial class LeI64S : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return unchecked((long)lhs <= (long)rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.le_u" instruction.
    /// </summary>
    [OpCode(0x58)]
    public partial class LeI64U : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.ge_s" instruction.
    /// </summary>
    [OpCode(0x59)]
    public partial class GeI64S : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return unchecked((long)lhs >= (long)rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.ge_u" instruction.
    /// </summary>
    [OpCode(0x5A)]
    public partial class GeI64U : BinaryBoolInstruction<ulong, ValueTypeI64>
    {
        protected override bool Operate(ulong lhs, ulong rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Operators (F32)

    /// <summary>
    ///     Represents "f32.eq" instruction.
    /// </summary>
    [OpCode(0x5B)]
    public partial class EqF32 : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.ne" instruction.
    /// </summary>
    [OpCode(0x5C)]
    public partial class NeF32 : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.lt" instruction.
    /// </summary>
    [OpCode(0x5D)]
    public partial class LtF32 : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.gt" instruction.
    /// </summary>
    [OpCode(0x5E)]
    public partial class GtF32 : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.le" instruction.
    /// </summary>
    [OpCode(0x5F)]
    public partial class LeF32S : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.ge" instruction.
    /// </summary>
    [OpCode(0x60)]
    public partial class GeF32S : BinaryBoolInstruction<float, ValueTypeF32>
    {
        protected override bool Operate(float lhs, float rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Operators (F64)

    /// <summary>
    ///     Represents "f64.eq" instruction.
    /// </summary>
    [OpCode(0x61)]
    public partial class EqF64 : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.ne" instruction.
    /// </summary>
    [OpCode(0x62)]
    public partial class NeF64 : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.lt" instruction.
    /// </summary>
    [OpCode(0x63)]
    public partial class LtF64 : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.gt" instruction.
    /// </summary>
    [OpCode(0x64)]
    public partial class GtF64 : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.le" instruction.
    /// </summary>
    [OpCode(0x65)]
    public partial class LeF64S : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.ge" instruction.
    /// </summary>
    [OpCode(0x66)]
    public partial class GeF64S : BinaryBoolInstruction<double, ValueTypeF64>
    {
        protected override bool Operate(double lhs, double rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Operators (I32)

    /// <summary>
    ///     Represents "i32.clz" instruction.
    /// </summary>
    [OpCode(0x67)]
    public partial class ClzI32 : UnaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint value)
        {
            uint i = 8 * sizeof(uint);
            var c = value;
            while (c != 0)
            {
                c >>= 1;
                i--;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i32.ctz" instruction.
    /// </summary>
    [OpCode(0x68)]
    public partial class CtzI32 : UnaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint value)
        {
            uint i = 8 * sizeof(uint);
            var c = value;
            while (c != 0)
            {
                c <<= 1;
                i--;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i32.popcnt" instruction.
    /// </summary>
    [OpCode(0x69)]
    public partial class PopcntI32 : UnaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint value)
        {
            uint i = 0;
            var c = value;
            while (c != 0)
            {
                i += c & 1;
                c >>= 1;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i32.add" instruction.
    /// </summary>
    [OpCode(0x6A)]
    public partial class AddI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.sub" instruction.
    /// </summary>
    [OpCode(0x6B)]
    public partial class SubI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.mul" instruction.
    /// </summary>
    [OpCode(0x6C)]
    public partial class MulI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.div_s" instruction.
    /// </summary>
    [OpCode(0x6D)]
    public partial class DivI32S : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            var result = unchecked((int)lhs / (int)rhs);
            if (result == 1 << (sizeof(uint) * 8 - 1)) throw new InvalidCodeException();
            return unchecked((uint)result);
        }
    }

    /// <summary>
    ///     Represents "i32.div_u" instruction.
    /// </summary>
    [OpCode(0x6E)]
    public partial class DivI32U : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.rem_s" instruction.
    /// </summary>
    [OpCode(0x6F)]
    public partial class RemI32S : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            unchecked
            {
                if (rhs == 0) throw new InvalidCodeException();
                var j1 = (long)(int)lhs;
                var j2 = (long)(int)rhs;
                if (Math.Abs(j1) < Math.Abs(j2)) return (uint)j1;
                return (uint)(j1 - j2 * (j1 / j2));
            }
        }
    }

    /// <summary>
    ///     Represents "i32.rem_u" instruction.
    /// </summary>
    [OpCode(0x70)]
    public partial class RemI32U : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            return lhs - rhs * (lhs / rhs);
        }
    }

    /// <summary>
    ///     Represents "i32.and" instruction.
    /// </summary>
    [OpCode(0x71)]
    public partial class AndI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return lhs & rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.or" instruction.
    /// </summary>
    [OpCode(0x72)]
    public partial class OrI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return lhs | rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.xor" instruction.
    /// </summary>
    [OpCode(0x73)]
    public partial class XorI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return lhs ^ rhs;
        }
    }

    /// <summary>
    ///     Represents "i32.shl" instruction.
    /// </summary>
    [OpCode(0x74)]
    public partial class ShlI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked(lhs << (byte)(rhs & (sizeof(uint) * 8 - 1)));
        }
    }

    /// <summary>
    ///     Represents "i32.shr_s" instruction.
    /// </summary>
    [OpCode(0x75)]
    public partial class ShrI32S : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked((uint)((int)lhs >> (byte)(rhs & (sizeof(uint) * 8 - 1))));
        }
    }

    /// <summary>
    ///     Represents "i32.shr_u" instruction.
    /// </summary>
    [OpCode(0x76)]
    public partial class ShrI32U : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            return unchecked(lhs >> (byte)(rhs & (sizeof(uint) * 8 - 1)));
        }
    }

    /// <summary>
    ///     Represents "i32.rotl" instruction.
    /// </summary>
    [OpCode(0x77)]
    public partial class RotlI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            unchecked
            {
                var k = (byte)(rhs & (sizeof(uint) * 8 - 1));
                return (lhs << k) | (lhs >> (sizeof(uint) * 8 - k));
            }
        }
    }

    /// <summary>
    ///     Represents "i32.rotr" instruction.
    /// </summary>
    [OpCode(0x78)]
    public partial class RotrI32 : BinaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint lhs, uint rhs)
        {
            unchecked
            {
                var k = (byte)(rhs & (sizeof(uint) * 8 - 1));
                return (lhs >> k) | (lhs << (sizeof(uint) * 8 - k));
            }
        }
    }

    #endregion

    #region Operators (I64)

    /// <summary>
    ///     Represents "i64.clz" instruction.
    /// </summary>
    [OpCode(0x79)]
    public partial class ClzI64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            ulong i = 8 * sizeof(ulong);
            var c = value;
            while (c != 0)
            {
                c >>= 1;
                i--;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i64.ctz" instruction.
    /// </summary>
    [OpCode(0x7A)]
    public partial class CtzI64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            ulong i = 8 * sizeof(ulong);
            var c = value;
            while (c != 0)
            {
                c <<= 1;
                i--;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i64.popcnt" instruction.
    /// </summary>
    [OpCode(0x7B)]
    public partial class PopcntI64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            ulong i = 0;
            var c = value;
            while (c != 0)
            {
                i += c & 1;
                c >>= 1;
            }

            return i;
        }
    }

    /// <summary>
    ///     Represents "i64.add" instruction.
    /// </summary>
    [OpCode(0x7C)]
    public partial class AddI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            var a = lhs + rhs;
            return unchecked(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.sub" instruction.
    /// </summary>
    [OpCode(0x7D)]
    public partial class SubI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return unchecked(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.mul" instruction.
    /// </summary>
    [OpCode(0x7E)]
    public partial class MulI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return unchecked(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.div_s" instruction.
    /// </summary>
    [OpCode(0x7F)]
    public partial class DivI64S : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            var result = unchecked((long)lhs / (long)rhs);
            if (result == (long)1 << (sizeof(ulong) * 8 - 1)) throw new InvalidCodeException();
            return unchecked((ulong)result);
        }
    }

    /// <summary>
    ///     Represents "i64.div_u" instruction.
    /// </summary>
    [OpCode(0x80)]
    public partial class DivI64U : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.rem_s" instruction.
    /// </summary>
    [OpCode(0x81)]
    public partial class RemI64S : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            unchecked
            {
                if (rhs == 0) throw new InvalidCodeException();
                var j1 = (long)lhs;
                var j2 = (long)rhs;
                if (j1 < 0 && j2 < 0 && j1 != j2) j2 = -j2;

                return (ulong)(j1 - j2 * (j1 / j2));
            }
        }
    }

    /// <summary>
    ///     Represents "i64.rem_u" instruction.
    /// </summary>
    [OpCode(0x82)]
    public partial class RemI64U : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            if (rhs == 0) throw new InvalidCodeException();
            return lhs - rhs * (lhs / rhs);
        }
    }

    /// <summary>
    ///     Represents "i64.and" instruction.
    /// </summary>
    [OpCode(0x83)]
    public partial class AndI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return lhs & rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.or" instruction.
    /// </summary>
    [OpCode(0x84)]
    public partial class OrI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return lhs | rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.xor" instruction.
    /// </summary>
    [OpCode(0x85)]
    public partial class XorI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return lhs ^ rhs;
        }
    }

    /// <summary>
    ///     Represents "i64.shl" instruction.
    /// </summary>
    [OpCode(0x86)]
    public partial class ShlI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return unchecked(lhs << (byte)(rhs & (sizeof(ulong) * 8 - 1)));
        }
    }

    /// <summary>
    ///     Represents "i64.shr_s" instruction.
    /// </summary>
    [OpCode(0x87)]
    public partial class ShrI64S : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return unchecked((ulong)((long)lhs >> (byte)(rhs & (sizeof(ulong) * 8 - 1))));
        }
    }

    /// <summary>
    ///     Represents "i64.shr_u" instruction.
    /// </summary>
    [OpCode(0x88)]
    public partial class ShrI64U : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            return unchecked(lhs >> (byte)(rhs & (sizeof(ulong) * 8 - 1)));
        }
    }

    /// <summary>
    ///     Represents "i64.rotl" instruction.
    /// </summary>
    [OpCode(0x89)]
    public partial class RotlI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            unchecked
            {
                var k = (byte)(rhs & (sizeof(ulong) * 8 - 1));
                return (lhs << k) | (lhs >> (sizeof(ulong) * 8 - k));
            }
        }
    }

    /// <summary>
    ///     Represents "i64.rotr" instruction.
    /// </summary>
    [OpCode(0x8A)]
    public partial class RotrI64 : BinaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong lhs, ulong rhs)
        {
            unchecked
            {
                var k = (byte)(rhs & (sizeof(ulong) * 8 - 1));
                return (lhs >> k) | (lhs << (sizeof(ulong) * 8 - k));
            }
        }
    }

    #endregion

    #region Operation (F32)

    /// <summary>
    ///     Represents "f32.abs" instruction.
    /// </summary>
    [OpCode(0x8B)]
    public partial class AbsF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return Math.Abs(value);
        }
    }

    /// <summary>
    ///     Represents "f32.neg" instruction.
    /// </summary>
    [OpCode(0x8C)]
    public partial class NegF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return -value;
        }
    }

    /// <summary>
    ///     Represents "f32.ceil" instruction.
    /// </summary>
    [OpCode(0x8D)]
    public partial class CeilF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return MathF.Ceiling(value);
        }
    }

    /// <summary>
    ///     Represents "f32.floor" instruction.
    /// </summary>
    [OpCode(0x8E)]
    public partial class FloorF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return MathF.Floor(value);
        }
    }

    /// <summary>
    ///     Represents "f32.trunc" instruction.
    /// </summary>
    [OpCode(0x8F)]
    public partial class TruncF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return MathF.Truncate(value);
        }
    }

    /// <summary>
    ///     Represents "f32.nearest" instruction.
    /// </summary>
    [OpCode(0x90)]
    public partial class NearestF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return MathF.Round(value);
        }
    }

    /// <summary>
    ///     Represents "f32.sqrt" instruction.
    /// </summary>
    [OpCode(0x91)]
    public partial class SqrtF32 : UnaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float value)
        {
            return MathF.Sqrt(value);
        }
    }

    /// <summary>
    ///     Represents "f32.add" instruction.
    /// </summary>
    [OpCode(0x92)]
    public partial class AddF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return lhs + rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.sub" instruction.
    /// </summary>
    [OpCode(0x93)]
    public partial class SubF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return lhs - rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.mul" instruction.
    /// </summary>
    [OpCode(0x94)]
    public partial class MulF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.div" instruction.
    /// </summary>
    [OpCode(0x95)]
    public partial class DivF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "f32.min" instruction.
    /// </summary>
    [OpCode(0x96)]
    public partial class MinF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return MathF.Min(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f32.max" instruction.
    /// </summary>
    [OpCode(0x97)]
    public partial class MaxF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            return MathF.Max(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f32.copysign" instruction.
    /// </summary>
    [OpCode(0x98)]
    public partial class CopysignF32 : BinaryInstruction<float, ValueTypeF32>
    {
        protected override float Operate(float lhs, float rhs)
        {
            const uint signMask = 0x8000_0000;

            var xbits = Unsafe.As<float, uint>(ref lhs);
            var ybits = Unsafe.As<float, uint>(ref rhs);

            var result = (xbits & ~signMask) | (ybits & signMask);

            return Unsafe.As<uint, float>(ref result);
        }
    }

    #endregion

    #region Operation (F64)

    /// <summary>
    ///     Represents "f64.abs" instruction.
    /// </summary>
    [OpCode(0x99)]
    public partial class AbsF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Abs(value);
        }
    }

    /// <summary>
    ///     Represents "f64.neg" instruction.
    /// </summary>
    [OpCode(0x9A)]
    public partial class NegF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return -value;
        }
    }

    /// <summary>
    ///     Represents "f64.ceil" instruction.
    /// </summary>
    [OpCode(0x9B)]
    public partial class CeilF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Ceiling(value);
        }
    }

    /// <summary>
    ///     Represents "f64.floor" instruction.
    /// </summary>
    [OpCode(0x9C)]
    public partial class FloorF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Floor(value);
        }
    }

    /// <summary>
    ///     Represents "f64.trunc" instruction.
    /// </summary>
    [OpCode(0x9D)]
    public partial class TruncF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Truncate(value);
        }
    }

    /// <summary>
    ///     Represents "f64.nearest" instruction.
    /// </summary>
    [OpCode(0x9E)]
    public partial class NearestF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Round(value);
        }
    }

    /// <summary>
    ///     Represents "f64.sqrt" instruction.
    /// </summary>
    [OpCode(0x9F)]
    public partial class SqrtF64 : UnaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double value)
        {
            return Math.Sqrt(value);
        }
    }

    /// <summary>
    ///     Represents "f64.add" instruction.
    /// </summary>
    [OpCode(0xA0)]
    public partial class AddF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return lhs + rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.sub" instruction.
    /// </summary>
    [OpCode(0xA1)]
    public partial class SubF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return lhs - rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.mul" instruction.
    /// </summary>
    [OpCode(0xA2)]
    public partial class MulF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.div" instruction.
    /// </summary>
    [OpCode(0xA3)]
    public partial class DivF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "f64.min" instruction.
    /// </summary>
    [OpCode(0xA4)]
    public partial class MinF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return Math.Min(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f64.max" instruction.
    /// </summary>
    [OpCode(0xA5)]
    public partial class MaxF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            return Math.Max(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f64.copysign" instruction.
    /// </summary>
    [OpCode(0xA6)]
    public partial class CopysignF64 : BinaryInstruction<double, ValueTypeF64>
    {
        protected override double Operate(double lhs, double rhs)
        {
            const ulong signMask = 0x8000_0000_0000_0000;

            var xbits = Unsafe.As<double, ulong>(ref lhs);
            var ybits = Unsafe.As<double, ulong>(ref lhs);

            var result = (xbits & ~signMask) | (ybits & signMask);

            return Unsafe.As<ulong, double>(ref result);
        }
    }

    #endregion

    #region Operations (Misc)

    /// <summary>
    ///     Represents "i32.wrap_i64" instruction.
    /// </summary>
    [OpCode(0xA7)]
    public partial class WrapI64I32 : UnaryInstruction<ulong, ValueTypeI64, uint, ValueTypeI32>
    {
        protected override uint Operate(ulong value)
        {
            return unchecked((uint)value);
        }
    }

    /// <summary>
    ///     Represents "i32.trunc_f32_s" instruction.
    /// </summary>
    [OpCode(0xA8)]
    public partial class TruncF32I32S : UnaryInstruction<float, ValueTypeF32, uint, ValueTypeI32>
    {
        protected override uint Operate(float value)
        {
            return unchecked((uint)checked((int)value));
        }
    }

    /// <summary>
    ///     Represents "i32.trunc_f32_u" instruction.
    /// </summary>
    [OpCode(0xA9)]
    public partial class TruncF32I32U : UnaryInstruction<float, ValueTypeF32, uint, ValueTypeI32>
    {
        protected override uint Operate(float value)
        {
            return checked((uint)value);
        }
    }

    /// <summary>
    ///     Represents "i32.trunc_f64_s" instruction.
    /// </summary>
    [OpCode(0xAA)]
    public partial class TruncF64I32S : UnaryInstruction<double, ValueTypeF64, uint, ValueTypeI32>
    {
        protected override uint Operate(double value)
        {
            return unchecked((uint)checked((int)value));
        }
    }

    /// <summary>
    ///     Represents "i32.trunc_f64_u" instruction.
    /// </summary>
    [OpCode(0xAB)]
    public partial class TruncF64I32U : UnaryInstruction<double, ValueTypeF64, uint, ValueTypeI32>
    {
        protected override uint Operate(double value)
        {
            return checked((uint)value);
        }
    }

    /// <summary>
    ///     Represents "i64.extend_i32_s" instruction.
    /// </summary>
    [OpCode(0xAC)]
    public partial class ExtendS : UnaryInstruction<uint, ValueTypeI32, ulong, ValueTypeI64>
    {
        protected override ulong Operate(uint value)
        {
            return unchecked((ulong)(int)value);
        }
    }

    /// <summary>
    ///     Represents "i64.extend_i32_u" instruction.
    /// </summary>
    [OpCode(0xAD)]
    public partial class ExtendU : UnaryInstruction<uint, ValueTypeI32, ulong, ValueTypeI64>
    {
        protected override ulong Operate(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64.trunc_f32_s" instruction.
    /// </summary>
    [OpCode(0xAE)]
    public partial class TruncF32I64S : UnaryInstruction<float, ValueTypeF32, ulong, ValueTypeI64>
    {
        protected override ulong Operate(float value)
        {
            return unchecked((ulong)checked((long)value));
        }
    }

    /// <summary>
    ///     Represents "i64.trunc_f32_u" instruction.
    /// </summary>
    [OpCode(0xAF)]
    public partial class TruncF32I64U : UnaryInstruction<float, ValueTypeF32, ulong, ValueTypeI64>
    {
        protected override ulong Operate(float value)
        {
            return checked((ulong)value);
        }
    }

    /// <summary>
    ///     Represents "i64.trunc_f64_s" instruction.
    /// </summary>
    [OpCode(0xB0)]
    public partial class TruncF64I64S : UnaryInstruction<double, ValueTypeF64, ulong, ValueTypeI64>
    {
        protected override ulong Operate(double value)
        {
            return unchecked((ulong)checked((long)value));
        }
    }

    /// <summary>
    ///     Represents "i64.trunc_f64_u" instruction.
    /// </summary>
    [OpCode(0xB1)]
    public partial class TruncF64I64U : UnaryInstruction<double, ValueTypeF64, ulong, ValueTypeI64>
    {
        protected override ulong Operate(double value)
        {
            return checked((ulong)value);
        }
    }

    /// <summary>
    ///     Represents "f32.convert_i32_s" instruction.
    /// </summary>
    [OpCode(0xB2)]
    public partial class ConvertI32F32S : UnaryInstruction<uint, ValueTypeI32, float, ValueTypeF32>
    {
        protected override float Operate(uint value)
        {
            return unchecked((int)value);
        }
    }

    /// <summary>
    ///     Represents "f32.convert_i32_u" instruction.
    /// </summary>
    [OpCode(0xB3)]
    public partial class ConvertI32F32U : UnaryInstruction<uint, ValueTypeI32, float, ValueTypeF32>
    {
        protected override float Operate(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32.convert_i64_s" instruction.
    /// </summary>
    [OpCode(0xB4)]
    public partial class ConvertI64F32S : UnaryInstruction<ulong, ValueTypeI64, float, ValueTypeF32>
    {
        protected override float Operate(ulong value)
        {
            return unchecked((long)value);
        }
    }

    /// <summary>
    ///     Represents "f32.convert_i64_u" instruction.
    /// </summary>
    [OpCode(0xB5)]
    public partial class ConvertI64F32U : UnaryInstruction<ulong, ValueTypeI64, float, ValueTypeF32>
    {
        protected override float Operate(ulong value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32.demote_f64" instruction.
    /// </summary>
    [OpCode(0xB6)]
    public partial class DemoteF64F32 : UnaryInstruction<double, ValueTypeF64, float, ValueTypeF32>
    {
        protected override float Operate(double value)
        {
            return (float)value;
        }
    }

    /// <summary>
    ///     Represents "f64.convert_i32_s" instruction.
    /// </summary>
    [OpCode(0xB7)]
    public partial class ConvertI32F64S : UnaryInstruction<uint, ValueTypeI32, double, ValueTypeF64>
    {
        protected override double Operate(uint value)
        {
            return unchecked((int)value);
        }
    }

    /// <summary>
    ///     Represents "f64.convert_i32_u" instruction.
    /// </summary>
    [OpCode(0xB8)]
    public partial class ConvertI32F64U : UnaryInstruction<uint, ValueTypeI32, double, ValueTypeF64>
    {
        protected override double Operate(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f64.convert_i64_s" instruction.
    /// </summary>
    [OpCode(0xB9)]
    public partial class ConvertI64F64S : UnaryInstruction<ulong, ValueTypeI64, double, ValueTypeF64>
    {
        protected override double Operate(ulong value)
        {
            return unchecked((long)value);
        }
    }

    /// <summary>
    ///     Represents "f64.convert_i64_u" instruction.
    /// </summary>
    [OpCode(0xBA)]
    public partial class ConvertI64F64U : UnaryInstruction<ulong, ValueTypeI64, double, ValueTypeF64>
    {
        protected override double Operate(ulong value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f64.promote_f32" instruction.
    /// </summary>
    [OpCode(0xBB)]
    public partial class PromoteF32F64 : UnaryInstruction<float, ValueTypeF32, double, ValueTypeF64>
    {
        protected override double Operate(float value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i32.reinterpret_f32" instruction.
    /// </summary>
    [OpCode(0xBC)]
    public partial class ReinterpretF32I32 : UnaryInstruction<float, ValueTypeF32, uint, ValueTypeI32>
    {
        protected override uint Operate(float value)
        {
            return Unsafe.As<float, uint>(ref value);
        }
    }

    /// <summary>
    ///     Represents "i64.reinterpret_f64" instruction.
    /// </summary>
    [OpCode(0xBD)]
    public partial class ReinterpretF64I64 : UnaryInstruction<double, ValueTypeF64, ulong, ValueTypeI64>
    {
        protected override ulong Operate(double value)
        {
            return Unsafe.As<double, ulong>(ref value);
        }
    }

    /// <summary>
    ///     Represents "f32.reinterpret_i32" instruction.
    /// </summary>
    [OpCode(0xBE)]
    public partial class ReinterpretI32F32 : UnaryInstruction<uint, ValueTypeI32, float, ValueTypeF32>
    {
        protected override float Operate(uint value)
        {
            return Unsafe.As<uint, float>(ref value);
        }
    }

    /// <summary>
    ///     Represents "f64.reinterpret_i64" instruction.
    /// </summary>
    [OpCode(0xBF)]
    public partial class ReinterpretI64F64 : UnaryInstruction<ulong, ValueTypeI64, double, ValueTypeF64>
    {
        protected override double Operate(ulong value)
        {
            return Unsafe.As<ulong, double>(ref value);
        }
    }

    /// <summary>
    ///     Represents "i32.extend8_s" instruction.
    /// </summary>
    [OpCode(0xC0)]
    public partial class ExtendS8I32 : UnaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint value)
        {
            return unchecked((uint)(sbyte)(byte)value);
        }
    }

    /// <summary>
    ///     Represents "i32.extend16_s" instruction.
    /// </summary>
    [OpCode(0xC1)]
    public partial class ExtendS16I32 : UnaryInstruction<uint, ValueTypeI32>
    {
        protected override uint Operate(uint value)
        {
            return unchecked((uint)(short)(ushort)value);
        }
    }

    /// <summary>
    ///     Represents "i64.extend8_s" instruction.
    /// </summary>
    [OpCode(0xC2)]
    public partial class ExtendS8I64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            return unchecked((ulong)(sbyte)(byte)value);
        }
    }

    /// <summary>
    ///     Represents "i64.extend16_s" instruction.
    /// </summary>
    [OpCode(0xC3)]
    public partial class ExtendS16I64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            return unchecked((ulong)(short)(ushort)value);
        }
    }

    /// <summary>
    ///     Represents "i64.extend32_s" instruction.
    /// </summary>
    [OpCode(0xC4)]
    public partial class ExtendS32I64 : UnaryInstruction<ulong, ValueTypeI64>
    {
        protected override ulong Operate(ulong value)
        {
            return unchecked((ulong)(int)(uint)value);
        }
    }

    #endregion
}