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
            case StatusEffectKind.SlipperyToes: return new SlipperyToesEffect(duration);
            case StatusEffectKind.Lag: return new LagEffect(duration);
            case StatusEffectKind.Sleepwalker: return new SleepwalkerEffect(duration);
            case StatusEffectKind.Recoil: return new RecoilEffect(duration);
            case StatusEffectKind.Snail: return new SnailEffect(duration);
            default:
                Debug.LogWarning($"StatusEffectFactory: sem StatusEffect mapeado pra {kind}");
                return null;
        }
    }

    public static StatusEffectKind GetKind(StatusEffect effect) => effect switch
    {
        DrunkennessEffect _ => StatusEffectKind.Drunkenness,
        LimpEffect _ => StatusEffectKind.HeavyLimp,
        LabyrinthitisEffect _ => StatusEffectKind.Labyrinthitis,
        SlipperyToesEffect _ => StatusEffectKind.SlipperyToes,
        LagEffect _ => StatusEffectKind.Lag,
        SleepwalkerEffect _ => StatusEffectKind.Sleepwalker,
        RecoilEffect _ => StatusEffectKind.Recoil,
        SnailEffect _ => StatusEffectKind.Snail,
        _ => StatusEffectKind.None
    };

    // Descricao curta mostrada no menu de downgrade, pro jogador saber o que esta escolhendo
    public static string Describe(StatusEffectKind kind)
    {
        switch (kind)
        {
            case StatusEffectKind.Drunkenness: return "Você anda torto";
            case StatusEffectKind.HeavyLimp: return "Anda aos pulinhos, sem diagonal";
            case StatusEffectKind.Labyrinthitis: return "A câmera não para de girar";
            case StatusEffectKind.SlipperyToes: return "Não para de andar, mas é mais rápido";
            case StatusEffectKind.Lag: return "Seus comandos chegam atrasados";
            case StatusEffectKind.Sleepwalker: return "Continua andando depois de soltar";
            case StatusEffectKind.Recoil: return "Cada golpe te joga pra trás";
            case StatusEffectKind.Snail: return "Um caracol te persegue. Se encostar, você morre";
            default: return string.Empty;
        }
    }
}
