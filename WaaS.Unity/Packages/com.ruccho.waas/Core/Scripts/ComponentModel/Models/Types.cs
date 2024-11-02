#nullable enable

using System;
using System.Collections.Generic;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    [Variant(0x40, typeof(FunctionType))]
    [Variant(0x41, typeof(ComponentType))]
    [Variant(0x42, typeof(InstanceType))]
    [VariantFallback(typeof(IValueTypeDefinition))]
    [VariantFallback(typeof(IResourceTypeDefinition))]
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

        private static IPrimitiveValueType GetBoxed(PrimitiveValueTypeKind kind)
        {
            return boxedTypes[(int)kind - (int)PrimitiveValueTypeKind._EnumStart];
        }

        private IPrimitiveValueType GetBoxed()
        {
            return GetBoxed(Kind);
        }

        public PrimitiveValueTypeKind Kind { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return GetBoxed();
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
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

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
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
        public ReadOnlyMemory<Case> Cases { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
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
                for (; n >= 0; n >>= 1) d++;
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
    }

    [GenerateFormatter]
    public partial class ListType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType ElementType { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedListType(context.Resolve(ElementType));
        }
    }

    internal class ResolvedListType : IListType
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
        public ushort ElementSize => 4;
        public uint FlattenedCount => 2;

        public void Flatten(Span<ValueType> dest)
        {
            dest[0] = ValueType.I32;
            dest[1] = ValueType.I32;
        }
    }

    [GenerateFormatter]
    public partial class TupleType : IValueTypeDefinition
    {
        [DontAddToSort] public ReadOnlyMemory<IUnresolvedValueType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            var resolvedTypes = new IValueType[Type.Length];
            for (var i = 0; i < Type.Span.Length; i++) resolvedTypes[i] = context.Resolve(Type.Span[i]);

            return new ResolverTupleType(resolvedTypes);
        }
    }

    internal class ResolverTupleType : ITupleType
    {
        public ResolverTupleType(ReadOnlyMemory<IValueType> cases)
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

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
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

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return this;
        }

        internal static EnumType Create(ReadOnlyMemory<string> labels)
        {
            return new EnumType(labels);
        }
    }

    [GenerateFormatter]
    public partial class OptionType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType Type { get; init; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedOptionType(context.Resolve(Type));
        }
    }

    internal class ResolvedOptionType : IOptionType
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
    }

    [GenerateFormatter]
    public partial class ResultType : IValueTypeDefinition
    {
        [DontAddToSort] public IUnresolvedValueType? Type { get; }
        [DontAddToSort] public IUnresolvedValueType? ErrorType { get; }


        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedResultType(context.Resolve(Type), context.Resolve(ErrorType));
        }

        private class ResolvedResultType : IResultType
        {
            public ResolvedResultType(IValueType type, IValueType errorType)
            {
                Type = type;
                ErrorType = errorType;
            }

            private IDespecializedValueType? Despecialized { get; set; }

            public IValueType Type { get; }
            public IValueType ErrorType { get; }

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
        }
    }

    [GenerateFormatter]
    public partial class OwnedType : IValueTypeDefinition
    {
        public IUnresolved<IType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedOwnedType(context.Resolve(Type) as IResourceType ?? throw new LinkException());
        }

        private class ResolvedOwnedType : IOwnedType
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
        }
    }

    [GenerateFormatter]
    public partial class BorrowedType : IValueTypeDefinition
    {
        public IUnresolved<IType> Type { get; }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ((IUnresolved<IValueType>)this).ResolveFirstTime(context);
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedBorrowedType(context.Resolve(Type) as IResourceType ?? throw new LinkException());
        }

        private class ResolvedBorrowedType : IBorrowedType
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
        }
    }

    [GenerateFormatter]
    public readonly partial struct LabeledValueType
    {
        public string Label { get; }
        [DontAddToSort] public IUnresolvedValueType Type { get; }
    }

    [GenerateFormatter]
    public readonly partial struct Case
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

                if (type is not IValueTypeDefinition valueTypeDef) throw new InvalidModuleException(); // validation

                result = valueTypeDef;
                return true;
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
        private byte Unknown0 { get; init; }
        public IUnresolved<IFunction>? Destructor { get; init; }

        public IResourceType ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedResourceType(Destructor != null ? context.Resolve(Destructor) : null);
        }

        IType IUnresolved<IType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            return ResolveFirstTime(context);
        }

        private class ResolvedResourceType : IResourceType
        {
            public ResolvedResourceType(IFunction? destructor)
            {
                Destructor = destructor;
            }

            public IFunction? Destructor { get; }
        }
    }

    [GenerateFormatter]
    public partial class FunctionType : ITypeDefinition
    {
        public ReadOnlyMemory<LabeledValueType> Parameters { get; }
        public IResultList Result { get; }

        public IType ResolveFirstTime(IInstanceResolutionContext context)
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
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(ResultListSingle))]
    [Variant(0x01, typeof(ResultListNone))]
    public partial interface IResultList
    {
        IValueType? ResolveFirstTime(IInstanceResolutionContext context);
    }

    [GenerateFormatter]
    public readonly partial struct ResultListSingle : IResultList
    {
        [DontAddToSort] public IUnresolvedValueType Type { get; init; }

        public IValueType? ResolveFirstTime(IInstanceResolutionContext context)
        {
            return context.Resolve(Type);
        }
    }

    [GenerateFormatter]
    public readonly partial struct ResultListNone : IResultList
    {
        private byte Unknown0 { get; init; }

        public IValueType? ResolveFirstTime(IInstanceResolutionContext context)
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

        public IType ResolveFirstTime(IInstanceResolutionContext context)
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

                    if (decl is ImportDeclarator importDeclaration)
                        imports.Add(importDeclaration.ImportName.Name, importDeclaration.Descriptor);
                    else if (decl is ExportDeclarator exportDeclaration)
                        exports.Add(exportDeclaration.ExportName.Name, exportDeclaration.Descriptor);
                }

                result = new ComponentType(imports, exports);
                return true;
            }
        }

        private class ResolvedComponentType : IComponentType
        {
            // TODO
        }
    }

    public class InstanceType : ITypeDefinition
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

        public IType ResolveFirstTime(IInstanceResolutionContext context)
        {
            return new ResolvedInstanceType();
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

                    if (decl is ExportDeclarator exportDeclaration)
                        exports.Add(exportDeclaration.ExportName.Name, exportDeclaration.Descriptor);
                }

                result = new InstanceType(exports);
                return true;
            }
        }

        private class ResolvedInstanceType : IInstanceType
        {
            // TODO
        }
    }

    [GenerateFormatter]
    [Variant(0x03, typeof(ImportDeclarator))]
    [VariantFallback(typeof(IInstanceDeclarator))]
    public partial interface IComponentDeclarator
    {
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(InstanceDeclaratorCoreType))]
    [Variant(0x01, typeof(InstanceDeclaratorType))]
    [Variant(0x02, typeof(InstanceDeclaratorAlias))]
    [Variant(0x04, typeof(InstanceDeclaratorExportDecl))]
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


    [GenerateFormatter]
    public readonly partial struct InstanceDeclaratorExportDecl : IInstanceDeclarator
    {
        public ExportDeclarator Declaration { get; }
    }


    [GenerateFormatter]
    public readonly partial struct ImportDeclarator : IComponentDeclarator
    {
        public ImportExportName ImportName { get; }
        public IExportableDescriptor<ISortedExportable> Descriptor { get; init; }
    }

    [GenerateFormatter]
    public readonly partial struct ExportDeclarator : IInstanceDeclarator
    {
        public ImportExportName ExportName { get; }
        public IExportableDescriptor<ISortedExportable> Descriptor { get; init; }
    }

    public interface IExportableDescriptor<out T> where T : ISortedExportable
    {
        static IExportableDescriptor()
        {
            Formatter<IExportableDescriptor<ISortedExportable>>.Default = new ExportableDescriptorFormatter();
        }

        bool ValidateArgument(ISortedExportable argument);
    }

    internal class ExportableDescriptorFormatter : IFormatter<IExportableDescriptor<ISortedExportable>>
    {
        public bool TryRead(ref ModuleReader reader, IIndexSpace indexSpace,
            out IExportableDescriptor<ISortedExportable> result)
        {
            var tag = reader.Clone().ReadUnaligned<byte>();
            switch (tag)
            {
                case 0x00:
                    reader.ReadUnaligned<byte>();
                    result = Formatter<ExportableDescriptorCoreModule>.Read(ref reader, indexSpace);
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
    public readonly partial struct ExportableDescriptorCoreModule : IExportableDescriptor<ICoreModule>
    {
        private byte Unknown0 { get; }
        public ICoreModuleDeclaration Type { get; }

        public bool ValidateArgument(ISortedExportable argument)
        {
            return argument is ICoreModule;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorFunction : IExportableDescriptor<IFunction>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(ISortedExportable argument)
        {
            return argument is IFunction;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorType : IExportableDescriptor<IType>
    {
        public ITypeBound Type { get; }

        public bool ValidateArgument(ISortedExportable argument)
        {
            return argument is IType;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorComponent : IExportableDescriptor<IComponent>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(ISortedExportable argument)
        {
            return argument is IComponent;
        }
    }

    [GenerateFormatter]
    public readonly partial struct ExportableDescriptorInstance : IExportableDescriptor<IInstance>
    {
        public IUnresolved<IType> Type { get; }

        public bool ValidateArgument(ISortedExportable argument)
        {
            return argument is IInstance;
        }
    }

    [GenerateFormatter]
    [Variant(0x00, typeof(TypeBoundEquals))]
    [Variant(0x01, typeof(TypeBoundSubResource))]
    public partial interface ITypeBound : IValueTypeDefinition
    {
    }

    [GenerateFormatter]
    public readonly partial struct TypeBoundEquals : ITypeBound
    {
        public IUnresolved<IType> Type { get; }

        public IType ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }

    [GenerateFormatter]
    public readonly partial struct TypeBoundSubResource : ITypeBound
    {
        public IType ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
        }

        IValueType IUnresolved<IValueType>.ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}