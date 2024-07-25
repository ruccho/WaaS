#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Models
{
    public class Component : IUnresolved<IComponent>, IComponent
    {
        private readonly List<CustomSection> customSections = new();
        private readonly List<IExport<ISortedExportable>> exports = new();

        private readonly List<IImport<ISortedExportable>> imports = new();

        internal Component(ref ModuleReader reader, long? size = null, IIndexSpace parentIndexSpace = null)
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

                Console.WriteLine(sectionId.ToString());

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
                        indexSpace.Add(new CoreModule(ref reader, sectionSize));
                        break;
                    }
                    case SectionId.CoreInstance:
                    {
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<ICoreInstantiation>.Read(ref reader, indexSpace), indexSpace);
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
                        reader.ReadVector(
                            static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                                Formatter<IInstantiation>.Read(ref reader, indexSpace), indexSpace);
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

        public IInstance Instantiate(IInstanceResolutionContext? context,
            IReadOnlyDictionary<string, ISortedExportable> arguments)
        {
            // validate
            foreach (var import in imports)
                if (!arguments.TryGetValue(import.Name.Name, out var argument) ||
                    !import.Descriptor.ValidateArgument(argument))
                    throw new LinkException($"The import {import.Name.Name} is missing or invalid");

            var newContext = new InstanceResolutionContext(arguments, context);
            Dictionary<string, ISortedExportable> resolvedExports = new();
            foreach (var export in exports) resolvedExports.Add(export.Name.Name, newContext.Resolve(export.Target));

            return new Instance(resolvedExports);
        }

        public IComponent ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
        }

        public static Component Create(ReadOnlySpan<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new Component(ref reader);
        }

        public static Component Create(ReadOnlySequence<byte> buffer)
        {
            var reader = new ModuleReader(buffer);
            return new Component(ref reader);
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

    internal class CoreModule : IUnresolved<ICoreModule>, ICoreModule
    {
        private readonly Module module;

        public CoreModule(ref ModuleReader reader, long? moduleSize)
        {
            module = new Module(ref reader, moduleSize);
        }

        public ICoreInstance Instantiate(IReadOnlyDictionary<string, ICoreInstance> imports)
        {
            Imports moduleImports = new();
            foreach (var (key, value) in imports) moduleImports.Add(key, value.CoreExports);

            return new CoreInstance(new Instance(module, moduleImports));
        }

        public ICoreModule ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
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

            public bool TryGetExport<T>(string name, out ICoreSortedExportable<T> result) where T : IExternal
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
                    cachedExports.Add(name, result as ICoreSortedExportable<IExternal>);
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