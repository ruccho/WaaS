using System;
using System.Runtime.CompilerServices;
using WaaS.Runtime;

namespace WaaS.Models
{
    #region Vector Float Helpers

    /// <summary>
    ///     Floating-point helpers used by vector instructions.
    ///     They do not rely on the platform specific behaviors of <see cref="Math" /> and <see cref="MathF" />
    ///     regarding NaN and signed zeros.
    /// </summary>
    internal static class VectorFloat
    {
        private const uint F32SignMask = 0x8000_0000;
        private const uint F32QuietBit = 0x0040_0000;
        private const ulong F64SignMask = 0x8000_0000_0000_0000;
        private const ulong F64QuietBit = 0x0008_0000_0000_0000;

        private static uint ToBits(float value)
        {
            return Unsafe.As<float, uint>(ref value);
        }

        private static float FromBits(uint bits)
        {
            return Unsafe.As<uint, float>(ref bits);
        }

        private static ulong ToBits(double value)
        {
            return Unsafe.As<double, ulong>(ref value);
        }

        private static double FromBits(ulong bits)
        {
            return Unsafe.As<ulong, double>(ref bits);
        }

        public static float Abs(float value)
        {
            return FromBits(ToBits(value) & ~F32SignMask);
        }

        public static double Abs(double value)
        {
            return FromBits(ToBits(value) & ~F64SignMask);
        }

        public static float Neg(float value)
        {
            return FromBits(ToBits(value) ^ F32SignMask);
        }

        public static double Neg(double value)
        {
            return FromBits(ToBits(value) ^ F64SignMask);
        }

        private static float CopySign(float magnitude, float sign)
        {
            return FromBits((ToBits(magnitude) & ~F32SignMask) | (ToBits(sign) & F32SignMask));
        }

        private static double CopySign(double magnitude, double sign)
        {
            return FromBits((ToBits(magnitude) & ~F64SignMask) | (ToBits(sign) & F64SignMask));
        }

        private static float Quiet(float value)
        {
            return FromBits(ToBits(value) | F32QuietBit);
        }

        private static double Quiet(double value)
        {
            return FromBits(ToBits(value) | F64QuietBit);
        }

        public static float Min(float lhs, float rhs)
        {
            if (float.IsNaN(lhs) || float.IsNaN(rhs)) return lhs + rhs;
            // -0 is less than +0
            if (lhs == rhs) return FromBits(ToBits(lhs) | ToBits(rhs));
            return lhs < rhs ? lhs : rhs;
        }

        public static double Min(double lhs, double rhs)
        {
            if (double.IsNaN(lhs) || double.IsNaN(rhs)) return lhs + rhs;
            // -0 is less than +0
            if (lhs == rhs) return FromBits(ToBits(lhs) | ToBits(rhs));
            return lhs < rhs ? lhs : rhs;
        }

        public static float Max(float lhs, float rhs)
        {
            if (float.IsNaN(lhs) || float.IsNaN(rhs)) return lhs + rhs;
            // +0 is greater than -0
            if (lhs == rhs) return FromBits(ToBits(lhs) & ToBits(rhs));
            return lhs > rhs ? lhs : rhs;
        }

        public static double Max(double lhs, double rhs)
        {
            if (double.IsNaN(lhs) || double.IsNaN(rhs)) return lhs + rhs;
            // +0 is greater than -0
            if (lhs == rhs) return FromBits(ToBits(lhs) & ToBits(rhs));
            return lhs > rhs ? lhs : rhs;
        }

        public static float Ceiling(float value)
        {
            return float.IsNaN(value) ? Quiet(value) : CopySign(MathF.Ceiling(value), value);
        }

        public static double Ceiling(double value)
        {
            return double.IsNaN(value) ? Quiet(value) : CopySign(Math.Ceiling(value), value);
        }

        public static float Floor(float value)
        {
            return float.IsNaN(value) ? Quiet(value) : CopySign(MathF.Floor(value), value);
        }

        public static double Floor(double value)
        {
            return double.IsNaN(value) ? Quiet(value) : CopySign(Math.Floor(value), value);
        }

        public static float Truncate(float value)
        {
            return float.IsNaN(value) ? Quiet(value) : CopySign(MathF.Truncate(value), value);
        }

        public static double Truncate(double value)
        {
            return double.IsNaN(value) ? Quiet(value) : CopySign(Math.Truncate(value), value);
        }

