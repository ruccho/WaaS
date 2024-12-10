using UnityEditor;
using UnityEngine;
using WaaS.ComponentModel.Models;
using WaaS.ComponentModel.Runtime;

namespace WaaS.Unity.Editor
{
    [CustomEditor(typeof(ComponentAsset))]
    public class ComponentAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var componentAsset = (ComponentAsset)target;
            GUILayout.Label($"{componentAsset.Size} bytes");
            var componentSource = componentAsset.LoadComponent().Source;

            GUILayout.Label("Imports", EditorStyles.boldLabel);

            foreach (var import in componentSource.Imports)
                EditorGUILayout.LabelField(import.Name.Name,
                    import.Descriptor switch
                    {
                        IExportableDescriptor<IFunction> => "Function",
                        IExportableDescriptor<IValue> => "Value",
                        IExportableDescriptor<IType> => "Type",
                        IExportableDescriptor<IComponent> => "Component",
                        IExportableDescriptor<IInstance> => "Instance",
                        IExportableDescriptor<ICoreModule> => "Core Module",
                        _ => $"Unknown ({import.Descriptor?.GetType()})"
                    });

            GUILayout.Label("Exports", EditorStyles.boldLabel);

            foreach (var export in componentSource.Exports)
                EditorGUILayout.LabelField(export.Name.Name, GetSort(export.Target));
        }

        private string GetSort(IUnresolved<ISortedExportable> sorted)
        {
            switch (sorted)
            {
                case IUnresolved<IComponent>:
                    return "Component";
                case IUnresolved<ICoreModule>:
                    return "Module";
                case IUnresolved<ICoreType>:
                    return "Core Type";
                case IUnresolved<IFunction>:
                    return "Function";
                case IUnresolved<IInstance>:
                    return "Instance";
                case IUnresolved<IType>:
                    return "Type";
                case IUnresolved<IValue>:
                    return "Value";
                default:
                    return $"Unknown ({sorted.GetType()})";
            }
        }
    }
}