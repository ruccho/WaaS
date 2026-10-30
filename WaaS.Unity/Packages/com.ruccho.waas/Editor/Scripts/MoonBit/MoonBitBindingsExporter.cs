#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Generates MoonBit bindings according to <see cref="MoonBitBindingsSettings" />.
    ///     MoonBit scripts that depend on the generated files are reimported when they change.
    /// </summary>
    public static class MoonBitBindingsExporter
    {
        private static readonly UTF8Encoding Utf8NoBom = new(false);

        /// <summary>
        ///     Generates the bindings of all settings in the project. Returns whether any file is changed.
        /// </summary>
        public static bool ExportAll(bool log)
        {
            var changed = false;
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(MoonBitBindingsSettings)}"))
            {
                var settings =
                    AssetDatabase.LoadAssetAtPath<MoonBitBindingsSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null) continue;
                try
                {
                    changed |= Export(settings, log);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, settings);
                }
            }

            return changed;
        }

        private static bool Export(MoonBitBindingsSettings settings, bool log)
        {
            var interfaces = settings.Interfaces.Where(i => !string.IsNullOrEmpty(i)).ToArray();
            if (interfaces.Length == 0 || string.IsNullOrEmpty(settings.PackagePath)) return false;

            var files = Generate(settings.WitDirectory, interfaces, settings.PackagePath,
                settings.ExternalInterfaces.ToDictionary(e => e.Interface, e => e.PackagePath));
            return WriteDirectory(settings.OutputDirectory, files, log);
        }

        /// <summary>
        ///     Generates the bindings of the interfaces. The bindings of external interfaces are not generated, and the
        ///     generated code refers to the given package paths instead.
        /// </summary>
        public static IReadOnlyDictionary<string, string> Generate(
            string witDirectory,
            IReadOnlyCollection<string> interfaces,
            string packagePath,
            IReadOnlyDictionary<string, string> externalInterfaces)
        {
            var temp = Path.Combine("Temp", "com.ruccho.waas", "moonbit-bindings", Guid.NewGuid().ToString("N"));
            try
            {
                // the world imports the interfaces, and the WIT packages are resolved as its dependencies
                Directory.CreateDirectory(temp);
                var world = new StringBuilder("package waas-moonbit:bindings;\n\nworld bindings {\n");
                foreach (var @interface in interfaces) world.Append($"    import {@interface};\n");
                world.Append("}\n");
                File.WriteAllText(Path.Combine(temp, "world.wit"), world.ToString(), Utf8NoBom);
                CopyWitPackages(witDirectory, Path.Combine(temp, "deps"));

                var generated = NativeMethods.GenerateMoonBitBindings(temp, "bindings", packagePath);

                var external = externalInterfaces.ToDictionary(e => ToPackageName(e.Key), e => e.Value);
                var importRegex =
                    new Regex($"\"{Regex.Escape(packagePath)}/interface/([^\"/]+)/([^\"/]+)/([^\"/]+)\"");
                var result = new Dictionary<string, string>();
                foreach (var (path, contents) in generated)
                {
                    // only the bindings of the interfaces are used; the entry point package is for components
                    var segments = path.Replace('\\', '/').Split('/');
                    if (segments.Length < 5 || segments[0] != "interface") continue;

                    // "interface/ns/pkg/iface" is flattened to "ns_pkg_iface" to keep the paths of the build short
                    var packageName = ToPackageName(segments[1], segments[2], segments[3]);
                    if (external.ContainsKey(packageName)) continue;

                    var text = Utf8NoBom.GetString(contents);
                    if (segments[^1] == "moon.pkg")
                        text = importRegex.Replace(text, m =>
                        {
                            var name = ToPackageName(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value);
                            return external.TryGetValue(name, out var externalPackagePath)
                                ? $"\"{externalPackagePath}\""
                                : $"\"{packagePath}/{name}\"";
                        });

                    // the helper functions are generated in an unstable order; sort them to keep the files unchanged
                    if (segments[^1] == "ffi.mbt") text = SortTopLevelBlocks(text);

                    result[$"{packageName}/{string.Join("/", segments.Skip(4))}"] = text;
                }

                return result;
            }
            finally
            {
                if (Directory.Exists(temp)) Directory.Delete(temp, true);
            }
        }

        /// <summary>
        ///     Sorts the top-level blocks, each of which starts with "///|". The order of the top-level declarations does
        ///     not matter in MoonBit.
        /// </summary>
        private static string SortTopLevelBlocks(string text)
        {
            const string marker = "///|\n";
            var normalized = text.Replace("\r\n", "\n");
            var first = normalized.IndexOf(marker, StringComparison.Ordinal);
            if (first < 0) return text;

            var blocks = normalized.Substring(first)
                .Split(new[] { marker }, StringSplitOptions.RemoveEmptyEntries)
                .Select(b => marker + b.TrimEnd('\n') + "\n")
                .OrderBy(b => b, StringComparer.Ordinal);
            return normalized.Substring(0, first) + string.Join("\n", blocks);
        }

        /// <summary>
        ///     The package name of the bindings of an interface: "ns:pkg/iface" is generated to "ns_pkg_iface".
        /// </summary>
        public static string ToPackageName(string @interface)
        {
            var name = @interface.Split('@')[0];
            var colon = name.IndexOf(':');
            var slash = name.IndexOf('/');
            return ToPackageName(name.Substring(0, colon), name.Substring(colon + 1, slash - colon - 1),
                name.Substring(slash + 1));
        }

        private static string ToPackageName(string @namespace, string package, string @interface)
        {
            return $"{@namespace}_{package}_{@interface}".Replace('-', '_');
        }

        private static void CopyWitPackages(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (var directory in Directory.GetDirectories(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(directory));
                Directory.CreateDirectory(target);
                foreach (var file in Directory.GetFiles(directory, "*.wit"))
                    File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        ///     Writes the files to the directory, which is used only by the generated files: files that are no longer
        ///     generated are deleted. Returns whether any file is changed.
        /// </summary>
        public static bool WriteDirectory(string directory, IReadOnlyDictionary<string, string> files, bool log)
        {
            var changed = false;
            var fullDirectory = Path.GetFullPath(directory);

            if (Directory.Exists(fullDirectory))
                foreach (var existing in Directory.GetFiles(fullDirectory, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(fullDirectory, existing).Replace('\\', '/');
                    if (files.ContainsKey(relative)) continue;
                    File.Delete(existing);
                    ExternalFileDependency.Register(existing);
                    changed = true;
                }

            foreach (var (relative, contents) in files)
            {
                var path = Path.Combine(fullDirectory, relative);
                if (!File.Exists(path) || File.ReadAllText(path, Utf8NoBom) != contents)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, contents, Utf8NoBom);
                    changed = true;
                    if (log) Debug.Log($"[WaaS] Generated MoonBit bindings: {path}");
                }

                ExternalFileDependency.Register(path);
            }

            if (Directory.Exists(fullDirectory)) DeleteEmptyDirectories(fullDirectory);

            return changed;
        }

        private static void DeleteEmptyDirectories(string directory)
        {
            foreach (var child in Directory.GetDirectories(directory))
            {
                DeleteEmptyDirectories(child);
                if (!Directory.EnumerateFileSystemEntries(child).Any()) Directory.Delete(child);
            }
        }
    }
}
