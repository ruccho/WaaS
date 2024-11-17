using System;
using WaaS.Models;

namespace WaaS.Runtime
{
    /// <summary>
    ///     Represents an instance of functions.
    /// </summary>
    public class FunctionInstance
    {
        public FunctionInstance(Instance instance, ImportSection importSection, IImports importObject)
        {
            var moduleFunctions = instance.Module.InternalFunctions.Span;

            var imports = importSection != null ? importSection.Imports.Span : Span<Import>.Empty;

            var numFunctions = moduleFunctions.Length;
            foreach (var import in imports)
            {
                var t = import.Description.TypeIndex;
                if (!t.HasValue) continue;

                numFunctions++;
            }

            var functions = new IInvocableFunction[numFunctions];
            var cursor = 0;
            foreach (var import in imports)
            {
                var t = import.Description.TypeIndex;
                if (!t.HasValue) continue;

                if (!importObject.TryGetImportable(import.ModuleName, import.Name,
                        out IInvocableFunction invocableFunction))
                    throw new InvalidOperationException();

                var type = instance.Module.TypeSection.FuncTypes.Span[checked((int)t.Value)];
                if (!type.Match(invocableFunction.Type))
                    throw new InvalidOperationException(
                        $"Imported function type mismatch: {import.Name} (expected: {type}, actual: {invocableFunction.Type})");

                functions[cursor++] = invocableFunction;
            }

            foreach (var moduleFunction in moduleFunctions)
                functions[cursor++] = new InstanceFunction(instance, moduleFunction);

            Functions = functions;
        }

        public ReadOnlyMemory<IInvocableFunction> Functions { get; }
    }
}