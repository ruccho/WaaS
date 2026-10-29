#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    [Serializable]
    public class MoonBitImporterSettings
    {
        [SerializeField] [Tooltip("Modules the script depends on.")]
        private ModuleDependency[]? dependencies;

        [SerializeField] [Tooltip("Packages the script imports.")]
        private PackageImport[]? imports;

        [SerializeField] private bool componentize = true;

        [SerializeField]
        [Tooltip(
            "Directories that contain the WIT packages imported by the component, each in its own subdirectory. Relative to the project root. The \"wit\" directories of the local modules are also used.")]
        private string[]? witDirectories;

        [SerializeReference] [ManagedReferenceSelector]
        private IMoonBitGenerator?[]? generators;

        public IEnumerable<ModuleDependency> Dependencies =>
            dependencies?.Where(d => d != null && !string.IsNullOrEmpty(d.Name)) ??
            Enumerable.Empty<ModuleDependency>();

        public IEnumerable<PackageImport> Imports =>
            imports?.Where(i => i != null && !string.IsNullOrEmpty(i.Path)) ?? Enumerable.Empty<PackageImport>();

        public bool Componentize => componentize;

        public IEnumerable<string> WitDirectories =>
            witDirectories?.Where(d => !string.IsNullOrEmpty(d)) ?? Enumerable.Empty<string>();

        public IEnumerable<IMoonBitGenerator> Generators =>
            generators?.Where(g => g != null).Select(g => g!) ?? Enumerable.Empty<IMoonBitGenerator>();

        [Serializable]
        public class ModuleDependency
        {
            [SerializeField] [Tooltip("Name of the module, such as \"my-name/my-module\".")]
            private string name = "";

            [SerializeField] [Tooltip("Version of the module on mooncakes.io. Ignored for a local module.")]
            private string version = "";

            [SerializeField]
            [Tooltip("Directory of the module on the local file system, relative to the project root. Empty to use mooncakes.io.")]
            private string localPath = "";

            public string Name => name;
            public string Version => version;
            public string? LocalPath => string.IsNullOrEmpty(localPath) ? null : localPath;
        }

        [Serializable]
        public class PackageImport
        {
            [SerializeField] [Tooltip("Path of the package, such as \"my-name/my-module/my-package\".")]
            private string path = "";

            [SerializeField] [Tooltip("Alias of the package in the script. Empty to use the last part of the path.")]
            private string alias = "";

            public string Path => path;
            public string? Alias => string.IsNullOrEmpty(alias) ? null : alias;
        }
    }
}
