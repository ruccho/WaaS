using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wukuro.Editor
{
    /// <summary>
    ///     Shows <see cref="ObjectOf{T}" /> as an object field of the type.
    /// </summary>
    [CustomPropertyDrawer(typeof(ObjectOf<>))]
    public class ObjectOfDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative("_value"), property.displayName);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_value"), label);
        }
    }
}
