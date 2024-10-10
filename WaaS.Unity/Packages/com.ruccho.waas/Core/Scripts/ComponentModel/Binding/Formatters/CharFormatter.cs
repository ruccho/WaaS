#nullable enable

using System.Threading.Tasks;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

namespace WaaS.ComponentModel.Binding
{
    public class CharFormatter : IFormatter<ComponentChar>
    {
        public IValueType Type { get; } = new PrimitiveValueType { Kind = PrimitiveValueTypeKind.Char };

        public async ValueTask<ComponentChar> PullAsync(Pullable adapter)
        {
            return await adapter.PullPrimitiveValueAsync<uint>();
        }

        public void Push(ComponentChar value, ValuePusher pusher)
        {
            pusher.PushChar32(value);
        }
    }
}