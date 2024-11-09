#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WaaS.Runtime
{
    /// <summary>
    ///     A set of "module exports" that can be imported by a WebAssembly module.
    /// </summary>
    public interface IImports
    {
        bool TryGetImportable<T>(string moduleName, string name, [NotNullWhen(true)] out T? value) where T : IExternal;
    }

    /// <summary>
    ///     A set of "module exports" that can be imported by a WebAssembly module.
    /// </summary>
    public sealed class Imports : Dictionary<string, IModuleExports>, IImports
    {
        public bool TryGetImportable<T>(string moduleName, string name, [NotNullWhen(true)] out T? value)
            where T : IExternal
        {
            if (TryGetValue(moduleName, out var moduleImports) &&
                moduleImports.TryGetExport(name, out T? valueTyped))
            {
                value = valueTyped;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    ///     A set of items that can be exported by a WebAssembly module.
    /// </summary>
    public interface IModuleExports
    {
        bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? value) where T : IExternal;
    }

    /// <summary>
    ///     A set of items that can be exported by a WebAssembly module.
    /// </summary>
    public sealed class ModuleExports : Dictionary<string, IExternal>, IModuleExports
    {
        bool IModuleExports.TryGetExport<T>(string name, [NotNullWhen(true)] out T? value) where T : default
        {
            if (TryGetValue(name, out var item) && item is T itemTyped)
            {
                value = itemTyped;
                return true;
            }

            value = default;
            return false;
        }
    }
}