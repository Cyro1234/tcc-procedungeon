using UnityEngine;

// Literalmente gira a camera no eixo Z a cada segundo
public class LabyrinthitisModifier : ICameraModifier
{
    private readonly float degreesPerSecond;
    private float accumulatedAngle;

    public LabyrinthitisModifier(float degreesPerSecond = 5f)
    {
        this.degreesPerSecond = degreesPerSecond;
    }

    public void Modify(ref CameraModifierContext ctx, float dt)
    {
        accumulatedAngle = Mathf.Repeat(accumulatedAngle + degreesPerSecond * dt, 360f);
        ctx.rotation = Quaternion.Euler(0f, 0f, accumulatedAngle) * ctx.rotation;
    }
}

public class LabyrinthitisEffect : StatusEffect
{
    private readonly LabyrinthitisModifier modifier;

    public LabyrinthitisEffect(float duration = 0f, float degreesPerSecond = 25f)
    {
        Duration = duration;
        modifier = new LabyrinthitisModifier(degreesPerSecond);
    }

    public override void OnApply(PlayerContext ctx)
    {
        ctx.Camera?.CameraModifiers.Register(modifier);
    }

    public override void OnRemove(PlayerContext ctx)
    {
        ctx.Camera?.CameraModifiers.Unregister(modifier);
    }
}
