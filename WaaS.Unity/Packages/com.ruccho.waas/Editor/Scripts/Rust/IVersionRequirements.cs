#nullable enable
namespace WaaS.Unity.Editor.Rust
{
    public interface IVersionRequirement
    {
        VersionRequirementKind Kind { get; }
        string Version { get; }
    }
}