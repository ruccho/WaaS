#nullable enable
using System;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    public partial class Alias : IReadCallbackReceiver<Alias>
    {
        public ISort Sort { get; }
        public IAliasTarget Target { get; }

        public Alias OnAfterRead(IIndexSpace indexSpace)
        {
            indexSpace.AddUntyped(Target.ResolveAlias(Sort));
            return this;
        }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(AliasTargetExport))]
    [Variant(0x01, typeof(AliasTargetCoreExport))]
    [Variant(0x02, typeof(AliasTargetOuter))]
    public partial interface IAliasTarget
    {
        IUnresolved<ISorted> ResolveAlias(ISort sort);
    }

    [GenerateFormatter]
    public partial class AliasTargetExport : IAliasTarget
    {
        public IUnresolved<IInstance> Instance { get; }
        public string Name { get; }

        public IUnresolved<ISorted> ResolveAlias(ISort sort)
        {
            switch (sort)
            {
                case ComponentSort componentSort:
                    switch (componentSort.Tag)
                    {
                        case SortTag.Function:
                            return new Aliased<IFunction>(this);
                        case SortTag.Value:
                            return new Aliased<IValue>(this);
                        case SortTag.Type:
                            return new Aliased<IType>(this);
                        case SortTag.Component:
                            return new Aliased<IComponent>(this);
                        case SortTag.Instance:
                            return new Aliased<IInstance>(this);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case CoreSort coreSort:
                    switch (coreSort.Tag)
                    {
                        case CoreSortTag.Module:
                            return new Aliased<ICoreModule>(this);
                        case CoreSortTag.Function:
                        case CoreSortTag.Table:
                        case CoreSortTag.Memory:
                        case CoreSortTag.Global:
                        case CoreSortTag.Type:
                        case CoreSortTag.Instance:
                            throw new InvalidModuleException(); // non-exportable
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(sort));
            }
        }

        private class Aliased<T> : IUnresolved<T> where T : ISortedExportable
        {
            public Aliased(AliasTargetExport source)
            {
                Source = source;
            }

            private AliasTargetExport Source { get; }

            public T ResolveFirstTime(IInstanceResolutionContext context)
            {
                var instance = context.Resolve(Source.Instance);
                if (!instance.TryGetExport(Source.Name, out T? result)) throw new LinkException();
                return result;
            }
        }
    }

    [GenerateFormatter]
    public partial class AliasTargetCoreExport : IAliasTarget
    {
        public IUnresolved<ICoreInstance> CoreInstance { get; }
        public string Name { get; }

        public IUnresolved<ISorted> ResolveAlias(ISort sort)
        {
            switch (sort)
            {
                case CoreSort coreSort:
                    switch (coreSort.Tag)
                    {
                        case CoreSortTag.Function:
                            return new Aliased<IInvocableFunction>(this);
                        case CoreSortTag.Table:
                            return new Aliased<Table>(this);
                        case CoreSortTag.Memory:
                            return new Aliased<Memory>(this);
                        case CoreSortTag.Global:
                            return new Aliased<Global>(this);
                        case CoreSortTag.Type:
                        case CoreSortTag.Module:
                        case CoreSortTag.Instance:
                            throw new InvalidModuleException(); // non-exportable
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ComponentSort componentSort:
                    switch (componentSort.Tag)
                    {
                        case SortTag.Function:
                        case SortTag.Value:
                        case SortTag.Type:
                        case SortTag.Component:
                        case SortTag.Instance:
                            throw new InvalidModuleException();
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(sort));
            }
        }

        private class Aliased<T> : IUnresolved<ICoreSortedExportable<T>> where T : IExternal
        {
            public Aliased(AliasTargetCoreExport source)
            {
                Source = source;
            }

            private AliasTargetCoreExport Source { get; }

            public ICoreSortedExportable<T> ResolveFirstTime(IInstanceResolutionContext context)
            {
                var coreInstance = context.Resolve(Source.CoreInstance);
                if (!coreInstance.TryGetExport(Source.Name, out ICoreSortedExportable<T>? result))
                    throw new LinkException();
                return result;
            }
        }
    }

    public class AliasTargetOuter : IAliasTarget
    {
        static AliasTargetOuter()
        {
            Formatter<AliasTargetOuter>.Default = new Formatter();
        }

        public AliasTargetOuter(IIndexSpace indexSpace, uint depth, uint index)
        {
            IndexSpace = indexSpace;
            Index = index;
            Depth = depth;
        }

        private uint Depth { get; }

        private IIndexSpace IndexSpace { get; }
        private uint Index { get; }

        public IUnresolved<ISorted> ResolveAlias(ISort sort)
        {
            return sort switch
            {
                // sort is restricted to static entities
                ComponentSort componentSort => componentSort.Tag switch
                {
                    SortTag.Type => new Resolver<IType>(Depth, IndexSpace.Get<IType>(Index)),
                    SortTag.Component => new Resolver<IComponent>(Depth, IndexSpace.Get<IComponent>(Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                CoreSort coreSort => coreSort.Tag switch
                {
                    CoreSortTag.Module => new Resolver<ICoreModule>(Depth, IndexSpace.Get<ICoreModule>(Index)),
                    _ => throw new ArgumentOutOfRangeException()
                },
                _ => throw new ArgumentOutOfRangeException(nameof(sort))
            };
        }

        internal class Formatter : IFormatter<AliasTargetOuter>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out AliasTargetOuter result)
            {
                var depth = reader.ReadUnalignedLeb128U32();
                var index = reader.ReadUnalignedLeb128U32();

                for (var i = 0; i < depth; i++) indexSpace = indexSpace.Parent;

                result = new AliasTargetOuter(indexSpace, depth, index);
                return true;
            }
        }

        private class Resolver<T> : IUnresolved<T> where T : ISorted
        {
            public Resolver(uint depth, IUnresolved<T> target)
            {
                Depth = depth;
                Target = target;
            }

            private uint Depth { get; }
            private IUnresolved<T> Target { get; }

            public T ResolveFirstTime(IInstanceResolutionContext context)
            {
                for (var i = 0; i < Depth; i++) context = context.Parent;
                return context.Resolve(Target);
            }
        }
    }
}