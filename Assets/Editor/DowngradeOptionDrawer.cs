using UnityEditor;
using UnityEngine;

// So mostra minMagnitude/maxMagnitude quando useRange ta marcado, e fixedMagnitude
// quando nao ta - assim o Inspector nao mostra campo nenhum que nao vai ser usado.
[CustomPropertyDrawer(typeof(DowngradeOption))]
public class DowngradeOptionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty isActive = property.FindPropertyRelative("isActive");
        SerializedProperty displayName = property.FindPropertyRelative("displayName");
        SerializedProperty statType = property.FindPropertyRelative("statType");
        SerializedProperty modifierType = property.FindPropertyRelative("modifierType");
        SerializedProperty modifierSource = property.FindPropertyRelative("modifierSource");
        SerializedProperty useRange = property.FindPropertyRelative("useRange");
        SerializedProperty minMagnitude = property.FindPropertyRelative("minMagnitude");
        SerializedProperty maxMagnitude = property.FindPropertyRelative("maxMagnitude");
        SerializedProperty fixedMagnitude = property.FindPropertyRelative("fixedMagnitude");

        float y = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect NextLine()
        {
            Rect r = new Rect(position.x, y, position.width, lineHeight);
            y += lineHeight + spacing;
            return r;
        }

        EditorGUI.PropertyField(NextLine(), isActive);
        EditorGUI.PropertyField(NextLine(), displayName);
        EditorGUI.PropertyField(NextLine(), statType);
        EditorGUI.PropertyField(NextLine(), modifierType);
        EditorGUI.PropertyField(NextLine(), modifierSource);
        EditorGUI.PropertyField(NextLine(), useRange);

        if (useRange.boolValue)
        {
            EditorGUI.PropertyField(NextLine(), minMagnitude);
            EditorGUI.PropertyField(NextLine(), maxMagnitude);
        }
        else
        {
            EditorGUI.PropertyField(NextLine(), fixedMagnitude);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty useRange = property.FindPropertyRelative("useRange");

        int lineCount = 6; // isActive, displayName, statType, modifierType, modifierSource, useRange
        lineCount += useRange.boolValue ? 2 : 1;

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return lineCount * lineHeight + (lineCount - 1) * spacing;
    }
}
