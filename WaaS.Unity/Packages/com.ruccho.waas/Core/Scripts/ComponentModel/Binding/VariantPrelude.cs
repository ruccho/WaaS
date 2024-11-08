#nullable enable

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Prelude for a variant type in Canonical ABI.
    /// </summary>
    public readonly struct VariantPrelude
    {
        public Pullable BodyPullable { get; init; }
        public int CaseIndex { get; init; }
    }
}