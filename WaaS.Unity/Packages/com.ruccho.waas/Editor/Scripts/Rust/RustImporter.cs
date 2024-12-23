#if !WAAS_DISABLE_RUST_IMPORTER
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor.Rust
{
    [ScriptedImporter(1, "rs", 100)]
    public class RustImporter : ScriptedImporter
    {
        [SerializeField] private bool crateRoot = true;
        [SerializeField] private LazyLoadReference<RustImporterPreset> preset;
        [SerializeField] private RustImporterSettings? settings;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (!crateRoot)
            {
                ctx.SetMainObject(null);
                return;
            }

            EnsureWorkspaceCreated();

            var assetPath = ctx.assetPath;
            var hash = Hash128.Compute(assetPath);
            var cratePath = $"Library/com.ruccho.waas/rust/crates/waas_{hash}";
            Directory.CreateDirectory(cratePath);
            var cargoTomlPath = Path.Combine(cratePath, "Cargo.toml");

            var cargoTomlBuilder = new StringBuilder();

            cargoTomlBuilder.AppendLine(
                $"[package]\nname = \"waas_{hash}\"\nedition = \"2021\"\n\n[lib]\ncrate-type = [\"cdylib\"]\npath = \"../../../../../{assetPath}\"");
            cargoTomlBuilder.AppendLine("[dependencies]");

            RustImporterSettings settings;
            if (preset.isSet &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(preset, out var presetGuid, out var presetLocalId))
            {
                ctx.DependsOnArtifact(new GUID(presetGuid));
                AssetDatabase.LoadAssetAtPath<RustImporterPreset>(AssetDatabase.GUIDToAssetPath(presetGuid));
                settings = preset.asset.settings;
            }
            else
            {
                settings = this.settings ?? new RustImporterSettings();
            }

            foreach (var dependency in settings.Dependencies)
            {
                cargoTomlBuilder.Append(dependency.Name);
                cargoTomlBuilder.Append(" = {");
                var hasVersion = false;
                if (dependency.VersionRequirements is { } versionRequirements)
                {
                    cargoTomlBuilder.Append(" version = \"");
                    foreach (var versionRequirement in versionRequirements)
                    {
                        if (hasVersion) cargoTomlBuilder.Append(", ");

                        hasVersion = true;
                        _ = versionRequirement.Kind switch
                        {
                            VersionRequirementKind.Caret => cargoTomlBuilder.Append('^'),
                            VersionRequirementKind.Tilde => cargoTomlBuilder.Append('~'),
                            VersionRequirementKind.GreaterThan => cargoTomlBuilder.Append('>'),
                            VersionRequirementKind.GreaterThanOrEqual => cargoTomlBuilder.Append(">="),
                            VersionRequirementKind.LessThan => cargoTomlBuilder.Append('<'),
                            VersionRequirementKind.LessThanOrEqual => cargoTomlBuilder.Append("<="),
                            VersionRequirementKind.Equal => cargoTomlBuilder.Append('='),
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        cargoTomlBuilder.Append(versionRequirement.Version);
                    }

                    if (!hasVersion) cargoTomlBuilder.Append("*");
                    hasVersion = true;

                    cargoTomlBuilder.Append("\"");
                }

                switch (dependency)
                {
                    case IRustPackageRegistryDependency regDep:
                    {
                        if (regDep.Registry is { } registry)
                        {
                            if (hasVersion) cargoTomlBuilder.Append(", ");
                            cargoTomlBuilder.Append("registry = \"");
                            cargoTomlBuilder.Append(registry);
                            cargoTomlBuilder.Append("\"");
                        }

                        break;
                    }
                    case IRustPackageWorkspaceDependency:
                    {
                        if (hasVersion) cargoTomlBuilder.Append(", ");
                        cargoTomlBuilder.Append("workspace = true");
                        break;
                    }
                    case IRustPackageGitDependency gitDep:
                    {
                        if (hasVersion) cargoTomlBuilder.Append(", ");
                        cargoTomlBuilder.Append("git = \"");
                        cargoTomlBuilder.Append(gitDep.GitUrl);
                        cargoTomlBuilder.Append("\"");
                        if (gitDep.Branch is { } branch)
                        {
                            cargoTomlBuilder.Append(", branch = \"");
                            cargoTomlBuilder.Append(branch);
                            cargoTomlBuilder.Append("\"");
                        }

                        if (gitDep.Tag is { } tag)
                        {
                            cargoTomlBuilder.Append(", tag = \"");
                            cargoTomlBuilder.Append(tag);
                            cargoTomlBuilder.Append("\"");
                        }

                        if (gitDep.Rev is { } rev)
                        {
                            cargoTomlBuilder.Append(", rev = \"");
                            cargoTomlBuilder.Append(rev);
                            cargoTomlBuilder.Append("\"");
                        }

                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(dependency));
                }

                cargoTomlBuilder.AppendLine(" }");
            }

            File.WriteAllText(cargoTomlPath,
                cargoTomlBuilder.ToString(),
                Encoding.UTF8);

            var args =
                $@"build -p ""waas_{hash}"" --target wasm32-unknown-unknown --release --target-dir Library/com.ruccho.waas/rust/target --config ""build.dep-info-basedir=\"".\""""";
            var psi = new ProcessStartInfo(
#if UNITY_EDITOR_WIN
                "cargo", args
#else // in macOS, we need to run bash gracefully in order to set the PATH correctly
                "/bin/bash", $"-cl 'cargo {args}'"
#endif
            )
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);

            var messageQueue = new ConcurrentQueue<string>();

            void LogStream(StreamReader reader, StringBuilder builder)
            {
                Task.Run(async () =>
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        builder.AppendLine(line);
                        Console.WriteLine(line);
                        messageQueue.Enqueue(line);
                    }
                });
            }

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            LogStream(process.StandardOutput, standardOutput);
            LogStream(process.StandardError, standardError);

            var title = $"Compiling: {assetPath}";

            EditorUtility.DisplayProgressBar(title, title, 0f);
            try
            {
                while (!process.HasExited)
                {
                    string? message = null;
                    while (messageQueue.TryDequeue(out var dequeued)) message = dequeued;

                    if (message != null) EditorUtility.DisplayProgressBar(title, message, 0f);

                    Thread.Sleep(100);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (process.ExitCode != 0)
            {
                ctx.LogImportError($"Error importing {assetPath}: \n{standardError}\n\nstdout:\n{standardOutput}");
                return;
            }

            // TODO: get artifact path from stdout

            var outPath = $"Library/com.ruccho.waas/rust/target/wasm32-unknown-unknown/release/waas_{hash}.wasm";
            var depInfoPath = $"Library/com.ruccho.waas/rust/target/wasm32-unknown-unknown/release/waas_{hash}.d";

            var depInfo = File.ReadAllText(depInfoPath);

            static ReadOnlySpan<char> ReadSegment(ref ReadOnlySpan<char> content)
            {
                for (var i = 0; i < content.Length; i++)
                {
                    var c = content[i];
                    switch (c)
                    {
                        case '\\':
                        {
                            // escaped space
                            i++;
                            continue;
                        }
                        case ' ' or '\n':
                        {
                            var segment = content[..i];
                            content = content[(i + 1)..];
                            return segment;
                        }
                    }
                }

                return content;
            }

            var span = depInfo.AsSpan();
            var found = false;
            while (span.Length > 0)
            {
                var segment = ReadSegment(ref span).ToString();
                segment = segment.Replace("\\ ", " ");
                segment = segment.Replace("\\", "/");
                if (segment[^1] == ':')
                {
                    if (segment.AsSpan()[..^1].SequenceEqual(outPath.AsSpan()))
                        found = true;
                    else
                        found = false;
                }
                else if (found && segment != assetPath)
                {
                    ctx.DependsOnSourceAsset(segment);
                }
            }

            var moduleBytes = File.ReadAllBytes(outPath);

            if (settings.ComponentizationSettings is { } componentizationSettings)
            {
                var componentBytes = NativeMethods.Componentize(moduleBytes, componentizationSettings.WitDirectory,
                    componentizationSettings.World ?? "", StringEncoding.UTF8);
                var component = ScriptableObject.CreateInstance<ComponentAsset>();
                component.SetData(componentBytes);
                ctx.AddObjectToAsset("component", component);
                ctx.SetMainObject(component);
            }
            else
            {
                var module = ScriptableObject.CreateInstance<ModuleAsset>();
                module.SetData(moduleBytes);
                ctx.AddObjectToAsset("module", module);
                ctx.SetMainObject(module);
            }
        }

        private static void EnsureWorkspaceCreated()
        {
            if (File.Exists("Cargo.toml")) return;
            File.WriteAllText("Cargo.toml",
                "[workspace]\nmembers = [\"Library/com.ruccho.waas/rust/crates/*\"] # Used by WaaS\nresolver = \"2\" # Used by WaaS\n",
                Encoding.UTF8);
        }

        [MenuItem("Assets/Recompile Rust scripts for WaaS")]
        private static void Reimport()
        {
            foreach (var guid in AssetDatabase.FindAssets(@"glob:""**/*.rs"""))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        [MenuItem("Assets/Create/WaaS/Rust Script")]
        private static void CreateRust()
        {
            ProjectWindowUtil.CreateAssetWithContent("script.rs", "");
        }

        [CustomEditor(typeof(RustImporter))]
        private class Editor : ScriptedImporterEditor
        {
            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();

                var crateRootProp = serializedObject.FindProperty(nameof(crateRoot));
                EditorGUILayout.PropertyField(crateRootProp);

                if (crateRootProp.boolValue)
                {
                    var presetProp = serializedObject.FindProperty(nameof(preset));
                    EditorGUILayout.PropertyField(presetProp);

                    if (presetProp.objectReferenceValue == null)
                    {
                        var settingsProp = serializedObject.FindProperty(nameof(settings)).Copy();
                        var initialDepth = settingsProp.depth;
                        if (settingsProp.NextVisible(true))
                            do
                            {
                                if (settingsProp.depth <= initialDepth) break;
                                EditorGUILayout.PropertyField(settingsProp);
                            } while (settingsProp.NextVisible(false));
                    }
                }

                if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();

                ApplyRevertGUI();
            }
        }
    }
}
#endif