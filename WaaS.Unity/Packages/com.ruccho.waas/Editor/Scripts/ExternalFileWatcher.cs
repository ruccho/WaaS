#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    /// <summary>
    ///     Reimports the assets when the files outside the asset database that they were built from change, such as the
    ///     sources of the crates referenced by the path in Rust scripts. The files are recorded by
    ///     <see cref="ExternalFileDependency.Track" /> with their timestamps and sizes at the import, and compared while the
    ///     editor is active if Auto Refresh is enabled in the preferences (Asset Pipeline). Otherwise they are compared
    ///     with <c>Assets &gt; Reimport Scripts with Changed Files for WaaS</c>.
    /// </summary>
    [InitializeOnLoad]
    internal static class ExternalFileWatcher
    {
        private const string StorePath = "Library/com.ruccho.waas/external-files.tsv";
        private const double IntervalSeconds = 1;

        // asset path -> external file -> the state of the file at the import
        private static readonly Dictionary<string, Dictionary<string, FileState>> Dependencies = new();
        private static bool isDirty;
        private static double lastCheckTime;

        static ExternalFileWatcher()
        {
            Load();
            EditorApplication.update += Update;
        }

        public static void BeginImport(string assetPath)
        {
            Dependencies.Remove(assetPath);
            isDirty = true;
        }

        public static void Record(string assetPath, string path)
        {
            if (!Dependencies.TryGetValue(assetPath, out var files)) Dependencies[assetPath] = files = new Dictionary<string, FileState>();
            files[path] = FileState.Of(path);
            isDirty = true;
        }

        private static void Update()
        {
            if (isDirty)
            {
                isDirty = false;
                Save();
            }

            if (EditorApplication.isUpdating || EditorApplication.isCompiling) return;
            // the files are usually changed in other applications, and checked when the editor gets the focus
            if (!InternalEditorUtility.isApplicationActive || !IsAutoRefreshEnabled()) return;
            if (EditorApplication.timeSinceStartup - lastCheckTime < IntervalSeconds) return;
            lastCheckTime = EditorApplication.timeSinceStartup;

            ReimportChangedAssets();
        }

        // the reimports follow Auto Refresh of the preferences, so that the scripts are not built while it is disabled
        private static bool IsAutoRefreshEnabled()
        {
            // the values of AssetPipelineAutoRefreshMode: 0 disabled, 1 enabled, 2 enabled outside play mode
            var mode = EditorPrefs.GetInt("kAutoRefreshMode", EditorPrefs.GetBool("kAutoRefresh", true) ? 1 : 0);
            return mode == 1 || (mode == 2 && !EditorApplication.isPlayingOrWillChangePlaymode);
        }

        /// <summary>
        ///     Reimports the assets whose external files changed since their imports.
        /// </summary>
        [MenuItem("Assets/Reimport Scripts with Changed Files for WaaS")]
        public static void ReimportChangedAssets()
        {
            var changedAssets = new List<string>();
            foreach (var (assetPath, files) in Dependencies)
            {
                var changed = false;
                foreach (var path in files.Keys.ToArray())
                {
                    var state = FileState.Of(path);
                    if (state.Equals(files[path])) continue;
                    // updated before the reimport, so that a failed import is not repeated
                    files[path] = state;
                    changed = true;
                }

                if (changed) changedAssets.Add(assetPath);
            }

            if (changedAssets.Count == 0) return;
            isDirty = true;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var assetPath in changedAssets)
                {
                    Debug.Log($"[WaaS] Reimporting {assetPath} because the files it depends on changed.");
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void Load()
        {
            if (!File.Exists(StorePath)) return;
            try
            {
                foreach (var line in File.ReadAllLines(StorePath))
                {
                    var columns = line.Split('\t');
                    if (columns.Length != 4) continue;
                    if (!Dependencies.TryGetValue(columns[0], out var files))
                        Dependencies[columns[0]] = files = new Dictionary<string, FileState>();
                    files[columns[1]] = new FileState(long.Parse(columns[2]), long.Parse(columns[3]));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WaaS] Failed to read {StorePath}: {ex.Message}");
                Dependencies.Clear();
            }
        }

        private static void Save()
        {
            var builder = new StringBuilder();
            foreach (var (assetPath, files) in Dependencies)
            foreach (var (path, state) in files)
                builder.Append(assetPath).Append('\t').Append(path).Append('\t').Append(state.Ticks).Append('\t')
                    .Append(state.Length).Append('\n');

            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            File.WriteAllText(StorePath, builder.ToString());
        }

        private readonly struct FileState : IEquatable<FileState>
        {
            public readonly long Ticks;
            public readonly long Length;

            public FileState(long ticks, long length)
            {
                Ticks = ticks;
                Length = length;
            }

            public static FileState Of(string path)
            {
                var info = new FileInfo(path);
                return info.Exists ? new FileState(info.LastWriteTimeUtc.Ticks, info.Length) : new FileState(0, -1);
            }

            public bool Equals(FileState other)
            {
                return Ticks == other.Ticks && Length == other.Length;
            }
        }
    }
}
