using System.Runtime.CompilerServices;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Bit operations for the targets without System.Numerics.BitOperations.
    /// </summary>
    internal static class BitUtils
    {
        // a de Bruijn sequence: the top 6 bits of its product with a power of two are unique
        private const ulong DeBruijn = 0x03F79D71B4CB0A89;

        private static readonly byte[] TrailingZeroCounts = CreateTrailingZeroCounts();

        private static byte[] CreateTrailingZeroCounts()
        {
            var table = new byte[64];
            for (var i = 0; i < 64; i++) table[unchecked(DeBruijn << i) >> 58] = (byte)i;
            return table;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong TrailingZeroCount(ulong value)
        {
            if (value == 0) return 64;
            return TrailingZeroCounts[unchecked((value & (0 - value)) * DeBruijn) >> 58];
        }
    }
}
