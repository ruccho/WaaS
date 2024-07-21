#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    public interface ISorted
    {
    }

    public interface ISortedExportable : ISorted
    {
    }

    public interface ICoreSortedExportable<out T> : ICoreSorted where T : IExternal
    {
        T CoreExternal { get; }
    }

    public interface ICoreSorted : ISorted
    {
    }

    public interface ICoreType : ICoreSorted
    {
    }

    public interface ICoreModule : ICoreSorted, ISortedExportable
    {
        ICoreInstance Instantiate(IReadOnlyDictionary<string, ICoreInstance> imports);
    }

    public interface ICoreInstance : ICoreSorted
    {
        IModuleExports CoreExports { get; }
        bool TryGetExport<T>(string name, [NotNullWhen(true)] out ICoreSortedExportable<T>? result) where T : IExternal;
    }

    public interface IFunction : ISortedExportable
    {
    }

    public interface IValue : ISortedExportable
    {
    }

    public interface IType : ISortedExportable
    {
    }

    public interface IComponent : ISortedExportable
    {
        IInstance Instantiate(IInstanceResolutionContext? context,
            IReadOnlyDictionary<string, ISortedExportable> arguments);
    }

    public interface IInstance : ISortedExportable
    {
        bool TryGetExport<T>(string name, [NotNullWhen(true)] out T? result) where T : ISortedExportable;
    }

    public interface IValueType : IType
    {
    }

    public interface IRecordType : IValueType
    {
        ReadOnlyMemory<IRecordField> Fields { get; }
    }

    public interface IRecordField
    {
        string Label { get; }
        IType Type { get; }
    }

    public interface IVariantType : IValueType
    {
        ReadOnlyMemory<IVariantCase> Cases { get; }
    }

    public interface IVariantCase
    {
        string Label { get; }
        IValueType? Type { get; }
    }

    public interface IListType : IValueType
    {
        IType ElementType { get; }
    }

    public interface ITupleType : IValueType
    {
        ReadOnlyMemory<IValueType> Cases { get; }
    }

    public interface IFlagsType : IValueType
    {
        public ReadOnlyMemory<string> Labels { get; }
    }

    public interface IEnumType : IValueType
    {
        public ReadOnlyMemory<string> Labels { get; }
    }

    public interface IOptionType : IValueType
    {
        IType Type { get; }
    }

    public interface IResultType : IValueType
    {
        IType Type { get; }
        IType ErrorType { get; }
    }

    public interface IOwnedType : IValueType
    {
        IResourceType Type { get; }
    }

    public interface IBorrowedType : IValueType
    {
        IResourceType Type { get; }
    }

    public interface IResourceType : IType
    {
        IFunction Destructor { get; }
    }

    public interface IFunctionType : IType
    {
        public ReadOnlyMemory<IParameter> Parameters { get; }
        public IValueType? Result { get; }
    }

    public interface IParameter
    {
        string Label { get; }
        IValueType Type { get; }
    }

    public interface IComponentType : IType
    {
    }

    public interface IInstanceType : IType
    {
    }
}