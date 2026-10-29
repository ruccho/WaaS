#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Generates code of the entry package of a MoonBit script, such as the exports of the component.
    ///     Set to <see cref="MoonBitImporterSettings" /> as a serialized reference.
    /// </summary>
    public interface IMoonBitGenerator
    {
        void Generate(MoonBitGenerationContext context);
    }

    /// <summary>
    ///     Input and output of <see cref="IMoonBitGenerator" />.
    ///     The script is in the package imported as <c>@script</c> by the entry package.
    /// </summary>
    public class MoonBitGenerationContext
    {
        internal readonly List<(string FunctionName, string ExportName)> exports = new();
        internal readonly Dictionary<string, string> imports = new();
        internal readonly List<(string Name, byte[] Data)> customSections = new();
        internal readonly Dictionary<string, string> sources = new();
        internal readonly List<string> worldExports = new();
        internal readonly Dictionary<string, string> witPackages = new();

        internal MoonBitGenerationContext(AssetImportContext importContext, string source, string scriptInterface,
            IReadOnlyList<string> sourceDirectories)
        {
            ImportContext = importContext;
            Source = source;
            ScriptInterface = scriptInterface;
            SourceDirectories = sourceDirectories;
        }

        public AssetImportContext ImportContext { get; }

        /// <summary>
        ///     Source code of the script.
        /// </summary>
        public string Source { get; }

        /// <summary>
        ///     Public interface of the script package (<c>pkg.generated.mbti</c> of <c>moon info</c>).
        /// </summary>
        public string ScriptInterface { get; }

        /// <summary>
        ///     Directories of the script package and the local modules, which contain the source files of the packages
        ///     the script can import.
        /// </summary>
        public IReadOnlyList<string> SourceDirectories { get; }

        /// <summary>
        ///     Adds a source file to the entry package.
        /// </summary>
        public void AddSource(string fileName, string source)
        {
            sources.Add(fileName, source);
        }

        /// <summary>
        ///     Imports a package to the entry package.
        /// </summary>
        public void AddImport(string packagePath, string alias)
        {
            imports[packagePath] = alias;
        }

        /// <summary>
        ///     Exports a function of the entry package from the core module.
        /// </summary>
        public void AddExport(string functionName, string exportName)
        {
            exports.Add((functionName, exportName));
        }

        /// <summary>
        ///     Adds a WIT package used to componentize the module.
        /// </summary>
        public void AddWitPackage(string name, string source)
        {
            witPackages.Add(name, source);
        }

        /// <summary>
        ///     Exports an interface from the world of the component, such as "my-namespace:my-package/my-interface".
        /// </summary>
        public void AddWorldExport(string @interface)
        {
            worldExports.Add(@interface);
        }

        /// <summary>
        ///     Adds a custom section to the core module.
        /// </summary>
        public void AddCustomSection(string name, byte[] data)
        {
            customSections.Add((name, data ?? throw new ArgumentNullException(nameof(data))));
        }
    }
}
