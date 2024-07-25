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

            return new LiftFunction(context.Resolve(CoreFunction).CoreExternal, functionType, stringEncoding.Value,
                realloc,
                postReturn, memory);
        }

        private class LiftFunction : IFunction
        {
            public LiftFunction(IInvocableFunction coreFunction, IFunctionType type,
                CanonOptionStringEncodingKind stringEncoding, IInvocableFunction? reallocFunction,
                IInvocableFunction? postReturnFunction, Memory? memoryToRealloc)
            {
                CoreFunction = coreFunction;
                Type = type;
                StringEncoding = stringEncoding;
                ReallocFunction = reallocFunction;
                PostReturnFunction = postReturnFunction;
                MemoryToRealloc = memoryToRealloc;
            }

            private CanonOptionStringEncodingKind StringEncoding { get; }
            private IInvocableFunction? PostReturnFunction { get; }
            public IInvocableFunction CoreFunction { get; }
            public IFunctionType Type { get; }
            public IInvocableFunction? ReallocFunction { get; }
            public Memory? MemoryToRealloc { get; }
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
            throw new NotImplementedException();
        }

        private class Resolved : ExternalFunction, ICoreSortedExportable<IInvocableFunction>
        {
            private ICoreSortedExportable<IInvocableFunction> CoreFunction { get; }
            private CanonOptionStringEncodingKind StringEncoding { get; }
            private ICoreSortedExportable<Memory>? CoreMemory { get; }
            private ICoreSortedExportable<IInvocableFunction>? ReallocFunction { get; }
            public override WaaS.Models.FunctionType Type { get; }

            public IInvocableFunction CoreExternal => this;

            public override void Invoke(ReadOnlySpan<StackValueItem> parameters, Span<StackValueItem> results)
            {
                throw new NotImplementedException();
            }
        }
    }

    [GenerateFormatter]
    public partial class CanonResourceDrop : ICanon, IUnresolved<ICoreSortedExportable<IInvocableFunction>>
    {
        public IUnresolved<IType> CoreFunction { get; }

        public ICoreSortedExportable<IInvocableFunction> ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
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
    public readonly partial struct CanonOptionStringEncoding : ICanonOption
    {
        public CanonOptionStringEncodingKind Kind { get; }
    }

    public enum CanonOptionStringEncodingKind : byte
    {
        Utf8,
        Utf16,
        Latin1Utf16
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionMemory : ICanonOption
    {
        public IUnresolved<ICoreSortedExportable<Memory>> Memory { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionRealloc : ICanonOption
    {
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> Function { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionPostReturn : ICanonOption
    {
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> Function { get; }
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionAsync : ICanonOption
    {
    }

    [GenerateFormatter]
    public readonly partial struct CanonOptionCallback : ICanonOption
    {
        public IUnresolved<ICoreSortedExportable<Memory>> Function { get; }
    }
}