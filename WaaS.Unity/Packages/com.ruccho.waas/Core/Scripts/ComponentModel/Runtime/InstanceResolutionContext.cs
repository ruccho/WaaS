#nullable enable

using System.Collections.Generic;
using WaaS.ComponentModel.Models;

namespace WaaS.ComponentModel.Runtime
{
    public interface IInstantiationContext
    {
        IInstantiationContext? Parent { get; }
        T Resolve<T>(IUnresolved<T> unresolved) where T : ISorted;
        T ResolveImport<T>(IImport<T> import) where T : ISortedExportable;
    }

    internal class InstantiationContext : IInstantiationContext
    {
        private readonly IReadOnlyDictionary<string, ISortedExportable>? imports;
        private readonly Dictionary<object, ISorted> resolved = new();
        private readonly HashSet<object> resolving = new();

        public InstantiationContext(IReadOnlyDictionary<string, ISortedExportable>? imports,
            IInstantiationContext? parent)
        {
            this.imports = imports;
            Parent = parent;
        }

        public IInstantiationContext? Parent { get; }

        public T Resolve<T>(IUnresolved<T> unresolved) where T : ISorted
        {
            if (resolved.TryGetValue(unresolved, out var cached))
            {
                if (cached is not T cachedTyped) throw new LinkException();
                return cachedTyped;
            }

            if (!resolving.Add(unresolved)) throw new LinkException(); // circular resolution
            var result = unresolved.ResolveFirstTime(this);
            resolved.Add(unresolved, result);
            resolving.Remove(unresolved);

            return result;
        }

        public T ResolveImport<T>(IImport<T> import) where T : ISortedExportable
        {
            if (imports == null || !imports.TryGetValue(import.Name.Name, out var imported)) throw new LinkException();

            if (imported is not T importedTyped) throw new LinkException();

            return importedTyped;
        }
    }
}