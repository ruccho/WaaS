#nullable enable

namespace WaaS
{
    public interface IVersionedDisposable<T>
    {
        T Version { get; }
        void Dispose(T version);
    }
}