#if !WAAS_DISABLE_MOONBIT_IMPORTER
#nullable enable
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WaaS.Unity.Editor.MoonBit
{
    /// <summary>
    ///     The settings shared by the importers of MoonBit scripts.
    /// </summary>
    public abstract class MoonBitScriptImporterBase : ScriptedImporter
    {
        [SerializeField] [Tooltip("Whether to import as a script. Disable for a file or a package of a library.")]
        private bool script = true;

        [SerializeField] private LazyLoadReference<MoonBitImporterPreset> preset;
        [SerializeField] private MoonBitImporterSettings? settings;

        protected bool IsScript => script;

        protected MoonBitImporterSettings ResolveSettings(AssetImportContext ctx)
        {
            if (preset.isSet &&
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(preset, out var presetGuid, out _))
            {
                ctx.DependsOnArtifact(new GUID(presetGuid));
                AssetDatabase.LoadAssetAtPath<MoonBitImporterPreset>(AssetDatabase.GUIDToAssetPath(presetGuid));
                return preset.asset.Settings;
            }

            return settings ?? new MoonBitImporterSettings();
        }

        [CustomEditor(typeof(MoonBitScriptImporterBase), true)]
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
