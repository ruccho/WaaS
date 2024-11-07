#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    [Variant(0x00, typeof(CoreModuleInstantiation))]
    [Variant(0x01, typeof(CoreInlineInstantiation))]
    public partial interface ICoreInstantiation : IUnresolved<ICoreInstance>
    {
    }

    [GenerateFormatter]
    public partial class CoreModuleInstantiation : ICoreInstantiation
    {
        public IUnresolved<ICoreModule> CoreModule { get; }
        public ReadOnlyMemory<CoreInstantiationArgument> Arguments { get; }

        public ICoreInstance ResolveFirstTime(IInstanceResolutionContext context)
        {
            var module = context.Resolve(CoreModule);
            Dictionary<string, ICoreInstance> imports = new();
            foreach (var argument in Arguments.Span) imports.Add(argument.Name, context.Resolve(argument.Value));

            return module.Instantiate(imports);
        }
    }


    [GenerateFormatter]
    public partial class CoreInlineInstantiation : ICoreInstantiation
    {
        public ReadOnlyMemory<ICoreInlineExport<ICoreSortedExportable<IExternal>>> Exports { get; }

        public ICoreInstance ResolveFirstTime(IInstanceResolutionContext context)
        {
            Dictionary<string, ICoreSortedExportable<IExternal>> exports = new();
            foreach (var export in Exports.Span) exports.Add(export.Name, context.Resolve(export.Item));

            return new CoreInstance(exports);
        }

        private class CoreInstance : ICoreInstance, IModuleExports
        {
            private readonly IReadOnlyDictionary<string, ICoreSortedExportable<IExternal>> exports;

            public CoreInstance(IReadOnlyDictionary<string, ICoreSortedExportable<IExternal>> exports)
            {
                this.exports = exports;
            }

            public IModuleExports CoreExports => this;

            public bool TryGetExport<T>(string name, [NotNullWhen(true)] out ICoreSortedExportable<T>? result)
                where T : IExternal
            {
                if (exports.TryGetValue(name, out var export))
                    if (export is ICoreSortedExportable<T> t)
                    {
                        result = t;
                        return true;
                    }

                result = default;
                return false;
            }

            bool IModuleExports.TryGetExport<T>(string name, [NotNullWhen(true)] out T? value) where T : default
            {
                if (exports.TryGetValue(name, out var export))
                {
                    IExternal coreExportable = export switch
                    {
                        ICoreSortedExportable<IInvocableFunction> coreFunction => coreFunction.CoreExternal,
                        ICoreSortedExportable<Global> coreGlobal => coreGlobal.CoreExternal,
                        ICoreSortedExportable<Memory> coreMemory => coreMemory.CoreExternal,
                        ICoreSortedExportable<Table> coreTable => coreTable.CoreExternal,
                        _ => throw new ArgumentOutOfRangeException(nameof(export))
                    };

                    if (coreExportable is not T typed) throw new LinkException();
                    value = typed;
                    return true;
                }

                value = default;
                return false;
            }
        }
    }


    [GenerateFormatter]
    public partial class CoreInstantiationArgument
    {
        public string Name { get; }
        private byte Unknown0 { get; }
        public IUnresolved<ICoreInstance> Value { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CoreSortIndex
    {
        public CoreSort Sort { get; }
        public uint Index { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CoreSort : ISort
    {
        public CoreSortTag Tag { get; }
    }

    public enum CoreSortTag : byte
    {
        Function = 0x00,
        Table = 0x01,
        Memory = 0x02,
        Global = 0x03,
        Type = 0x10,
        Module = 0x11,
        Instance = 0x12
    }

    public interface ICoreInlineExport<out T> where T : ICoreSortedExportable<IExternal>
    {
        static ICoreInlineExport()
        {
            Formatter<ICoreInlineExport<ICoreSortedExportable<IExternal>>>.Default = new CoreInlineExportFormatter();
        }

        public string Name { get; }
        public IUnresolved<T> Item { get; }
    }

    internal class CoreInlineExportFormatter : IFormatter<ICoreInlineExport<ICoreSortedExportable<IExternal>>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out ICoreInlineExport<ICoreSortedExportable<IExternal>> result)
        {
            var name = reader.ReadUtf8String();
            var sortIndex = Formatter<CoreSortIndex>.Read(ref reader, indexSpace);

            result = sortIndex.Sort.Tag switch
            {
                CoreSortTag.Function => new CoreInlineExport<ICoreSortedExportable<IInvocableFunction>>(name,
                    indexSpace.Get<ICoreSortedExportable<IInvocableFunction>>(sortIndex.Index)),
                CoreSortTag.Table => new CoreInlineExport<ICoreSortedExportable<Table>>(name,
                    indexSpace.Get<ICoreSortedExportable<Table>>(sortIndex.Index)),
                CoreSortTag.Memory => new CoreInlineExport<ICoreSortedExportable<Memory>>(name,
                    indexSpace.Get<ICoreSortedExportable<Memory>>(sortIndex.Index)),
                CoreSortTag.Global => new CoreInlineExport<ICoreSortedExportable<Global>>(name,
                    indexSpace.Get<ICoreSortedExportable<Global>>(sortIndex.Index)),
                _ => throw new ArgumentOutOfRangeException()
            };

            return true;
        }
    }

    public readonly struct CoreInlineExport<T> : ICoreInlineExport<T> where T : ICoreSortedExportable<IExternal>
    {
        public string Name { get; }
        public IUnresolved<T> Item { get; }

        public CoreInlineExport(string name, IUnresolved<T> item)
        {
            Name = name;
            Item = item;
        }
    }


    [GenerateFormatter]
    [Variant(0x00, typeof(ComponentInstantiation))]
    [Variant(0x01, typeof(InlineInstantiation))]
    public partial interface IInstantiation : IUnresolved<IInstance>
    {
    }


    [GenerateFormatter]
    public partial class ComponentInstantiation : IInstantiation
    {
        public IUnresolved<IComponent> Component { get; }
        public ReadOnlyMemory<IInstantiationArgument<ISortedExportable>> Arguments { get; }

        public IInstance ResolveFirstTime(IInstanceResolutionContext context)
        {
            var args = new Dictionary<string, ISortedExportable>();
            foreach (var argument in Arguments.Span) args.Add(argument.Name, context.Resolve(argument.Value));
            var component = context.Resolve(Component);

            return component.Instantiate(context, args);
        }
    }

    [GenerateFormatter]
    public partial class InlineInstantiation : IInstantiation
    {
        public ReadOnlyMemory<IInlineExport<ISortedExportable>> Exports { get; }

        public IInstance ResolveFirstTime(IInstanceResolutionContext context)
        {
            Dictionary<string, ISortedExportable> exports = new();
            foreach (var export in Exports.Span) exports.Add(export.ExportName, context.Resolve(export.Item));

            return new Instance(exports);
        }

        public bool TryGetExport<T>(string name, out IUnresolved<T>? result) where T : ISortedExportable
        {
            foreach (var export in Exports.Span)
            {
                if (export.ExportName != name) continue;
                if (export.Item is not IUnresolved<T> item) break;

                result = item;
                return true;
            }

            result = default;
            return false;
        }

        private class Instance : IInstance
        {
            private readonly IReadOnlyDictionary<string, ISortedExportable> exports;

            public Instance(IReadOnlyDictionary<string, ISortedExportable> exports)
            {
                this.exports = exports;
            }

            public bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? result) where T : ISortedExportable
            {
                if (exports.TryGetValue(name, out var export))
                    if (export is T t)
                    {
                        result = t;
                        return true;
                    }

                result = default;
                return false;
            }
        }
    }

    public interface IInstantiationArgument<out T> where T : ISortedExportable
    {
        static IInstantiationArgument()
        {
            Formatter<IInstantiationArgument<ISortedExportable>>.Default = new InstantiationArgumentFormatter();
        }

        public string Name { get; }
        public IUnresolved<T> Value { get; }
    }

    internal class InstantiationArgumentFormatter : IFormatter<IInstantiationArgument<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out IInstantiationArgument<ISortedExportable> result)
        {
            var name = reader.ReadUtf8String();
            var sortIndex = Formatter<SortIndex>.Read(ref reader, indexSpace);

            result = sortIndex.Sort switch
            {
                ComponentSort componentSort => componentSort.Tag switch
                {
                    SortTag.Function => new InstantiationArgument<IFunction>(name,
                        indexSpace.Get<IFunction>(sortIndex.Index)),
                    SortTag.Value => new InstantiationArgument<IValue>(name,
                        indexSpace.Get<IValue>(sortIndex.Index)),
                    SortTag.Type => new InstantiationArgument<IType>(name, indexSpace.Get<IType>(sortIndex.Index)),
                    SortTag.Component => new InstantiationArgument<IComponent>(name,
                        indexSpace.Get<IComponent>(sortIndex.Index)),
                    SortTag.Instance => new InstantiationArgument<IInstance>(name,
                        indexSpace.Get<IInstance>(sortIndex.Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                CoreSort coreSort => coreSort.Tag switch
                {
                    CoreSortTag.Module => new InstantiationArgument<ICoreModule>(name,
                        indexSpace.Get<ICoreModule>(sortIndex.Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };
            return true;
        }
    }

    public class InstantiationArgument<T> : IInstantiationArgument<T> where T : ISortedExportable
    {
        public InstantiationArgument(string name, IUnresolved<T> value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public IUnresolved<T> Value { get; init; }
    }

    [GenerateFormatter]
    public readonly partial struct SortIndex
    {
        public ISort Sort { get; }
        public uint Index { get; }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(CoreSort))]
    [VariantFallback(typeof(ComponentSort))]
    public partial interface ISort
    {
    }

    [GenerateFormatter]
    public readonly partial struct ComponentSort : ISort
    {
        public SortTag Tag { get; }
    }

    public enum SortTag : byte
    {
        Function = 0x01,
        Value = 0x02,
        Type = 0x03,
        Component = 0x04,
        Instance = 0x05
    }

    public interface IInlineExport<out T> where T : ISortedExportable
    {
        static IInlineExport()
        {
            Formatter<IInlineExport<ISortedExportable>>.Default = new InlineExportFormatter();
        }

        string ExportName { get; }
        IUnresolved<T> Item { get; }
    }

    internal class InlineExportFormatter : IFormatter<IInlineExport<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out IInlineExport<ISortedExportable> result)
        {
            var name = reader.ReadUtf8String();
            var sortIndex = Formatter<SortIndex>.Read(ref reader, indexSpace);

            result = sortIndex.Sort switch
            {
                ComponentSort componentSort => componentSort.Tag switch
                {
                    SortTag.Function => new InlineExport<IFunction>(name,
                        indexSpace.Get<IFunction>(sortIndex.Index)),
                    SortTag.Value => new InlineExport<IValue>(name,
                        indexSpace.Get<IValue>(sortIndex.Index)),
                    SortTag.Type => new InlineExport<IType>(name, indexSpace.Get<IType>(sortIndex.Index)),
                    SortTag.Component => new InlineExport<IComponent>(name,
                        indexSpace.Get<IComponent>(sortIndex.Index)),
                    SortTag.Instance => new InlineExport<IInstance>(name,
                        indexSpace.Get<IInstance>(sortIndex.Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                CoreSort coreSort => coreSort.Tag switch
                {
                    CoreSortTag.Module => new InlineExport<ICoreModule>(name,
                        indexSpace.Get<ICoreModule>(sortIndex.Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException()
            };

            return true;
        }
    }

    public readonly struct InlineExport<T> : IInlineExport<T> where T : ISortedExportable
    {
        public string ExportName { get; }
        public IUnresolved<T> Item { get; }

        public InlineExport(string exportName, IUnresolved<T> item)
        {
            ExportName = exportName;
            Item = item;
        }
    }
}