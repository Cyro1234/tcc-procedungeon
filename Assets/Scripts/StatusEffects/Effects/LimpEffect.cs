using UnityEngine;

// A ideia desse buff e de limitar o movimento do jogador como se fosse de um rpg antigo pq eu achei a ideia engracada.
public class HeavyLimpModifier : IMovementModifier
{
    private readonly float hopDuration;
    private readonly float pauseDuration;

    private float timer;
    private bool paused;

    public HeavyLimpModifier(float hopDuration, float pauseDuration)
    {
        this.hopDuration = hopDuration;
        this.pauseDuration = pauseDuration;
    }

    public Vector2 ModifyDirection(Vector2 direction, float dt)
    {
        // Esse aqui faz com que nao de pra andar na diagonal!
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            direction.y = 0f;
        }
        else
        {
            direction.x = 0f;
        }

        timer += dt;
        float currentPhaseDuration = paused ? pauseDuration : hopDuration;

        if (timer >= currentPhaseDuration)
        {
            timer = 0f;
            paused = !paused;
        }

        return paused ? Vector2.zero : direction;
    }
}

public class LimpEffect : StatusEffect
{
    private readonly HeavyLimpModifier modifier;

    public LimpEffect(float duration, float hopDuration = 0.2f, float pauseDuration = 0.2f)
    {
        Duration = duration;
        modifier = new HeavyLimpModifier(hopDuration, pauseDuration);
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
