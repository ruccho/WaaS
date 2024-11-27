using System;
using System.Runtime.InteropServices;

namespace WaaS.Models
{
    /// <summary>
    ///     Represents the preamble of a WebAssembly module.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Preamble : IEquatable<Preamble>
    {
        public const uint DefinedMagic = 0x6D736100;
        public const uint DefinedVersion = 1;

        public readonly uint magic;
        public readonly uint version;

        public bool IsValid()
        {
            return magic == DefinedMagic && version == DefinedVersion;
        }

        public bool Equals(Preamble other)
        {
            return magic == other.magic && version == other.version;
        }

        public override bool Equals(object obj)
        {
            return obj is Preamble other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(magic, version);
        }

        public static bool operator ==(Preamble left, Preamble right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Preamble left, Preamble right)
        {
            return !left.Equals(right);
        }
    }
}