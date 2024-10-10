#nullable enable

namespace WaaS.ComponentModel
{
    public interface IVersionedDisposable<T>
    {
        T Version { get; }
        void Dispose(T version);
    }
}