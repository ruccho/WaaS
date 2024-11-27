#nullable enable

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Prelude for a list in Canonical ABI.
    /// </summary>
    public readonly struct ListPrelude
    {
        public Pullable ElementPullable { get; init; }
        public int Length { get; init; }
    }
}