using UnityEngine;

// Sonambulo: quando o jogador solta as teclas, o corpo continua andando mais um pouquinho
// na ultima direcao antes de parar. Combina com o Recuo: depois do empurrao ele continua deslizando pra tras.
public class SleepwalkerModifier : IMovementModifier
{
    private readonly float extraTime;
    private Vector2 lastDirection = Vector2.zero;
    private float peakMagnitude;
    private float idleTime;

    public SleepwalkerModifier(float extraTime)
    {
        this.extraTime = extraTime;
    }

    public Vector2 ModifyDirection(Vector2 direction, float dt)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            // Lembra a direcao e a maior intensidade do movimento (um empurrao de recuo vai
            // diminuindo ate zero; sem isso o "sonambulo" so continuaria o finalzinho fraco dele)
            if (idleTime > 0f) peakMagnitude = 0f;
            lastDirection = direction.normalized;
            peakMagnitude = Mathf.Max(peakMagnitude, Mathf.Min(direction.magnitude, 1f));
            idleTime = 0f;
            return direction;
        }

        idleTime += dt;
        return idleTime < extraTime ? lastDirection * peakMagnitude : Vector2.zero;
    }
}

public class SleepwalkerEffect : StatusEffect
{
    private readonly SleepwalkerModifier modifier;

    public SleepwalkerEffect(float duration, float extraTime = 0.5f)
    {
        Duration = duration;
        modifier = new SleepwalkerModifier(extraTime);
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
