#nullable enable

namespace WaaS.ComponentModel.Binding
{
    public readonly struct ListPrelude
    {
        public Pullable ElementPullable { get; init; }
        public int Length { get; init; }
    }
}