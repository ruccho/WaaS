using Wukuro.Core;

namespace Wukuro
{
    public interface IArguments
    {
        bool TryGetArgument(IContext context, string name, out ITypes.Argument argument);
    }
}