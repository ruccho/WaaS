#nullable enable

using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public interface IResourceImpl
    {
        IResourceType Type { get; }
    }

    public readonly struct Owned<T> where T : IResourceImpl
    {
        static Owned()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Owned<T>>
        {
            public async STask<Owned<T>> PullAsync(Pullable adapter)
            {
                return (Owned<T>)await adapter.PullPrimitiveValueAsync<Owned>();
            }

            public void Push(Owned<T> value, ValuePusher pusher)
            {
                pusher.PushOwned(value);
            }
        }

        public readonly Owned handle;

        private Owned(Owned handle)
        {
            this.handle = handle;
        }

        public static implicit operator Owned(Owned<T> owned)
        {
            return owned.handle;
        }

        public static explicit operator Owned<T>(Owned owned)
        {
            return new Owned<T>(owned);
        }
    }

    public readonly struct Borrowed<T> where T : IResourceImpl
    {
        static Borrowed()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Borrowed<T>>
        {
            public async STask<Borrowed<T>> PullAsync(Pullable adapter)
            {
                return (Borrowed<T>)await adapter.PullPrimitiveValueAsync<Borrowed>();
            }

            public void Push(Borrowed<T> value, ValuePusher pusher)
            {
                pusher.PushBorrowed(value);
            }
        }

        public readonly Borrowed handle;

        private Borrowed(Borrowed handle)
        {
            this.handle = handle;
        }

        public static implicit operator Borrowed(Borrowed<T> borrowed)
        {
            return borrowed.handle;
        }

        public static explicit operator Borrowed<T>(Borrowed borrowed)
        {
            return new Borrowed<T>(borrowed);
        }
    }
}