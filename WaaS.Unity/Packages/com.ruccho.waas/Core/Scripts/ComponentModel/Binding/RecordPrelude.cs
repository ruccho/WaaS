#nullable enable

namespace WaaS.ComponentModel.Binding
{
    /// <summary>
    ///     Prelude for a record in Canonical ABI.
    /// </summary>
    public struct RecordPrelude
    {
        public Pullable BodyPullable { get; init; }
    }
}