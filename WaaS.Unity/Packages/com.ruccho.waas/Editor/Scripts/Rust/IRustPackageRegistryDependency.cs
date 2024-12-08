#nullable enable
using System.Collections.Generic;

namespace WaaS.Unity.Editor.Rust
{
    public interface IRustPackageRegistryDependency : IRustPackageDependency
    {
        new IEnumerable<IVersionRequirement> VersionRequirements { get; }
        string? Registry { get; }
    }
}