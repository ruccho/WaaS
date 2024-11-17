#nullable enable

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

    public readonly struct CoreFunctionType : ICoreTypeDefinition, ICoreType, IEquatable<CoreFunctionType>
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

        public bool Equals(CoreFunctionType other)
        {
            return Equals(Type, other.Type);
        }

        public override bool Equals(object obj)
        {
            return obj is CoreFunctionType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Type != null ? Type.GetHashCode() : 0;
        }

        public static bool operator ==(CoreFunctionType left, CoreFunctionType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CoreFunctionType left, CoreFunctionType right)
        {
            return !left.Equals(right);
        }
    }

    public interface ICoreModuleType : ICoreType
    {
        ReadOnlyMemory<ICoreModuleDeclaration> Declarations { get; }
    }

    public class CoreModuleType : ICoreTypeDefinition, ICoreModuleType
    {
        static CoreModuleType()
        {
            Formatter<CoreModuleType>.Default = new CoreModuleTypeFormatter();
        }

        private class CoreModuleTypeFormatter : IFormatter<CoreModuleType>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out CoreModuleType result)
            {
                var newIndexSpace = new IndexSpace(indexSpace);
                result = new CoreModuleType(
                    reader.ReadVector(
                        static (ref ModuleReader reader, IIndexSpace indexSpace) =>
                            Formatter<ICoreModuleDeclaration>.Read(ref reader, indexSpace), newIndexSpace)
                );
                return true;
            }
        }

        private CoreModuleType(ReadOnlyMemory<ICoreModuleDeclaration> declarations)
        {
            Declarations = declarations;
        }

        public ReadOnlyMemory<ICoreModuleDeclaration> Declarations { get; }

        public ICoreType ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
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

        private class Formatter : IFormatter<CoreImportDeclaration>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out CoreImportDeclaration result)
            {
                var import = new Import(ref reader);
                IUnresolved<ICoreType>? type = null;
                switch (import.Description.Kind)
                {
                    case ImportKind.Type:
                    {
                        type = indexSpace.Get<ICoreType>(import.Description.TypeIndex!.Value);
                        break;
                    }
                    case ImportKind.Table:
                    case ImportKind.Memory:
                    case ImportKind.Global:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                result = new CoreImportDeclaration(import.ModuleName, import.Name, import.Description, type);
                return true;
            }
        }

        public string ModuleName { get; }
        public string Name { get; }
        public ImportDescriptor Descriptor { get; }
        public ImportKind Kind => Descriptor.Kind;
        public IUnresolved<ICoreType>? Type { get; }

        public CoreImportDeclaration(string moduleName, string name, ImportDescriptor descriptor, IUnresolved<ICoreType>? type)
        {
            ModuleName = moduleName;
            Name = name;
            Descriptor = descriptor;
            Type = type;
        }
    }

    [GenerateFormatter]
    public readonly partial struct CoreTypeDeclaration : ICoreModuleDeclaration
    {
        public ICoreTypeDefinition Type { get; }
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
            for (var i = 0; i < Depth; i++) indexSpace = indexSpace.Parent ?? throw new InvalidOperationException();

            IUnresolved<ICoreSorted> target = Sort switch
            {
                CoreSortTag.Function => indexSpace.Get<ICoreSortedExportable<IInvocableFunction>>(Index),
                CoreSortTag.Table => indexSpace.Get<ICoreSortedExportable<Table>>(Index),
                CoreSortTag.Memory => indexSpace.Get<ICoreSortedExportable<Memory>>(Index),
                CoreSortTag.Global => indexSpace.Get<ICoreSortedExportable<Global>>(Index),
                CoreSortTag.Type => indexSpace.Get<ICoreType>(Index),
                CoreSortTag.Module => indexSpace.Get<ICoreModule>(Index),
                CoreSortTag.Instance => indexSpace.Get<ICoreInstance>(Index),
                _ => throw new ArgumentOutOfRangeException()
            };

            indexSpace.AddUntyped(target);

            return new CoreAliasDeclaration
            {
                Target = target
            };
        }

        partial void Validate()
        {
            if (AliasKind != 1) throw new InvalidModuleException();
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

                CoreFunctionType? functionType = null;
                switch (descriptor.Kind)
                {
                    case ImportKind.Type:
                    {
                        if (indexSpace.Get<ICoreType>(descriptor.TypeIndex!.Value) is not CoreFunctionType
                            coreFunctionType)
                        {
                            result = default;
                            return false;
                        }

                        functionType = coreFunctionType;

                        break;
                    }
                    case ImportKind.Table:
                    case ImportKind.Memory:
                    case ImportKind.Global:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                result = new CoreExportDeclaration(name, descriptor, functionType);
                return true;
            }
        }

        public string Name { get; }
        public ImportDescriptor Descriptor { get; }

        public ImportKind Kind => Descriptor.Kind;
        public CoreFunctionType? FunctionType { get; }

        public CoreExportDeclaration(
            string name,
            ImportDescriptor descriptor,
            CoreFunctionType? functionType)
        {
            Name = name;
            Descriptor = descriptor;
            FunctionType = functionType;
        }
    }
}