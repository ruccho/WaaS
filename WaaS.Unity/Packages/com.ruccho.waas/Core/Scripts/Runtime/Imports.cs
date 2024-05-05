using System.Collections.Generic;

namespace WaaS.Runtime
{
    public sealed class Imports : Dictionary<string, ModuleImports>
    {
        public bool TryGetValue<T>(string moduleName, string name, out T value) where T : IImportItem
        {
            if (TryGetValue(moduleName, out var moduleImports) &&
                moduleImports.TryGetValue(name, out var valueUntyped) && valueUntyped is T valueTyped)
            {
                value = valueTyped;
                return true;
            }

            value = default;
            return false;
        }
    }

    public sealed class ModuleImports : Dictionary<string, IImportItem>
    {
    }

    public interface IImportItem
    {
    }
}