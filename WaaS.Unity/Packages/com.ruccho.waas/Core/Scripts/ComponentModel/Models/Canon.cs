#nullable enable

using System;
using WaaS.ComponentModel.Runtime;
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
            throw new NotImplementedException();
        }

        private class LiftFunction : IFunction
        {
            private ICoreSortedExportable<IInvocableFunction> CoreFunction { get; }
            private CanonOptionStringEncodingKind StringEncoding { get; }
            private ICoreSortedExportable<Memory>? CoreMemory { get; }
            private ICoreSortedExportable<IInvocableFunction>? ReallocFunction { get; }
            private ICoreSortedExportable<IInvocableFunction>? PostReturnFunction { get; }
        }
    }

    [GenerateFormatter]
    public partial class CanonLower : ICanon, IUnresolved<ICoreSortedExportable<IInvocableFunction>>
    {
        private byte Unknown0 { get; }
        public IUnresolved<ICoreSortedExportable<IInvocableFunction>> CoreFunction { get; }
        public ReadOnlyMemory<ICanonOption> Options { get; }

        public ICoreSortedExportable<IInvocableFunction> ResolveFirstTime(IInstanceResolutionContext context)
        {
            throw new NotImplementedException();
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