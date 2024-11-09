#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using STask;
using WaaS.ComponentModel.Binding;

namespace WaaS.ComponentModel.Runtime
{
    internal class Ownership
    {
        [ThreadStatic] private static Stack<Ownership>? pool;
        private int version;

        public int Version => version;

        public IResourceType? Type { get; private set; }
        private uint Value { get; set; }

        public static Owned GetHandle(IResourceType type, uint value)
        {
            pool ??= new Stack<Ownership>();
            if (!pool.TryPop(out var popped)) popped = new Ownership();

            popped.Type = type;
            popped.Value = value;
            return new Owned(popped);
        }

        public void Drop(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version) return;

            Type = null;
            Value = 0;

            if (version != -2)
            {
                pool ??= new Stack<Ownership>();
                pool.Push(this);
            }

            CallDestructor();
        }

        public uint GetValue(int version)
        {
            var value = Value;
            if (this.version != version)
                throw new InvalidOperationException("This resource handle is already dropped.");
            return value;
        }

        public uint MoveOut(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version)
                throw new InvalidOperationException("This resource handle is already dropped.");
            var value = Value;

            Type = null;
            Value = 0;

            if (version != -2)
            {
                pool ??= new Stack<Ownership>();
                pool.Push(this);
            }

            return value;
        }

        public bool TryMoveOut(int version, out uint value)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version)
            {
                value = default;
                return false;
            }

            value = Value;

            Type = null;
            Value = 0;

            if (version != -2)
            {
                pool ??= new Stack<Ownership>();
                pool.Push(this);
            }

            return true;
        }

        ~Ownership()
        {
            if (Type == null) return;
            Interlocked.Increment(ref version);
            CallDestructor();
        }

        private void CallDestructor()
        {
            Type!.Drop(Value);
        }
    }

    public readonly struct Owned : IEquatable<Owned>
    {
        static Owned()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Owned>
        {
            public async STask<Owned> PullAsync(Pullable adapter)
            {
                return await adapter.PullPrimitiveValueAsync<Owned>();
            }

            public void Push(Owned value, ValuePusher pusher)
            {
                pusher.PushOwned(value);
            }
        }

        private Ownership Ownership { get; }
        private int Version { get; }

        public IResourceType Type
        {
            get
            {
                if (Version != Ownership.Version) throw new InvalidOperationException();
                return Ownership.Type ?? throw new InvalidOperationException();
            }
        }

        public uint GetValue(bool moveOut = true)
        {
            return moveOut ? Ownership.MoveOut(Version) : Ownership.GetValue(Version);
        }

        internal Owned(Ownership core) : this()
        {
            Ownership = core;
            Version = core.Version;
        }

        public void Dispose()
        {
            Ownership.Drop(Version);
        }

        public Borrowed Borrow()
        {
            return new Borrowed(this);
        }

        public bool Equals(Owned other)
        {
            return Ownership.Equals(other.Ownership) && Version == other.Version;
        }

        public override bool Equals(object? obj)
        {
            return obj is Owned other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ownership, Version);
        }

        public static bool operator ==(Owned left, Owned right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Owned left, Owned right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct Borrowed : IEquatable<Borrowed>
    {
        static Borrowed()
        {
            FormatterProvider.Register(new Formatter());
        }

        private class Formatter : IFormatter<Borrowed>
        {
            public async STask<Borrowed> PullAsync(Pullable adapter)
            {
                return await adapter.PullPrimitiveValueAsync<Borrowed>();
            }

            public void Push(Borrowed value, ValuePusher pusher)
            {
                pusher.PushBorrowed(value);
            }
        }

        private readonly Owned owned;

        public IResourceType Type => owned.Type;

        public uint GetValue()
        {
            return owned.GetValue(false);
        }

        internal Borrowed(Owned source)
        {
            owned = source;
        }

        public bool Equals(Borrowed other)
        {
            return owned.Equals(other.owned);
        }

        public override bool Equals(object? obj)
        {
            return obj is Borrowed other && Equals(other);
        }

        public override int GetHashCode()
        {
            return owned.GetHashCode();
        }

        public static bool operator ==(Borrowed left, Borrowed right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Borrowed left, Borrowed right)
        {
            return !left.Equals(right);
        }
    }
}