using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using WaaS.Unity;

namespace WaaS.Unity.Editor
{
    [CustomEditor(typeof(ModuleAsset))]
    public class ModuleAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var moduleAsset = (ModuleAsset) target;
            var module = moduleAsset.LoadModule();

            var imports = module.ImportSection?.Imports;

            if (imports != null)
            {
                foreach (var import in imports.Value.Span)
                {
                    EditorGUILayout.LabelField(import.ModuleName, $"{import.Name} ({import.Description.Kind})");
                }
            }
        }
    }
}
