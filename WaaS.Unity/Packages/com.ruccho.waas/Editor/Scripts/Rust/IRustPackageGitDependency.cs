#nullable enable
namespace WaaS.Unity.Editor.Rust
{
    public interface IRustPackageGitDependency : IRustPackageDependency
    {
        string GitUrl { get; }
        string? Branch { get; }
        string? Tag { get; }
        string? Rev { get; }
    }
}