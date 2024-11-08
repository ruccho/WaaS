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
        private readonly IPullableCore core;

        private IPullableCore Core
        {
            get
            {
                if (core.Version != version) throw new InvalidOperationException();
                return core;
            }
        }

        public STask<T> PullValueAsync<T>()
        {
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

        internal Pullable(IPullableCore core)
        {
            version = core.Version;
            this.core = core;
        }
    }

    internal interface IPullableCore : IVersionedDisposable<ushort>
    {
        STask<T> PullValueAsync<T>();
        STask<T> PullPrimitiveValueAsync<T>();
    }
}