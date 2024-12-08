using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace WaaS.Unity.Editor
{
    [CustomPropertyDrawer(typeof(ManagedReferenceSelectorAttribute))]
    internal class ManagedReferenceSelectorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var fieldType = property.GetManagedReferenceFieldType();
                var type = property.GetManagedReferenceType();
                var r = new Rect((float)(position.x + (double)EditorGUIUtility.labelWidth + 2.0), position.y,
                    (float)(position.width - (double)EditorGUIUtility.labelWidth - 2.0), position.height);

                var displayName = type?.GetCustomAttribute<ManagedReferenceTypeDisplayNameAttribute>()?.Name;

                r.height = EditorGUIUtility.singleLineHeight;
                if (GUI.Button(r, displayName ?? type?.FullName?.Replace('+', '.') ?? "<null>", EditorStyles.popup))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(
                        new GUIContent(
                            "<null>"),
                        type == null,
                        static state =>
                        {
                            var stateTyped = state as SerializedProperty;
                            stateTyped!.managedReferenceValue = null;
                            stateTyped.serializedObject.ApplyModifiedProperties();
                        },
                        property);
                    foreach (var derivedType in TypeCache.GetTypesDerivedFrom(fieldType))
                    {
                        if (derivedType.IsAbstract || derivedType.IsInterface) continue;
                        var derivedTypeDisplayName =
                            derivedType.GetCustomAttribute<ManagedReferenceTypeDisplayNameAttribute>()?.Name;
                        menu.AddItem(
                            new GUIContent(derivedTypeDisplayName ??
                                           $"{derivedType.Namespace}/{derivedType.FullName.Substring(derivedType.Namespace.Length + 1)}"),
                            derivedType == type,
                            static state =>
                            {
                                var stateTyped = ((SerializedProperty, Type))state;
                                var instance = Activator.CreateInstance(stateTyped.Item2);
                                stateTyped.Item1.managedReferenceValue = instance;
                                stateTyped.Item1.serializedObject.ApplyModifiedProperties();
                            },
                            (property, derivedType));
                    }

                    menu.ShowAsContext();
                }
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
}