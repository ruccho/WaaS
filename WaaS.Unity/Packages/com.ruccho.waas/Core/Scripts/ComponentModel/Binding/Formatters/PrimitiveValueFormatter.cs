#nullable enable

using System;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public class PrimitiveValueFormatter<T> : IFormatter<T>

    {
        private readonly Action<T, ValuePusher> push;

        public PrimitiveValueFormatter(IValueType type, Action<T, ValuePusher> push)
        {
            this.push = push;
        }

        public STask<T> PullAsync(Pullable adapter)
        {
            return adapter.PullPrimitiveValueAsync<T>();
        }

        public void Push(T value, ValuePusher pusher)
        {
            push(value, pusher);
        }
    }
}