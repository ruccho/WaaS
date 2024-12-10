#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaaS.Unity.Editor.Rust
{
    [Serializable]
    public class RustImporterSettings : IRustImporterSettings
    {
        [SerializeReference] [ManagedReferenceSelector]
        private PackageDependency?[]? dependencies;

        [SerializeField] private bool componentize;
        [SerializeField] private ComponentizationSettings? componentizationSettings;

        public IEnumerable<IRustPackageDependency> Dependencies =>
            dependencies?.Where(dep => dep != null).Select(dep => dep!) ?? Enumerable.Empty<IRustPackageDependency>();

        public ComponentizationSettings? ComponentizationSettings => componentize ? componentizationSettings : null;

        [Serializable]
        private abstract class PackageDependency : IRustPackageDependency
        {
            [SerializeField] private string? name;
            [SerializeField] protected VersionRequirement[]? versionRequirement;
            [SerializeField] private string[]? features;

            public string Name => name ?? throw new InvalidOperationException();

            IEnumerable<IVersionRequirement>? IRustPackageDependency.VersionRequirements =>
                versionRequirement?.Length is 0 or null ? null : versionRequirement;

            public string[]? Features => features;
        }

        [Serializable]
        [ManagedReferenceTypeDisplayName("via Git")]
        private class PackageGitDependency : PackageDependency, IRustPackageGitDependency
        {
            [SerializeField] private string? gitUrl;
            [SerializeField] private string? branch;
            [SerializeField] private string? tag;
            [SerializeField] private string? rev;

            public string GitUrl => gitUrl ?? throw new InvalidOperationException();
            public string? Branch => branch;
            public string? Tag => tag;
            public string? Rev => rev;
        }

        [Serializable]
        [ManagedReferenceTypeDisplayName("via Registry")]
        private class PackageRegistryDependency : PackageDependency, IRustPackageRegistryDependency
        {
            [SerializeField] private string? registry;
            public string? Registry => string.IsNullOrEmpty(registry) ? null : registry;

            public IEnumerable<IVersionRequirement> VersionRequirements =>
                versionRequirement ?? throw new InvalidOperationException();
        }

        [Serializable]
        private class VersionRequirement : IVersionRequirement
        {
            [SerializeField] private VersionRequirementKind kind;
            [SerializeField] private string? version;

            public VersionRequirementKind Kind => kind;
            public string Version => version ?? throw new InvalidOperationException();
        }
    }
}