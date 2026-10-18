using System.Collections.Generic;
using UnityEngine;

// Lag: o movimento acontece segundos depois do comando, como jogar online com ping alto!!!
// Guarda uma fila com o historico do input e devolve o que foi apertado la atras.
public class LagModifier : IMovementModifier, IInputDelay
{
    private readonly float delay;
    private readonly Queue<(float time, Vector2 direction)> history = new Queue<(float, Vector2)>();
    private float clock;
    private Vector2 current = Vector2.zero;

    public float Delay => delay;

    public LagModifier(float delay)
    {
        this.delay = delay;
    }

    public Vector2 ModifyDirection(Vector2 direction, float dt)
    {
        clock += dt;
        history.Enqueue((clock, direction));

        // Solta da fila tudo que ja "chegou"
        while (history.Count > 0 && history.Peek().time <= clock - delay)
        {
            current = history.Dequeue().direction;
        }
        return current;
    }
}

public class LagEffect : StatusEffect
{
    private readonly LagModifier modifier;

    public LagEffect(float duration, float delay = 0.25f)
    {
        Duration = duration;
        modifier = new LagModifier(delay);
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
