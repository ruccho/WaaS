using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    // instantiated via Activator.CreateInstance in NullableFormatterProvider
    [Preserve]
    internal class NullableFormatter<T> : IFormatter<T?> where T : struct
    {
        public async STask<T?> PullAsync(Pullable adapter)
        {
            return (await FormatterProvider.GetFormatter<Option<T>>().PullAsync(adapter)).ToNullable();
        }

        public void Push(T? value, ValuePusher pusher)
        {
            FormatterProvider.GetFormatter<Option<T>>().Push(value.ToOption(), pusher);
        }
    }
}