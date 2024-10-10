#nullable enable

namespace WaaS.ComponentModel.Binding
{
    public readonly struct VariantPrelude
    {
        public Pullable BodyPullable { get; init; }
        public int CaseIndex { get; init; }
    }
}