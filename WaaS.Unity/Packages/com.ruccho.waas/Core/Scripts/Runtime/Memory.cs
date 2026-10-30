using System;
using System.Buffers;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents an instance of a linear memory.
    /// </summary>
    public class Memory : IDisposable, IExternal
    {
        public const int PageSizeRank = 16; // 64KiB

        // the maximum length of a byte array (Array.MaxLength in .NET 6 and later)
        private const int MaxArrayLength = 0x7FFFFFC7;

        /// <summary>
        ///     The maximum number of pages that WaaS can allocate, which is limited by the length of arrays.
        /// </summary>
        public const int MaxNumPages = MaxArrayLength >> PageSizeRank;

        private readonly object locker = new();
        private byte[] buffer;

        public Memory(Limits pageLimits)
        {
            if (pageLimits.Min > MaxNumPages)
                throw new NotSupportedException(
                    $"A memory of {pageLimits.Min} pages is larger than {MaxNumPages} pages, which WaaS can allocate.");
            var minPages = (int)pageLimits.Min;
            Length = minPages << PageSizeRank;
            NumPages = minPages;
            PageLimits = pageLimits;
            buffer = ArrayPool<byte>.Shared.Rent(Length);
            Array.Clear(buffer, 0, buffer.Length);
        }

        public Limits PageLimits { get; }
        public Span<byte> Span => buffer.AsSpan()[..Length];

        // the buffer is replaced when the memory grows
        internal byte[] Buffer => buffer;

        public int Length { get; private set; }
        public int NumPages { get; private set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DisposeCore();
        }

        public Memory<byte> AsMemory()
        {
            return buffer.AsMemory()[..Length];
        }

        ~Memory()
        {
            DisposeCore();
        }

        private void DisposeCore()
        {
            lock (locker)
            {
                if (buffer != null)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = null;
                }
            }
        }

        public bool TryGrow(int numPagesToGrow)
        {
            return TryGrow(numPagesToGrow, null);
        }

        /// <param name="maxNumPages">An additional limit on the number of pages imposed by the host.</param>
        public bool TryGrow(int numPagesToGrow, uint? maxNumPages)
        {
            if (numPagesToGrow < 0) throw new ArgumentOutOfRangeException(nameof(numPagesToGrow));
            var afterNumPages = (long)NumPages + numPagesToGrow;
            if (afterNumPages > MaxNumPages) return false;
            if (PageLimits.Max.HasValue && afterNumPages > PageLimits.Max) return false;
            if (maxNumPages.HasValue && afterNumPages > maxNumPages) return false;

            var prevLength = Length;
            NumPages = (int)afterNumPages;
            Length = NumPages << PageSizeRank;

            if (Length <= buffer.Length)
            {
                // the buffer has enough capacity
                Array.Clear(buffer, prevLength, Length - prevLength);
                return true;
            }

            // grow the capacity geometrically to avoid copying the whole memory on every grow
            var capacity = (int)Math.Min(Math.Max(Length, (long)buffer.Length * 2), MaxArrayLength);

            var prevBuffer = buffer;

            buffer = ArrayPool<byte>.Shared.Rent(capacity);

            prevBuffer.AsSpan(0, prevLength).CopyTo(buffer);

            Array.Clear(buffer, prevLength, buffer.Length - prevLength);

            ArrayPool<byte>.Shared.Return(prevBuffer);

            return true;
        }
    }
}