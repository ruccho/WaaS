using System;

namespace WaaS.Models
{
    public readonly struct Local : IEquatable<Local>
    {
        public uint Count { get; }
        public ValueType Type { get; }

        internal Local(ref ModuleReader reader)
        {
            Count = reader.ReadUnalignedLeb128U32();
            Type = reader.ReadUnaligned<ValueType>();
        }

        public bool Equals(Local other)
        {
            return Count == other.Count && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is Local other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Count, (int)Type);
        }
    }
}