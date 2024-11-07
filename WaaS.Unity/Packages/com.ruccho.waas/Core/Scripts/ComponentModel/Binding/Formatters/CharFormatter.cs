#nullable enable

using STask;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    internal class CharFormatter : IFormatter<ComponentChar>
    {
        public async STask<ComponentChar> PullAsync(Pullable adapter)
        {
            return await adapter.PullPrimitiveValueAsync<uint>();
        }

        public void Push(ComponentChar value, ValuePusher pusher)
        {
            pusher.PushChar32(value);
        }
    }
}