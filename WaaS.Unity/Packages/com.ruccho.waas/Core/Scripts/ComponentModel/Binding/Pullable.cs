#nullable enable

using System;
using STask;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Pullable interface for Canonical ABI.
    /// </summary>
    public struct Pullable : IDisposable
    {
        private readonly ushort version;
        // the concrete type avoids the dispatch of generic virtual methods, which is slow on some runtimes
        private readonly PushPullAdapter core;

        private PushPullAdapter Core
        {
            get
            {
                if (core.Version != version) throw new InvalidOperationException();
                return core;
            }
        }

        public STask<T> PullValueAsync<T>()
        {
            // None has no values to pull, and the adapter of a payload without values is already disposed
            // (such as the payload of a case of Result<TOk, TError> typed with None)
            if (typeof(T) == typeof(None)) return new STask<T>(default(T)!);
            return Core.PullValueAsync<T>();
        }

        public STask<T> PullPrimitiveValueAsync<T>()
        {
            return Core.PullPrimitiveValueAsync<T>();
        }

        public void Dispose()
        {
            core.Dispose(version);
        }

        internal Pullable(PushPullAdapter core) : this(core, core.Version)
        {
        }

        internal Pullable(PushPullAdapter core, ushort version)
        {
            this.version = version;
            this.core = core;
        }
    }

    internal interface IPullableCore : IVersionedDisposable<ushort>
    {
        STask<T> PullValueAsync<T>();
        STask<T> PullPrimitiveValueAsync<T>();
    }
}