using System.Collections.Generic;
using Wukuro.Core;

namespace Wukuro
{
    /// <summary>
    ///     Variables of scripts kept in memory. The values are lost when the instance is discarded; implement
    ///     <see cref="IVariables" /> to save them with the save data of the game.
    /// </summary>
    public class MemoryVariables : IVariables
    {
        private readonly Dictionary<string, ITypes.Argument> values = new();

        public IReadOnlyDictionary<string, ITypes.Argument> Values => values;

        public IVariable GetVariable(string name)
        {
            return new Variable(this, name);
        }

        private class Variable : IVariable
        {
            private readonly MemoryVariables owner;
            private readonly string name;

            public Variable(MemoryVariables owner, string name)
            {
                this.owner = owner;
                this.name = name;
            }

            public ITypes.Argument? Get(IContext context)
            {
                return owner.values.TryGetValue(name, out var value) ? value : null;
            }

            public void Set(IContext context, ITypes.Argument value)
            {
                owner.values[name] = value;
            }
        }
    }
}
