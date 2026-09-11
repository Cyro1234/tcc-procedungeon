using UnityEngine;

public static class StatusEffectFactory
{
    // Se duration <= 0 vai cria um efeito permanente!!!!!!! StatusEffectManager so conta o tempo pra remover quando duration > 0
    public static StatusEffect Create(StatusEffectKind kind, float duration = 0f)
    {
        switch (kind)
        {
            case StatusEffectKind.Drunkenness: return new DrunkennessEffect(duration);
            case StatusEffectKind.HeavyLimp: return new LimpEffect(duration);
            case StatusEffectKind.Labyrinthitis: return new LabyrinthitisEffect(duration);
            default:
                Debug.LogWarning($"StatusEffectFactory: sem StatusEffect mapeado pra {kind}");
                return null;
        }
    }
}
