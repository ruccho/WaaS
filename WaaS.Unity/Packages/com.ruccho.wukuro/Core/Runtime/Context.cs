using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WaaS.ComponentModel.Binding;
using WaaS.ComponentModel.Runtime;
using Wukuro.Core;

namespace Wukuro
{
    public interface IVariables
    {
        IVariable GetVariable(string name);
    }

    internal class Context : IContext
    {
        private readonly IArguments _arguments;
        private readonly IVariables _variables;
        private readonly List<IWukuroModule> _modules;

        public HostResourceTypeBase<Action> ActionType => ActionImpl.Instance;
        public HostResourceTypeBase<object> ObjectType => ObjectImpl.Instance;

        public Context(
            IArguments arguments,
            IVariables variables,
            IEnumerable<IWukuroModule> modules,
            bool isFirstTime)
        {
            _arguments = arguments;
            _variables = variables;
            _modules = modules.ToList();
            IsFirstTime = isFirstTime;
        }

        public bool IsFirstTime { get; }

        public ITypes.Argument? GetArgument(string name)
        {
            if (_arguments.TryGetArgument(this, name, out var argument))
            {
                return argument;
            }

            return null;
        }

        public Owned<ITypes.IVariableResourceImpl> GetVariable(string name)
        {
            return (Owned<ITypes.IVariableResourceImpl>)VariableImpl.Instance.Wrap(
                new VariableWithContext(this, _variables.GetVariable(name)));
        }

        public T GetModule<T>() where T : IWukuroModule
        {
            // modules are typically requested by interface type, so look up by assignability
            foreach (var module in _modules)
                if (module is T typed)
                    return typed;

            throw new KeyNotFoundException($"Module {typeof(T)} is not registered.");
        }
    }

    internal class ContextImpl : HostResourceTypeBase<IContext>, ITypes.IContextResourceImpl
    {
        public static readonly ContextImpl Instance = new();
        public IResourceType Type => this;

        public async ValueTask<ITypes.Argument?> GetArgument(Borrowed<ITypes.IContextResourceImpl> self,
            string name)
        {
            return Unwrap(self).GetArgument(name);
        }

        public async ValueTask<Owned<ITypes.IVariableResourceImpl>> GetVariable(
            Borrowed<ITypes.IContextResourceImpl> self, string name)
        {
            return Unwrap(self).GetVariable(name);
        }
    }

    public interface IVariable
    {
        ITypes.Argument? Get(IContext context);
        void Set(IContext context, ITypes.Argument value);
    }

    internal class VariableWithContext
    {
        public IContext Context { get; }
        public IVariable Variable { get; }

        public VariableWithContext(IContext context, IVariable variable)
        {
            Context = context;
            Variable = variable;
        }
    }

    internal class VariableImpl : HostResourceTypeBase<VariableWithContext>, ITypes.IVariableResourceImpl
    {
        public static readonly VariableImpl Instance = new();
        public IResourceType Type => this;

        public ValueTask<ITypes.Argument?> Get(Borrowed<ITypes.IVariableResourceImpl> self)
        {
            var v = Unwrap(self);
            return new ValueTask<ITypes.Argument?>(v.Variable.Get(v.Context));
        }

        public ValueTask Set(Borrowed<ITypes.IVariableResourceImpl> self, ITypes.Argument value)
        {
            var v = Unwrap(self);
            v.Variable.Set(v.Context, value);
            return default;
        }
    }
}