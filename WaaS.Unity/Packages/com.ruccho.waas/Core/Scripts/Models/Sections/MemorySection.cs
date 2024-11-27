using System;

namespace WaaS.Models
{
    /// <summary>
    ///     Memory section in a WebAssembly module.
    /// </summary>
    public class MemorySection : Section
    {
        internal MemorySection(ref ModuleReader reader)
        {
            var numMemTypes = reader.ReadVectorSize();
            var memoryTypes = new MemoryType[numMemTypes];

            for (var i = 0; i < numMemTypes; i++) memoryTypes[i] = new MemoryType(ref reader);
            MemoryTypes = memoryTypes;
        }

        public ReadOnlyMemory<MemoryType> MemoryTypes { get; }
    }

    /// <summary>
    ///     Single memory type entry in a memory section.
    /// </summary>
    public readonly struct MemoryType : IEquatable<MemoryType>
    {
        public Limits Limits { get; }

        internal MemoryType(ref ModuleReader reader)
        {
            Limits = new Limits(ref reader, 16);
        }

        public bool Equals(MemoryType other)
        {
            return Limits.Equals(other.Limits);
        }

        public override bool Equals(object obj)
        {
            return obj is MemoryType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Limits.GetHashCode();
        }

        public static bool operator ==(MemoryType left, MemoryType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MemoryType left, MemoryType right)
        {
            return !left.Equals(right);
        }
    }
}