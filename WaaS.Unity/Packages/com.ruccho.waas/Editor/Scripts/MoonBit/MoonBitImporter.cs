#if !WAAS_DISABLE_MOONBIT_IMPORTER
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using WaaS.Models;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Imports a MoonBit source file as a script, which is built with the MoonBit toolchain (<c>moon</c>) and
    ///     componentized. Each script is built in its own workspace under <c>Library/com.ruccho.waas/moonbit</c>.
    /// </summary>
    [ScriptedImporter(1, "mbt", 100)]
    public class MoonBitImporter : ScriptedImporter
    {
        private const string ModuleName = "waas/script";
        private const string ScriptPackage = ModuleName + "/script";
        private const string EntryPackage = ModuleName + "/main";
        private const string WorldName = "script";

        private static readonly UTF8Encoding Utf8NoBom = new(false);

        [SerializeField] [Tooltip("Whether the file is a script. Disable for a file of a MoonBit package.")]
        private bool script = true;

        [SerializeField] private LazyLoadReference<MoonBitImporterPreset> preset;
        [SerializeField] private MoonBitImporterSettings? settings;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var assetPath = ctx.assetPath;

            // a file of a MoonBit package (e.g. a library placed in the project) is not a script
            var directory = Path.GetDirectoryName(assetPath) ?? "";
            if (!script || File.Exists(Path.Combine(directory, "moon.pkg")) ||
                File.Exists(Path.Combine(directory, "moon.pkg.json")))
            {
                ctx.SetMainObject(null);
                return;
            }

            MoonBitImporterSettings settings;
            if (preset.isSet &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(preset, out var presetGuid, out _))
            {
                ctx.DependsOnArtifact(new GUID(presetGuid));
                AssetDatabase.LoadAssetAtPath<MoonBitImporterPreset>(AssetDatabase.GUIDToAssetPath(presetGuid));
                settings = preset.asset.Settings;
            }
            else
            {
                settings = this.settings ?? new MoonBitImporterSettings();
            }

            // the paths of the build are kept short for the limit of the path length on Windows
            var workspace = Path.GetFullPath($"Library/com.ruccho.waas/moonbit/{Hash128.Compute(assetPath).ToString().Substring(0, 12)}");
            var module = Path.Combine(workspace, "module");
            var scriptDirectory = Path.Combine(module, "script");
            var entryDirectory = Path.Combine(module, "main");
            var copiedScriptPath = Path.Combine(scriptDirectory, "script.mbt");

            Directory.CreateDirectory(scriptDirectory);
            if (Directory.Exists(entryDirectory)) Directory.Delete(entryDirectory, true);

            var source = File.ReadAllText(assetPath);
            var dependencies = settings.Dependencies.ToArray();
            WriteWorkspace(workspace, dependencies);
            WriteModule(module, dependencies);
            WriteIfChanged(copiedScriptPath, source);
            WriteIfChanged(Path.Combine(scriptDirectory, "moon.pkg"), GeneratePackage(settings.Imports
                .Select(i => (i.Path, i.Alias))));

            string MapPaths(string text)
            {
                // diagnostics refer to the copy of the script
                return text.Replace(copiedScriptPath, assetPath)
                    .Replace(copiedScriptPath.Replace('\\', '/'), assetPath);
            }

            foreach (var dependency in dependencies)
                if (dependency.LocalPath is { } localPath)
                    TrackDirectory(ctx, localPath);

            var generators = settings.Generators.ToArray();
            var sourceDirectories = new[] { scriptDirectory }
                .Concat(dependencies.Select(d => d.LocalPath).OfType<string>().Select(Path.GetFullPath))
                .ToArray();
            var context = new MoonBitGenerationContext(ctx, source, "", sourceDirectories);
            if (generators.Length > 0)
            {
                // the generators read the public interface of the script
                if (!RunMoon(ctx, assetPath, workspace, $"info \"{scriptDirectory}\"", MapPaths)) return;
                var interfacePath = Path.Combine(scriptDirectory, "pkg.generated.mbti");
                context = new MoonBitGenerationContext(ctx, source,
                    File.Exists(interfacePath) ? File.ReadAllText(interfacePath) : "", sourceDirectories);
                try
                {
                    foreach (var generator in generators) generator.Generate(context);
                }
                catch (Exception ex)
                {
                    ctx.LogImportError($"Error importing {assetPath}: {ex.Message}");
                    return;
                }
            }

            WriteEntryPackage(entryDirectory, context);

            if (!RunMoon(ctx, assetPath, workspace, $"build \"{entryDirectory}\" --target wasm --release", MapPaths))
                return;

            var outPath = Path.Combine(workspace, "_build", "wasm", "release", "build", ModuleName, "main",
                "main.wasm");
            if (!File.Exists(outPath))
            {
                ctx.LogImportError($"Error importing {assetPath}: the built module is not found at {outPath}");
                return;
            }

            var moduleBytes = File.ReadAllBytes(outPath);
            foreach (var (name, data) in context.customSections) moduleBytes = AppendCustomSection(moduleBytes, name, data);

            if (!settings.Componentize)
            {
                var moduleAsset = ScriptableObject.CreateInstance<ModuleAsset>();
                moduleAsset.SetData(moduleBytes);
                ctx.AddObjectToAsset("module", moduleAsset);
                ctx.SetMainObject(moduleAsset);
                return;
            }

            byte[] componentBytes;
            try
            {
                var witDirectory = Path.Combine(workspace, "wit");
                WriteWit(ctx, witDirectory, moduleBytes, settings, dependencies, context);
                componentBytes = NativeMethods.Componentize(moduleBytes, witDirectory, WorldName, StringEncoding.UTF16);
            }
            catch (Exception ex)
            {
                ctx.LogImportError($"Error componentizing {assetPath}: {ex.Message}");
                return;
            }

            var component = ScriptableObject.CreateInstance<ComponentAsset>();
            component.SetData(componentBytes);
            component.SetWit(ComponentWitDecoder.Decode(ctx, componentBytes));
            ctx.AddObjectToAsset("component", component);
            ctx.SetMainObject(component);
        }

        private static void WriteWorkspace(string workspace, IEnumerable<MoonBitImporterSettings.ModuleDependency> dependencies)
        {
            var builder = new StringBuilder("members = [\n  \"module\",\n");
            foreach (var dependency in dependencies)
            {
                if (dependency.LocalPath is not { } localPath) continue;
                var relative = Path.GetRelativePath(workspace, Path.GetFullPath(localPath)).Replace('\\', '/');
                builder.Append($"  \"{relative}\",\n");
            }

            builder.Append("]\n");
            WriteIfChanged(Path.Combine(workspace, "moon.work"), builder.ToString());
        }

        private static void WriteModule(string module, IEnumerable<MoonBitImporterSettings.ModuleDependency> dependencies)
        {
            var builder = new StringBuilder($"name = \"{ModuleName}\"\n\n");
            var imports = dependencies.ToArray();
            if (imports.Length > 0)
            {
                builder.Append("import {\n");
                foreach (var dependency in imports)
                {
                    // the version of a local module is ignored
                    var version = string.IsNullOrEmpty(dependency.Version) ? "0.0.0" : dependency.Version;
                    builder.Append($"  \"{dependency.Name}@{version}\",\n");
                }

                builder.Append("}\n\n");
            }

            builder.Append("preferred_target = \"wasm\"\n");
            WriteIfChanged(Path.Combine(module, "moon.mod"), builder.ToString());
        }

        private static string GeneratePackage(IEnumerable<(string Path, string? Alias)> imports)
        {
            var builder = new StringBuilder();
            var list = imports.ToArray();
            if (list.Length == 0) return "";

            builder.Append("import {\n");
            foreach (var (path, alias) in list)
                builder.Append(alias != null ? $"  \"{path}\" @{alias},\n" : $"  \"{path}\",\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        private static void WriteEntryPackage(string directory, MoonBitGenerationContext context)
        {
            Directory.CreateDirectory(directory);

            var builder = new StringBuilder(GeneratePackage(new (string, string?)[] { (ScriptPackage, "script") }
                .Concat(context.imports.Select(i => (i.Key, (string?)i.Value)))));
            // the runtime functions call the internal functions of MoonBit from inline wasm (warning 44)
            builder.Append("\nwarnings = \"-44\"\n");
            builder.Append("\npkgtype(kind: \"foreign_library\")\n\noptions(\n  link: {\n    \"wasm\": {\n");
            builder.Append("      \"export-memory-name\": \"memory\",\n      \"heap-start-address\": 16,\n");
            builder.Append("      \"exports\": [\n        \"waas_cabi_realloc:cabi_realloc\",\n");
            foreach (var (functionName, exportName) in context.exports)
                builder.Append($"        \"{functionName}:{exportName}\",\n");
            builder.Append("      ],\n    },\n  },\n)\n");
            File.WriteAllText(Path.Combine(directory, "moon.pkg"), builder.ToString(), Utf8NoBom);

            File.WriteAllText(Path.Combine(directory, "waas_runtime.mbt"), RuntimeSource, Utf8NoBom);
            foreach (var (fileName, source) in context.sources)
                File.WriteAllText(Path.Combine(directory, fileName), source, Utf8NoBom);
        }

        /// <summary>
        ///     Writes the WIT to componentize the module: a world that imports the interfaces imported by the module and
        ///     exports the interfaces added by the generators, and the WIT packages it depends on.
        /// </summary>
        private static void WriteWit(
            AssetImportContext ctx,
            string directory,
            byte[] moduleBytes,
            MoonBitImporterSettings settings,
            IEnumerable<MoonBitImporterSettings.ModuleDependency> dependencies,
            MoonBitGenerationContext context)
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            var deps = Path.Combine(directory, "deps");
            Directory.CreateDirectory(deps);

            var witDirectories = settings.WitDirectories.ToList();
            foreach (var dependency in dependencies)
                if (dependency.LocalPath is { } localPath && Directory.Exists(Path.Combine(localPath, "wit")))
                    witDirectories.Add(Path.Combine(localPath, "wit"));

            foreach (var witDirectory in witDirectories)
            {
                if (!Directory.Exists(witDirectory))
                    throw new DirectoryNotFoundException($"WIT directory {witDirectory} is not found.");
                foreach (var packageDirectory in Directory.GetDirectories(witDirectory))
                {
                    var target = Path.Combine(deps, Path.GetFileName(packageDirectory));
                    Directory.CreateDirectory(target);
                    foreach (var file in Directory.GetFiles(packageDirectory, "*.wit"))
                    {
                        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                        TrackFile(ctx, file);
                    }
                }
            }

            foreach (var (name, source) in context.witPackages)
            {
                var target = Path.Combine(deps, name);
                Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(target, $"{name}.wit"), source, Utf8NoBom);
            }

            var world = new StringBuilder($"package waas-moonbit:script;\n\nworld {WorldName} {{\n");
            var importSection = Module.Create(moduleBytes).ImportSection;
            var interfaces = importSection == null
                ? Enumerable.Empty<string>()
                : importSection.Imports.ToArray().Select(i => i.ModuleName).Distinct();
            foreach (var @interface in interfaces)
            {
                // imports of interfaces are named as "namespace:package/interface"
                if (!@interface.Contains(':') || !@interface.Contains('/')) continue;
                world.Append($"    import {@interface};\n");
            }

            foreach (var @interface in context.worldExports) world.Append($"    export {@interface};\n");
            world.Append("}\n");
            File.WriteAllText(Path.Combine(directory, "world.wit"), world.ToString(), Utf8NoBom);
        }

        private static byte[] AppendCustomSection(byte[] module, string name, byte[] data)
        {
            var nameBytes = Encoding.UTF8.GetBytes(name);
            var payload = new List<byte>();
            WriteLeb128(payload, (uint)nameBytes.Length);
            payload.AddRange(nameBytes);
            payload.AddRange(data);

            var result = new List<byte>(module) { 0x00 };
            WriteLeb128(result, (uint)payload.Count);
            result.AddRange(payload);
            return result.ToArray();
        }

        private static void WriteLeb128(List<byte> output, uint value)
        {
            do
            {
                var b = (byte)(value & 0x7F);
                value >>= 7;
                if (value != 0) b |= 0x80;
                output.Add(b);
            } while (value != 0);
        }

        private static void TrackDirectory(AssetImportContext ctx, string directory)
        {
            if (!Directory.Exists(directory)) return;
            foreach (var file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relative = file.Replace('\\', '/');
                if (relative.Contains("/_build/") || relative.Contains("/target/") || relative.Contains("/.mooncakes/"))
                    continue;
                var extension = Path.GetExtension(file);
                if (extension is ".mbt" or ".wit" || Path.GetFileName(file) is "moon.pkg" or "moon.mod")
                    TrackFile(ctx, file);
            }
        }

        private static void TrackFile(AssetImportContext ctx, string path)
        {
            // files outside the asset database are tracked as custom dependencies
            if (ExternalFileDependency.ToAssetPath(path) is { } dependencyAssetPath)
                ctx.DependsOnSourceAsset(dependencyAssetPath);
            else
                ctx.DependsOnCustomDependency(ExternalFileDependency.GetDependencyName(path));
        }

        private static void WriteIfChanged(string path, string contents)
        {
            if (File.Exists(path) && File.ReadAllText(path) == contents) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, Utf8NoBom);
        }

        private static bool RunMoon(AssetImportContext ctx, string assetPath, string workingDirectory, string args,
            Func<string, string> mapPaths)
        {
            var psi = new ProcessStartInfo(
#if UNITY_EDITOR_WIN
                "moon", args
#else // in macOS, we need to run bash gracefully in order to set the PATH correctly
                "/bin/bash", $"-cl 'moon {args.Replace("'", "'\\''")}'"
#endif
            )
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = workingDirectory
            };

            Process process;
            try
            {
                process = Process.Start(psi)!;
            }
            catch (Exception ex)
            {
                ctx.LogImportError(
                    $"Error importing {assetPath}: failed to run moon, the MoonBit toolchain. Make sure it is installed and on PATH.\n{ex.Message}");
                return false;
            }

            process.StandardInput.Close();

            var messageQueue = new ConcurrentQueue<string>();

            Task LogStream(StreamReader reader, StringBuilder builder)
            {
                return Task.Run(async () =>
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        lock (builder)
                        {
                            builder.AppendLine(line);
                        }

                        messageQueue.Enqueue(line);
                    }
                });
            }

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();
            var readers = Task.WhenAll(LogStream(process.StandardOutput, standardOutput),
                LogStream(process.StandardError, standardError));

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

                readers.Wait();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (process.ExitCode == 0) return true;

            ctx.LogImportError(
                $"Error importing {assetPath}: \n{mapPaths(standardError.ToString())}\n\nstdout:\n{mapPaths(standardOutput.ToString())}");
            return false;
        }

        /// <summary>
        ///     Functions for the canonical ABI, included in the entry package.
        /// </summary>
        private const string RuntimeSource = @"// Generated by WaaS. DO NOT EDIT!

