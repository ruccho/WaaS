#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Models
{
    /// <summary>
    ///     Represents a WebAssembly component.
    /// </summary>
    public class Component : IUnresolved<IComponent>
    {
        private readonly List<CustomSection> customSections = new();
        private readonly List<IExport<ISortedExportable>> exports = new();

        private readonly List<IImport<ISortedExportable>> imports = new();
        private readonly List<IUnresolved<ISorted>> instantiations = new();

        internal Component(ref ModuleReader reader, long? size = null, IIndexSpace? parentIndexSpace = null)
        {
            long rest = 0;
            if (size.HasValue)
                checked
                {
                    rest = reader.Available - size.Value;
                }

            if (rest < 0) throw new ArgumentException(nameof(size));

            var preamble = reader.ReadUnaligned<Preamble>();
            if (preamble.magic != 0x6D736100) throw new InvalidModuleException();
            if (preamble.version != 0x0001000d) throw new InvalidModuleException();

            var indexSpace = new IndexSpace(parentIndexSpace);

            while (reader.Available > rest)
            {
                var sectionId = reader.ReadUnaligned<SectionId>();
                var sectionSize = reader.ReadUnalignedLeb128U32();

                // Console.WriteLine(sectionId.ToString());

                var next = reader.Position + sectionSize;

                switch (sectionId)
                {
                    case SectionId.CoreCustom:
                    {
                        customSections.Add(new CustomSection(ref reader, sectionSize));
                        break;
                    }
                    case SectionId.CoreModule:
                    {
                        var module = new CoreModule(ref reader, sectionSize);
                        indexSpace.Add(module, out var index);
                        module.Module.SourceDescription = $"core module (;{index};)";
                        break;
                    }
                    case SectionId.CoreInstance:
                    {
                        instantiations.AddRange(reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<ICoreInstantiation>.Read(ref reader, indexSpace), indexSpace));
                        break;
                    }
                    case SectionId.CoreType:
                    {
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<ICoreTypeDefinition>.Read(ref reader, indexSpace), indexSpace);
                        break;
                    }
                    case SectionId.Component:
                    {
                        indexSpace.Add(new Component(ref reader, sectionSize, indexSpace));
                        break;
                    }
                    case SectionId.Instance:
                    {
                        instantiations.AddRange(reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<IInstantiation>.Read(ref reader, indexSpace), indexSpace));
                        break;
                    }
                    case SectionId.Alias:
                    {
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<Alias>.Read(ref reader, indexSpace), indexSpace);
                        break;
                    }
                    case SectionId.Type:
                    {
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<ITypeDefinition>.Read(ref reader, indexSpace), indexSpace);
                        break;
                    }
                    case SectionId.Canon:
                    {
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<ICanon>.Read(ref reader, indexSpace), indexSpace);
                        break;
                    }
                    case SectionId.Start:
                        throw new NotImplementedException();
                    case SectionId.Import:
                    {
                        imports.AddRange(reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<IImport<ISortedExportable>>.Read(ref reader, indexSpace), indexSpace));
                        break;
                    }
                    case SectionId.Export:
                    {
                        exports.AddRange(reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<IExport<ISortedExportable>>.Read(ref reader, indexSpace), indexSpace));
                        break;
                    }
                    case SectionId.Value:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (reader.Position != next) throw new InvalidModuleException("section size mismatch");
            }
        }

        public IComponent ResolveFirstTime(IInstantiationContext context)
        {
            return new CapturedComponent(this, context);
        }

        private class CapturedComponent : IComponent
        {
            private readonly Component source;
            private readonly IInstantiationContext? outerContext;

            public CapturedComponent(Component source, IInstantiationContext? outerContext)
            {
                this.source = source;
                this.outerContext = outerContext;
            }

            public IInstance Instantiate(IReadOnlyDictionary<string, ISortedExportable> arguments)
            {
                // validate
                var newContext = new InstantiationContext(arguments, outerContext);

                List<IImport<ISortedExportable>>? missingImports = null;
                foreach (var import in source.imports)
                {
                    arguments.TryGetValue(import.Name.Name, out var argument);
                    if (!import.Descriptor.ValidateArgument(newContext, argument))
                    {
                        missingImports ??= new List<IImport<ISortedExportable>>();
                        missingImports.Add(import);
                    }
                }

                if (missingImports != null)
                    throw new LinkException(
                        $"The import is missing or invalid: \n{string.Join("\n", missingImports.Select(i => i.Name.Name))}");

                Dictionary<string, ISortedExportable> resolvedExports = new();
                foreach (var export in source.exports) resolvedExports.Add(export.Name.Name, newContext.Resolve(export.Target));

                // instantiations need to be resolved (unexposed instances may initialize other instance's state)
                for (var i = 0; i < source.instantiations.Count; i++)
                {
                    var instantiation = source.instantiations[i];
                    newContext.Resolve(instantiation);
                }

                return new Instance(resolvedExports);
            }
            
        }

        /// <summary>
        ///     Creates a new component from the given buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static IComponent Create(ReadOnlySpan<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new CapturedComponent(new Component(ref reader), null);
        }

        /// <summary>
        ///     Creates a new component from the given buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static IComponent Create(ReadOnlySequence<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new CapturedComponent(new Component(ref reader), null);
        }

        private class Instance : IInstance
        {
            private readonly Dictionary<string, ISortedExportable> exports;

            public Instance(Dictionary<string, ISortedExportable> exports)
            {
                this.exports = exports;
            }

            public bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? result) where T : ISortedExportable
            {
                if (!exports.TryGetValue(name, out var export) || export is not T typed)
                {
                    result = default;
                    return false;
                }

                result = typed;
                return true;
            }
        }
    }

    internal class CoreModule : IUnresolved<ICoreModule>
    {
        public CoreModule(ref ModuleReader reader, long? moduleSize)
        {
            Module = new Module(ref reader, moduleSize);
        }

        public Module Module { get; }

        public ICoreModule ResolveFirstTime(IInstantiationContext context)
        {
            return new CapturedModule(Module, context);
        }

        private class CapturedModule : ICoreModule
        {
            private readonly Module module;
            private readonly IInstantiationContext outerContext;

            public CapturedModule(Module module, IInstantiationContext outerContext)
            {
                this.module = module;
                this.outerContext = outerContext;
            }

            public ICoreInstance Instantiate(IReadOnlyDictionary<string, ICoreInstance> imports)
            {
                Imports moduleImports = new();
                foreach (var (key, value) in imports) moduleImports.Add(key, value.CoreExports);

                return new CoreInstance(new Instance(module, moduleImports));
            }

            public bool Validate(IInstantiationContext context, ICoreModuleType coreModuleType)
            {
            var importSection = module.ImportSection;

            foreach (var decl in coreModuleType.Declarations.Span)
            {
                if (decl is not CoreExportDeclaration exportDecl) continue;

                Export exported;
                {
                    foreach (var export in module.ExportSection.Exports.Span)
                        if (export.Name == exportDecl.Name)
                        {
                            exported = export;
                            goto FOUND;
                        }

                    return false;
                    FOUND: ;
                }

                var providedDesc = exported.Descriptor;

                switch (exportDecl.Kind)
                {
                    case ImportKind.Type:
                    {
                        var index = 0;
                        var descModuleFuncIndex = providedDesc.FunctionIndex ?? throw new InvalidOperationException();
                        WaaS.Models.FunctionType functionType;
                        if (importSection != null)
                            foreach (var import in importSection.Imports.Span)
                            {
                                var desc = import.Description;
                                if (desc.Kind == ImportKind.Type)
                                    if (index++ == descModuleFuncIndex)
                                    {
                                        functionType =
                                            module.TypeSection.FuncTypes.Span[checked((int)desc.TypeIndex!.Value)];
                                        goto FOUND;
                                    }
                            }

                        var functions = module.InternalFunctions.Span;
                        index = checked((int)providedDesc.FunctionIndex!.Value) - index;
                        if (index >= functions.Length) return false;
                        functionType = functions[index].Type;
                        FOUND: ;

                        if (!functionType.Match(exportDecl.FunctionType!.Value.Type)) return false;
                        break;
                    }
                    case ImportKind.Table:
                    {
                        var requestedTableType = exportDecl.Descriptor.TableType!.Value;
                        if (providedDesc.Kind != ExportKind.Table) return false;
                        var index = 0;
                        TableType providedTableType;
                        if (importSection != null)
                            foreach (var import in importSection.Imports.Span)
                            {
                                var desc = import.Description;
                                if (desc.Kind == ImportKind.Table)
                                    if (index++ == providedDesc.TableIndex)
                                    {
                                        providedTableType = import.Description.TableType!.Value;
                                        goto FOUND;
                                    }
                            }

                        var tables = module.TableSection.TableTypes.Span;
                        index = checked((int)providedDesc.TableIndex!.Value) - index;
                        if (index >= tables.Length) return false;
                        providedTableType = tables[index];

                        FOUND: ;

                        if (requestedTableType.ElementType != providedTableType.ElementType) return false;

                        // allow subtyping
                        if (requestedTableType.Limits.Min > providedTableType.Limits.Min) return false;
                        break;
                    }
                    case ImportKind.Memory:
                    {
                        var requestedType = exportDecl.Descriptor.MemoryType!.Value;
                        if (providedDesc.Kind != ExportKind.Memory) return false;
                        var index = 0;
                        MemoryType providedType;
                        if (importSection != null)
                            foreach (var import in importSection.Imports.Span)
                            {
                                var desc = import.Description;
                                if (desc.Kind == ImportKind.Memory)
                                    if (index++ == providedDesc.MemoryIndex)
                                    {
                                        providedType = import.Description.MemoryType!.Value;
                                        goto FOUND;
                                    }
                            }

                        var memories = module.MemorySection.MemoryTypes.Span;
                        index = checked((int)providedDesc.MemoryIndex!.Value) - index;
                        if (index >= memories.Length) return false;
                        providedType = memories[index];

                        FOUND: ;

                        // allow subtyping
                        if (requestedType.Limits.Min > providedType.Limits.Min) return false;
                        break;
                    }
                    case ImportKind.Global:
                    {
                        var requestedType = exportDecl.Descriptor.GlobalType!.Value;
                        if (providedDesc.Kind != ExportKind.Global) return false;
                        var index = 0;
                        GlobalType providedType;
                        if (importSection != null)
                            foreach (var import in importSection.Imports.Span)
                            {
                                var desc = import.Description;
                                if (desc.Kind == ImportKind.Global)
                                    if (index++ == providedDesc.GlobalIndex)
                                    {
                                        providedType = import.Description.GlobalType!.Value;
                                        goto FOUND;
                                    }
                            }

                        var globals = module.GlobalSection.Globals.Span;
                        index = checked((int)providedDesc.GlobalIndex!.Value) - index;
                        if (index >= globals.Length) return false;
                        providedType = globals[index].Type;

                        FOUND: ;
                        if (requestedType != providedType) return false;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (importSection != null)
                foreach (var import in module.ImportSection.Imports.Span)
                {
                    CoreImportDeclaration importDecl;
                    {
                        foreach (var decl in coreModuleType.Declarations.Span)
                        {
                            if (decl is not CoreImportDeclaration coreImportDecl) continue;

                            if (coreImportDecl.ModuleName == import.ModuleName &&
                                coreImportDecl.Name == import.Name &&
                                coreImportDecl.Kind == import.Description.Kind)
                            {
                                importDecl = coreImportDecl;
                                goto FOUND;
                            }
                        }

                        return false;

                        FOUND: ;
                    }

                    var requestedImport = import.Description;
                    switch (requestedImport.Kind)
                    {
                        case ImportKind.Type:
                        {
                            var t = importDecl.Type ?? throw new InvalidOperationException();
                            if (context.Resolve(t) is not CoreFunctionType coreFunctionType) return false;
                            var tModule =
                                module.TypeSection.FuncTypes.Span[checked((int)requestedImport.TypeIndex!.Value)];
                            if (!coreFunctionType.Type.Match(tModule)) return false;
                            break;
                        }
                        case ImportKind.Table:
                        {
                            var provided = importDecl.Descriptor.TableType!.Value;

                            if (provided.ElementType != requestedImport.TableType!.Value.ElementType) return false;

                            // allow subtyping
                            if (requestedImport.TableType!.Value.Limits.Min > provided.Limits.Min) return false;
                            break;
                        }
                        case ImportKind.Memory:
                        {
                            var provided = importDecl.Descriptor.MemoryType!.Value;

                            // allow subtyping
                            if (requestedImport.MemoryType!.Value.Limits.Min > provided.Limits.Min) return false;
                            break;
                        }
                        case ImportKind.Global:
                        {
                            var provided = importDecl.Descriptor.GlobalType!.Value;
                            if (provided != requestedImport.GlobalType) return false;
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

            return true;
            }
        }

        private class CoreInstance : ICoreInstance, IModuleExports
        {
            private readonly Dictionary<string, ICoreSortedExportable<IExternal>> cachedExports = new();

            public CoreInstance(Instance instance)
            {
                Instance = instance;
            }

            public Instance Instance { get; }

            public IModuleExports CoreExports => this;

            public bool TryGetExport<T>(string name, [NotNullWhen(true)] out ICoreSortedExportable<T>? result)
                where T : IExternal
            {
                if (cachedExports.TryGetValue(name, out var cachedExport))
                {
                    if (cachedExport is not ICoreSortedExportable<T> typed) throw new LinkException();
                    result = typed;
                    return true;
                }

                if (Instance.TryGetExport(name, out T export))
                {
                    result = new WrappedExport<T>(export);
                    cachedExports.Add(name, (result as ICoreSortedExportable<IExternal>)!);
                    return true;
                }

                result = default;
                return false;
            }

            bool IModuleExports.TryGetExport<T>(string name, out T value)
            {
                return Instance.TryGetExport(name, out value);
            }

            private class WrappedExport<T> : ICoreSortedExportable<T> where T : IExternal
            {
                public WrappedExport(T coreExternal)
                {
                    CoreExternal = coreExternal;
                }

                public T CoreExternal { get; }
            }
        }
    }

    internal enum SectionId : byte
    {
        CoreCustom,
        CoreModule,
        CoreInstance,
        CoreType,
        Component,
        Instance,
        Alias,
        Type,
        Canon,
        Start,
        Import,
        Export,
        Value
    }
}