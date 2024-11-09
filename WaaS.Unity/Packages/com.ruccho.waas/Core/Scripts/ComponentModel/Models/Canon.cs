#nullable enable

using System;
using WaaS.ComponentModel.Runtime;
using WaaS.Models;
using WaaS.Runtime;

namespace WaaS.ComponentModel.Models
{
    [GenerateFormatter]
    [Variant(0x00, typeof(CanonLift))]
    [Variant(0x01, typeof(CanonLower))]
    [Variant(0x02, typeof(CanonResourceNew))]
    [Variant(0x03, typeof(CanonResourceDrop))]
    // TODO: other canons
    public partial interface ICanon
    {
    }

    [GenerateFormatter]
    public partial class CanonLift : ICanon, IUnresolved<IFunction>
    {
        private byte Unknown0 { get; }
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> CoreFunction { get; }
        public ReadOnlyMemory<ICanonOption> Options { get; }
        public IUnresolved<IType> FunctionType { get; }

        public IFunction ResolveFirstTime(IInstanceResolutionContext context)
        {
            var type = context.Resolve(FunctionType);
            if (type is not IFunctionType functionType)
                throw new InvalidModuleException($"referenced type by canon lift is not a function type: {type}");

            Memory? memory = null;
            IInvocableFunction? postReturn = null;
            IInvocableFunction? realloc = null;
            CanonOptionStringEncodingKind? stringEncoding = null;
            foreach (var canonOption in Options.Span)
                switch (canonOption)
                {
                    case CanonOptionAsync:
                    case CanonOptionCallback:
                        break;
                    case CanonOptionMemory canonOptionMemory:
                        if (memory != null) throw new InvalidModuleException();
                        memory = context.Resolve(canonOptionMemory.Memory).CoreExternal;
                        break;
                    case CanonOptionPostReturn canonOptionPostReturn:
                        if (postReturn != null) throw new InvalidModuleException();
                        postReturn = context.Resolve(canonOptionPostReturn.Function).CoreExternal;
                        break;
                    case CanonOptionRealloc canonOptionRealloc:
                        if (realloc != null) throw new InvalidModuleException();
                        realloc = context.Resolve(canonOptionRealloc.Function).CoreExternal;
                        break;
                    case CanonOptionStringEncoding canonOptionStringEncoding:
                        if (stringEncoding != null) throw new InvalidModuleException();
                        stringEncoding = canonOptionStringEncoding.Kind;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(canonOption));
                }

            stringEncoding ??= CanonOptionStringEncodingKind.Utf8;

