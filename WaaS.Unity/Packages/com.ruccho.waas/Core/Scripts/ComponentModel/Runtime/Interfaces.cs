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
        IFunctionType Type { get; }
        FunctionBinder GetBinder(ExecutionContext context);
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
        IDespecializedValueType Despecialize();
    }

    public interface IDespecializedValueType : IValueType
    {
        byte AlignmentRank { get; }
        ushort ElementSize { get; }
        uint FlattenedCount { get; }
        void Flatten(Span<ValueType> dest);
    }

    public interface IPrimitiveValueType : IDespecializedValueType
    {
        PrimitiveValueTypeKind Kind { get; }
    }

    public enum PrimitiveValueTypeKind : byte
    {
        // ReSharper disable once InconsistentNaming
        _EnumStart = 0x73,
        String = 0x73,
        Char,
        F64,
        F32,
        U64,
        S64,
        U32,
        S32,
        U16,
        S16,
        U8,
        S8,
        Bool,

        // ReSharper disable once InconsistentNaming
        _EnumEnd
    }

    public interface IRecordType : IDespecializedValueType
    {
        ReadOnlyMemory<IRecordField> Fields { get; }
    }

    public interface IRecordField
    {
        string Label { get; }
        IValueType Type { get; }
    }

    public interface IVariantType : IDespecializedValueType
    {
        ReadOnlyMemory<IVariantCase> Cases { get; }
        byte DiscriminantTypeSizeRank { get; }
    }

    public interface IVariantCase
    {
        string Label { get; }
        IValueType? Type { get; }
    }

    public interface IListType : IDespecializedValueType
    {
        IValueType ElementType { get; }
    }

    public interface ITupleType : IValueType
    {
        ReadOnlyMemory<IValueType> Cases { get; }
    }

    public interface IFlagsType : IDespecializedValueType
    {
        public ReadOnlyMemory<string> Labels { get; }
    }

    public interface IEnumType : IValueType
    {
        public ReadOnlyMemory<string> Labels { get; }
    }

    public interface IOptionType : IValueType
    {
        IValueType Type { get; }
    }

    public interface IResultType : IValueType
    {
        IValueType? Type { get; }
        IValueType? ErrorType { get; }
    }

    public interface IOwnedType : IDespecializedValueType
    {
        IResourceType Type { get; }
    }

    public interface IBorrowedType : IDespecializedValueType
    {
        IResourceType Type { get; }
    }

    public interface IResourceType : IType
    {
        uint New(uint rep);
        void Drop(uint index);
    }

    public interface IFunctionType : IType
    {
        ReadOnlyMemory<IParameter> Parameters { get; }
        IValueType? Result { get; }

        IRecordType ParameterType { get; }
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