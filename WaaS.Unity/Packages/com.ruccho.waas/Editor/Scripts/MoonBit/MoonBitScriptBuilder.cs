#if !WAAS_DISABLE_MOONBIT_IMPORTER
#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using WaaS.Models;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     A script to build: a source file, or a package of the project module.
    /// </summary>
    internal sealed class MoonBitScriptInput
    {
        public MoonBitScriptInput(string assetPath, string source, string scriptPackage, string scriptDirectory)
        {
            AssetPath = assetPath;
            Source = source;
            ScriptPackage = scriptPackage;
            ScriptDirectory = scriptDirectory;
        }

        public string AssetPath { get; }

        /// <summary>
        ///     Source code of the script, which is read by the generators.
        /// </summary>
        public string Source { get; }

        /// <summary>
        ///     Path of the package of the script, imported by the entry package as <c>@script</c>.
        /// </summary>
        public string ScriptPackage { get; }

        /// <summary>
        ///     Directory of the package of the script.
        /// </summary>
        public string ScriptDirectory { get; }

        /// <summary>
        ///     Directories of the modules added to the workspace other than the dependencies.
        /// </summary>
        public IReadOnlyList<string> ExtraMembers { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Names of the modules imported by the module of the entry package other than the dependencies.
        /// </summary>
        public IReadOnlyList<string> ExtraImports { get; set; } = Array.Empty<string>();

        /// <summary>
        ///     Writes the package of the script into the module of the workspace, if the package is in the module.
        /// </summary>
        public Action<string>? PrepareModule { get; set; }

        /// <summary>
        ///     Maps the paths in the diagnostics to the paths of the assets.
        /// </summary>
        public Func<string, string> MapPaths { get; set; } = text => text;
    }

    /// <summary>
    ///     Builds MoonBit scripts with the MoonBit toolchain (<c>moon</c>) and componentizes them. Each script is built in
    ///     its own workspace under <c>Library/com.ruccho.waas/moonbit</c>. On Windows, the build output of the workspace is
    ///     in a short directory under the temporary directory (see <see cref="GetTargetDirectory" />).
    /// </summary>
    internal static class MoonBitScriptBuilder
    {
        internal const string ModuleName = "waas/script";
        private const string WorldName = "script";

        /// <summary>
        ///     The length of the paths from which the MoonBit compiler can fail to write files on Windows.
        /// </summary>
        private const int MaxPath = 260;

        /// <summary>
        ///     The file in a target directory that records the path of its workspace.
        /// </summary>
        private const string WorkspaceRecordFileName = "waas-workspace.txt";

        private static readonly UTF8Encoding Utf8NoBom = new(false);

        private static readonly Regex SysErrorRegex = new(@"Sys_error\(""(?<path>(?:[^""\\]|\\.)*?): No such file");

#if UNITY_EDITOR_WIN
        private static bool targetDirectoriesCleaned;
#endif

        /// <summary>
        ///     The workspace of a script, which is kept short for the limit of the path length on Windows.
        /// </summary>
        public static string GetWorkspace(string assetPath)
        {
            return Path.GetFullPath(
                $"Library/com.ruccho.waas/moonbit/{Hash128.Compute(assetPath).ToString().Substring(0, 12)}");
        }

        /// <summary>
        ///     The directory of the build output of a workspace (<c>--target-dir</c> of moon).
        /// </summary>
        /// <remarks>
        ///     On Windows, the MoonBit compiler can fail to write an output file whose path is 260 characters or longer
        ///     (<c>Sys_error</c> with "No such file or directory"), even if long paths are enabled in the system. The output paths are the target directory followed by the
        ///     names of the packages, which are long for the packages of the dependencies, so a project at a moderately
        ///     deep path exceeds the limit. The target directory is therefore a directory with a short name under the
        ///     temporary directory, named by the hash of the path of the workspace. The directories of the workspaces that
        ///     no longer exist are deleted.
        /// </remarks>
        public static string GetTargetDirectory(string workspace)
        {
#if UNITY_EDITOR_WIN
            var root = Path.Combine(Path.GetTempPath(), "waas-moonbit");
            if (!targetDirectoriesCleaned)
            {
                targetDirectoriesCleaned = true;
                DeleteStaleTargetDirectories(root);
            }

            // the output of the earlier versions, which is in the workspace
            var legacyDirectory = Path.Combine(workspace, "_build");
            if (Directory.Exists(legacyDirectory))
                try
                {
                    Directory.Delete(legacyDirectory, true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // the directory is not used anymore, and is deleted later
                }

            var directory = Path.Combine(root, Hash128.Compute(workspace).ToString().Substring(0, 12));
            WriteIfChanged(Path.Combine(directory, WorkspaceRecordFileName), workspace);
            return directory;
#else
            return Path.Combine(workspace, "_build");
#endif
        }

#if UNITY_EDITOR_WIN
        /// <summary>
        ///     Deletes the target directories whose workspaces no longer exist, such as those of a deleted project or a
        ///     deleted Library folder.
        /// </summary>
        private static void DeleteStaleTargetDirectories(string root)
        {
            if (!Directory.Exists(root)) return;
            foreach (var directory in Directory.GetDirectories(root))
                try
                {
                    var record = Path.Combine(directory, WorkspaceRecordFileName);
                    var stale = File.Exists(record)
                        ? !Directory.Exists(File.ReadAllText(record).Trim())
                        // a directory without the record is being created, or was left by an interrupted build
                        : Directory.GetLastWriteTimeUtc(directory) < DateTime.UtcNow.AddDays(-1);
                    if (stale) Directory.Delete(directory, true);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // the directory is in use, and is deleted later
                }
        }
#endif

        public static void Build(AssetImportContext ctx, MoonBitImporterSettings settings, MoonBitScriptInput input)
        {
            var assetPath = input.AssetPath;

            var workspace = GetWorkspace(assetPath);
            var targetDirectory = GetTargetDirectory(workspace);
            var module = Path.Combine(workspace, "module");
            var entryDirectory = Path.Combine(module, "main");

            Directory.CreateDirectory(module);
            if (Directory.Exists(entryDirectory)) Directory.Delete(entryDirectory, true);

            var dependencies = settings.Dependencies.ToArray();
            WriteWorkspace(workspace, dependencies, input.ExtraMembers);
            WriteModule(module, dependencies, input.ExtraImports);
            input.PrepareModule?.Invoke(module);

            foreach (var dependency in dependencies)
                if (dependency.LocalPath is { } localPath)
                    TrackDirectory(ctx, localPath);

            var generators = settings.Generators.ToArray();
            var sourceDirectories = new[] { input.ScriptDirectory }
                .Concat(dependencies.Select(d => d.LocalPath).OfType<string>().Select(Path.GetFullPath))
                .ToArray();
            var context = new MoonBitGenerationContext(ctx, input.Source, "", sourceDirectories);
            if (generators.Length > 0)
            {
                // the generators read the public interface of the script, which moon info writes into the package
                var interfacePath = Path.Combine(input.ScriptDirectory, "pkg.generated.mbti");
                var existed = File.Exists(interfacePath);
                if (!RunMoon(ctx, assetPath, workspace,
                        $"info \"{input.ScriptDirectory}\" --target-dir \"{targetDirectory}\"", input.MapPaths))
                    return;
                var scriptInterface = File.Exists(interfacePath) ? File.ReadAllText(interfacePath) : "";
                if (!existed && File.Exists(interfacePath)) File.Delete(interfacePath);

                context = new MoonBitGenerationContext(ctx, input.Source, scriptInterface, sourceDirectories);
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

            WriteEntryPackage(entryDirectory, input.ScriptPackage, context);
            foreach (var (fileName, source) in context.scriptSources)
                File.WriteAllText(Path.Combine(input.ScriptDirectory, fileName), source, Utf8NoBom);

            if (!RunMoon(ctx, assetPath, workspace,
                    $"build \"{entryDirectory}\" --target wasm --release --target-dir \"{targetDirectory}\"",
                    input.MapPaths))
                return;

            var outPath = Path.Combine(targetDirectory, "wasm", "release", "build", ModuleName, "main", "main.wasm");
            if (!File.Exists(outPath))
            {
                ctx.LogImportError($"Error importing {assetPath}: the built module is not found at {outPath}");
                return;
            }

            var moduleBytes = File.ReadAllBytes(outPath);
            foreach (var (name, data) in context.customSections)
                moduleBytes = AppendCustomSection(moduleBytes, name, data);

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

        private static void WriteWorkspace(string workspace,
            IEnumerable<MoonBitImporterSettings.ModuleDependency> dependencies, IEnumerable<string> extraMembers)
        {
            var builder = new StringBuilder("members = [\n  \"module\",\n");
            var members = dependencies.Select(d => d.LocalPath).OfType<string>().Concat(extraMembers);
            foreach (var member in members)
            {
                var relative = Path.GetRelativePath(workspace, Path.GetFullPath(member)).Replace('\\', '/');
                builder.Append($"  \"{relative}\",\n");
            }

            builder.Append("]\n");
            WriteIfChanged(Path.Combine(workspace, "moon.work"), builder.ToString());
        }

        private static void WriteModule(string module,
            IEnumerable<MoonBitImporterSettings.ModuleDependency> dependencies, IEnumerable<string> extraImports)
        {
            var builder = new StringBuilder($"name = \"{ModuleName}\"\n\n");
            var imports = dependencies.Select(ToModuleImport).Concat(extraImports.Select(m => $"{m}@0.0.0")).ToArray();
            if (imports.Length > 0)
            {
                builder.Append("import {\n");
                foreach (var import in imports) builder.Append($"  \"{import}\",\n");
                builder.Append("}\n\n");
            }

            builder.Append("preferred_target = \"wasm\"\n");
            WriteIfChanged(Path.Combine(module, "moon.mod"), builder.ToString());
        }

        /// <summary>
        ///     The dependency in the import block of moon.mod. The version of a local module is ignored.
        /// </summary>
        internal static string ToModuleImport(MoonBitImporterSettings.ModuleDependency dependency)
        {
            var version = string.IsNullOrEmpty(dependency.Version) ? "0.0.0" : dependency.Version;
            return $"{dependency.Name}@{version}";
        }

        internal static string GeneratePackage(IEnumerable<(string Path, string? Alias)> imports)
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

        private static void WriteEntryPackage(string directory, string scriptPackage, MoonBitGenerationContext context)
        {
            Directory.CreateDirectory(directory);

            var builder = new StringBuilder(GeneratePackage(new (string, string?)[] { (scriptPackage, "script") }
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

        internal static void TrackFile(AssetImportContext ctx, string path)
        {
            // files outside the asset database are tracked as custom dependencies
            ExternalFileDependency.Track(ctx, path);
        }

        internal static void WriteIfChanged(string path, string contents)
        {
            if (File.Exists(path) && File.ReadAllText(path) == contents) return;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
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
                $"Error importing {assetPath}: \n{mapPaths(standardError.ToString())}\n\nstdout:\n{mapPaths(standardOutput.ToString())}{GetLongPathHint($"{standardError}\n{standardOutput}")}");
            return false;
        }

        /// <summary>
        ///     A hint for an error of the compiler that failed to access a file at a long path.
        /// </summary>
        private static string GetLongPathHint(string output)
        {
            foreach (Match match in SysErrorRegex.Matches(output))
            {
                var path = UnescapeOCamlString(match.Groups["path"].Value);
                if (path.Length < MaxPath) continue;
                return
                    $"\n\nThe MoonBit compiler failed to access a file whose path is {path.Length} characters long. On Windows, it can fail with the paths of {MaxPath} characters or longer. Move the project or the dependencies to a shorter path.";
            }

            return "";
        }

        /// <summary>
        ///     Unescapes a string literal printed by OCaml, whose bytes other than ASCII are escaped as <c>\ddd</c>.
        /// </summary>
        private static string UnescapeOCamlString(string text)
        {
            var bytes = new List<byte>();
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '\\' && i + 1 < text.Length)
                {
                    if (i + 3 < text.Length && char.IsDigit(text[i + 1]) && char.IsDigit(text[i + 2]) &&
                        char.IsDigit(text[i + 3]))
                    {
                        bytes.Add((byte)int.Parse(text.Substring(i + 1, 3)));
                        i += 3;
                    }
                    else
                    {
                        bytes.AddRange(Encoding.UTF8.GetBytes(text[++i].ToString()));
                    }
                }
                else
                {
                    bytes.AddRange(Encoding.UTF8.GetBytes(text[i].ToString()));
                }

            return Encoding.UTF8.GetString(bytes.ToArray());
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
    }
}
#endif
