using System.Collections.Generic;
using UnityEngine;

// Dono da lista de StatusEffects ativos no player, quase o mesmo tipo de escopo que o PlayerStatsHandler faz.
public class StatusEffectManager : MonoBehaviour
{
    private PlayerContext context;
    private readonly List<StatusEffect> activeEffects = new List<StatusEffect>();

    private void Start()
    {
        // CameraEffectsController.Instance so fica garantido depois que TODOS os Awake() da cena rodaram, entao essa montagem tem que ficar no Start().
        context = new PlayerContext
        {
            Movement = GetComponent<PlayerMovement>(),
            Stats = GetComponent<PlayerStatsHandler>(),
            Camera = CameraEffectsController.Instance
        };
    }

    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.OnTick(context, Time.deltaTime);

            if (effect.Duration > 0f)
            {
                effect.Duration -= Time.deltaTime;
                if (effect.Duration <= 0f)
                {
                    effect.OnRemove(context);
                    activeEffects.RemoveAt(i);
                }
            }
        }
    }

    public void ApplyEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        effect.OnApply(context);
    }

    // Podemos usar pra itens que removem um debuff especifico antes da duracao acabar ou algo assim tipo de shit.
    public void RemoveEffect(StatusEffect effect)
    {
        if (activeEffects.Remove(effect))
        {
            effect.OnRemove(context);
        }
    }
}
