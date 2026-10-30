using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WaaS
{
    /// <summary>
    ///     Represents a 128-bit vector value ("v128").
    ///     Lanes are laid out in little-endian order: lane 0 occupies the lowest bytes.
    /// </summary>
    /// <remarks>
    ///     The lane accessors reinterpret the underlying bytes, so they assume a little-endian host.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct V128 : IEquatable<V128>
    {
        /// <summary>
        ///     Lower 64 bits (bytes 0-7).
        /// </summary>
        public ulong Lower;

        /// <summary>
        ///     Upper 64 bits (bytes 8-15).
        /// </summary>
        public ulong Upper;

        public V128(ulong lower, ulong upper)
        {
            Lower = lower;
            Upper = upper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref T Lane<T>(ref V128 vector, int index) where T : unmanaged
        {
            return ref Unsafe.Add(ref Unsafe.As<V128, T>(ref vector), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T GetLane<T>(int index) where T : unmanaged
        {
            if ((uint)index >= 16 / Unsafe.SizeOf<T>()) throw new ArgumentOutOfRangeException(nameof(index));
            var copy = this;
            return Lane<T>(ref copy, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLane<T>(int index, T value) where T : unmanaged
        {
            if ((uint)index >= 16 / Unsafe.SizeOf<T>()) throw new ArgumentOutOfRangeException(nameof(index));
            Lane<T>(ref this, index) = value;
        }

        public static V128 Create(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 16) throw new ArgumentException("16 bytes are required.", nameof(bytes));
            return Unsafe.ReadUnaligned<V128>(ref MemoryMarshal.GetReference(bytes));
        }

        public readonly void CopyTo(Span<byte> destination)
        {
            if (destination.Length < 16) throw new ArgumentException("16 bytes are required.", nameof(destination));
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
        }

        public readonly bool Equals(V128 other)
        {
            return Lower == other.Lower && Upper == other.Upper;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is V128 other && Equals(other);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Lower, Upper);
        }

        public static bool operator ==(V128 left, V128 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(V128 left, V128 right)
        {
            return !left.Equals(right);
        }

        public override readonly string ToString()
        {
            return $"0x{Upper:X16}{Lower:X16}";
        }
    }
}
