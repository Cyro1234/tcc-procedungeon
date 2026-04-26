using UnityEditor;
public class StatModifier
{
    public string statName;            // Nome
    public StatType statType;
    public ModifierType modifierType;
    public ModifierSource modifierSource;
    public float value;
    public float duration; // Se for 0, ele é permanente!!

    public StatModifier(string statName, StatType statType, ModifierSource modifierSource, ModifierType modifierType, float value, float duration = 0f)
    {
        this.statName = statName;
        this.statType = statType;
        this.modifierSource = modifierSource;
        this.modifierType = modifierType;
        this.value = value;
        this.duration = duration;
    }
}