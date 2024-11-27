#nullable enable

using System.Collections.Generic;
using WaaS.ComponentModel.Models;

namespace WaaS.ComponentModel.Runtime
{
    public interface IInstantiationContext
    {
        IInstance? Instance { get; }
        IInstantiationContext? Parent { get; }
        T Resolve<T>(IUnresolved<T> unresolved) where T : ISorted;
        T ResolveImport<T>(IImport<T> import) where T : ISortedExportable;
        T ResolveExport<T>(IExportDeclarator<T> exportDecl) where T : ISortedExportable;
    }

    internal class InstantiationContext : IInstantiationContext
    {
        private readonly IInstance? exports;
        private readonly IReadOnlyDictionary<string, ISortedExportable>? imports;
        private readonly Dictionary<object, ISorted> resolved = new();
        private readonly HashSet<object> resolving = new();

        public InstantiationContext(IInstance? instance, IInstantiationContext? parent,
            IReadOnlyDictionary<string, ISortedExportable>? imports = null,
            IInstance? exports = null)
        {
            Instance = instance;
            Parent = parent;
            this.imports = imports;
            this.exports = exports;
        }

        public IInstance? Instance { get; }
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

        public T ResolveExport<T>(IExportDeclarator<T> exportDecl) where T : ISortedExportable
        {
            if (exports == null || !exports.TryGetExport<T>(exportDecl.ImportName.Name, out var exported))
                throw new LinkException();
            return exported;
        }
    }
}