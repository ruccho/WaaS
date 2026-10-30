using System;
using System.Collections.Generic;
using WaaS.ComponentModel.Binding;
using Wukuro.Core;
using Object = UnityEngine.Object;

namespace Wukuro
{
    /// <summary>
    ///     Arguments given by the host code at runtime, such as callbacks and values that cannot be serialized.
    ///     The names are the parameter names of the entry point in kebab-case.
    /// </summary>
    public class WukuroRuntimeArguments : IArguments
    {
        private readonly Dictionary<string, Func<IContext, ITypes.Argument>> arguments = new();
        private readonly Dictionary<string, object> values = new();

        /// <summary>
        ///     Adds a value of a record or an enum defined in C# (<see cref="WukuroArgumentTypes" />).
        /// </summary>
        public WukuroRuntimeArguments AddValue<T>(string name, T value)
        {
            if (!WukuroArgumentTypes.IsArgumentType(typeof(T)))
                throw new ArgumentException(
                    $"{typeof(T).Name} is not a record or an enum declared in a component interface.", nameof(value));
            values[name] = value;
            return this;
        }

        public WukuroRuntimeArguments Add(string name, Action action)
        {
            arguments[name] = context =>
                ITypes.Argument.CreateTyAction((Owned<ITypes.IActionResourceImpl>)context.ActionType.Wrap(action));
            return this;
        }

        public WukuroRuntimeArguments Add(string name, Object value)
        {
            arguments[name] = context =>
                ITypes.Argument.CreateTyObject((Owned<ITypes.IObjectResourceImpl>)context.ObjectType.Wrap(value));
            return this;
        }

        public WukuroRuntimeArguments Add(string name, bool value)
        {
            arguments[name] = _ => ITypes.Argument.CreateTyBool(value);
            return this;
        }

        public WukuroRuntimeArguments Add(string name, int value)
        {
            arguments[name] = _ => ITypes.Argument.CreateTyS32(value);
            return this;
        }

        public WukuroRuntimeArguments Add(string name, float value)
        {
            arguments[name] = _ => ITypes.Argument.CreateTyF32(value);
            return this;
        }

        public WukuroRuntimeArguments Add(string name, string value)
        {
            arguments[name] = _ => ITypes.Argument.CreateTyString(value);
            return this;
        }

        public bool TryGetArgument(IContext context, string name, out ITypes.Argument argument)
        {
            if (arguments.TryGetValue(name, out var create))
            {
                argument = create(context);
                return true;
            }

            argument = default;
            return false;
        }

        public bool TryGetValue(string name, out object value)
        {
            return values.TryGetValue(name, out value);
        }

        /// <summary>
        ///     Combines arguments. The former takes precedence over the latter.
        /// </summary>
        internal class Combined : IArguments
        {
            private readonly IArguments first;
            private readonly IArguments second;

            public Combined(IArguments first, IArguments second)
            {
                this.first = first;
                this.second = second;
            }

            public bool TryGetArgument(IContext context, string name, out ITypes.Argument argument)
            {
                return first.TryGetArgument(context, name, out argument) ||
                       second.TryGetArgument(context, name, out argument);
            }

            public bool TryGetValue(string name, out object value)
            {
                return first.TryGetValue(name, out value) || second.TryGetValue(name, out value);
            }
        }
    }
}
