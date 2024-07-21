using System;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;
using Global = WaaS.Runtime.Global;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    [Variant(0x50, typeof(CoreModuleType))]
    [VariantFallback(typeof(CoreFunctionType))]
    public partial interface ICoreTypeDefinition : IUnresolved<ICoreType>
    {
    }

    public readonly struct CoreFunctionType : ICoreTypeDefinition, ICoreType
    {
        static CoreFunctionType()
        {
            Formatter<CoreFunctionType>.Default = new Formatter();
        }

        internal class Formatter : IFormatter<CoreFunctionType>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out CoreFunctionType result)
            {
                var type = new WaaS.Models.FunctionType(ref reader);
                result = new CoreFunctionType(type);
                return true;
            }
        }

        public WaaS.Models.FunctionType Type { get; }

        public CoreFunctionType(WaaS.Models.FunctionType type)
        {
            Type = type;
        }

        public ICoreType ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
        }
    }

    [GenerateFormatter]
    public readonly partial struct CoreModuleType : ICoreTypeDefinition
    {
        public ReadOnlyMemory<ICoreModuleDeclaration> Declarations { get; }

        public ICoreType ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(CoreImportDeclaration))]
    [Variant(0x01, typeof(CoreTypeDeclaration))]
    [Variant(0x02, typeof(CoreAliasDeclaration))]
    [Variant(0x03, typeof(CoreExportDeclaration))]
    public partial interface ICoreModuleDeclaration
    {
    }

    public readonly struct CoreImportDeclaration : ICoreModuleDeclaration
    {
        static CoreImportDeclaration()
        {
            Formatter<CoreImportDeclaration>.Default = new Formatter();
        }

        internal class Formatter : IFormatter<CoreImportDeclaration>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out CoreImportDeclaration result)
            {
                var import = new Import(ref reader);
                result = new CoreImportDeclaration(import);
                return true;
            }
        }

        public Import Import { get; }

        public CoreImportDeclaration(Import import)
        {
            Import = import;
        }
    }

    [GenerateFormatter]
    public readonly partial struct CoreTypeDeclaration : ICoreModuleDeclaration
    {
        public ICoreType Type { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CoreAliasDeclaration : ICoreModuleDeclaration,
        IReadCallbackReceiver<CoreAliasDeclaration>
    {
        private CoreSortTag Sort { get; }
        private byte AliasKind { get; }
        private uint Depth { get; }
        private uint Index { get; }
        [Ignore] public IUnresolved<ICoreSorted> Target { get; private init; }

        public CoreAliasDeclaration OnAfterRead(IIndexSpace indexSpace)
        {
            for (var i = 0; i < Depth; i++) indexSpace = indexSpace.Parent;
            return new CoreAliasDeclaration
            {
                Target = Sort switch
                {
                    CoreSortTag.Function => indexSpace.Get<ICoreSortedExportable<IInvocableFunction>>(Index),
                    CoreSortTag.Table => indexSpace.Get<ICoreSortedExportable<Table>>(Index),
                    CoreSortTag.Memory => indexSpace.Get<ICoreSortedExportable<Memory>>(Index),
                    CoreSortTag.Global => indexSpace.Get<ICoreSortedExportable<Global>>(Index),
                    CoreSortTag.Type => indexSpace.Get<ICoreType>(Index),
                    CoreSortTag.Module => indexSpace.Get<ICoreModule>(Index),
                    CoreSortTag.Instance => indexSpace.Get<ICoreInstance>(Index),
                    _ => throw new ArgumentOutOfRangeException()
                }
            };
        }
    }

    public readonly struct CoreExportDeclaration : ICoreModuleDeclaration
    {
        static CoreExportDeclaration()
        {
            Formatter<CoreExportDeclaration>.Default = new Formatter();
        }

        internal class Formatter : IFormatter<CoreExportDeclaration>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out CoreExportDeclaration result)
            {
                var name = reader.ReadUtf8String();
                var descriptor = new ImportDescriptor(ref reader);
                result = new CoreExportDeclaration(name, descriptor);
                return true;
            }
        }

        public string Name { get; }
        public ImportDescriptor Descriptor { get; }

        public CoreExportDeclaration(string name, ImportDescriptor descriptor)
        {
            Name = name;
            Descriptor = descriptor;
        }
    }
}