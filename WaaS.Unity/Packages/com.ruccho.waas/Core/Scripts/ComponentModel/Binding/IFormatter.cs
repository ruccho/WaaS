#nullable enable

using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Canonical ABI formatter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFormatter<T>
    {
        STask<T> PullAsync(Pullable adapter);
        void Push(T value, ValuePusher pusher);
    }
}