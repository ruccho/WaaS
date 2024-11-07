#nullable enable

using System;
using System.Runtime.CompilerServices;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    internal class FlagsFormatter<TEnum> : IFormatter<TEnum> where TEnum : unmanaged, Enum
    {
        public async STask<TEnum> PullAsync(Pullable adapter)
        {
            var value = await adapter.PullPrimitiveValueAsync<uint>();
            return Convert(value);
        }

        public unsafe void Push(TEnum value, ValuePusher pusher)
        {
            switch (sizeof(TEnum))
            {
                case 1:
                {
                    pusher.Push(Unsafe.As<TEnum, byte>(ref value));
                    break;
                }
                case 2:
                {
                    pusher.Push(Unsafe.As<TEnum, ushort>(ref value));
                    break;
                }
                case 4:
                {
                    pusher.Push(Unsafe.As<TEnum, uint>(ref value));
                    break;
                }
                case 8:
                {
                    pusher.Push(Unsafe.As<TEnum, ulong>(ref value));
                    break;
                }
            }
        }

        private static unsafe TEnum Convert(uint value)
        {
            switch (sizeof(TEnum))
            {
                case 1:
                {
                    var a = unchecked((byte)value);
                    return Unsafe.As<byte, TEnum>(ref a);
                }
                case 2:
                {
                    var a = unchecked((ushort)value);
                    return Unsafe.As<ushort, TEnum>(ref a);
                }
                case 4:
                {
                    var a = value;
                    return Unsafe.As<uint, TEnum>(ref a);
                }
                case 8:
                {
                    var a = (ulong)value;
                    return Unsafe.As<ulong, TEnum>(ref a);
                }
            }

            throw new InvalidOperationException();
        }

        private class FlagsType : IFlagsType
        {
            public IDespecializedValueType Despecialize()
            {
                return this;
            }

            public unsafe byte AlignmentRank => sizeof(TEnum) switch
            {
                1 => 0,
                2 => 1,
                4 => 2,
                _ => throw new InvalidOperationException()
            };

            public unsafe ushort ElementSize => sizeof(TEnum) switch
            {
                1 => 1,
                2 => 2,
                4 => 4,
                _ => throw new InvalidOperationException()
            };

            public uint FlattenedCount => 1;

            public void Flatten(Span<ValueType> dest)
            {
                dest[0] = ValueType.I32;
            }

            public ReadOnlyMemory<string> Labels { get; }
        }
    }
}