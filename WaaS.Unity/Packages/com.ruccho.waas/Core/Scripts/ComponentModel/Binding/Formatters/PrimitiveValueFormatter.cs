#nullable enable

using System;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public class PrimitiveValueFormatter<T> : IFormatter<T>
    {
        public PrimitiveValueFormatter(IValueType type)
        {
            Type = type;
        }

        public IValueType Type { get; }

        public STask<T> PullAsync(Pullable adapter)
        {
            return adapter.PullPrimitiveValueAsync<T>();
        }

        public void Push(T value, ValuePusher pusher)
        {
            if (value is bool valueBool)
                pusher.Push(valueBool);
            else if (value is byte valueByte)
                pusher.Push(valueByte);
            else if (value is sbyte valueSByte)
                pusher.Push(valueSByte);
            else if (value is ushort valueUShort)
                pusher.Push(valueUShort);
            else if (value is short valueShort)
                pusher.Push(valueShort);
            else if (value is uint valueUInt)
                pusher.Push(valueUInt);
            else if (value is int valueInt)
                pusher.Push(valueInt);
            else if (value is ulong valueULong)
                pusher.Push(valueULong);
            else if (value is long valueLong)
                pusher.Push(valueLong);
            else if (value is float valueFloat)
                pusher.Push(valueFloat);
            else if (value is double valueDouble)
                pusher.Push(valueDouble);
            else if (value is string valueString)
                pusher.Push(valueString);
            else
                throw new InvalidOperationException();
        }
    }
}