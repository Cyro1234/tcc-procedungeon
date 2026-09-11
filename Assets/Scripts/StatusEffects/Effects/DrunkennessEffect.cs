using UnityEngine;

// Roda a direcao do input num angulo que oscila com o tempo, entao o jogador fica andando torto o tempo todo (esse e meio irado eu gosto dele)
public class DrunkennessModifier : IMovementModifier
{
    private readonly float wobbleSpeed;
    private readonly float wobbleAmplitudeDegrees;
    private float phase;

    public DrunkennessModifier(float wobbleSpeed = 1.5f, float wobbleAmplitudeDegrees = 60f)
    {
        this.wobbleSpeed = wobbleSpeed;
        this.wobbleAmplitudeDegrees = wobbleAmplitudeDegrees;
    }

    public Vector2 ModifyDirection(Vector2 direction, float dt)
    {
        phase += dt * wobbleSpeed;
        float angle = Mathf.Sin(phase) * wobbleAmplitudeDegrees;
        return Quaternion.Euler(0f, 0f, angle) * direction;
    }
}

public class DrunkennessEffect : StatusEffect
{
    private readonly DrunkennessModifier modifier;

    public DrunkennessEffect(float duration, float wobbleSpeed = 1.5f, float wobbleAmplitudeDegrees = 60f)
    {
        Duration = duration;
        modifier = new DrunkennessModifier(wobbleSpeed, wobbleAmplitudeDegrees);
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
