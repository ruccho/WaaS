using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor.Rust
{
    [ScriptedImporter(1, "rustimporterpreset", -100)]
    public class RustImporterPresetImporter : ScriptedImporter
    {
        [SerializeField] private RustImporterSettings settings;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var preset = ScriptableObject.CreateInstance<RustImporterPreset>();
            preset.settings = settings;
            ctx.AddObjectToAsset("preset", preset);

            ctx.SetMainObject(preset);
        }

        [MenuItem("Assets/Create/WaaS/Rust Importer Preset")]
        private static void CreatePreset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Rust Importer Preset.rustimporterpreset", "");
        }
    }
}