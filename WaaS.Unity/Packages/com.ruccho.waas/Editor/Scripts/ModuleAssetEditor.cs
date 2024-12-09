using UnityEditor;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    [CustomEditor(typeof(ModuleAsset))]
    public class ModuleAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var moduleAsset = (ModuleAsset)target;
            GUILayout.Label($"{moduleAsset.Size} bytes");
            var module = moduleAsset.LoadModule();

            var imports = module.ImportSection?.Imports;

            GUILayout.Label("Imports", EditorStyles.boldLabel);

            if (imports != null)
                foreach (var import in imports.Value.Span)
                    EditorGUILayout.LabelField(import.ModuleName, $"{import.Name} ({import.Descriptor.Kind})");

            var exports = module.ExportSection?.Exports;

            GUILayout.Label("Exports", EditorStyles.boldLabel);
            if (exports != null)
                foreach (var export in exports.Value.Span)
                    EditorGUILayout.LabelField(export.Name, $"({export.Descriptor.Kind})");
        }
    }
}