using UnityEngine;


// Aqui tem uns exemplos de debuffs que montei

public class DebuffExamples : MonoBehaviour, IDebuff
{
    private PlayerStatsHandler stats;

    public void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
    }

    public void Apply(PlayerStatsHandler stats)
    {
        stats.AddTemporaryModifier(new StatModifier("MudarDirecao" ,StatType.Speed, ModifierSource.Debuff, ModifierType.Percent, -2f, 5f));
    }

    // Exemplos de debuffs pra testar:

    //public void Apply(PlayerStatsHandler stats)
    //{
    //    stats.AddTemporaryModifier(new StatModifier("DiminuirDano", StatType.Speed, ModifierSource.Debuff, ModifierType.Percent, -2f, 5f));
    //}

    //public void Apply(PlayerStatsHandler stats)
    //{
    //    stats.AddTemporaryModifier(new StatModifier("MudarDirecao", StatType.Speed, ModifierSource.Debuff, ModifierType.Percent, -2f, 5f));
    //}
}