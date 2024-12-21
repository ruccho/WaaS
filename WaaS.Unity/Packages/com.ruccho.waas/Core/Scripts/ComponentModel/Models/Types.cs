#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    [Variant(0x40, typeof(FunctionType))]
    [Variant(0x41, typeof(ComponentType))]
    [Variant(0x42, typeof(InstanceType))]
    [VariantFallback(typeof(IResourceTypeDefinition))]
    [VariantFallback(typeof(IValueTypeDefinition))]
    public partial interface ITypeDefinition : IUnresolved<IType>
    {
    }

    [GenerateFormatter]
    [Variant(0x68, typeof(BorrowedType))]
    [Variant(0x69, typeof(OwnedType))]
    [Variant(0x6A, typeof(ResultType))]
    [Variant(0x6B, typeof(OptionType))]
    [Variant(0x6D, typeof(EnumType))]
    [Variant(0x6E, typeof(FlagsType))]
    [Variant(0x6F, typeof(TupleType))]
    [Variant(0x70, typeof(ListType))]
    [Variant(0x71, typeof(VariantType))]
    [Variant(0x72, typeof(RecordType))]
    [VariantFallback(typeof(PrimitiveValueType))]
    public partial interface IValueTypeDefinition : ITypeDefinition, IUnresolvedValueType
    {
    }

    [GenerateFormatter]
    public readonly partial struct PrimitiveValueType : IPrimitiveValueType, IValueTypeDefinition,
        IEquatable<PrimitiveValueType>
    {
        private static IPrimitiveValueType[] boxedTypes;

        static partial void StaticConstructor()
        {
            boxedTypes =
                new IPrimitiveValueType[(int)PrimitiveValueTypeKind._EnumEnd - (int)PrimitiveValueTypeKind._EnumStart];
            for (var i = 0; i < boxedTypes.Length; i++)
                boxedTypes[i] =
                    new PrimitiveValueType((PrimitiveValueTypeKind)(i + (int)PrimitiveValueTypeKind._EnumStart));
        }

        public static IPrimitiveValueType GetBoxed(PrimitiveValueTypeKind kind)
        {
            return boxedTypes[(int)kind - (int)PrimitiveValueTypeKind._EnumStart];
        }

        private IPrimitiveValueType GetBoxed()
        {
            return GetBoxed(Kind);
        }

        public PrimitiveValueTypeKind Kind { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return GetBoxed();
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return GetBoxed();
        }

        public bool Equals(PrimitiveValueType other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is PrimitiveValueType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Kind;
        }

        public bool ValidateEquals(IType other)
        {
            return other is PrimitiveValueType primitiveValueType && Equals(primitiveValueType);
        }

        public IDespecializedValueType Despecialize()
        {
            return GetBoxed();
        }

        public static bool operator ==(PrimitiveValueType left, PrimitiveValueType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PrimitiveValueType left, PrimitiveValueType right)
        {
            return !left.Equals(right);
        }

        [Ignore]
        public byte AlignmentRank =>
            Kind switch
            {
                PrimitiveValueTypeKind.String => 2,
                PrimitiveValueTypeKind.Char => 2,
                PrimitiveValueTypeKind.F64 => 3,
                PrimitiveValueTypeKind.F32 => 2,
                PrimitiveValueTypeKind.U64 => 3,
                PrimitiveValueTypeKind.S64 => 3,
                PrimitiveValueTypeKind.U32 => 2,
                PrimitiveValueTypeKind.S32 => 2,
                PrimitiveValueTypeKind.U16 => 1,
                PrimitiveValueTypeKind.S16 => 1,
                PrimitiveValueTypeKind.U8 => 0,
                PrimitiveValueTypeKind.S8 => 0,
                PrimitiveValueTypeKind.Bool => 0,
                _ => throw new ArgumentOutOfRangeException()
            };

        [Ignore]
        public ushort ElementSize =>
            Kind switch
            {
                PrimitiveValueTypeKind.String => 8,
                PrimitiveValueTypeKind.Char => 4,
                PrimitiveValueTypeKind.F64 => 8,
                PrimitiveValueTypeKind.F32 => 4,
                PrimitiveValueTypeKind.U64 => 8,
                PrimitiveValueTypeKind.S64 => 8,
                PrimitiveValueTypeKind.U32 => 4,
                PrimitiveValueTypeKind.S32 => 4,
                PrimitiveValueTypeKind.U16 => 2,
                PrimitiveValueTypeKind.S16 => 2,
                PrimitiveValueTypeKind.U8 => 1,
                PrimitiveValueTypeKind.S8 => 1,
                PrimitiveValueTypeKind.Bool => 1,
                _ => throw new ArgumentOutOfRangeException()
            };

        [Ignore]
        public uint FlattenedCount => Kind switch
        {
            PrimitiveValueTypeKind.String => 2,
            _ => 1
        };

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = Kind switch
            {
                PrimitiveValueTypeKind.String => ValueType.I32,
                PrimitiveValueTypeKind.Char => ValueType.I32,
                PrimitiveValueTypeKind.F64 => ValueType.F64,
                PrimitiveValueTypeKind.F32 => ValueType.F32,
                PrimitiveValueTypeKind.U64 => ValueType.I64,
                PrimitiveValueTypeKind.S64 => ValueType.I64,
                PrimitiveValueTypeKind.U32 => ValueType.I32,
                PrimitiveValueTypeKind.S32 => ValueType.I32,
                PrimitiveValueTypeKind.U16 => ValueType.I32,
                PrimitiveValueTypeKind.S16 => ValueType.I32,
                PrimitiveValueTypeKind.U8 => ValueType.I32,
                PrimitiveValueTypeKind.S8 => ValueType.I32,
                PrimitiveValueTypeKind.Bool => ValueType.I32,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (Kind is PrimitiveValueTypeKind.String) dest[1] = ValueType.I32;
        }
    }

    [GenerateFormatter]
    public partial class RecordType : IValueTypeDefinition
    {
        public ReadOnlyMemory<LabeledValueType> Fields { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            var fields = new IRecordField[Fields.Length];
            for (var i = 0; i < Fields.Span.Length; i++)
            {
                var field = Fields.Span[i];
                var resolvedType = context.Resolve(field.Type);

                fields[i] = new ResolvedRecordField(field.Label, resolvedType);
            }

            return new ResolvedRecordType(fields);
        }
    }

    public class ResolvedRecordType : IRecordType
    {
        public ResolvedRecordType(ReadOnlyMemory<IRecordField> fields)
        {
            Fields = fields;
        }

        public ReadOnlyMemory<IRecordField> Fields { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        public byte AlignmentRank
        {
            get
            {
                byte result = 0;
                foreach (var field in Fields.Span)
                {
                    var type = field.Type;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();
                    result = Math.Max(result, despecialized.AlignmentRank);
                }

                return result;
            }
        }

        public ushort ElementSize
        {
            get
            {
                uint s = 0;
                foreach (var field in Fields.Span)
                {
                    var type = field.Type;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();
                    s = Utils.ElementSizeAlignTo(s, despecialized.AlignmentRank);
                    s += despecialized.ElementSize;
                }

                if (s == 0) throw new InvalidOperationException();

                return checked((ushort)Utils.ElementSizeAlignTo(s, AlignmentRank));
            }
        }

        public uint FlattenedCount
        {
            get
            {
                uint count = 0;
                foreach (var field in Fields.Span)
                {
                    var type = field.Type;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();

                    count += despecialized.FlattenedCount;
                }

                return count;
            }
        }

        public void Flatten(Span<ValueType> dest)
        {
            foreach (var field in Fields.Span)
            {
                var type = field.Type;
                if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();

                var count = checked((int)despecialized.FlattenedCount);
                despecialized.Flatten(dest[..count]);
                dest = dest[count..];
            }
        }

        public bool ValidateEquals(IType other)
        {
            if (other is not IRecordType recordType) return false;
            if (Fields.Length != recordType.Fields.Length) return false;

            for (var i = 0; i < Fields.Length; i++)
            {
                var field = Fields.Span[i];
                var otherField = recordType.Fields.Span[i];
                if (field.Label != otherField.Label) return false;
                if (!field.Type.ValidateEquals(otherField.Type)) return false;
            }

            return true;
        }
    }

    public class ResolvedRecordField : IRecordField
    {
        public ResolvedRecordField(string label, IValueType type)
        {
            Label = label;
            Type = type;
        }

        public string Label { get; }
        public IValueType Type { get; }
    }

    [GenerateFormatter]
    public partial class VariantType : IValueTypeDefinition
    {
        public ReadOnlyMemory<VariantCase> Cases { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            var cases = new IVariantCase[Cases.Length];
            for (var i = 0; i < Cases.Span.Length; i++)
            {
                var @case = Cases.Span[i];
                var resolvedType = @case.Type == null ? null : context.Resolve(@case.Type);

                cases[i] = new ResolvedVariantCase(@case.Label, resolvedType);
            }

            return new ResolvedVariantType(cases);
        }
    }

    public class ResolvedVariantCase : IVariantCase
    {
        public ResolvedVariantCase(string label, IValueType? type)
        {
            Label = label;
            Type = type;
        }

        public string Label { get; }
        public IValueType? Type { get; }
    }

    public class ResolvedVariantType : IVariantType
    {
        public ResolvedVariantType(ReadOnlyMemory<IVariantCase> cases)
        {
            Cases = cases;
        }

        private byte MaxCaseAlignmentRank
        {
            get
            {
                byte s = 1;
                foreach (var @case in Cases.Span)
                {
                    var type = @case.Type;
                    if (type == null) continue;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();
                    s = Math.Max(s, despecialized.AlignmentRank);
                }

                return s;
            }
        }

        public ReadOnlyMemory<IVariantCase> Cases { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        public byte DiscriminantTypeSizeRank
        {
            get
            {
                var length = Cases.Length;
                var n = length >> 8;
                var d = 0;
                for (; n > 0; n >>= 1) d++;
                if ((length & ((1 << 8) - 1)) != 0) d++; // ceil

                return (d >> 3) switch
                {
                    0 or 1 => 1,
                    2 => 2,
                    3 => 4,
                    _ => throw new InvalidOperationException()
                };
            }
        }

        public byte AlignmentRank => Math.Max(DiscriminantTypeSizeRank, MaxCaseAlignmentRank);

        public ushort ElementSize
        {
            get
            {
                var s = checked((ushort)(1 << DiscriminantTypeSizeRank));
                s = checked((ushort)Utils.ElementSizeAlignTo(s, MaxCaseAlignmentRank));
                ushort cs = 0;
                foreach (var @case in Cases.Span)
                {
                    var type = @case.Type;
                    if (type == null) continue;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();
                    cs = Math.Max(cs, despecialized.ElementSize);
                }

                s += cs;
                return checked((ushort)Utils.ElementSizeAlignTo(s, AlignmentRank));
            }
        }

        public uint FlattenedCount
        {
            get
            {
                uint count = 0;
                foreach (var @case in Cases.Span)
                {
                    var type = @case.Type;
                    if (type == null) continue;
                    if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();

                    count = Math.Max(count, despecialized.FlattenedCount);
                }

                return count + 1 /* discriminant */;
            }
        }

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32; // discriminant
            dest = dest[1..];

            Span<ValueType> tempTypes = stackalloc ValueType[dest.Length];
            uint count = 0;
            foreach (var @case in Cases.Span)
            {
                var type = @case.Type;
                if (type == null) continue;
                if (type is not IDespecializedValueType despecialized) despecialized = type.Despecialize();
                var tempCount = checked((int)despecialized.FlattenedCount);
                var tempTypesSliced = tempTypes[..tempCount];
                despecialized.Flatten(tempTypesSliced);

                for (var i = 0; i < tempTypesSliced.Length; i++)
                {
                    var valueType = tempTypesSliced[i];
                    if (i < count)
                    {
                        // join
                        ref var existing = ref dest[i];
                        if (existing == valueType) continue;
                        if ((existing is ValueType.I32 && valueType is ValueType.F32) ||
                            (existing is ValueType.F32 && valueType is ValueType.I32)) existing = ValueType.I32;
                        else existing = ValueType.I64;
                    }
                    else
                    {
                        dest[i] = valueType;
                        count++;
                    }
                }
            }
        }

        public bool ValidateEquals(IType other)
        {
            if (other is not IVariantType variantType) return false;
            if (Cases.Length != variantType.Cases.Length) return false;

            for (var i = 0; i < Cases.Length; i++)
            {
                var @case = Cases.Span[i];
                var otherCase = variantType.Cases.Span[i];
                if (@case.Type == null && otherCase.Type == null) continue;
                if (@case.Type == null || otherCase.Type == null) return false;
                if (!@case.Type.ValidateEquals(otherCase.Type)) return false;
            }

            return true;
        }
    }

    [GenerateFormatter]
    public partial class ListType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType ElementType { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedListType(context.Resolve(ElementType));
        }
    }

    public class ResolvedListType : IListType
    {
        public ResolvedListType(IValueType elementType)
        {
            ElementType = elementType;
        }

        public IValueType ElementType { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        public byte AlignmentRank => 2;
        public ushort ElementSize => 8;
        public uint FlattenedCount => 2;

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32;
            dest[1] = ValueType.I32;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IListType listType && ElementType.ValidateEquals(listType.ElementType);
        }
    }

    [GenerateFormatter]
    public partial class TupleType : IValueTypeDefinition
    {
        [DontAddToSort] public ReadOnlyMemory<IUnresolvedValueType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            var resolvedTypes = new IValueType[Type.Length];
            for (var i = 0; i < Type.Span.Length; i++) resolvedTypes[i] = context.Resolve(Type.Span[i]);

            return new ResolvedTupleType(resolvedTypes);
        }
    }

    public class ResolvedTupleType : ITupleType
    {
        public ResolvedTupleType(ReadOnlyMemory<IValueType> cases)
        {
            Cases = cases;
        }

        private IDespecializedValueType? Despecialized { get; set; }

        public ReadOnlyMemory<IValueType> Cases { get; }

        public IDespecializedValueType Despecialize()
        {
            if (Despecialized == null)
            {
                var fields = new ResolvedRecordField[Cases.Length];
                for (var i = 0; i < Cases.Span.Length; i++)
                {
                    var @case = Cases.Span[i];
                    fields[i] = new ResolvedRecordField(i.ToString(), @case);
                }

                Despecialized = new ResolvedRecordType(fields);
            }

            return Despecialized;
        }

        public bool ValidateEquals(IType other)
        {
            if (other is not ITupleType tupleType) return false;
            if (Cases.Length != tupleType.Cases.Length) return false;

            for (var i = 0; i < Cases.Length; i++)
            {
                var @case = Cases.Span[i];
                var otherCase = tupleType.Cases.Span[i];
                if (!@case.ValidateEquals(otherCase)) return false;
            }

            return true;
        }
    }

    [GenerateFormatter]
    public partial class FlagsType : IValueTypeDefinition, IFlagsType
    {
        public ReadOnlyMemory<string> Labels { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        [Ignore]
        public byte AlignmentRank => Labels.Length switch
        {
            <= 8 => 0,
            > 8 and <= 16 => 1,
            _ => 2
        };

        [Ignore]
        public ushort ElementSize => Labels.Length switch
        {
            <= 8 => 1,
            > 8 and <= 16 => 2,
            _ => 4
        };

        [Ignore] public uint FlattenedCount => 1;

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IFlagsType flags && flags.Labels.Length == Labels.Length;
        }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return this;
        }

        public static FlagsType Create(ReadOnlyMemory<string> labels)
        {
            return new FlagsType(labels);
        }

        partial void Validate()
        {
            if (Labels.Length > 32) throw new NotSupportedException("Too many flags");
        }
    }

    [GenerateFormatter]
    public partial class EnumType : IValueTypeDefinition, IEnumType
    {
        [Ignore] private IDespecializedValueType? Despecialized { get; set; }
        public ReadOnlyMemory<string> Labels { get; }

        public IDespecializedValueType Despecialize()
        {
            if (Despecialized == null)
            {
                var cases = new IVariantCase[Labels.Length];
                for (var i = 0; i < Labels.Span.Length; i++)
                {
                    var label = Labels.Span[i];
                    cases[i] = new ResolvedVariantCase(label, null);
                }

                Despecialized = new ResolvedVariantType(cases);
            }

            return Despecialized;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IEnumType @enum && @enum.Labels.Length == Labels.Length;
        }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return this;
        }

        public static EnumType Create(ReadOnlyMemory<string> labels)
        {
            return new EnumType(labels);
        }
    }

    [GenerateFormatter]
    public partial class OptionType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType Type { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedOptionType(context.Resolve(Type));
        }
    }

    public class ResolvedOptionType : IOptionType
    {
        public ResolvedOptionType(IValueType type)
        {
            Type = type;
        }

        private IDespecializedValueType? Despecialized { get; set; }

        public IValueType Type { get; }

        public IDespecializedValueType Despecialize()
        {
            if (Despecialized == null)
            {
                var cases = new IVariantCase[2];
                cases[0] = new ResolvedVariantCase("none", null);
                cases[1] = new ResolvedVariantCase("some", Type);

                Despecialized = new ResolvedVariantType(cases);
            }

            return Despecialized;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IOptionType optionType && Type.ValidateEquals(optionType.Type);
        }
    }

    [GenerateFormatter]
    public partial class ResultType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType? Type { get; }
        [DontAddToSort] public IUnresolvedValueType? ErrorType { get; }


        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedResultType(Type != null ? context.Resolve(Type) : null,
                ErrorType != null ? context.Resolve(ErrorType) : null);
        }
    }

    public class ResolvedResultType : IResultType
    {
        public ResolvedResultType(IValueType? type, IValueType? errorType)
        {
            Type = type;
            ErrorType = errorType;
        }

        private IDespecializedValueType? Despecialized { get; set; }

        public IValueType? Type { get; }
        public IValueType? ErrorType { get; }

        public IDespecializedValueType Despecialize()
        {
            if (Despecialized == null)
            {
                var cases = new IVariantCase[2];
                cases[0] = new ResolvedVariantCase("ok", Type);
                cases[1] = new ResolvedVariantCase("error", ErrorType);

                Despecialized = new ResolvedVariantType(cases);
            }

            return Despecialized;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IResultType resultType &&
                   ((Type == null && resultType.Type == null) ||
                    (Type != null && resultType.Type != null &&
                     Type.ValidateEquals(resultType.Type) &&
                     ((ErrorType == null && resultType.ErrorType == null) ||
                      (ErrorType != null && resultType.ErrorType != null &&
                       ErrorType.ValidateEquals(resultType.ErrorType)))));
        }
    }

    [GenerateFormatter]
    public partial class OwnedType : IValueTypeDefinition
    {
        public IUnresolved<IType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedOwnedType(context.Resolve(Type) as IResourceType ?? throw new LinkException());
        }
    }

    public class ResolvedOwnedType : IOwnedType
    {
        public ResolvedOwnedType(IResourceType type)
        {
            Type = type;
        }

        public IResourceType Type { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        public byte AlignmentRank => 2;
        public ushort ElementSize => 4;
        public uint FlattenedCount => 1;

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IOwnedType ownedType && Type.ValidateEquals(ownedType.Type);
        }
    }

    [GenerateFormatter]
    public partial class BorrowedType : IValueTypeDefinition
    {
        public IUnresolved<IType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedBorrowedType(context.Resolve(Type) as IResourceType ?? throw new LinkException());
        }
    }

    public class ResolvedBorrowedType : IBorrowedType
    {
        public ResolvedBorrowedType(IResourceType type)
        {
            Type = type;
        }

        public IResourceType Type { get; }

        public IDespecializedValueType Despecialize()
        {
            return this;
        }

        public byte AlignmentRank => 2;
        public ushort ElementSize => 4;
        public uint FlattenedCount => 1;

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32;
        }

        public bool ValidateEquals(IType other)
        {
            return other is IBorrowedType borrowedType && Type.ValidateEquals(borrowedType.Type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct LabeledValueType : IEquatable<LabeledValueType>
    {
        public string Label { get; }
        [DontAddToSort] public IUnresolvedValueType Type { get; }

        public bool Equals(LabeledValueType other)
        {
            return Label == other.Label && Type.Equals(other.Type);
        }

        public override bool Equals(object? obj)
        {
            return obj is LabeledValueType other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Label, Type);
        }

        public static bool operator ==(LabeledValueType left, LabeledValueType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LabeledValueType left, LabeledValueType right)
        {
            return !left.Equals(right);
        }
    }

    [GenerateFormatter]
    public readonly partial struct VariantCase
    {
        public string Label { get; }
        [DontAddToSort] public IUnresolvedValueType? Type { get; }
        private byte Unknown0 { get; }
    }

    public interface IUnresolvedValueType : IUnresolved<IValueType>
    {
        static IUnresolvedValueType()
        {
            Formatter<IUnresolvedValueType>.Default = new Formatter();
        }

        private class Formatter : IFormatter<IUnresolvedValueType>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out IUnresolvedValueType result)
            {
                var first = reader.Clone().ReadUnaligned<byte>();
                if (first >> 6 == 1)
                {
                    // negative one-byte SLEB128S
                    result = Formatter<PrimitiveValueType>.Read(ref reader, indexSpace, false);
                    return true;
                }

                var type = indexSpace.Get<IType>(reader.ReadUnalignedLeb128U32());

                result = new UnresolvedValueType(type);
                return true;
            }
        }

        private class UnresolvedValueType : IUnresolvedValueType
        {
            private readonly IUnresolved<IType> core;

            public UnresolvedValueType(IUnresolved<IType> core)
            {
                this.core = core;
            }

            public IValueType ResolveFirstTime(IInstantiationContext context)
            {
                return context.Resolve(core) as IValueType ?? throw new InvalidModuleException("Expected IValueType");
            }
        }
    }

    [GenerateFormatter]
    [Variant(0x3F, typeof(ResourceType))]
    public partial interface IResourceTypeDefinition : IUnresolved<IResourceType>, ITypeDefinition
    {
    }

    [GenerateFormatter]
    public partial class ResourceType : IResourceTypeDefinition
    {
        private byte Representation { get; }
        private IUnresolved<ICoreSortedExportable<IInvocableFunction>>? Destructor { get; }

        public IResourceType ResolveFirstTime(IInstantiationContext context)
        {
            if (Representation != 0x7F)
                throw new NotSupportedException(
                    $"This type of resource representation is not supported: 0x{Representation:X}");
            return new ResolvedResourceType(Destructor != null ? context.Resolve(Destructor).CoreExternal : null,
                context.Instance ?? throw new InvalidOperationException());
        }

        IType IUnresolved<IType>.ResolveFirstTime(IInstantiationContext context)
        {
            return ResolveFirstTime(context);
        }

        private class ResolvedResourceType : IResourceType
        {
            [ThreadStatic] private static ExecutionContext? contextForDestructor;
            private readonly IInvocableFunction? destructor;
            private readonly ResourceTable<uint> table = new();

            public ResolvedResourceType(IInvocableFunction? destructor, IInstance instance)
            {
                this.destructor = destructor;
                Instance = instance;
            }

            public uint New(uint rep)
            {
                var index = unchecked((uint)table.Add(rep));
                // Console.WriteLine($"new resource {index}: {rep}");
                return index;
            }

            public void Drop(uint index)
            {
                var rep = table.RemoveAt(unchecked((int)index));

                if (destructor != null)
                {
                    var repValue = new StackValueItem(rep);
                    contextForDestructor ??= new ExecutionContext();
                    contextForDestructor.InterruptFrame(destructor, MemoryMarshal.CreateSpan(ref repValue, 1),
                        Span<StackValueItem>.Empty);
                }

                // Console.WriteLine($"drop resource {index}: {rep}");
            }

            public uint Rep(uint index)
            {
                var rep = table.Get(unchecked((int)index));
                // Console.WriteLine($"rep resource {index}: {rep}");
                return rep;
            }

            public IInstance? Instance { get; }

            public bool ValidateEquals(IType other)
            {
                return other is IResourceType resourceType && ReferenceEquals(this, resourceType);
            }
        }
    }

    [GenerateFormatter]
    public partial class FunctionType : ITypeDefinition
    {
        public ReadOnlyMemory<LabeledValueType> Parameters { get; }
        public IResultList Result { get; }

        public IType ResolveFirstTime(IInstantiationContext context)
        {
            var parameters = new ResolvedParameter[Parameters.Length];
            for (var i = 0; i < Parameters.Span.Length; i++)
            {
                var parameter = Parameters.Span[i];
                var resolvedType = context.Resolve(parameter.Type);

                parameters[i] = new ResolvedParameter(parameter.Label, resolvedType);
            }

            return new ResolvedFunctionType(parameters, Result.ResolveFirstTime(context));
        }
    }

    public class ResolvedParameter : IParameter
    {
        public ResolvedParameter(string label, IValueType type)
        {
            Label = label;
            Type = type;
        }

        public string Label { get; }
        public IValueType Type { get; }
    }

    public class ResolvedFunctionType : IFunctionType
    {
        private IRecordType? parameterType;

        public ResolvedFunctionType(ReadOnlyMemory<IParameter> parameters, IValueType? result)
        {
            Parameters = parameters;
            Result = result;
        }

        public ReadOnlyMemory<IParameter> Parameters { get; }
        public IValueType? Result { get; }

        public IRecordType ParameterType
        {
            get
            {
                if (parameterType == null)
                {
                    var fields = new ResolvedRecordField[Parameters.Length];
                    var parameters = Parameters.Span;
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var parameter = parameters[i];
                        fields[i] = new ResolvedRecordField(parameter.Label, parameter.Type);
                    }

                    parameterType = new ResolvedRecordType(fields);
                }

                return parameterType;
            }
        }

        public bool ValidateEquals(IType other)
        {
            if (other is not IFunctionType functionType) return false;
            if (Parameters.Length != functionType.Parameters.Length) return false;

            for (var i = 0; i < Parameters.Length; i++)
            {
                var parameter = Parameters.Span[i];
                var otherParameter = functionType.Parameters.Span[i];
                if (!parameter.Type.ValidateEquals(otherParameter.Type)) return false;
            }

            if (Result == null && functionType.Result == null) return true;
            if (Result == null || functionType.Result == null) return false;
            return Result.ValidateEquals(functionType.Result);
        }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(ResultListSingle))]
    [Variant(0x01, typeof(ResultListNone))]
    public partial interface IResultList
    {
        IValueType? ResolveFirstTime(IInstantiationContext context);
    }

    [GenerateFormatter]
    public readonly partial struct ResultListSingle : IResultList
    {
        [DontAddToSort] public IUnresolvedValueType Type { get; init; }

        public IValueType? ResolveFirstTime(IInstantiationContext context)
        {
            return context.Resolve(Type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ResultListNone : IResultList
    {
        private byte Unknown0 { get; init; }

        public IValueType? ResolveFirstTime(IInstantiationContext context)
        {
            return null;
        }
    }

    public class ComponentType : ITypeDefinition
    {
        static ComponentType()
        {
            Formatter<ComponentType>.Default = new Formatter();
        }

        public ComponentType(IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> imports,
            IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> exports)
        {
            Imports = imports;
            Exports = exports;
        }

        public IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> Imports { get; }
        public IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> Exports { get; }

        public IType ResolveFirstTime(IInstantiationContext context)
        {
            return new ResolvedComponentType();
        }

        private class Formatter : IFormatter<ComponentType>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out ComponentType result)
            {
                var newIndexSpace = new IndexSpace(indexSpace);
                var numDecls = reader.ReadVectorSize();
                Dictionary<string, IExportableDescriptor<ISortedExportable>> imports = new();
                Dictionary<string, IExportableDescriptor<ISortedExportable>> exports = new();
                for (var i = 0; i < numDecls; i++)
                {
                    var decl = Formatter<IComponentDeclarator>.Read(ref reader, newIndexSpace);

                    if (decl is IImportDeclarator<ISortedExportable> importDeclaration)
                        imports.Add(importDeclaration.ImportName.Name, importDeclaration.Descriptor);
                    else if (decl is IExportDeclarator<ISortedExportable> exportDeclaration)
                        exports.Add(exportDeclaration.ImportName.Name, exportDeclaration.Descriptor);
                }

                result = new ComponentType(imports, exports);
                return true;
            }
        }

        private class ResolvedComponentType : IComponentType
        {
            // TODO
            public bool ValidateEquals(IType other)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InstanceType : ITypeDefinition, IInstanceType
    {
        static InstanceType()
        {
            Formatter<InstanceType>.Default = new Formatter();
        }

        public InstanceType(IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> exports)
        {
            Exports = exports;
        }

        public IReadOnlyDictionary<string, IExportableDescriptor<ISortedExportable>> Exports { get; }

        public bool ValidateEquals(IType other)
        {
            throw new NotImplementedException();
        }

        public IType ResolveFirstTime(IInstantiationContext context)
        {
            return this;
        }

        private class Formatter : IFormatter<InstanceType>
        {
            public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace, out InstanceType result)
            {
                var newIndexSpace = new IndexSpace(indexSpace);
                var numDecls = reader.ReadVectorSize();
                Dictionary<string, IExportableDescriptor<ISortedExportable>> exports = new();
                for (var i = 0; i < numDecls; i++)
                {
                    var decl = Formatter<IInstanceDeclarator>.Read(ref reader, newIndexSpace);

                    if (decl is IExportDeclarator<ISortedExportable> exportDeclaration)
                        exports.Add(exportDeclaration.ImportName.Name, exportDeclaration.Descriptor);
                }

                result = new InstanceType(exports);
                return true;
            }
        }
    }

    [GenerateFormatter]
    [Variant(0x03, typeof(IImportDeclarator<ISortedExportable>))]
    [VariantFallback(typeof(IInstanceDeclarator))]
    public partial interface IComponentDeclarator
    {
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(InstanceDeclaratorCoreType))]
    [Variant(0x01, typeof(InstanceDeclaratorType))]
    [Variant(0x02, typeof(InstanceDeclaratorAlias))]
    [Variant(0x04, typeof(IExportDeclarator<ISortedExportable>))]
    public partial interface IInstanceDeclarator : IComponentDeclarator
    {
    }

    [GenerateFormatter]
    public readonly partial struct InstanceDeclaratorCoreType : IInstanceDeclarator
    {
        public ICoreTypeDefinition CoreType { get; }
    }

    [GenerateFormatter]
    public readonly partial struct InstanceDeclaratorType : IInstanceDeclarator
    {
        public ITypeDefinition Type { get; }
    }


    [GenerateFormatter]
    public readonly partial struct InstanceDeclaratorAlias : IInstanceDeclarator
    {
        private Alias Alias { get; }
    }

    public interface IImportDeclarator<out T> : IComponentDeclarator, IUnresolved<T> where T : ISortedExportable
    {
        static IImportDeclarator()
        {
            Formatter<IImportDeclarator<ISortedExportable>>.Default = new ImportDeclaratorFormatter();
        }

        ImportExportName ImportName { get; }
        IExportableDescriptor<T> Descriptor { get; }
    }

    public readonly struct ImportDeclarator<T> : IImportDeclarator<T> where T : ISortedExportable
    {
        public ImportExportName ImportName { get; }
        public IExportableDescriptor<T> Descriptor { get; init; }

        internal ImportDeclarator(ImportExportName exportName, IExportableDescriptor<T> descriptor)
        {
            ImportName = exportName;
            Descriptor = descriptor;
        }

        public T ResolveFirstTime(IInstantiationContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal class ImportDeclaratorFormatter : IFormatter<IImportDeclarator<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out IImportDeclarator<ISortedExportable> result)
        {
            var name = Formatter<ImportExportName>.Read(ref reader, indexSpace);
            var desc = Formatter<IExportableDescriptor<ISortedExportable>>.Read(ref reader, indexSpace);
            result = desc switch
            {
                IExportableDescriptor<ICoreModule> descTyped => new ImportDeclarator<ICoreModule>(name, descTyped),
                IExportableDescriptor<ICoreType> descTyped => new ImportDeclarator<ICoreType>(name, descTyped),
                IExportableDescriptor<IType> descTyped => new ImportDeclarator<IType>(name, descTyped),
                IExportableDescriptor<IFunction> descTyped => new ImportDeclarator<IFunction>(name, descTyped),
                IExportableDescriptor<IComponent> descTyped => new ImportDeclarator<IComponent>(name, descTyped),
                IExportableDescriptor<IInstance> descTyped => new ImportDeclarator<IInstance>(name, descTyped),
                _ => throw new InvalidOperationException()
            };
            return true;
        }
    }

    public interface IExportDeclarator<out T> : IInstanceDeclarator, IUnresolved<T> where T : ISortedExportable
    {
        static IExportDeclarator()
        {
            Formatter<IExportDeclarator<ISortedExportable>>.Default = new ExportDeclaratorFormatter();
        }

        ImportExportName ImportName { get; }
        IExportableDescriptor<T> Descriptor { get; }
    }

    internal class ExportDeclaratorFormatter : IFormatter<IExportDeclarator<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out IExportDeclarator<ISortedExportable> result)
        {
            var name = Formatter<ImportExportName>.Read(ref reader, indexSpace);
            var desc = Formatter<IExportableDescriptor<ISortedExportable>>.Read(ref reader, indexSpace);
            result = desc switch
            {
                IExportableDescriptor<ICoreModule> descTyped => new ExportDeclarator<ICoreModule>(name, descTyped),
                IExportableDescriptor<ICoreType> descTyped => new ExportDeclarator<ICoreType>(name, descTyped),
                IExportableDescriptor<IType> descTyped => new ExportDeclarator<IType>(name, descTyped),
                IExportableDescriptor<IFunction> descTyped => new ExportDeclarator<IFunction>(name, descTyped),
                IExportableDescriptor<IComponent> descTyped => new ExportDeclarator<IComponent>(name, descTyped),
                IExportableDescriptor<IInstance> descTyped => new ExportDeclarator<IInstance>(name, descTyped),
                _ => throw new InvalidOperationException()
            };
            return true;
        }
    }

    public readonly struct ExportDeclarator<T> : IExportDeclarator<T> where T : ISortedExportable
    {
        public ImportExportName ImportName { get; }
        public IExportableDescriptor<T> Descriptor { get; init; }

        internal ExportDeclarator(ImportExportName exportName, IExportableDescriptor<T> descriptor)
        {
            ImportName = exportName;
            Descriptor = descriptor;
        }

        public T ResolveFirstTime(IInstantiationContext context)
        {
            return context.ResolveExport(this);
        }
    }

    public interface IExportableDescriptor<out T> where T : ISortedExportable
    {
        static IExportableDescriptor()
        {
            Formatter<IExportableDescriptor<ISortedExportable>>.Default = new ExportableDescriptorFormatter();
        }

        bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument);
    }

    internal static class ExportableDescriptorExtensions
    {
        public static bool ValidateExported(this IExportableDescriptor<ISortedExportable> desc,
            IInstantiationContext context, IInstance? instance, string name)
        {
            ISortedExportable? result = null;
            instance?.TryGetExport(name, out result);
            return desc.ValidateArgument(context, result);
        }
    }

    internal class ExportableDescriptorFormatter : IFormatter<IExportableDescriptor<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            [NotNullWhen(true)] out IExportableDescriptor<ISortedExportable>? result)
        {
            var tag = reader.Clone().ReadUnaligned<byte>();
            switch (tag)
            {
                case 0x00:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorCoreModuleType>.Read(ref reader, indexSpace);
                    return true;
                case 0x01:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorFunction>.Read(ref reader, indexSpace);
                    return true;
                case 0x03:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorType>.Read(ref reader, indexSpace);
                    return true;
                case 0x04:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorComponent>.Read(ref reader, indexSpace);
                    return true;
                case 0x05:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorInstance>.Read(ref reader, indexSpace);
                    return true;
            }

            result = default;
            return false;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorCoreModuleType : IExportableDescriptor<ICoreModule>
    {
        private byte Unknown0 { get; }
        public IUnresolved<ICoreType> Type { get; }

        partial void Validate()
        {
            if (Unknown0 != 0x11) throw new InvalidModuleException();
        }

        public bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument)
        {
            if (argument is not ICoreModule module) return false;
            var type = context.Resolve(Type);
            if (type is not ICoreModuleType moduleType) return false;
            return module.Validate(context, moduleType);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorFunction : IExportableDescriptor<IFunction>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument)
        {
            if (argument is not IFunction typeTyped) return false;
            var type = context.Resolve(Type);
            return type.ValidateEquals(typeTyped.Type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorType : IExportableDescriptor<IType>
    {
        [DontAddToSort] public ITypeBound Type { get; }

        public bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument)
        {
            return argument is IType type && Type.ValidateBound(context, type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorComponent : IExportableDescriptor<IComponent>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument)
        {
            return true;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorInstance : IExportableDescriptor<IInstance>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(IInstantiationContext context, ISortedExportable? argument)
        {
            var type = context.Resolve(Type);
            if (type is not IInstanceType instanceType) return false;
            var exports = instanceType.Exports;
            if (!exports.Any() && argument is null) return true;

            if (argument is not null && argument is not IInstance) return false;
            var instance = argument as IInstance;

            context = new InstantiationContext(null, context, null, instance);
            foreach (var (key, desc) in exports)
                if (!desc.ValidateExported(context, (IInstance?)argument, key))
                    return false;

            return true;
        }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(TypeBoundEquals))]
    [Variant(0x01, typeof(TypeBoundSubResource))]
    public partial interface ITypeBound
    {
        bool ValidateBound(IInstantiationContext context, IType type);
    }

    [GenerateFormatter]
    public readonly partial struct TypeBoundEquals : ITypeBound
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateBound(IInstantiationContext context, IType type)
        {
            var resolved = context.Resolve(Type);
            return resolved.ValidateEquals(type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct TypeBoundSubResource : ITypeBound
    {
        public bool ValidateBound(IInstantiationContext context, IType? type)
        {
            return type is IResourceType;
        }
    }
}