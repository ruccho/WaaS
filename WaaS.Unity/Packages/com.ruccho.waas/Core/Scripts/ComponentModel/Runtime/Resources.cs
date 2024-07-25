#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace WaaS.ComponentModel.Runtime
{
    public interface IOwned : IDisposable
    {
        IResourceType Type { get; }
    }

    public interface IBorrowed
    {
        IResourceType Type { get; }
    }

    internal class OwnedCore
    {
        [ThreadStatic] private static Stack<OwnedCore>? pool;
        private int version;

        public int Version => version;

        public IResourceType Type { get; private set; }
        private uint Value { get; set; }

        public static Owned GetHandle(IResourceType type, uint value)
        {
            pool ??= new Stack<OwnedCore>();
            if (!pool.TryPop(out var popped)) popped = new OwnedCore();

            popped.Type = type;
            popped.Value = value;
            return new Owned(popped);
        }

        public void Drop(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version) return;

            if (version != -2)
            {
                pool ??= new Stack<OwnedCore>();
                pool.Push(this);
            }

            // TODO: call destructor
        }

        public void MoveOut(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version)
                throw new InvalidOperationException();

            if (version != -2)
            {
                pool ??= new Stack<OwnedCore>();
                pool.Push(this);
            }
        }
    }

    public readonly struct Owned : IOwned
    {
        private OwnedCore Core { get; }
        private int Version { get; }

        public IResourceType Type => Core.Type;

        internal Owned(OwnedCore core) : this()
        {
            Core = core;
            Version = core.Version;
        }

        public void Dispose()
        {
            Core.Drop(Version);
        }
    }

    internal class BorrowedCore
    {
        [ThreadStatic] private static Stack<BorrowedCore>? pool;
        private int version;

        public int Version => version;

        public IResourceType Type { get; private set; }
        private uint Value { get; set; }

        public static Borrowed GetHandle(IResourceType type, uint value)
        {
            pool ??= new Stack<BorrowedCore>();
            if (!pool.TryPop(out var popped)) popped = new BorrowedCore();

            popped.Type = type;
            popped.Value = value;
            return new Borrowed(popped);
        }

        public void Expire(int version)
        {
            if (Interlocked.CompareExchange(ref this.version, unchecked(version + 1), version) != version) return;

            if (version != -2)
            {
                pool ??= new Stack<BorrowedCore>();
                pool.Push(this);
            }

            // TODO: call destructor
        }
    }

    public readonly struct Borrowed : IBorrowed
    {
        private BorrowedCore Core { get; }
        private int Version { get; }

        public IResourceType Type => Core.Type;

        internal Borrowed(BorrowedCore core) : this()
        {
            Core = core;
            Version = core.Version;
        }
    }
}