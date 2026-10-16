using UnityEngine;

// Pes Escorregadios: igual a cobrinha. Depois que o jogador comeca a andar ele NUNCA para;
// o input so troca a direcao. Pra compensar, anda um pouco mais rapido, mas pode ser lento tb, temos que testar kkk
public class SlipperyToesModifier : IMovementModifier
{
    private readonly float speedBonus;
    private Vector2 heading = Vector2.zero;

    public SlipperyToesModifier(float speedBonus)
    {
        this.speedBonus = speedBonus;
    }

    public Vector2 ModifyDirection(Vector2 direction, float dt)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            heading = direction.normalized;

            // Empurroes mais fortes que o andar normal (ex: Recuo) mantem a forca
            if (direction.sqrMagnitude > 1.02f) return direction * speedBonus;
        }
        return heading * speedBonus;
    }
}

public class SlipperyToesEffect : StatusEffect
{
    private readonly SlipperyToesModifier modifier;

    public SlipperyToesEffect(float duration, float speedBonus = 1.15f)
    {
        Duration = duration;
        modifier = new SlipperyToesModifier(speedBonus);
    }

    public override void OnApply(PlayerContext ctx)
    {
        ctx.Movement.MovementModifiers.Register(modifier);
    }

    public override void OnRemove(PlayerContext ctx)
    {
        ctx.Movement.MovementModifiers.Unregister(modifier);
    }
}
