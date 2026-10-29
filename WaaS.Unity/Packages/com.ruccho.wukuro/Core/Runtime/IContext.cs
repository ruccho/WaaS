using System;
using WaaS.ComponentModel.Binding;
using Wukuro.Core;

namespace Wukuro
{
    public interface IContext
    {
        ITypes.Argument? GetArgument(string name);

        /// <summary>
        ///     Whether the entry point is run for the first time, given by the caller of the script.
        /// </summary>
        bool IsFirstTime { get; }

        Owned<ITypes.IVariableResourceImpl> GetVariable(string name);
        T GetModule<T>() where T : IWukuroModule;
        HostResourceTypeBase<Action> ActionType { get; }
        HostResourceTypeBase<Object> ObjectType { get; }
    }
}