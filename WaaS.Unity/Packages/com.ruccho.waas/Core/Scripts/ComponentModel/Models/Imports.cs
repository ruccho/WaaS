#nullable enable

using System;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;

namespace WaaS.ComponentModel.Models
{
    public interface IImport<out T> : IUnresolved<T> where T : ISortedExportable
    {
        static IImport()
        {
            Formatter<IImport<ISortedExportable>>.Default = new ImportFormatter();
        }

        ImportExportName Name { get; }
        IExportableDescriptor<T> Descriptor { get; }
    }

    internal class ImportFormatter : IFormatter<IImport<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out IImport<ISortedExportable> result)
        {
            var name = Formatter<ImportExportName>.Read(ref reader, indexSpace);
            var externDesc =
                Formatter<IExportableDescriptor<ISortedExportable>>.Read(ref reader, indexSpace);

            result = externDesc switch
            {
                IExportableDescriptor<IFunction> function => new Import<IFunction>(name, function),
                IExportableDescriptor<IValue> value => new Import<IValue>(name, value),
                IExportableDescriptor<IType> type => new Import<IType>(name, type),
                IExportableDescriptor<IComponent> component => new Import<IComponent>(name, component),
                IExportableDescriptor<IInstance> instance => new Import<IInstance>(name, instance),
                IExportableDescriptor<ICoreModule> coreModule => new Import<ICoreModule>(name, coreModule),
                _ => throw new ArgumentOutOfRangeException()
            };
            return true;
        }
    }

    public class Import<T> : IImport<T> where T : ISortedExportable
    {
        public Import(ImportExportName name, IExportableDescriptor<T> descriptor)
        {
            Name = name;
            Descriptor = descriptor;
        }

        public ImportExportName Name { get; }
        public IExportableDescriptor<T> Descriptor { get; }

        public T ResolveFirstTime(IInstanceResolutionContext context)
        {
            return context.ResolveImport(this);
        }
    }

    public interface IExport<out T> : IUnresolved<T> where T : ISortedExportable
    {
        static IExport()
        {
            Formatter<IExport<ISortedExportable>>.Default = new ExportFormatter();
        }

        ImportExportName Name { get; }
        IUnresolved<T> Target { get; }
        IExportableDescriptor<T>? Descriptor { get; }
    }

    internal class ExportFormatter : IFormatter<IExport<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out IExport<ISortedExportable> result)
        {
            var name = Formatter<ImportExportName>.Read(ref reader, indexSpace);
            var sortIndex = Formatter<SortIndex>.Read(ref reader, indexSpace);
            var externDesc =
                Formatter<IExportableDescriptor<ISortedExportable>>.ReadOptional(ref reader, indexSpace);

            var index = sortIndex.Index;

            static IExport<TExport> GetExport<TExport>(ImportExportName name, uint index, IIndexSpace indexSpace,
                IExportableDescriptor<ISortedExportable>? desc) where TExport : ISortedExportable
            {
                return new Export<TExport>(
                    name,
                    indexSpace.Get<TExport>(index),
                    desc != null
                        ? desc as IExportableDescriptor<TExport> ?? throw new InvalidModuleException()
                        : null);
            }

            result = sortIndex.Sort switch
            {
                ComponentSort componentSort => componentSort.Tag switch
                {
                    SortTag.Function => GetExport<IFunction>(name, index, indexSpace, externDesc),
                    SortTag.Value => GetExport<IValue>(name, index, indexSpace, externDesc),
                    SortTag.Type => GetExport<IType>(name, index, indexSpace, externDesc),
                    SortTag.Component => GetExport<IComponent>(name, index, indexSpace, externDesc),
                    SortTag.Instance => GetExport<IInstance>(name, index, indexSpace, externDesc),
                    _ => throw new ArgumentOutOfRangeException()
                },
                CoreSort coreSort => coreSort.Tag switch
                {
                    CoreSortTag.Module => GetExport<ICoreModule>(name, index, indexSpace, externDesc),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
            return true;
        }
    }

    public class Export<T> : IExport<T> where T : ISortedExportable
    {
        public Export(ImportExportName name, IUnresolved<T> target, IExportableDescriptor<T>? descriptor)
        {
            Name = name;
            Target = target;
            Descriptor = descriptor;
        }

        public ImportExportName Name { get; }
        public IUnresolved<T> Target { get; }
        public IExportableDescriptor<T>? Descriptor { get; }

        public T ResolveFirstTime(IInstanceResolutionContext context)
        {
            return context.Resolve(Target);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ImportExportName
    {
        private byte Unknown0 { get; }
        public string Name { get; }
    }
}