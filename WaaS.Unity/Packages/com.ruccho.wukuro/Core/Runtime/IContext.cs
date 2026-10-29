using System;
using WaaS.ComponentModel.Binding;
using Wukuro.Core;

namespace Wukuro
{
    public interface IContext
    {
        ITypes.Argument? GetArgument(string name);

        Owned<ITypes.IVariableResourceImpl> GetVariable(string name);
        T GetModule<T>() where T : IWukuroModule;
        HostResourceTypeBase<Action> ActionType { get; }
        HostResourceTypeBase<Object> ObjectType { get; }
    }
}