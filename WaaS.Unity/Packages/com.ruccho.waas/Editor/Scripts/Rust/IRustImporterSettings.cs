#nullable enable

using System.Collections.Generic;

namespace WaaS.Unity.Editor.Rust
{
    public interface IRustImporterSettings
    {
        IEnumerable<IRustPackageDependency> Dependencies { get; }
        ComponentizationSettings? ComponentizationSettings { get; }
    }
}