            return new LiftedFunction(context.Resolve(CoreFunction).CoreExternal, functionType, stringEncoding.Value,
                realloc,
                postReturn, memory);
        }
    }

    [GenerateFormatter]
    public partial class CanonLower : ICanon, IUnresolved<ICoreSortedExportable<IInvocableFunction>>
    {
        private byte Unknown0 { get; }
        public IUnresolved<IFunction> Function { get; }
        public ReadOnlyMemory<ICanonOption> Options { get; }

        public ICoreSortedExportable<IInvocableFunction> ResolveFirstTime(IInstanceResolutionContext context)
        {
            var componentFunction = context.Resolve(Function);

            Memory? memory = null;
            IInvocableFunction? realloc = null;
            CanonOptionStringEncodingKind? stringEncoding = null;
            foreach (var canonOption in Options.Span)
                switch (canonOption)
                {
                    case CanonOptionAsync:
                    case CanonOptionCallback:
                        break;
                    case CanonOptionMemory canonOptionMemory:
                        if (memory != null) throw new InvalidModuleException();
                        memory = context.Resolve(canonOptionMemory.Memory).CoreExternal;
                        break;
                    case CanonOptionRealloc canonOptionRealloc:
                        if (realloc != null) throw new InvalidModuleException();
                        realloc = context.Resolve(canonOptionRealloc.Function).CoreExternal;
                        break;
                    case CanonOptionStringEncoding canonOptionStringEncoding:
                        if (stringEncoding != null) throw new InvalidModuleException();
                        stringEncoding = canonOptionStringEncoding.Kind;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(canonOption));
                }

            stringEncoding ??= CanonOptionStringEncodingKind.Utf8;

            return new LoweredFunction(stringEncoding.Value, realloc, memory, componentFunction);
        }
    }

    [GenerateFormatter]
    public partial class CanonResourceNew : ICanon, IUnresolved<ICoreSortedExportable<IInvocableFunction>>
    {
        public IUnresolved<IType> ResourceType { get; }

        public ICoreSortedExportable<IInvocableFunction> ResolveFirstTime(IInstanceResolutionContext context)
        {
            var resourceType = context.Resolve(ResourceType) as IResourceType ?? throw new InvalidModuleException();
            return new NewFunction(resourceType);
        }

        private class NewFunction : ExternalFunctionDelegate, ICoreSortedExportable<IInvocableFunction>
        {
            public NewFunction(IResourceType type) : base(static (state, parameters, results) =>
            {
                var rep = parameters[0].ExpectValueI32();
                var type = state as IResourceType ?? throw new InvalidOperationException();
                results[0] = new StackValueItem(type.New(rep));
            }, type, new WaaS.Models.FunctionType(new[] { ValueType.I32 }, new[] { ValueType.I32 }))
            {
            }

            public IInvocableFunction CoreExternal => this;
        }
    }

    [GenerateFormatter]
    public partial class CanonResourceDrop : ICanon, IUnresolved<ICoreSortedExportable<IInvocableFunction>>
    {
        public IUnresolved<IType> ResourceType { get; }

        public ICoreSortedExportable<IInvocableFunction> ResolveFirstTime(IInstanceResolutionContext context)
        {
            var resourceType = context.Resolve(ResourceType) as IResourceType ?? throw new InvalidModuleException();
            return new DropFunction(resourceType);
        }

        private class DropFunction : ExternalFunction, ICoreSortedExportable<IInvocableFunction>
        {
            private static readonly WaaS.Models.FunctionType DropFunctionType =
                new(new[] { ValueType.I32 }, Array.Empty<ValueType>());

            private readonly IResourceType type;

            public DropFunction(IResourceType type)
            {
                this.type = type;
            }

            public override WaaS.Models.FunctionType Type => DropFunctionType;
            public IInvocableFunction CoreExternal => this;

            public override void Invoke(ExecutionContext context, ReadOnlySpan<StackValueItem> parameters,
                Span<StackValueItem> results)
            {
                type.Drop(parameters[0].ExpectValueI32());
            }
        }
    }

    [GenerateFormatter]
    [Variant(0x03, typeof(CanonOptionMemory))]
    [Variant(0x04, typeof(CanonOptionRealloc))]
    [Variant(0x05, typeof(CanonOptionPostReturn))]
    [Variant(0x06, typeof(CanonOptionAsync))]
    [Variant(0x07, typeof(CanonOptionCallback))]
    [VariantFallback(typeof(CanonOptionStringEncoding))]
    public partial interface ICanonOption
    {
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionStringEncoding : ICanonOption, IEquatable<CanonOptionStringEncoding>
    {
        public CanonOptionStringEncodingKind Kind { get; }

        public bool Equals(CanonOptionStringEncoding other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionStringEncoding other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Kind;
        }

        public static bool operator ==(CanonOptionStringEncoding left, CanonOptionStringEncoding right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionStringEncoding left, CanonOptionStringEncoding right)
        {
            return !left.Equals(right);
        }
    }

    public enum CanonOptionStringEncodingKind : byte
    {
        Utf8,
        Utf16,
        Latin1Utf16
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionMemory : ICanonOption, IEquatable<CanonOptionMemory>
    {
        public IUnresolved<ICoreSortedExportable<Memory>> Memory { get; }

        public bool Equals(CanonOptionMemory other)
        {
            return Memory.Equals(other.Memory);
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionMemory other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Memory.GetHashCode();
        }

        public static bool operator ==(CanonOptionMemory left, CanonOptionMemory right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionMemory left, CanonOptionMemory right)
        {
            return !left.Equals(right);
        }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionRealloc : ICanonOption, IEquatable<CanonOptionRealloc>
    {
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> Function { get; }

        public bool Equals(CanonOptionRealloc other)
        {
            return Function.Equals(other.Function);
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionRealloc other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }

        public static bool operator ==(CanonOptionRealloc left, CanonOptionRealloc right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionRealloc left, CanonOptionRealloc right)
        {
            return !left.Equals(right);
        }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionPostReturn : ICanonOption, IEquatable<CanonOptionPostReturn>
    {
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> Function { get; }

        public bool Equals(CanonOptionPostReturn other)
        {
            return Function.Equals(other.Function);
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionPostReturn other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }

        public static bool operator ==(CanonOptionPostReturn left, CanonOptionPostReturn right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionPostReturn left, CanonOptionPostReturn right)
        {
            return !left.Equals(right);
        }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionAsync : ICanonOption, IEquatable<CanonOptionAsync>
    {
        public bool Equals(CanonOptionAsync other)
        {
            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionAsync other && Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public static bool operator ==(CanonOptionAsync left, CanonOptionAsync right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionAsync left, CanonOptionAsync right)
        {
            return !left.Equals(right);
        }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionCallback : ICanonOption, IEquatable<CanonOptionCallback>
    {
        public IUnresolved<ICoreSortedExportable<Memory>> Function { get; }

        public bool Equals(CanonOptionCallback other)
        {
            return Function.Equals(other.Function);
        }

        public override bool Equals(object? obj)
        {
            return obj is CanonOptionCallback other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }

        public static bool operator ==(CanonOptionCallback left, CanonOptionCallback right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CanonOptionCallback left, CanonOptionCallback right)
        {
            return !left.Equals(right);
        }
    }
}