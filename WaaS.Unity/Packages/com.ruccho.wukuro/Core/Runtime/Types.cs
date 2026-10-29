using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    internal class Types : ITypes
    {
        public static readonly IInstance Instance = ITypes.CreateWaaSInstance(new Types());
        public ITypes.IContextResourceImpl Context => ContextImpl.Instance;
        public ITypes.IVariableResourceImpl Variable => VariableImpl.Instance;
        public ITypes.IActionResourceImpl Action => ActionImpl.Instance;
        public ITypes.ITaskResourceImpl Task => WukuroTasks.ResourceType;
        public ITypes.IObjectResourceImpl Object => ObjectImpl.Instance;
    }
}