#if !WAAS_DISABLE_MOONBIT_IMPORTER
#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Imports a MoonBit package in the project (a directory with <c>moon.pkg</c> and its source files) as a script.
    ///     The packages are in the project module (<c>moon.mod</c> at the root of the project, whose source directory is
    ///     <c>Assets</c>), so the scripts can be edited with an IDE that opens the project root.
    /// </summary>
    [ScriptedImporter(1, "pkg", 100)]
    public class MoonBitPackageImporter : MoonBitScriptImporterBase
    {
        private const string ProjectModulePath = "moon.mod";
        private const string ProjectWorkspacePath = "moon.work";
        private const string DefaultProjectModuleName = "unity/project";
        private const string SourceDirectory = "Assets";

        private static readonly Regex NameRegex = new(@"^name\s*=\s*""(?<name>[^""]+)""", RegexOptions.Multiline);
        private static readonly Regex SourceRegex = new(@"^source\s*=\s*""(?<source>[^""]*)""", RegexOptions.Multiline);
        private static readonly Regex ImportBlockRegex = new(@"^import\s*\{[^\n]*\n", RegexOptions.Multiline);
        private static readonly Regex MembersRegex = new(@"^members\s*=\s*\[[^\n]*\n", RegexOptions.Multiline);
        private static readonly Regex PackageNameRegex = new(@"^[A-Za-z0-9_-]+$");

        /// <summary>
        ///     Whether the file is a source file of a package, which is not a script by itself.
        /// </summary>
        public static bool IsInPackage(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath) ?? "";
            return File.Exists(Path.Combine(directory, "moon.pkg")) ||
                   File.Exists(Path.Combine(directory, "moon.pkg.json"));
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var assetPath = ctx.assetPath;
            if (!IsScript || Path.GetFileName(assetPath) != "moon.pkg")
            {
                ctx.SetMainObject(null);
                return;
            }

            var directory = Path.GetDirectoryName(assetPath)!.Replace('\\', '/');
            if (!directory.StartsWith(SourceDirectory + "/", StringComparison.Ordinal))
            {
                ctx.LogImportError($"Error importing {assetPath}: a script package must be in {SourceDirectory}.");
                return;
            }

            var relative = directory.Substring(SourceDirectory.Length + 1);
            var invalid = relative.Split('/').FirstOrDefault(segment => !PackageNameRegex.IsMatch(segment));
            if (invalid != null)
            {
                ctx.LogImportError(
                    $"Error importing {assetPath}: the directory name \"{invalid}\" cannot be a part of a MoonBit package name. Use letters, digits, '_' and '-'.");
                return;
            }

            var settings = ResolveSettings(ctx);
            var dependencies = settings.Dependencies.ToArray();

            string projectModuleName;
            try
            {
                projectModuleName = EnsureProjectModule(dependencies);
            }
            catch (Exception ex)
            {
                ctx.LogImportError($"Error importing {assetPath}: {ex.Message}");
                return;
            }

            // the source files of the package are the script
            var source = new StringBuilder();
            foreach (var file in Directory.GetFiles(directory, "*.mbt").OrderBy(f => f, StringComparer.Ordinal))
            {
                if (file.EndsWith("_test.mbt", StringComparison.Ordinal) ||
                    file.EndsWith("_wbtest.mbt", StringComparison.Ordinal))
                    continue;
                ctx.DependsOnSourceAsset(file.Replace('\\', '/'));
                source.Append(File.ReadAllText(file)).Append('\n');
            }

            MoonBitScriptBuilder.Build(ctx, settings,
                new MoonBitScriptInput(assetPath, source.ToString(), $"{projectModuleName}/{relative}",
                    Path.GetFullPath(directory))
                {
                    ExtraMembers = new[] { Directory.GetCurrentDirectory() },
                    ExtraImports = new[] { projectModuleName }
                });
        }

        /// <summary>
        ///     Creates the project module and the workspace at the root of the project, or adds the dependencies to them.
        ///     Returns the name of the project module.
        /// </summary>
        private static string EnsureProjectModule(IReadOnlyList<MoonBitImporterSettings.ModuleDependency> dependencies)
        {
            var module = File.Exists(ProjectModulePath)
                ? File.ReadAllText(ProjectModulePath)
                : $"name = \"{DefaultProjectModuleName}\"\n\nsource = \"{SourceDirectory}\"\n\npreferred_target = \"wasm\"\n";

            var name = NameRegex.Match(module) is { Success: true } nameMatch
                ? nameMatch.Groups["name"].Value
                : throw new InvalidOperationException($"{ProjectModulePath} has no name.");
            if (SourceRegex.Match(module) is not { Success: true } sourceMatch ||
                sourceMatch.Groups["source"].Value != SourceDirectory)
                throw new InvalidOperationException(
                    $"The source directory of {ProjectModulePath} must be \"{SourceDirectory}\" (source = \"{SourceDirectory}\").");

            foreach (var dependency in dependencies)
            {
                if (module.Contains($"\"{dependency.Name}@")) continue;
                var line = $"  \"{MoonBitScriptBuilder.ToModuleImport(dependency)}\",\n";
                module = ImportBlockRegex.Match(module) is { Success: true } block
                    ? module.Insert(block.Index + block.Length, line)
                    : module.TrimEnd('\n') + $"\n\nimport {{\n{line}}}\n";
            }

            MoonBitScriptBuilder.WriteIfChanged(ProjectModulePath, module);

            // the workspace lets an IDE resolve the local modules
            var workspace = File.Exists(ProjectWorkspacePath)
                ? File.ReadAllText(ProjectWorkspacePath)
                : "members = [\n  \".\",\n]\n";
            foreach (var dependency in dependencies)
            {
                if (dependency.LocalPath is not { } localPath) continue;
                var member = localPath.Replace('\\', '/');
                if (workspace.Contains($"\"{member}\"")) continue;
                var line = $"  \"{member}\",\n";
                workspace = MembersRegex.Match(workspace) is { Success: true } members
                    ? workspace.Insert(members.Index + members.Length, line)
                    : throw new InvalidOperationException($"{ProjectWorkspacePath} has no members.");
            }

            MoonBitScriptBuilder.WriteIfChanged(ProjectWorkspacePath, workspace);

            return name;
        }
    }

    /// <summary>
    ///     Reimports a script package when a source file is added to, removed from or moved into it.
    /// </summary>
    internal class MoonBitPackagePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var packages = new HashSet<string>();
            foreach (var path in importedAssets.Concat(deletedAssets).Concat(movedAssets).Concat(movedFromAssetPaths))
            {
                if (!path.EndsWith(".mbt", StringComparison.Ordinal)) continue;
                var package = Path.Combine(Path.GetDirectoryName(path) ?? "", "moon.pkg").Replace('\\', '/');
                if (File.Exists(package) && !importedAssets.Contains(package)) packages.Add(package);
            }

            foreach (var package in packages) AssetDatabase.ImportAsset(package);
        }
    }
}
#endif
