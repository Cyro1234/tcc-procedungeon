public class PlayerContext
{
    public PlayerMovement Movement;
    public CameraEffectsController Camera;
    public PlayerStatsHandler Stats;
}

// Cuida do ciclo de vida do efeito de status, tipo alicar, tickar para buffs com tempo e remover. A aplicacao de modificadores fica nas classes concretas,
// que registram modificadores nos pipelines por ctx!!!!!!!!!!!!!!!!!!!!!
public abstract class StatusEffect
{
    // valor 0 significa que e permanente ate ser removido manualmente
    public float Duration { get; set; }

    public virtual void OnApply(PlayerContext ctx) { }
    public virtual void OnTick(PlayerContext ctx, float dt) { }
    public virtual void OnRemove(PlayerContext ctx) { }
}
