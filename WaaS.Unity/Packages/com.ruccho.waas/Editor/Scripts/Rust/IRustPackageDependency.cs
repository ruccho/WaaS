#nullable enable
using System.Collections.Generic;

namespace WaaS.Unity.Editor.Rust
{
    public interface IRustPackageDependency
    {
        string Name { get; }
        IEnumerable<IVersionRequirement>? VersionRequirements { get; }
        string[]? Features { get; }
    }
}