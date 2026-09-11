using UnityEngine;

// Uma entrada configuravel no pool de downgrades de stat (ex: "-1 a -3 de dano flat").
// O valor sorteado fica entre minMagnitude e maxMagnitude e sempre eh aplicado como negativo,
// entao o Inspector so precisa de numeros positivos mesmo pra reduzir um stat.
// Debuffs comportamentais (StatusEffect) usam StatusEffectDowngradeOption, nao esta classe.
[System.Serializable]
public class DowngradeOption
{
    [Tooltip("Desmarcar tira essa entrada do sorteio sem precisar remover ela da lista - util pra entradas de debug ou pra desativar temporariamente.")]
    public bool isActive = true;

    public string displayName;
    public StatType statType;
    public ModifierType modifierType;

    [Tooltip("Debuff aplica o valor sorteado como negativo, Buff aplica como positivo. Isso decide se essa entrada piora ou melhora o stat.")]
    public ModifierSource modifierSource = ModifierSource.Debuff;

    [Tooltip("Marcado: sorteia um valor entre minMagnitude e maxMagnitude toda vez que a entrada aparece no menu. Desmarcado: usa sempre fixedMagnitude.")]
    public bool useRange;

    [Tooltip("Sempre positivo aqui (o sinal final vem do modifierSource acima). Se modifierType for Percent, use uma fracao (0.05 = 5%, 0.3 = 30%), NAO 5 ou 30.")]
    public float minMagnitude;
    [Tooltip("Sempre positivo aqui (o sinal final vem do modifierSource acima). Se modifierType for Percent, use uma fracao (0.05 = 5%, 0.3 = 30%), NAO 5 ou 30.")]
    public float maxMagnitude;

    [Tooltip("Sempre positivo aqui (o sinal final vem do modifierSource acima). Se modifierType for Percent, use uma fracao (0.05 = 5%, 0.3 = 30%), NAO 5 ou 30.")]
    public float fixedMagnitude;
}
