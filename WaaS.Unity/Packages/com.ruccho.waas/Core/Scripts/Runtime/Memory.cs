using System;
using System.Buffers;
using WaaS.Models;

namespace WaaS.Runtime
{
    public class Memory : IDisposable, IImportItem, IExportItem
    {
        public const int PageSizeRank = 16; // 64KiB

        private readonly object locker = new();
        private byte[] buffer;

        public Memory(Limits pageLimits)
        {
            var minPages = checked((int)pageLimits.Min);
            Length = minPages << PageSizeRank;
            NumPages = minPages;
            PageLimits = pageLimits;
            buffer = ArrayPool<byte>.Shared.Rent(Length);
            Array.Clear(buffer, 0, buffer.Length);
        }

        public Limits PageLimits { get; }

        public Span<byte> Span => buffer.AsSpan()[..Length];

        public int Length { get; private set; }
        public int NumPages { get; private set; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DisposeCore();
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
            var afterNumPages = NumPages + numPagesToGrow;
            if (afterNumPages > 1 << 16) return false;
            if (PageLimits.Max.HasValue && afterNumPages > PageLimits.Max) return false;

            NumPages = afterNumPages;
            Length = NumPages << PageSizeRank;

            var prevBuffer = buffer;

            buffer = ArrayPool<byte>.Shared.Rent(Length);

            prevBuffer.CopyTo(buffer, 0);

            Array.Clear(buffer, prevBuffer.Length, buffer.Length - prevBuffer.Length);

            ArrayPool<byte>.Shared.Return(prevBuffer);

            return true;
        }
    }
}