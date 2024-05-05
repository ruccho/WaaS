using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WaaS.Models
{
    internal ref struct ModuleReader
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        private ReadOnlySequence<byte> next;
        private ReadOnlySpan<byte> first;
        private byte[] tempBuffer;

        public int Position { get; private set; }
        public long Available => first.Length + next.Length;

        public ModuleReader(in ReadOnlySequence<byte> buffer)
        {
            first = buffer.FirstSpan;
            next = buffer.Slice(first.Length);
            tempBuffer = null;
            Position = 0;
        }

        public ModuleReader(ReadOnlySpan<byte> buffer)
        {
            next = ReadOnlySequence<byte>.Empty;
            first = buffer;
            tempBuffer = null;
            Position = 0;
        }

        public ref byte ReadRef(int size)
        {
            return ref MemoryMarshal.GetReference(Read(size));
        }

        public ReadOnlySpan<byte> Read(int size)
        {
            Position += size;

            if (size <= first.Length)
            {
                var result = first[..size];
                first = first[size..];

                if (first.IsEmpty)
                {
                    first = next.FirstSpan;
                    next = next.Slice(first.Length);
                }

                return result;
            }

            if (tempBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
                tempBuffer = null;
            }

            tempBuffer = ArrayPool<byte>.Shared.Rent(size);

            var remains = size;
            var dest = tempBuffer.AsSpan();
            while (remains > 0)
            {
                if (first.Length == 0) throw new InvalidModuleException();
                var consume = Math.Min(remains, first.Length);

                first[..consume].CopyTo(dest);
                dest = dest[consume..];
                first = first[consume..];

                if (first.IsEmpty)
                {
                    first = next.FirstSpan;
                    next = next.Slice(first.Length);
                }

                remains -= consume;
            }

            return tempBuffer.AsSpan(0, size);
        }

        public ModuleReader Clone()
        {
            var result = new ModuleReader
            {
                first = first,
                next = next,
                Position = Position
            };

            return result;
        }

        public void Dispose()
        {
            if (tempBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(tempBuffer);
                tempBuffer = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ReadUnaligned<T>()
        {
            var read = Read(Unsafe.SizeOf<T>());

            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(read));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUtf8String()
        {
            var byteLength = ReadVectorSize();

            var bytes = Read(checked((int)byteLength));
            return Utf8.GetString(bytes);
        }

        public uint ReadVectorSize()
        {
            return ReadUnalignedLeb128U32();
        }

        public uint ReadUnalignedLeb128U32()
        {
            const int maxNumSrcBytes = 5; // ceil(32 / 7)

            uint result = default;

            int i;
            for (i = 0;; i++)
            {
                // next
                var source = Read(1)[0];

                var bits = (byte)(source & 0x7F);
                var mask = (uint)bits << (7 * i);
                if (mask < bits) throw new InvalidModuleException(); // there are thrown bits

                result |= mask;

                if (source >> 7 == 0) break; // no continue
                if (i == maxNumSrcBytes - 1) throw new InvalidModuleException();
            }

            return result;
        }

        public ulong ReadUnalignedLeb128U64()
        {
            const int maxNumSrcBytes = 10; // ceil(64 / 7)

            ulong result = default;

            int i;
            for (i = 0;; i++)
            {
                // next
                var source = Read(1)[0];

                var bits = (byte)(source & 0x7F);
                var mask = (ulong)bits << (7 * i);
                if (mask < bits) throw new InvalidModuleException(); // there are thrown bits

                result |= mask;

                if (source >> 7 == 0) break; // no continue
                if (i == maxNumSrcBytes - 1) throw new InvalidOperationException();
            }

            return result;
        }

        public uint ReadUnalignedLeb128S32()
        {
            const int maxNumSrcBytes = 5; // ceil(32 / 7)

            uint result = default;

            int i;
            for (i = 0;; i++)
            {
                // next
                var source = Read(1)[0];

                var bits = (byte)(source & 0x7F);
                var mask = (uint)bits << (7 * i);

                // thrown bit check
                if (i == maxNumSrcBytes - 1)
                {
                    if ((bits & (1 << (sizeof(uint) % 7 - 1))) != 0) // sign
                    {
                        if ((byte)(bits | 0b10001111) is not 0xFF) throw new InvalidModuleException();
                    }
                    else
                    {
                        if ((byte)(bits & 0b01110000) is not 0x00) throw new InvalidModuleException();
                    }
                }

                result |= mask;

                if (source >> 7 == 0) break; // no continue
                if (i == maxNumSrcBytes - 1) throw new InvalidOperationException();
            }

            i++;

            var width = Math.Min(sizeof(uint) * 8, i * 7);

            var sign = (result & (1U << (width - 1))) != 0;

            if (sign) result = ~((uint)(1UL << width) - 1) | result;

            return result;
        }

        public ulong ReadUnalignedLeb128S64()
        {
            const int maxNumSrcBytes = 10; // ceil(64 / 7)

            ulong result = default;

            int i;
            for (i = 0;; i++)
            {
                // next
                var source = Read(1)[0];

                var bits = (byte)(source & 0x7F);
                var mask = (ulong)bits << (7 * i);

                // thrown bit check
                if (i == maxNumSrcBytes - 1)
                {
                    if ((bits & (1 << (sizeof(ulong) % 7 - 1))) != 0) // sign
                    {
                        if ((byte)(bits | 0b10000001) is not 0xFF) throw new InvalidModuleException();
                    }
                    else
                    {
                        if ((byte)(bits & 0b01111110) is not 0x00) throw new InvalidModuleException();
                    }
                }

                result |= mask;

                if (source >> 7 == 0) break; // no continue
                if (i == maxNumSrcBytes - 1) throw new InvalidOperationException();
            }

            i++;

            var width = Math.Min(sizeof(ulong) * 8, i * 7);
            var sign = (result & (1UL << (width - 1))) != 0;

            if (sign)
            {
                var a = width < 64 ? 1UL << width : 0;
                result = unchecked(~((a - 1) & ~result));
            }

            return result;
        }
    }
}