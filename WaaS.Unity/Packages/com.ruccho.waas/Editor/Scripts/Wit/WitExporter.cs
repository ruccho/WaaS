#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using WaaS.ComponentModel.Binding;
using WaaS.Unity.Editor.MoonBit;

namespace WaaS.Unity.Editor.Wit
{
    /// <summary>
    ///     Exports WIT files generated from C# interfaces according to <see cref="WitExportSettings" />.
    ///     Runs after every script compilation. Rust scripts that read the exported files are reimported when they change.
    /// </summary>
    [InitializeOnLoad]
    public static class WitExporter
    {
        private static readonly UTF8Encoding Utf8NoBom = new(false);

        static WitExporter()
        {
            // EditorApplication.delayCall may not be invoked while the editor is in the background,
            // so EditorApplication.update is used instead.
            EditorApplication.update += ExportWhenReady;
        }

        private static void ExportWhenReady()
        {
            // the asset database cannot find the settings while importing, e.g. when the editor is starting
            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;

            EditorApplication.update -= ExportWhenReady;
            ExportAll(false);
        }

        [MenuItem("Assets/Export WIT for WaaS")]
        private static void ExportFromMenu()
        {
            ExportAll(true);
        }

        public static void ExportAll(bool log)
        {
            var changed = ExportWit(log);

            // the MoonBit bindings are generated from the exported WIT
            changed |= MoonBitBindingsExporter.ExportAll(log);

            // reimport the assets depending on the changed files
            if (changed) AssetDatabase.Refresh();
        }

        private static bool ExportWit(bool log)
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(WitExportSettings)}");
            if (guids.Length == 0) return false;

            IReadOnlyDictionary<string, string> packages;
            try
            {
                packages = WitPackageBuilder.Build(TypeCache.GetTypesWithAttribute<ComponentWitDefinitionAttribute>());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

            var changed = false;
            foreach (var guid in guids)
            {
                var settings = AssetDatabase.LoadAssetAtPath<WitExportSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null) continue;
                changed |= Export(settings, packages, log);
            }

            return changed;
        }

        private static bool Export(WitExportSettings settings, IReadOnlyDictionary<string, string> packages, bool log)
        {
            var changed = false;
            foreach (var package in settings.Packages)
            {
                if (string.IsNullOrEmpty(package)) continue;
                if (!packages.TryGetValue(package, out var source))
                {
                    Debug.LogWarning($"[WaaS] WIT package \"{package}\" is not defined by any C# interface.", settings);
                    continue;
                }

                var directoryName = WitPackageBuilder.GetDirectoryName(package);
                var directory = Path.Combine(settings.OutputDirectory, directoryName);
                var path = Path.Combine(directory, $"{directoryName}.wit");

                if (!File.Exists(path) || File.ReadAllText(path, Utf8NoBom) != source)
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path, source, Utf8NoBom);
                    changed = true;
                    if (log) Debug.Log($"[WaaS] Exported WIT: {path}");
                }

                ExternalFileDependency.Register(path);
            }

            return changed;
        }
    }
}
