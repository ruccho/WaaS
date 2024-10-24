#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using STask;
using WaaS.ComponentModel.Binding;

namespace WaaS.ComponentModel.Runtime
{
    internal class OwnedCore
    {
        [ThreadStatic] private static Stack<OwnedCore>? pool;
        private int version;

        public int Version => version;

        public IResourceType? Type { get; private set; }
        private uint Value { get; set; }

        public static Owned<T> GetHandle<T>(T type, uint value) where T : class, IResourceType
        {
            pool ??= new Stack<OwnedCore>();
            if (!pool.TryPop(out var popped)) popped = new OwnedCore();

            popped.Type = type;
            popped.Value = value;
            return new Owned<T>(popped);
        }

        public void Drop(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version) return;

            Type = null;
            Value = 0;

            if (version != -2)
            {
                pool ??= new Stack<OwnedCore>();
                pool.Push(this);
            }

            CallDestructor();
        }

        public uint MoveOut(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version)
                throw new InvalidOperationException();
            var value = Value;

            Type = null;
            Value = 0;

            if (version != -2)
            {
                pool ??= new Stack<OwnedCore>();
                pool.Push(this);
            }

            return value;
        }

        ~OwnedCore()
        {
            if (Type == null) return;
            Interlocked.Increment(ref version);
            CallDestructor();
        }

        private void CallDestructor()
        {
            // TODO: call destructor
        }
    }

    public readonly struct Owned<T> where T : class, IResourceType
    {
        static Owned()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Owned<T>>
        {
            public IValueType Type { get; }

            public async STask<Owned<T>> PullAsync(Pullable adapter)
            {
                var borrowed = await adapter.PullPrimitiveValueAsync<Owned<IResourceType>>();
                if (borrowed.Type is not T) throw new InvalidOperationException("Invalid type");
                var typed = Unsafe.As<Owned<IResourceType>, Owned<T>>(ref borrowed);
                return typed;
            }

            public void Push(Owned<T> value, ValuePusher pusher)
            {
                var typed = Unsafe.As<Owned<T>, Owned<IResourceType>>(ref value);
                pusher.PushOwned(typed);
            }
        }

        private OwnedCore Core { get; }
        private int Version { get; }

        public T Type
        {
            get
            {
                if (Version != Core.Version) throw new InvalidOperationException();
                return (T?)Core.Type ?? throw new InvalidOperationException();
            }
        }

        public uint MoveOut()
        {
            return Core.MoveOut(Version);
        }

        internal Owned(OwnedCore core) : this()
        {
            Core = core;
            Version = core.Version;
        }

        public void Dispose()
        {
            Core.Drop(Version);
        }

        public void Borrow()
        {
        }
    }

    internal class BorrowedCore
    {
        [ThreadStatic] private static Stack<BorrowedCore>? pool;
        private int version;

        public int Version => version;

        public IResourceType Type { get; private set; }
        private uint Value { get; set; }

        public static Borrowed<T> GetHandle<T>(IResourceType type, uint value) where T : class, IResourceType
        {
            pool ??= new Stack<BorrowedCore>();
            if (!pool.TryPop(out var popped)) popped = new BorrowedCore();

            popped.Type = type;
            popped.Value = value;
            return new Borrowed<T>(popped);
        }

        public uint MoveOut(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version)
                throw new InvalidOperationException();

            if (version != -2)
            {
                pool ??= new Stack<BorrowedCore>();
                pool.Push(this);
            }

            return Value;
        }
    }

    public readonly struct Borrowed<T> where T : class, IResourceType
    {
        static Borrowed()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Borrowed<T>>
        {
            public IValueType Type { get; }

            public async STask<Borrowed<T>> PullAsync(Pullable adapter)
            {
                var borrowed = await adapter.PullPrimitiveValueAsync<Borrowed<IResourceType>>();
                if (borrowed.Type is not T) throw new InvalidOperationException("Invalid type");
                var typed = Unsafe.As<Borrowed<IResourceType>, Borrowed<T>>(ref borrowed);
                return typed;
            }

            public void Push(Borrowed<T> value, ValuePusher pusher)
            {
                var typed = Unsafe.As<Borrowed<T>, Borrowed<IResourceType>>(ref value);
                pusher.PushBorrowed(typed);
            }
        }

        private readonly Owned<T>? owned;
        private BorrowedCore Core { get; }
        private int Version { get; }

        public T Type
        {
            get
            {
                if (Version != Core.Version) throw new InvalidOperationException();
                return (T?)Core.Type ?? throw new InvalidOperationException();
            }
        }

        public uint MoveOut()
        {
            return Core.MoveOut(Version);
        }

        internal Borrowed(BorrowedCore core) : this()
        {
            Core = core;
            Version = core.Version;
        }
    }
}