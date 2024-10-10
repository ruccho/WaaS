#nullable enable

using System;
using System.Threading.Tasks;

namespace WaaS.ComponentModel.Binding
{
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

        public ValueTask<T> PullValueAsync<T>()
        {
            return Core.PullValueAsync<T>();
        }

        public ValueTask<T> PullPrimitiveValueAsync<T>()
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
        ValueTask<T> PullValueAsync<T>();
        ValueTask<T> PullPrimitiveValueAsync<T>();
    }
}