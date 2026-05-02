using UnityEngine;


// Aqui tem uns exemplos de debuffs que montei

public class DebuffExamples : MonoBehaviour, IDebuff
{
    private PlayerStatsHandler stats;
    [SerializeField]private HeartSystem heartSystem;

    public void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
    }

    public void Apply(PlayerStatsHandler stats)
    {
        stats.AddTemporaryModifier(new StatModifier("MudarDirecao" ,StatType.Speed, ModifierSource.Debuff, ModifierType.Percent, -2f, 5f));
        //stats.AddTemporaryModifier(new StatModifier("VIDAAAAA", StatType.Health, ModifierSource.Debuff, ModifierType.Flat, 2f, 0f));


        //heartSystem.updateMaxLife(); // Atualiza a vida maxima - (TODO: ARRUMAR PARA NAO AUMENTAR A VIDA AO APLICAR OUTROS DEBUFFS E TALS)

        Debug.Log("DEVERIA TER AUMENTADO A VIDA");
        
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