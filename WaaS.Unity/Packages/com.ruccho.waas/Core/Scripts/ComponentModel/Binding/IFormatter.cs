#nullable enable

using System.Threading.Tasks;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public interface IFormatter<T>
    {
        IValueType Type { get; }
        ValueTask<T> PullAsync(Pullable adapter);
        void Push(T value, ValuePusher pusher);
    }
}