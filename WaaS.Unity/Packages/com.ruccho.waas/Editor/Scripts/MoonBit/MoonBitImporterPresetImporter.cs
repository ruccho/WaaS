using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    [ScriptedImporter(1, "mbtimporterpreset", -100)]
    public class MoonBitImporterPresetImporter : ScriptedImporter
    {
        [SerializeField] private MoonBitImporterSettings settings;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var preset = ScriptableObject.CreateInstance<MoonBitImporterPreset>();
            preset.settings = settings;
            ctx.AddObjectToAsset("preset", preset);

            ctx.SetMainObject(preset);
        }

        [MenuItem("Assets/Create/WaaS/MoonBit Importer Preset")]
        private static void CreatePreset()
        {
#if UNITY_6000_4_OR_NEWER
            ProjectWindowUtil.CreateAssetWithTextContent("New MoonBit Importer Preset.mbtimporterpreset", "");
#else
            ProjectWindowUtil.CreateAssetWithContent("New MoonBit Importer Preset.mbtimporterpreset", "");
#endif
        }
    }
}
