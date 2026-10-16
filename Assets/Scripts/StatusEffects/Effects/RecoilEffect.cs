using UnityEngine;

// Recuo: cada golpe empurra o jogador pra tras (direcao contraria pra onde ele esta olhando).
// O empurrao entra no PlayerMovement como se fosse input (AddPush), entao passa pelos outros debuffs!!!!

public class RecoilEffect : StatusEffect
{
    private readonly float strength;
    private PlayerContext context;

    public RecoilEffect(float duration, float strength = 2.2f)
    {
        Duration = duration;
        this.strength = strength;
    }

    public override void OnApply(PlayerContext ctx)
    {
        context = ctx;
        if (ctx.Attack != null) ctx.Attack.Swung += OnSwing;
    }

    public override void OnRemove(PlayerContext ctx)
    {
        if (ctx.Attack != null) ctx.Attack.Swung -= OnSwing;
        context = null;
    }

    private void OnSwing()
    {
        if (context == null || context.Movement == null) return;

        context.Movement.AddPush(-context.Movement.FacingDirection * strength);
        GameEffects.Instance.Shake(0.08f);
    }
}
