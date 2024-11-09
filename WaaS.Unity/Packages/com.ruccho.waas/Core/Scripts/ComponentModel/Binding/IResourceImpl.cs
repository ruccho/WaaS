#nullable enable

using System;
using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Represents the implementation of a resource.
    /// </summary>
    public interface IResourceImpl
    {
        IResourceType Type { get; }
    }

    /// <summary>
    ///     Owned resource handle.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Owned<T> : IEquatable<Owned<T>> where T : IResourceImpl
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

        public bool Equals(Owned<T> other)
        {
            return handle.Equals(other.handle);
        }

        public override bool Equals(object? obj)
        {
            return obj is Owned<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }

        public static bool operator ==(Owned<T> left, Owned<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Owned<T> left, Owned<T> right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    ///     Borrowed resource handle.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Borrowed<T> : IEquatable<Borrowed<T>> where T : IResourceImpl
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

        public bool Equals(Borrowed<T> other)
        {
            return handle.Equals(other.handle);
        }

        public override bool Equals(object? obj)
        {
            return obj is Borrowed<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }

        public static bool operator ==(Borrowed<T> left, Borrowed<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Borrowed<T> left, Borrowed<T> right)
        {
            return !left.Equals(right);
        }
    }
}