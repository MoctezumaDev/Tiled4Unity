using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomPropertyDrawer(typeof(AssetReference))]
public class AssetReferenceDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.LabelField(position,
            "Asset Reference: ",
            property.FindPropertyRelative("_guid").stringValue);
    }
}
