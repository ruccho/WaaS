#nullable enable

using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public interface IFormatter<T>
    {
        STask<T> PullAsync(Pullable adapter);
        void Push(T value, ValuePusher pusher);
    }
}