#if !WAAS_DISABLE_MOONBIT_IMPORTER
#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     Imports a MoonBit source file as a script. The file is copied into the package of the script in its workspace,
    ///     with the imports of the settings. A file of a script package (<see cref="MoonBitPackageImporter" />) is
    ///     imported with the package.
    /// </summary>
    [ScriptedImporter(1, "mbt", 100)]
    public class MoonBitImporter : MoonBitScriptImporterBase
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var assetPath = ctx.assetPath;

            if (!IsScript || MoonBitPackageImporter.IsInPackage(assetPath))
            {
                ctx.SetMainObject(null);
                return;
            }

            var settings = ResolveSettings(ctx);
            var source = File.ReadAllText(assetPath);
            var scriptDirectory = Path.Combine(MoonBitScriptBuilder.GetWorkspace(assetPath), "module", "script");
            var copiedScriptPath = Path.Combine(scriptDirectory, "script.mbt");

            MoonBitScriptBuilder.Build(ctx, settings,
                new MoonBitScriptInput(assetPath, source, $"{MoonBitScriptBuilder.ModuleName}/script", scriptDirectory)
                {
                    PrepareModule = _ =>
                    {
                        MoonBitScriptBuilder.WriteIfChanged(copiedScriptPath, source);
                        MoonBitScriptBuilder.WriteIfChanged(Path.Combine(scriptDirectory, "moon.pkg"),
                            MoonBitScriptBuilder.GeneratePackage(settings.Imports.Select(i => (i.Path, i.Alias))));
                    },
                    // diagnostics refer to the copy of the script
                    MapPaths = text => text.Replace(copiedScriptPath, assetPath)
                        .Replace(copiedScriptPath.Replace('\\', '/'), assetPath)
                });
        }

        [MenuItem("Assets/Recompile MoonBit scripts for WaaS")]
        private static void Reimport()
        {
            foreach (var guid in AssetDatabase.FindAssets(@"glob:""**/*.mbt"""))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var guid in AssetDatabase.FindAssets(@"glob:""**/moon.pkg"""))
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
    }
}
#endif
