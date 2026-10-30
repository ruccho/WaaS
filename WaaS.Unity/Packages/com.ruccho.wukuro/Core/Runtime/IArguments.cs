using Wukuro.Core;

namespace Wukuro
{
    public interface IArguments
    {
        bool TryGetArgument(IContext context, string name, out ITypes.Argument argument);

        /// <summary>
        ///     Gets the value of a parameter whose type is a record or an enum defined in C#
        ///     (<see cref="WukuroArgumentTypes" />).
        /// </summary>
        bool TryGetValue(string name, out object value)
        {
            value = null;
            return false;
        }
    }
}