///|
pub fn waas_cabi_realloc(
  src_offset : Int,
  src_size : Int,
  _dst_alignment : Int,
  dst_size : Int,
) -> Int {
  if src_offset == 0 && src_size == 0 {
    return waas_malloc(dst_size)
  }
  if dst_size == 0 {
    waas_free(src_offset)
    return 0
  }
  let dst = waas_malloc(dst_size)
  waas_copy(dst, src_offset, if src_size < dst_size { src_size } else { dst_size })
  waas_free(src_offset)
  dst
}

///|
/// Takes a string of UTF-16 code units allocated by `cabi_realloc`.
extern ""wasm"" fn waas_ptr2str(ptr : Int, len : Int) -> String =
#|(func (param i32) (param i32) (result i32)
#| local.get 0
#| local.get 1 call $moonbit.init_array16
#| local.get 0)

///|
extern ""wasm"" fn waas_copy(dest : Int, src : Int, len : Int) =
#|(func (param i32) (param i32) (param i32) local.get 0 local.get 1 local.get 2 memory.copy)

///|
extern ""wasm"" fn waas_malloc(size : Int) -> Int =
#|(func (param i32) (result i32) (local i32)
#| local.get 0 i32.const 4 i32.add call $moonbit.gc.malloc
#| local.tee 1 i32.const 0 call $moonbit.init_array8
#| local.get 1)

///|
extern ""wasm"" fn waas_free(position : Int) =
#|(func (param i32) local.get 0 call $moonbit.decref)
";

        [MenuItem("Assets/Recompile MoonBit scripts for WaaS")]
        private static void Reimport()
        {
            foreach (var guid in AssetDatabase.FindAssets(@"glob:""**/*.mbt"""))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid));
        }

        [MenuItem("Assets/Create/WaaS/MoonBit Script")]
        private static void CreateMoonBit()
        {
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.CreateAssetWithTextContent("script.mbt", "");
#else
            ProjectWindowUtil.CreateAssetWithContent("script.mbt", "");
#endif
        }

        [CustomEditor(typeof(MoonBitImporter))]
        private class Editor : ScriptedImporterEditor
        {
            public override void OnInspectorGUI()
            {
                EditorGUI.BeginChangeCheck();

                var scriptProp = serializedObject.FindProperty(nameof(script));
                EditorGUILayout.PropertyField(scriptProp);

                if (scriptProp.boolValue)
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
                                EditorGUILayout.PropertyField(settingsProp, true);
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
