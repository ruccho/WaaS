using WaaS.Runtime;

namespace WaaS.Models
{
    #region Vector Integer Helpers

    /// <summary>
    ///     Bit manipulation helpers used by vector instructions.
    /// </summary>
    internal static class VectorBits
    {
        public static byte PopCount(byte value)
        {
            var v = value - ((value >> 1) & 0x55);
            v = (v & 0x33) + ((v >> 2) & 0x33);
            return (byte)((v + (v >> 4)) & 0x0F);
        }

        /// <summary>
        ///     Determines whether any of the 8-bit lanes of the value is zero.
        /// </summary>
        public static bool HasZeroByte(ulong value)
        {
            return ((value - 0x0101_0101_0101_0101UL) & ~value & 0x8080_8080_8080_8080UL) != 0;
        }

        /// <summary>
        ///     Determines whether any of the 16-bit lanes of the value is zero.
        /// </summary>
        public static bool HasZeroUInt16(ulong value)
        {
            return ((value - 0x0001_0001_0001_0001UL) & ~value & 0x8000_8000_8000_8000UL) != 0;
        }
    }

    #endregion

    #region Comparison

    /// <summary>
    ///     Represents "i8x16.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0x23)]
    public partial class EqI8x16 : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0x24)]
    public partial class NeI8x16 : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.lt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x25)]
    public partial class LtI8x16S : V128LanewiseCompareInstruction<sbyte>
    {
        protected override bool OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.lt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x26)]
    public partial class LtI8x16U : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.gt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x27)]
    public partial class GtI8x16S : V128LanewiseCompareInstruction<sbyte>
    {
        protected override bool OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.gt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x28)]
    public partial class GtI8x16U : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.le_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x29)]
    public partial class LeI8x16S : V128LanewiseCompareInstruction<sbyte>
    {
        protected override bool OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.le_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2A)]
    public partial class LeI8x16U : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.ge_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2B)]
    public partial class GeI8x16S : V128LanewiseCompareInstruction<sbyte>
    {
        protected override bool OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.ge_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2C)]
    public partial class GeI8x16U : V128LanewiseCompareInstruction<byte>
    {
        protected override bool OperateLane(byte lhs, byte rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2D)]
    public partial class EqI16x8 : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2E)]
    public partial class NeI16x8 : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.lt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x2F)]
    public partial class LtI16x8S : V128LanewiseCompareInstruction<short>
    {
        protected override bool OperateLane(short lhs, short rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.lt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x30)]
    public partial class LtI16x8U : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.gt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x31)]
    public partial class GtI16x8S : V128LanewiseCompareInstruction<short>
    {
        protected override bool OperateLane(short lhs, short rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.gt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x32)]
    public partial class GtI16x8U : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.le_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x33)]
    public partial class LeI16x8S : V128LanewiseCompareInstruction<short>
    {
        protected override bool OperateLane(short lhs, short rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.le_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x34)]
    public partial class LeI16x8U : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.ge_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x35)]
    public partial class GeI16x8S : V128LanewiseCompareInstruction<short>
    {
        protected override bool OperateLane(short lhs, short rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.ge_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x36)]
    public partial class GeI16x8U : V128LanewiseCompareInstruction<ushort>
    {
        protected override bool OperateLane(ushort lhs, ushort rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0x37)]
    public partial class EqI32x4 : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0x38)]
    public partial class NeI32x4 : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.lt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x39)]
    public partial class LtI32x4S : V128LanewiseCompareInstruction<int>
    {
        protected override bool OperateLane(int lhs, int rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.lt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3A)]
    public partial class LtI32x4U : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.gt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3B)]
    public partial class GtI32x4S : V128LanewiseCompareInstruction<int>
    {
        protected override bool OperateLane(int lhs, int rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.gt_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3C)]
    public partial class GtI32x4U : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.le_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3D)]
    public partial class LeI32x4S : V128LanewiseCompareInstruction<int>
    {
        protected override bool OperateLane(int lhs, int rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.le_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3E)]
    public partial class LeI32x4U : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.ge_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x3F)]
    public partial class GeI32x4S : V128LanewiseCompareInstruction<int>
    {
        protected override bool OperateLane(int lhs, int rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.ge_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x40)]
    public partial class GeI32x4U : V128LanewiseCompareInstruction<uint>
    {
        protected override bool OperateLane(uint lhs, uint rhs)
        {
            return lhs >= rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.eq" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD6)]
    public partial class EqI64x2 : V128LanewiseCompareInstruction<ulong>
    {
        protected override bool OperateLane(ulong lhs, ulong rhs)
        {
            return lhs == rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.ne" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD7)]
    public partial class NeI64x2 : V128LanewiseCompareInstruction<ulong>
    {
        protected override bool OperateLane(ulong lhs, ulong rhs)
        {
            return lhs != rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.lt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD8)]
    public partial class LtI64x2S : V128LanewiseCompareInstruction<long>
    {
        protected override bool OperateLane(long lhs, long rhs)
        {
            return lhs < rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.gt_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD9)]
    public partial class GtI64x2S : V128LanewiseCompareInstruction<long>
    {
        protected override bool OperateLane(long lhs, long rhs)
        {
            return lhs > rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.le_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDA)]
    public partial class LeI64x2S : V128LanewiseCompareInstruction<long>
    {
        protected override bool OperateLane(long lhs, long rhs)
        {
            return lhs <= rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.ge_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDB)]
    public partial class GeI64x2S : V128LanewiseCompareInstruction<long>
    {
        protected override bool OperateLane(long lhs, long rhs)
        {
            return lhs >= rhs;
        }
    }

    #endregion

    #region Arithmetic

    /// <summary>
    ///     Represents "i8x16.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0x60)]
    public partial class AbsI8x16 : V128LanewiseUnaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte value)
        {
            return (sbyte)(value < 0 ? -value : value);
        }
    }

    /// <summary>
    ///     Represents "i8x16.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0x61)]
    public partial class NegI8x16 : V128LanewiseUnaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte value)
        {
            return (sbyte)-value;
        }
    }

    /// <summary>
    ///     Represents "i8x16.popcnt" instruction.
    /// </summary>
    [OpCode(0xFD, 0x62)]
    public partial class PopcntI8x16 : V128LanewiseUnaryInstruction<byte>
    {
        protected override byte OperateLane(byte value)
        {
            return VectorBits.PopCount(value);
        }
    }

    /// <summary>
    ///     Represents "i8x16.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6E)]
    public partial class AddI8x16 : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return (byte)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.add_sat_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6F)]
    public partial class AddSatI8x16S : V128LanewiseBinaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte lhs, sbyte rhs)
        {
            return VectorSaturation.ToSByte(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.add_sat_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x70)]
    public partial class AddSatI8x16U : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return VectorSaturation.ToByte(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0x71)]
    public partial class SubI8x16 : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return (byte)(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.sub_sat_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x72)]
    public partial class SubSatI8x16S : V128LanewiseBinaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte lhs, sbyte rhs)
        {
            return VectorSaturation.ToSByte(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.sub_sat_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x73)]
    public partial class SubSatI8x16U : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return VectorSaturation.ToByte(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i8x16.min_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x76)]
    public partial class MinI8x16S : V128LanewiseBinaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.min_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x77)]
    public partial class MinI8x16U : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.max_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x78)]
    public partial class MaxI8x16S : V128LanewiseBinaryInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte lhs, sbyte rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.max_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x79)]
    public partial class MaxI8x16U : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i8x16.avgr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7B)]
    public partial class AvgrI8x16U : V128LanewiseBinaryInstruction<byte>
    {
        protected override byte OperateLane(byte lhs, byte rhs)
        {
            return (byte)((lhs + rhs + 1) >> 1);
        }
    }

    /// <summary>
    ///     Represents "i16x8.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0x80)]
    public partial class AbsI16x8 : V128LanewiseUnaryInstruction<short>
    {
        protected override short OperateLane(short value)
        {
            return (short)(value < 0 ? -value : value);
        }
    }

    /// <summary>
    ///     Represents "i16x8.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0x81)]
    public partial class NegI16x8 : V128LanewiseUnaryInstruction<short>
    {
        protected override short OperateLane(short value)
        {
            return (short)-value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.q15mulr_sat_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x82)]
    public partial class Q15mulrSatI16x8S : V128LanewiseBinaryInstruction<short>
    {
        protected override short OperateLane(short lhs, short rhs)
        {
            return VectorSaturation.ToInt16((lhs * rhs + 0x4000) >> 15);
        }
    }

    /// <summary>
    ///     Represents "i16x8.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8E)]
    public partial class AddI16x8 : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return (ushort)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.add_sat_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8F)]
    public partial class AddSatI16x8S : V128LanewiseBinaryInstruction<short>
    {
        protected override short OperateLane(short lhs, short rhs)
        {
            return VectorSaturation.ToInt16(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.add_sat_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x90)]
    public partial class AddSatI16x8U : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return VectorSaturation.ToUInt16(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0x91)]
    public partial class SubI16x8 : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return (ushort)(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.sub_sat_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x92)]
    public partial class SubSatI16x8S : V128LanewiseBinaryInstruction<short>
    {
        protected override short OperateLane(short lhs, short rhs)
        {
            return VectorSaturation.ToInt16(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.sub_sat_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x93)]
    public partial class SubSatI16x8U : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return VectorSaturation.ToUInt16(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.mul" instruction.
    /// </summary>
    [OpCode(0xFD, 0x95)]
    public partial class MulI16x8 : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return (ushort)(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.min_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x96)]
    public partial class MinI16x8S : V128LanewiseBinaryInstruction<short>
    {
        protected override short OperateLane(short lhs, short rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.min_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x97)]
    public partial class MinI16x8U : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.max_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x98)]
    public partial class MaxI16x8S : V128LanewiseBinaryInstruction<short>
    {
        protected override short OperateLane(short lhs, short rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.max_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x99)]
    public partial class MaxI16x8U : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i16x8.avgr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x9B)]
    public partial class AvgrI16x8U : V128LanewiseBinaryInstruction<ushort>
    {
        protected override ushort OperateLane(ushort lhs, ushort rhs)
        {
            return (ushort)((lhs + rhs + 1) >> 1);
        }
    }

    /// <summary>
    ///     Represents "i32x4.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA0)]
    public partial class AbsI32x4 : V128LanewiseUnaryInstruction<int>
    {
        protected override int OperateLane(int value)
        {
            return unchecked(value < 0 ? -value : value);
        }
    }

    /// <summary>
    ///     Represents "i32x4.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA1)]
    public partial class NegI32x4 : V128LanewiseUnaryInstruction<int>
    {
        protected override int OperateLane(int value)
        {
            return unchecked(-value);
        }
    }

    /// <summary>
    ///     Represents "i32x4.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0xAE)]
    public partial class AddI32x4 : V128LanewiseBinaryInstruction<uint>
    {
        protected override uint OperateLane(uint lhs, uint rhs)
        {
            return unchecked(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB1)]
    public partial class SubI32x4 : V128LanewiseBinaryInstruction<uint>
    {
        protected override uint OperateLane(uint lhs, uint rhs)
        {
            return unchecked(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.mul" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB5)]
    public partial class MulI32x4 : V128LanewiseBinaryInstruction<uint>
    {
        protected override uint OperateLane(uint lhs, uint rhs)
        {
            return unchecked(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.min_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB6)]
    public partial class MinI32x4S : V128LanewiseBinaryInstruction<int>
    {
        protected override int OperateLane(int lhs, int rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.min_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB7)]
    public partial class MinI32x4U : V128LanewiseBinaryInstruction<uint>
    {
        protected override uint OperateLane(uint lhs, uint rhs)
        {
            return lhs < rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.max_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB8)]
    public partial class MaxI32x4S : V128LanewiseBinaryInstruction<int>
    {
        protected override int OperateLane(int lhs, int rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.max_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xB9)]
    public partial class MaxI32x4U : V128LanewiseBinaryInstruction<uint>
    {
        protected override uint OperateLane(uint lhs, uint rhs)
        {
            return lhs > rhs ? lhs : rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.abs" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC0)]
    public partial class AbsI64x2 : V128LanewiseUnaryInstruction<long>
    {
        protected override long OperateLane(long value)
        {
            return unchecked(value < 0 ? -value : value);
        }
    }

    /// <summary>
    ///     Represents "i64x2.neg" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC1)]
    public partial class NegI64x2 : V128LanewiseUnaryInstruction<long>
    {
        protected override long OperateLane(long value)
        {
            return unchecked(-value);
        }
    }

    /// <summary>
    ///     Represents "i64x2.add" instruction.
    /// </summary>
    [OpCode(0xFD, 0xCE)]
    public partial class AddI64x2 : V128LanewiseBinaryInstruction<ulong>
    {
        protected override ulong OperateLane(ulong lhs, ulong rhs)
        {
            return unchecked(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i64x2.sub" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD1)]
    public partial class SubI64x2 : V128LanewiseBinaryInstruction<ulong>
    {
        protected override ulong OperateLane(ulong lhs, ulong rhs)
        {
            return unchecked(lhs - rhs);
        }
    }

    /// <summary>
    ///     Represents "i64x2.mul" instruction.
    /// </summary>
    [OpCode(0xFD, 0xD5)]
    public partial class MulI64x2 : V128LanewiseBinaryInstruction<ulong>
    {
        protected override ulong OperateLane(ulong lhs, ulong rhs)
        {
            return unchecked(lhs * rhs);
        }
    }

    #endregion

    #region Shift

    /// <summary>
    ///     Represents "i8x16.shl" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6B)]
    public partial class ShlI8x16 : V128ShiftInstruction<byte>
    {
        protected override byte OperateLane(byte value, int shift)
        {
            return (byte)(value << shift);
        }
    }

    /// <summary>
    ///     Represents "i8x16.shr_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6C)]
    public partial class ShrI8x16S : V128ShiftInstruction<sbyte>
    {
        protected override sbyte OperateLane(sbyte value, int shift)
        {
            return (sbyte)(value >> shift);
        }
    }

    /// <summary>
    ///     Represents "i8x16.shr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x6D)]
    public partial class ShrI8x16U : V128ShiftInstruction<byte>
    {
        protected override byte OperateLane(byte value, int shift)
        {
            return (byte)(value >> shift);
        }
    }

    /// <summary>
    ///     Represents "i16x8.shl" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8B)]
    public partial class ShlI16x8 : V128ShiftInstruction<ushort>
    {
        protected override ushort OperateLane(ushort value, int shift)
        {
            return (ushort)(value << shift);
        }
    }

    /// <summary>
    ///     Represents "i16x8.shr_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8C)]
    public partial class ShrI16x8S : V128ShiftInstruction<short>
    {
        protected override short OperateLane(short value, int shift)
        {
            return (short)(value >> shift);
        }
    }

    /// <summary>
    ///     Represents "i16x8.shr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8D)]
    public partial class ShrI16x8U : V128ShiftInstruction<ushort>
    {
        protected override ushort OperateLane(ushort value, int shift)
        {
            return (ushort)(value >> shift);
        }
    }

    /// <summary>
    ///     Represents "i32x4.shl" instruction.
    /// </summary>
    [OpCode(0xFD, 0xAB)]
    public partial class ShlI32x4 : V128ShiftInstruction<uint>
    {
        protected override uint OperateLane(uint value, int shift)
        {
            return value << shift;
        }
    }

    /// <summary>
    ///     Represents "i32x4.shr_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xAC)]
    public partial class ShrI32x4S : V128ShiftInstruction<int>
    {
        protected override int OperateLane(int value, int shift)
        {
            return value >> shift;
        }
    }

    /// <summary>
    ///     Represents "i32x4.shr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xAD)]
    public partial class ShrI32x4U : V128ShiftInstruction<uint>
    {
        protected override uint OperateLane(uint value, int shift)
        {
            return value >> shift;
        }
    }

    /// <summary>
    ///     Represents "i64x2.shl" instruction.
    /// </summary>
    [OpCode(0xFD, 0xCB)]
    public partial class ShlI64x2 : V128ShiftInstruction<ulong>
    {
        protected override ulong OperateLane(ulong value, int shift)
        {
            return value << shift;
        }
    }

    /// <summary>
    ///     Represents "i64x2.shr_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xCC)]
    public partial class ShrI64x2S : V128ShiftInstruction<long>
    {
        protected override long OperateLane(long value, int shift)
        {
            return value >> shift;
        }
    }

    /// <summary>
    ///     Represents "i64x2.shr_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xCD)]
    public partial class ShrI64x2U : V128ShiftInstruction<ulong>
    {
        protected override ulong OperateLane(ulong value, int shift)
        {
            return value >> shift;
        }
    }

    #endregion

    #region Conversion

    /// <summary>
    ///     Represents "i8x16.narrow_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x65)]
    public partial class NarrowI16x8I8x16S : V128NarrowInstruction<short, sbyte>
    {
        protected override sbyte OperateLane(short value)
        {
            return VectorSaturation.ToSByte(value);
        }
    }

    /// <summary>
    ///     Represents "i8x16.narrow_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x66)]
    public partial class NarrowI16x8I8x16U : V128NarrowInstruction<short, byte>
    {
        protected override byte OperateLane(short value)
        {
            return VectorSaturation.ToByte(value);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extadd_pairwise_i8x16_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7C)]
    public partial class ExtAddPairwiseI8x16I16x8S : V128ExtAddPairwiseInstruction<sbyte, short>
    {
        protected override short OperateLane(sbyte lhs, sbyte rhs)
        {
            return (short)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extadd_pairwise_i8x16_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7D)]
    public partial class ExtAddPairwiseI8x16I16x8U : V128ExtAddPairwiseInstruction<byte, ushort>
    {
        protected override ushort OperateLane(byte lhs, byte rhs)
        {
            return (ushort)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.extadd_pairwise_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7E)]
    public partial class ExtAddPairwiseI16x8I32x4S : V128ExtAddPairwiseInstruction<short, int>
    {
        protected override int OperateLane(short lhs, short rhs)
        {
            return (int)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.extadd_pairwise_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x7F)]
    public partial class ExtAddPairwiseI16x8I32x4U : V128ExtAddPairwiseInstruction<ushort, uint>
    {
        protected override uint OperateLane(ushort lhs, ushort rhs)
        {
            return (uint)(lhs + rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.narrow_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x85)]
    public partial class NarrowI32x4I16x8S : V128NarrowInstruction<int, short>
    {
        protected override short OperateLane(int value)
        {
            return VectorSaturation.ToInt16(value);
        }
    }

    /// <summary>
    ///     Represents "i16x8.narrow_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x86)]
    public partial class NarrowI32x4I16x8U : V128NarrowInstruction<int, ushort>
    {
        protected override ushort OperateLane(int value)
        {
            return VectorSaturation.ToUInt16(value);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extend_low_i8x16_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x87)]
    public partial class ExtendLowI8x16I16x8S : V128ExtendInstruction<sbyte, short>
    {
        protected override bool IsHigh => false;

        protected override short OperateLane(sbyte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.extend_high_i8x16_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x88)]
    public partial class ExtendHighI8x16I16x8S : V128ExtendInstruction<sbyte, short>
    {
        protected override bool IsHigh => true;

        protected override short OperateLane(sbyte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.extend_low_i8x16_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x89)]
    public partial class ExtendLowI8x16I16x8U : V128ExtendInstruction<byte, ushort>
    {
        protected override bool IsHigh => false;

        protected override ushort OperateLane(byte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.extend_high_i8x16_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x8A)]
    public partial class ExtendHighI8x16I16x8U : V128ExtendInstruction<byte, ushort>
    {
        protected override bool IsHigh => true;

        protected override ushort OperateLane(byte value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i16x8.extmul_low_i8x16_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x9C)]
    public partial class ExtMulLowI8x16I16x8S : V128ExtMulInstruction<sbyte, short>
    {
        protected override bool IsHigh => false;

        protected override short OperateLane(sbyte lhs, sbyte rhs)
        {
            return (short)(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extmul_high_i8x16_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0x9D)]
    public partial class ExtMulHighI8x16I16x8S : V128ExtMulInstruction<sbyte, short>
    {
        protected override bool IsHigh => true;

        protected override short OperateLane(sbyte lhs, sbyte rhs)
        {
            return (short)(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extmul_low_i8x16_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x9E)]
    public partial class ExtMulLowI8x16I16x8U : V128ExtMulInstruction<byte, ushort>
    {
        protected override bool IsHigh => false;

        protected override ushort OperateLane(byte lhs, byte rhs)
        {
            return (ushort)(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i16x8.extmul_high_i8x16_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0x9F)]
    public partial class ExtMulHighI8x16I16x8U : V128ExtMulInstruction<byte, ushort>
    {
        protected override bool IsHigh => true;

        protected override ushort OperateLane(byte lhs, byte rhs)
        {
            return (ushort)(lhs * rhs);
        }
    }

    /// <summary>
    ///     Represents "i32x4.extend_low_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA7)]
    public partial class ExtendLowI16x8I32x4S : V128ExtendInstruction<short, int>
    {
        protected override bool IsHigh => false;

        protected override int OperateLane(short value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extend_high_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA8)]
    public partial class ExtendHighI16x8I32x4S : V128ExtendInstruction<short, int>
    {
        protected override bool IsHigh => true;

        protected override int OperateLane(short value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extend_low_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA9)]
    public partial class ExtendLowI16x8I32x4U : V128ExtendInstruction<ushort, uint>
    {
        protected override bool IsHigh => false;

        protected override uint OperateLane(ushort value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extend_high_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xAA)]
    public partial class ExtendHighI16x8I32x4U : V128ExtendInstruction<ushort, uint>
    {
        protected override bool IsHigh => true;

        protected override uint OperateLane(ushort value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extmul_low_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xBC)]
    public partial class ExtMulLowI16x8I32x4S : V128ExtMulInstruction<short, int>
    {
        protected override bool IsHigh => false;

        protected override int OperateLane(short lhs, short rhs)
        {
            return (int)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extmul_high_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xBD)]
    public partial class ExtMulHighI16x8I32x4S : V128ExtMulInstruction<short, int>
    {
        protected override bool IsHigh => true;

        protected override int OperateLane(short lhs, short rhs)
        {
            return (int)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extmul_low_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xBE)]
    public partial class ExtMulLowI16x8I32x4U : V128ExtMulInstruction<ushort, uint>
    {
        protected override bool IsHigh => false;

        protected override uint OperateLane(ushort lhs, ushort rhs)
        {
            return (uint)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.extmul_high_i16x8_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xBF)]
    public partial class ExtMulHighI16x8I32x4U : V128ExtMulInstruction<ushort, uint>
    {
        protected override bool IsHigh => true;

        protected override uint OperateLane(ushort lhs, ushort rhs)
        {
            return (uint)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extend_low_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC7)]
    public partial class ExtendLowI32x4I64x2S : V128ExtendInstruction<int, long>
    {
        protected override bool IsHigh => false;

        protected override long OperateLane(int value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extend_high_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC8)]
    public partial class ExtendHighI32x4I64x2S : V128ExtendInstruction<int, long>
    {
        protected override bool IsHigh => true;

        protected override long OperateLane(int value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extend_low_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC9)]
    public partial class ExtendLowI32x4I64x2U : V128ExtendInstruction<uint, ulong>
    {
        protected override bool IsHigh => false;

        protected override ulong OperateLane(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extend_high_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xCA)]
    public partial class ExtendHighI32x4I64x2U : V128ExtendInstruction<uint, ulong>
    {
        protected override bool IsHigh => true;

        protected override ulong OperateLane(uint value)
        {
            return value;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extmul_low_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDC)]
    public partial class ExtMulLowI32x4I64x2S : V128ExtMulInstruction<int, long>
    {
        protected override bool IsHigh => false;

        protected override long OperateLane(int lhs, int rhs)
        {
            return (long)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extmul_high_i32x4_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDD)]
    public partial class ExtMulHighI32x4I64x2S : V128ExtMulInstruction<int, long>
    {
        protected override bool IsHigh => true;

        protected override long OperateLane(int lhs, int rhs)
        {
            return (long)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extmul_low_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDE)]
    public partial class ExtMulLowI32x4I64x2U : V128ExtMulInstruction<uint, ulong>
    {
        protected override bool IsHigh => false;

        protected override ulong OperateLane(uint lhs, uint rhs)
        {
            return (ulong)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i64x2.extmul_high_i32x4_u" instruction.
    /// </summary>
    [OpCode(0xFD, 0xDF)]
    public partial class ExtMulHighI32x4I64x2U : V128ExtMulInstruction<uint, ulong>
    {
        protected override bool IsHigh => true;

        protected override ulong OperateLane(uint lhs, uint rhs)
        {
            return (ulong)lhs * rhs;
        }
    }

    /// <summary>
    ///     Represents "i32x4.dot_i16x8_s" instruction.
    /// </summary>
    [OpCode(0xFD, 0xBA)]
    public partial class DotI16x8I32x4S : V128BinaryInstruction
    {
        protected override V128 Operate(in V128 lhs, in V128 rhs)
        {
            var result = default(V128);
            for (var i = 0; i < 4; i++)
            {
                var low = VectorLanes.Get<short>(lhs, i * 2) * VectorLanes.Get<short>(rhs, i * 2);
                var high = VectorLanes.Get<short>(lhs, i * 2 + 1) * VectorLanes.Get<short>(rhs, i * 2 + 1);
                // the sum overflows only when all of the operands are -32768, and then it wraps
                V128.Lane<int>(ref result, i) = unchecked(low + high);
            }

            return result;
        }
    }

    #endregion

    #region Test

    /// <summary>
    ///     Represents "i8x16.all_true" instruction.
    /// </summary>
    [OpCode(0xFD, 0x63)]
    public partial class AllTrueI8x16 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return VectorBits.HasZeroByte(value.Lower) || VectorBits.HasZeroByte(value.Upper) ? 0u : 1u;
        }
    }

    /// <summary>
    ///     Represents "i8x16.bitmask" instruction.
    /// </summary>
    [OpCode(0xFD, 0x64)]
    public partial class BitmaskI8x16 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            var result = 0u;
            for (var i = 0; i < 16; i++) result |= (uint)(VectorLanes.Get<byte>(value, i) >> 7) << i;
            return result;
        }
    }

    /// <summary>
    ///     Represents "i16x8.all_true" instruction.
    /// </summary>
    [OpCode(0xFD, 0x83)]
    public partial class AllTrueI16x8 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return VectorBits.HasZeroUInt16(value.Lower) || VectorBits.HasZeroUInt16(value.Upper) ? 0u : 1u;
        }
    }

    /// <summary>
    ///     Represents "i16x8.bitmask" instruction.
    /// </summary>
    [OpCode(0xFD, 0x84)]
    public partial class BitmaskI16x8 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            var result = 0u;
            for (var i = 0; i < 8; i++) result |= (uint)(VectorLanes.Get<ushort>(value, i) >> 15) << i;
            return result;
        }
    }

    /// <summary>
    ///     Represents "i32x4.all_true" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA3)]
    public partial class AllTrueI32x4 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return VectorLanes.Get<uint>(value, 0) != 0 &&
                   VectorLanes.Get<uint>(value, 1) != 0 &&
                   VectorLanes.Get<uint>(value, 2) != 0 &&
                   VectorLanes.Get<uint>(value, 3) != 0
                ? 1u
                : 0u;
        }
    }

    /// <summary>
    ///     Represents "i32x4.bitmask" instruction.
    /// </summary>
    [OpCode(0xFD, 0xA4)]
    public partial class BitmaskI32x4 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            var result = 0u;
            for (var i = 0; i < 4; i++) result |= (VectorLanes.Get<uint>(value, i) >> 31) << i;
            return result;
        }
    }

    /// <summary>
    ///     Represents "i64x2.all_true" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC3)]
    public partial class AllTrueI64x2 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return value.Lower != 0 && value.Upper != 0 ? 1u : 0u;
        }
    }

    /// <summary>
    ///     Represents "i64x2.bitmask" instruction.
    /// </summary>
    [OpCode(0xFD, 0xC4)]
    public partial class BitmaskI64x2 : V128TestInstruction
    {
        protected override uint Operate(in V128 value)
        {
            return (uint)(value.Lower >> 63) | ((uint)(value.Upper >> 63) << 1);
        }
    }

    #endregion
}
