using UnityEditor;
using UnityEngine;

// So mostra isRange/fixedDuration/minDuration/maxDuration quando hasDuration ta
// marcado - se nao, o efeito e permanente e nenhum desses campos faz sentido.
// Com hasDuration marcado, isRange decide entre fixedDuration (um valor) ou
// minDuration/maxDuration (sorteado), igual o useRange do DowngradeOptionDrawer.
[CustomPropertyDrawer(typeof(StatusEffectDowngradeOption))]
public class StatusEffectDowngradeOptionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty isActive = property.FindPropertyRelative("isActive");
        SerializedProperty displayName = property.FindPropertyRelative("displayName");
        SerializedProperty statusEffectKind = property.FindPropertyRelative("statusEffectKind");
        SerializedProperty hasDuration = property.FindPropertyRelative("hasDuration");
        SerializedProperty isRange = property.FindPropertyRelative("isRange");
        SerializedProperty fixedDuration = property.FindPropertyRelative("fixedDuration");
        SerializedProperty minDuration = property.FindPropertyRelative("minDuration");
        SerializedProperty maxDuration = property.FindPropertyRelative("maxDuration");

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
        EditorGUI.PropertyField(NextLine(), statusEffectKind);
        EditorGUI.PropertyField(NextLine(), hasDuration);

        if (hasDuration.boolValue)
        {
            EditorGUI.PropertyField(NextLine(), isRange);

            if (isRange.boolValue)
            {
                EditorGUI.PropertyField(NextLine(), minDuration);
                EditorGUI.PropertyField(NextLine(), maxDuration);
            }
            else
            {
                EditorGUI.PropertyField(NextLine(), fixedDuration);
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty hasDuration = property.FindPropertyRelative("hasDuration");
        SerializedProperty isRange = property.FindPropertyRelative("isRange");

        int lineCount = 4; // isActive, displayName, statusEffectKind, hasDuration
        if (hasDuration.boolValue)
        {
            lineCount += 1; // isRange
            lineCount += isRange.boolValue ? 2 : 1; // min+max, ou fixedDuration
        }

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        return lineCount * lineHeight + (lineCount - 1) * spacing;
    }
}
