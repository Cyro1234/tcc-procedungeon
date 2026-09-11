using UnityEngine;

// Classe para permitir que a gente escolha quais efeitos de status podem ser sorteados e com que duracao, para cada tipo de downgrade e coisa e tals
[System.Serializable]
public class StatusEffectDowngradeOption
{
    [Tooltip("Desmarcar tira o efeito que vc colocou do sorteio!!!!")]
    public bool isActive = true;

    public string displayName;

    public StatusEffectKind statusEffectKind;

    [Tooltip("Desmarcar faz o efeito ser permanente ate ser removido manualmente!!!!")]
    public bool hasDuration;

    [Tooltip("Desmarcar faz com que o efeito use sempre fixedDuration!!!!!!!!")]
    public bool isRange;

    public float fixedDuration = 5f;
    public float minDuration = 5f;
    public float maxDuration = 10f;
}
