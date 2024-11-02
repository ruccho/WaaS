#nullable enable

using System;
using System.Runtime.CompilerServices;
using STask;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    internal class EnumFormatter<TEnum> : IFormatter<TEnum> where TEnum : unmanaged, Enum
    {
        private readonly uint[] values;

        public EnumFormatter()
        {
            // TODO: implement without reflection
            var names = Enum.GetNames(typeof(TEnum));
            var values = Enum.GetValues(typeof(TEnum));
            this.values = new uint[values.Length];
            for (var i = 0; i < values.Length; i++) this.values[i] = Convert((TEnum)values.GetValue(i)!);
            Type = EnumType.Create(names);
        }

        public IValueType? Type { get; }

        public async STask<TEnum> PullAsync(Pullable adapter)
        {
            var prelude = await adapter.PullPrimitiveValueAsync<VariantPrelude>();
            var value = values[prelude.CaseIndex];
            return Convert(value);
        }

        public void Push(TEnum value, ValuePusher pusher)
        {
            var index = Array.IndexOf(values, Convert(value));
            if (index == -1) throw new ArgumentException(nameof(value));

            pusher.PushVariant(index).Dispose();
        }

        private static unsafe uint Convert(TEnum value)
        {
            return sizeof(TEnum) switch
            {
                1 => Unsafe.As<TEnum, byte>(ref value),
                2 => Unsafe.As<TEnum, byte>(ref value),
                4 => Unsafe.As<TEnum, byte>(ref value),
                _ => throw new ArgumentException(nameof(value))
            };
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
            }

            throw new InvalidOperationException();
        }
    }
}