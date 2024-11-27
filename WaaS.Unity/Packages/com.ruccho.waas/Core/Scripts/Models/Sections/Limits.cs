using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Limits for a resizable WebAssembly memory or table.
    /// </summary>
    public readonly struct Limits : IEquatable<Limits>
    {
        public uint Min { get; }
        public uint? Max { get; }

        public bool Equals(Limits other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            return obj is Limits other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Min, Max);
        }

        internal Limits(ref ModuleReader reader, int maxRank)
        {
            var flag = reader.ReadUnaligned<byte>();

            Min = reader.ReadUnalignedLeb128U32();
            if (Min > 1ul << maxRank) throw new InvalidModuleException();

            if (flag == 0)
            {
                Max = null;
            }
            else if (flag == 1)
            {
                Max = reader.ReadUnalignedLeb128U32();
                if (Max.Value > 1ul << maxRank) throw new InvalidModuleException();

                if (Min > Max.Value) throw new InvalidModuleException();
            }
            else
            {
                throw new InvalidModuleException();
            }
        }

        public Limits(uint min, uint? max = null)
        {
            Min = min;
            Max = max;
        }

        public bool IsImportable(Limits importSectionLimits)
        {
            if (Min < importSectionLimits.Min) return false;

            if (importSectionLimits.Max.HasValue &&
                (!Max.HasValue || importSectionLimits.Max < Max)) return false;

            return true;
        }
    }
}