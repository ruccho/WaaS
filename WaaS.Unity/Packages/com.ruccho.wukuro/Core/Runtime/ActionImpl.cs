using System;
using System.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    internal class ActionImpl : HostResourceTypeBase<Action>, ITypes.IActionResourceImpl
    {
        public static readonly ActionImpl Instance = new();
        public IResourceType Type => this;

        public ValueTask Run(Borrowed<ITypes.IActionResourceImpl> self)
        {
            Unwrap(self).Invoke();
            return default;
        }

        public ValueTask<Owned<ITypes.IActionResourceImpl>> Clone(Borrowed<ITypes.IActionResourceImpl> self)
        {
            return new ValueTask<Owned<ITypes.IActionResourceImpl>>(
                (Owned<ITypes.IActionResourceImpl>)Wrap(Unwrap(self)));
        }
    }
}