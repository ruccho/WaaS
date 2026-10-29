using System.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    internal class ObjectImpl : HostResourceTypeBase<object>, ITypes.IObjectResourceImpl
    {
        public static readonly ObjectImpl Instance = new();
        public IResourceType Type => this;

        public ValueTask<Owned<ITypes.IObjectResourceImpl>> Clone(Borrowed<ITypes.IObjectResourceImpl> self)
        {
            return new ValueTask<Owned<ITypes.IObjectResourceImpl>>(
                (Owned<ITypes.IObjectResourceImpl>)Wrap(Unwrap(self)));
        }
    }
}