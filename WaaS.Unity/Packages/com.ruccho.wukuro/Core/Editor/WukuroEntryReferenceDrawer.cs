using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using WaaS.Unity;

namespace Wukuro.Editor
{
    /// <summary>
    ///     Shows the entry points of the selected script as a popup, and the fields of the arguments of the selected entry point.
    ///     The arguments are synchronized with the parameters of the entry point.
    /// </summary>
    [CustomPropertyDrawer(typeof(WukuroEntryReference))]
    public class WukuroEntryReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var scriptProp = property.FindPropertyRelative("_script");
            var entryProp = property.FindPropertyRelative("_entry");
            var argumentsProp = property.FindPropertyRelative("_arguments._arguments");

            var root = new Foldout { text = property.displayName, value = true };
            root.Add(new PropertyField(scriptProp));

            var entryContainer = new VisualElement();
            root.Add(entryContainer);

            void Rebuild()
            {
                entryContainer.Clear();
                if (scriptProp.objectReferenceValue is not ComponentAsset script) return;

                // keep the identifier of the script in sync with the asset
                var scriptIdProp = property.FindPropertyRelative("_scriptId");
                var scriptId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(script));
                if (scriptIdProp.stringValue != scriptId)
                {
                    scriptIdProp.stringValue = scriptId;
                    property.serializedObject.ApplyModifiedProperties();
                }

                var entries = WukuroEntryPoints.Parse(script.Wit);
                if (entries.Count == 0)
                {
                    entryContainer.Add(new HelpBox("The script has no entry points.", HelpBoxMessageType.Warning));
                    return;
                }

                var names = entries.Select(e => e.Name).ToList();
                var current = entries.FirstOrDefault(e => e.Name == entryProp.stringValue);
                if (current == null)
                {
                    // select the first entry point when nothing valid is selected
                    current = entries[0];
                    entryProp.stringValue = current.Name;
                    property.serializedObject.ApplyModifiedProperties();
                }

                var popup = new PopupField<string>("Entry", names, current.Name);
                popup.RegisterValueChangedCallback(evt =>
                {
                    entryProp.stringValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                    Rebuild();
                });
                entryContainer.Add(popup);

                SyncArguments(argumentsProp, current.Parameters.Where(p => p.IsArgument).ToArray());

                var objectTypes = WukuroEntryPoints.GetObjectTypes(script);
                for (var i = 0; i < argumentsProp.arraySize; i++)
                {
                    var element = argumentsProp.GetArrayElementAtIndex(i);
                    var name = element.FindPropertyRelative("_name").stringValue;
                    var valueProp = element.FindPropertyRelative("_value");
                    if (valueProp == null) continue;

                    var label = ObjectNames.NicifyVariableName(name.Replace('-', ' '));
                    if (valueProp.propertyType == SerializedPropertyType.ObjectReference &&
                        objectTypes.TryGetValue((current.Name, name), out var typeName))
                    {
                        entryContainer.Add(CreateTypedObjectField(valueProp, label, typeName));
                        continue;
                    }

                    var field = new PropertyField(valueProp, label);
                    field.BindProperty(valueProp);
                    entryContainer.Add(field);
                }
            }

            Rebuild();
            root.TrackPropertyValue(scriptProp, _ => Rebuild());

            return root;
        }

        /// <summary>
        ///     An object field that accepts only objects of the type recorded in the script (<c>&amp;ObjectOf&lt;T&gt;</c>).
        /// </summary>
        private static VisualElement CreateTypedObjectField(SerializedProperty valueProp, string label, string typeName)
        {
            var container = new VisualElement();
            var type = System.Type.GetType(typeName);
            if (type == null)
            {
                var fallback = new PropertyField(valueProp, label);
                fallback.BindProperty(valueProp);
                container.Add(fallback);
                container.Add(new HelpBox($"The type {typeName} is not found.", HelpBoxMessageType.Warning));
                return container;
            }

            var field = new ObjectField(label) { objectType = type, allowSceneObjects = true };
            field.BindProperty(valueProp);
            container.Add(field);

            var help = new HelpBox($"The value is not {type.Name}.", HelpBoxMessageType.Error);
            container.Add(help);

            void Validate()
            {
                help.style.display = IsAssignable(valueProp.objectReferenceValue, type)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            Validate();
            field.TrackPropertyValue(valueProp, _ => Validate());
            return container;
        }

        /// <summary>
        ///     Whether the object can be passed as the type. A GameObject is accepted if it has a component of the type.
        /// </summary>
        public static bool IsAssignable(UnityEngine.Object value, System.Type type)
        {
            if (value == null || type.IsInstanceOfType(value)) return true;
            return value is UnityEngine.GameObject gameObject && typeof(UnityEngine.Component).IsAssignableFrom(type) &&
                   gameObject.GetComponent(type) != null;
        }

        private static void SyncArguments(SerializedProperty argumentsProp, IReadOnlyList<EntryParameterInfo> parameters)
        {
            var existing = new List<ArgumentContainerBase>();
            for (var i = 0; i < argumentsProp.arraySize; i++)
                existing.Add(argumentsProp.GetArrayElementAtIndex(i).managedReferenceValue as ArgumentContainerBase);

            // keep the containers whose name and type match, in the order of the parameters
            var synced = parameters
                .Select(p => existing.FirstOrDefault(c => c != null && c.Name == p.Name && c.WitType == p.WitType) ??
                             ArgumentContainerBase.Create(p))
                .Where(c => c != null)
                .ToArray();

            if (synced.SequenceEqual(existing)) return;

            argumentsProp.arraySize = synced.Length;
            for (var i = 0; i < synced.Length; i++)
                argumentsProp.GetArrayElementAtIndex(i).managedReferenceValue = synced[i];
            argumentsProp.serializedObject.ApplyModifiedProperties();
        }
    }
}
