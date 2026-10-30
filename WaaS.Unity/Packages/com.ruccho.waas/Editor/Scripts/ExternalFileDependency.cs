#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    /// <summary>
    ///     Tracks files outside the asset database (e.g. generated WIT files) as custom dependencies of imported assets.
    ///     An importer declares a dependency with <see cref="GetDependencyName" />, and the tool that writes the file
    ///     calls <see cref="Register" /> to update its hash, which reimports the dependent assets.
    /// </summary>
    public static class ExternalFileDependency
    {
        private const string Prefix = "waas-external-file:";

        /// <summary>
        ///     Returns the project-relative path of the asset if the path is in the asset database, otherwise null.
        /// </summary>
        public static string? ToAssetPath(string path)
        {
            var relative = ToProjectRelativePath(path);
            if (relative == null) return null;
            if (relative.StartsWith("Assets/", StringComparison.Ordinal) ||
                relative.StartsWith("Packages/", StringComparison.Ordinal))
                return relative;
            return null;
        }

        /// <summary>
        ///     Declares that the imported asset depends on the file. A file in the asset database is a source asset
        ///     dependency, and a file outside it is a custom dependency, which is also checked by its timestamp and size
        ///     so that the asset is reimported when the file changes without <see cref="Register" />.
        ///     <see cref="BeginImport" /> must be called at the start of the import.
        /// </summary>
        public static void Track(AssetImportContext ctx, string path)
        {
            if (ToAssetPath(path) is { } dependencyAssetPath)
            {
                ctx.DependsOnSourceAsset(dependencyAssetPath);
                return;
            }

            ctx.DependsOnCustomDependency(GetDependencyName(path));
            ExternalFileWatcher.Record(ctx.assetPath, ToFullPath(path));
        }

        /// <summary>
        ///     Forgets the files tracked by the previous import of the asset.
        /// </summary>
        public static void BeginImport(AssetImportContext ctx)
        {
            ExternalFileWatcher.BeginImport(ctx.assetPath);
        }

        public static string GetDependencyName(string path)
        {
            return Prefix + NormalizeFullPath(path);
        }

        /// <summary>
        ///     Updates the hash of the file. Assets depending on the file are reimported on the next refresh if the hash changed.
        /// </summary>
        public static void Register(string path)
        {
            var hash = File.Exists(path) ? Hash128.Compute(File.ReadAllBytes(path)) : default;
            AssetDatabase.RegisterCustomDependency(GetDependencyName(path), hash);
        }

        private static string ToFullPath(string path)
        {
            // dep-info files of cargo may contain extended-length paths
            if (path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith("//?/", StringComparison.Ordinal))
                path = path.Substring(4);

            return Path.GetFullPath(path).Replace('\\', '/');
        }

        private static string NormalizeFullPath(string path)
        {
            var fullPath = ToFullPath(path);
#if UNITY_EDITOR_WIN
            // paths are case-insensitive on Windows
            fullPath = fullPath.ToLowerInvariant();
#endif
            return fullPath;
        }

        private static string? ToProjectRelativePath(string path)
        {
            var projectRoot = NormalizeFullPath(Directory.GetCurrentDirectory()).TrimEnd('/') + "/";
            if (!NormalizeFullPath(path).StartsWith(projectRoot, StringComparison.Ordinal)) return null;

            // keep the original casing for asset paths
            return ToFullPath(path).Substring(projectRoot.Length);
        }
    }
}