        /// <summary>
        ///     Rounds to the nearest integer, ties to even.
        /// </summary>
        public static float Nearest(float value)
        {
            if (float.IsNaN(value)) return Quiet(value);
            var abs = Abs(value);
            // values of 2^23 or larger (including infinity) have no fractional part
            if (!(abs < 8388608f)) return value;
            var floor = MathF.Floor(abs);
            var fraction = abs - floor; // exact
            if (fraction > 0.5f || (fraction == 0.5f && floor % 2f != 0f)) floor += 1f;
            return CopySign(floor, value);
        }

        /// <summary>
        ///     Rounds to the nearest integer, ties to even.
        /// </summary>
        public static double Nearest(double value)
        {
            if (double.IsNaN(value)) return Quiet(value);
            var abs = Abs(value);
            // values of 2^52 or larger (including infinity) have no fractional part
            if (!(abs < 4503599627370496.0)) return value;
            var floor = Math.Floor(abs);
            var fraction = abs - floor; // exact
            if (fraction > 0.5 || (fraction == 0.5 && floor % 2.0 != 0.0)) floor += 1.0;
            return CopySign(floor, value);
        }
    }

    #endregion

    #region Comparison

    /// <summary>
    ///     Represents "f32x4.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0x41)]
    public partial class EqF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0x42)]
    public partial class NeF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.lt" instruction.
    /// </summary>
    [OpCode(0xFD, 0x43)]
    public partial class LtF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.gt" instruction.
    /// </summary>
    [OpCode(0xFD, 0x44)]
    public partial class GtF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.le" instruction.
    /// </summary>
    [OpCode(0xFD, 0x45)]
    public partial class LeF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.ge" instruction.
    /// </summary>
    [OpCode(0xFD, 0x46)]
    public partial class GeF32x4 : V128LanewiseCompareInstruction<float>
    {
        protected override bool OperateLane(float lhs, float rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0x47)]
    public partial class EqF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0x48)]
    public partial class NeF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.lt" instruction.
    /// </summary>
    [OpCode(0xFD, 0x49)]
    public partial class LtF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.gt" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4A)]
    public partial class GtF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.le" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4B)]
    public partial class LeF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.ge" instruction.
    /// </summary>
    [OpCode(0xFD, 0x4C)]
    public partial class GeF64x2 : V128LanewiseCompareInstruction<double>
    {
        protected override bool OperateLane(double lhs, double rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Arithmetic

    /// <summary>
    ///     Represents "f32x4.ceil" instruction.
    /// </summary>
    [OpCode(0xFD, 0x67)]
    public partial class CeilF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Ceiling(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.floor" instruction.
    /// </summary>
    [OpCode(0xFD, 0x68)]
    public partial class FloorF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Floor(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.trunc" instruction.
    /// </summary>
    [OpCode(0xFD, 0x69)]
    public partial class TruncF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Truncate(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.nearest" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6A)]
    public partial class NearestF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Nearest(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.ceil" instruction.
    /// </summary>
    [OpCode(0xFD, 0x74)]
    public partial class CeilF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Ceiling(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.floor" instruction.
    /// </summary>
    [OpCode(0xFD, 0x75)]
    public partial class FloorF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Floor(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.trunc" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7A)]
    public partial class TruncF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Truncate(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.nearest" instruction.
    /// </summary>
    [OpCode(0xFD, 0x94)]
    public partial class NearestF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Nearest(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE0)]
    public partial class AbsF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Abs(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE1)]
    public partial class NegF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return VectorFloat.Neg(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.sqrt" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE3)]
    public partial class SqrtF32x4 : V128LanewiseUnaryInstruction<float>
    {
        protected override float OperateLane(float value)
        {
            return MathF.Sqrt(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE4)]
    public partial class AddF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return lhs + rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE5)]
    public partial class SubF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return lhs - rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.mul" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE6)]
    public partial class MulF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.div" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE7)]
    public partial class DivF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.min" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE8)]
    public partial class MinF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return VectorFloat.Min(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f32x4.max" instruction.
    /// </summary>
    [OpCode(0xFD, 0xE9)]
    public partial class MaxF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return VectorFloat.Max(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f32x4.pmin" instruction.
    /// </summary>
    [OpCode(0xFD, 0xEA)]
    public partial class PminF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return rhs < lhs ? rhs : lhs;
        }
    }

    /// <summary>
    ///     Represents "f32x4.pmax" instruction.
    /// </summary>
    [OpCode(0xFD, 0xEB)]
    public partial class PmaxF32x4 : V128LanewiseBinaryInstruction<float>
    {
        protected override float OperateLane(float lhs, float rhs)
        {
            return lhs < rhs ? rhs : lhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0xEC)]
    public partial class AbsF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Abs(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0xED)]
    public partial class NegF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return VectorFloat.Neg(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.sqrt" instruction.
    /// </summary>
    [OpCode(0xFD, 0xEF)]
    public partial class SqrtF64x2 : V128LanewiseUnaryInstruction<double>
    {
        protected override double OperateLane(double value)
        {
            return Math.Sqrt(value);
        }
    }

    /// <summary>
    ///     Represents "f64x2.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF0)]
    public partial class AddF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return lhs + rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF1)]
    public partial class SubF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return lhs - rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.mul" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF2)]
    public partial class MulF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.div" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF3)]
    public partial class DivF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return lhs / rhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.min" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF4)]
    public partial class MinF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return VectorFloat.Min(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f64x2.max" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF5)]
    public partial class MaxF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return VectorFloat.Max(lhs, rhs);
        }
    }

    /// <summary>
    ///     Represents "f64x2.pmin" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF6)]
    public partial class PminF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return rhs < lhs ? rhs : lhs;
        }
    }

    /// <summary>
    ///     Represents "f64x2.pmax" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF7)]
    public partial class PmaxF64x2 : V128LanewiseBinaryInstruction<double>
    {
        protected override double OperateLane(double lhs, double rhs)
        {
            return lhs < rhs ? rhs : lhs;
        }
    }

    #endregion

    #region Conversion

    /// <summary>
    ///     Represents "i32x4.trunc_sat_f32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF8)]
    public partial class TruncSatF32x4I32x4S : V128LanewiseUnaryInstruction<float, int>
    {
        protected override int OperateLane(float value)
        {
            return SaturatingTruncation.ToInt32(value);
        }
    }

    /// <summary>
    ///     Represents "i32x4.trunc_sat_f32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xF9)]
    public partial class TruncSatF32x4I32x4U : V128LanewiseUnaryInstruction<float, uint>
    {
        protected override uint OperateLane(float value)
        {
            return SaturatingTruncation.ToUInt32(value);
        }
    }

    /// <summary>
    ///     Represents "f32x4.convert_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFA)]
    public partial class ConvertI32x4F32x4S : V128LanewiseUnaryInstruction<int, float>
    {
        protected override float OperateLane(int value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32x4.convert_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFB)]
    public partial class ConvertI32x4F32x4U : V128LanewiseUnaryInstruction<uint, float>
    {
        protected override float OperateLane(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "f32x4.demote_f64x2_zero" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5E)]
    public partial class DemoteF64x2F32x4Zero : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<float>(ref result, 0) = (float)VectorLanes.Get<double>(value, 0);
            V128.Lane<float>(ref result, 1) = (float)VectorLanes.Get<double>(value, 1);
            return result;
        }
    }

    /// <summary>
    ///     Represents "f64x2.promote_low_f32x4" instruction.
    /// </summary>
    [OpCode(0xFD, 0x5F)]
    public partial class PromoteLowF32x4F64x2 : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<double>(ref result, 0) = VectorLanes.Get<float>(value, 0);
            V128.Lane<double>(ref result, 1) = VectorLanes.Get<float>(value, 1);
            return result;
        }
    }

    /// <summary>
    ///     Represents "i32x4.trunc_sat_f64x2_s_zero" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFC)]
    public partial class TruncSatF64x2I32x4SZero : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<int>(ref result, 0) = SaturatingTruncation.ToInt32(VectorLanes.Get<double>(value, 0));
            V128.Lane<int>(ref result, 1) = SaturatingTruncation.ToInt32(VectorLanes.Get<double>(value, 1));
            return result;
        }
    }

    /// <summary>
    ///     Represents "i32x4.trunc_sat_f64x2_u_zero" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFD)]
    public partial class TruncSatF64x2I32x4UZero : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<uint>(ref result, 0) = SaturatingTruncation.ToUInt32(VectorLanes.Get<double>(value, 0));
            V128.Lane<uint>(ref result, 1) = SaturatingTruncation.ToUInt32(VectorLanes.Get<double>(value, 1));
            return result;
        }
    }

    /// <summary>
    ///     Represents "f64x2.convert_low_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFE)]
    public partial class ConvertLowI32x4F64x2S : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<double>(ref result, 0) = VectorLanes.Get<int>(value, 0);
            V128.Lane<double>(ref result, 1) = VectorLanes.Get<int>(value, 1);
            return result;
        }
    }

    /// <summary>
    ///     Represents "f64x2.convert_low_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xFF)]
    public partial class ConvertLowI32x4F64x2U : V128UnaryInstruction
    {
        protected override V128 Operate(in V128 value)
        {
            var result = default(V128);
            V128.Lane<double>(ref result, 0) = VectorLanes.Get<uint>(value, 0);
            V128.Lane<double>(ref result, 1) = VectorLanes.Get<uint>(value, 1);
            return result;
        }
    }

    #endregion
}
