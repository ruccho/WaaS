using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents a global instance.
    /// </summary>
    public class GlobalInstance
    {
        internal GlobalInstance(ImportSection importSection, GlobalSection globalSection, IImports importObject)
        {
            // starts import first, next global
            var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;
            var numGlobals = 0;
            foreach (var import in imports)
                if (import.Descriptor.GlobalType.HasValue)
                    numGlobals++;

            numGlobals += globalSection?.Globals.Length ?? 0;


            var globals = new Global[numGlobals];
            var cursor = 0;
            foreach (var import in imports)
            {
                var globalType = import.Descriptor.GlobalType;
                if (!globalType.HasValue) continue;

                if (!importObject.TryGetImportable(import.ModuleName, import.Name, out Global global) ||
                    global.Mutability != globalType.Value.Mutability || global.ValueType != globalType.Value.ValueType)
                    throw new ArgumentException(
                        $"Failed to import global in module: {import.ModuleName}, name: {import.Name}");

                globals[cursor++] = global;
            }

            var internalGlobals = globalSection != null ? globalSection.Globals.Span : Span<Models.Global>.Empty;
            foreach (var internalGlobal in internalGlobals)
            {
                var value = internalGlobal.Expression.Evaluate(globals);

                if (internalGlobal.Type.ValueType != value.valueType) throw new InvalidModuleException("type mismatch");

                globals[cursor++] = internalGlobal.Type.Mutability switch
                {
                    Mutability.Const => new Global(value),
                    Mutability.Var => new GlobalMutable(value),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            Globals = globals;
        }

        public ReadOnlyMemory<Global> Globals { get; }
    }
}