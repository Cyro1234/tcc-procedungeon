using System.Collections.Generic;
using UnityEngine;

public class PlayerStatsHandler : MonoBehaviour
{
    // TODO 

    // Debuffs devem ser triggados apos finalizar o nivel, menuzinho de escolha, 3 escolhas, no momento podem ser debuffs simples, mas no futuro usar mais complexos
    // Criação de debuffs mais complexos que interferem mais que só os status do jogador (debuff que fica girando a camera, etc e coisa e tal e tal)
    // Visualização dos buffs e debuffs ativos (UI, efeitos visuais, etc)

    // ---- Sumário!! Wowowww --------------------------------------------------------------------------------------------------------------------------------------

    // Oi gente! Por enquanto só foi feito suporte para buffs e debuffs simples como modificadores de dano e etc, esse pacote de classes é bem abrangente e da pra
    // usar os metodos dele em conjunto com outros scripts pra criar eventos mais complexos!! 

    // Ele tem suporte para modificadores permanentes e temporarios, modificadores flat e percentuais, e modificadores de diferentes fontes (itens, buffs e debuffs)

    // Status padrão do jogador
    private int basePlayerMaxHearts = 5;
    private int maxPossibleHealth = 5; // Vida maxima possivel do jogador. Se quiser aumentar, criar mais coracoes no canvas e adicionar no heartsystem
    private float basePlayerSpeed = 10f;
    private float basePlayerDamage = 1f;
    private float basePlayerAttackCooldown = 0f;

    [Header("Equipamento passivo")]
    [SerializeField, Min(0f)] private float bonusVelocidadeBota = 0.15f;
    [Tooltip("Fracao somada a velocidade (0.6 = +60%). Deve ser bem maior que a da bota comum.")]
    [SerializeField, Min(0f)] private float bonusVelocidadeBotaLendaria = 0.4f;
    [Tooltip("Ate esse bonus total as botas rendem inteiras (0.6 = +60%).")]
    [SerializeField, Min(0f)] private float softCapBotas = 0.5f;
    [Tooltip("Acima do soft cap, cada bota rende so essa fracao do bonus dela (0.5 = metade).")]
    [SerializeField, Range(0f, 1f)] private float eficienciaAcimaSoftCap = 0.5f;
    [Tooltip("Bonus maximo somando todas as botas (1 = +100%, o dobro da velocidade).")]
    [SerializeField, Min(0f)] private float hardCapBotas = 0.75f;

    private StatModifier modificadorBota;
    public bool TemBotaEquipada => botas.Count > 0;
    private readonly List<(float bonus, bool lendaria)> botas = new List<(float, bool)>();
    public bool TemBotaLendaria
    {
        get
        {
            foreach (var bota in botas)
                if (bota.lendaria) return true;
            return false;
        }
    }
    public float BonusTotalBotas => modificadorBota != null ? modificadorBota.value : 0f;
    public bool PossuiBotasAtivas => botas.Count > 0;
    public bool BotasNoMaximo => botas.Count > 0 && BonusTotalBotas >= hardCapBotas - 0.0001f;


    [Header("Pocao de poder")]
    [Tooltip("Fracao somada ao dano (0.5 = +50%).")]
    [SerializeField, Min(0f)] private float bonusDanoPocaoPoder = 0.5f;
    [Tooltip("Duracao em segundos. 0 deixa o efeito permanente.")]
    [SerializeField, Min(0f)] private float duracaoPocaoPoder = 30f;
    [SerializeField, Min(0f)] private float bonusDanoPocaoExtra = 0.25f;
    [SerializeField, Min(0f)] private float bonusDanoPocaoMaximo = 1f;
    private StatModifier modificadorPocaoPoder;
    public bool PocaoPoderAtiva => modificadorPocaoPoder != null && tempModifiers.Contains(modificadorPocaoPoder);

    // Cor usada no popup, no texto do bau e no placeholder da hotbar
    public static readonly Color CorPocaoPoder = new Color(1f, 0.55f, 0.2f);


    // Lista de modificadores permanentes
    private List<StatModifier> modifiers = new List<StatModifier>();

    public void EquiparBota()
    {
        AdicionarBota(bonusVelocidadeBota, false);
    }

    public void EquiparBotaLendaria()
    {
        AdicionarBota(bonusVelocidadeBotaLendaria, true);
    }

    private void AdicionarBota(float bonus, bool lendaria)
    {
        botas.Add((bonus, lendaria));

        // As botas somam ate 100% (cap)
        float bruto = 0f;
        foreach (var bota in botas) bruto += bota.bonus;
        float efetivo = bruto <= softCapBotas ? bruto : softCapBotas + (bruto - softCapBotas) * eficienciaAcimaSoftCap;
        efetivo = Mathf.Min(efetivo, hardCapBotas);

        if (modificadorBota != null) RemoveSimpleModifier(modificadorBota);
        modificadorBota = new StatModifier("Botas de velocidade", StatType.Speed,
            ModifierSource.Item, ModifierType.Percent, efetivo);
        AddSimpleModifier(modificadorBota);
    }


    public bool BeberPocaoPoder()
    {
        Vector3 posicaoTexto = transform.position + Vector3.up * 0.8f;
        if (duracaoPocaoPoder > 0f)
        {
            if (PocaoPoderAtiva)
            {
                modificadorPocaoPoder.duration = duracaoPocaoPoder;
                GameEffects.Instance.Popup(posicaoTexto, "Poder renovado!", CorPocaoPoder);
                return true;
            }
            modificadorPocaoPoder = new StatModifier("Pocao de poder", StatType.Damage,
                ModifierSource.Buff, ModifierType.Percent, bonusDanoPocaoPoder, duracaoPocaoPoder);
            AddTemporaryModifier(modificadorPocaoPoder);
            GameEffects.Instance.Popup(posicaoTexto, $"+{bonusDanoPocaoPoder * 100f:0}% Dano!", CorPocaoPoder);
            return true;
        }

        float atual = PocaoPoderAtiva ? modificadorPocaoPoder.value : 0f;
        if (atual >= bonusDanoPocaoMaximo - 0.0001f)
        {
            GameEffects.Instance.Popup(posicaoTexto, "Poder no máximo!", CorPocaoPoder);
            return false;
        }

        float novo = Mathf.Min(bonusDanoPocaoMaximo, atual <= 0f ? bonusDanoPocaoPoder : atual + bonusDanoPocaoExtra);
        if (modificadorPocaoPoder != null) RemoveTemporaryModifier(modificadorPocaoPoder);
        modificadorPocaoPoder = new StatModifier("Pocao de poder", StatType.Damage,
            ModifierSource.Buff, ModifierType.Percent, novo, 0f);
        AddTemporaryModifier(modificadorPocaoPoder);
        GameEffects.Instance.Popup(posicaoTexto, $"+{(novo - atual) * 100f:0}% Dano! (total +{novo * 100f:0}%)", CorPocaoPoder);
        return true;
    }


    public void AddSimpleModifier(StatModifier modifier)    // Adiciona um modificador permanente!!
    {
        modifiers.Add(modifier);
    }

    public void RemoveSimpleModifier(StatModifier modifier)
    {
        modifiers.Remove(modifier);     // Remove um modificador permanente!!
    }

    // Lista de modificadores temporarios
    private List<StatModifier> tempModifiers = new List<StatModifier>();

    public void AddTemporaryModifier(StatModifier modifier)
    {
        tempModifiers.Add(modifier);    // Adiciona um modificador temporario!!
    }

    public void RemoveTemporaryModifier(StatModifier modifier)
    {
        tempModifiers.Remove(modifier);     // Remove um modificador temporario!!
    }

    // Eu separei os dois pra facilitar a organização e não ter que ficar checando a duração dos modificadores permanentes.

    private void Start()
    {
        // Fiz isso so pra debugar mesmo!! Ta comentado por enquanto!

        //print($"Velocidade base do Player {GetPlayerWalkSpeed()}");
        //print($"Dano base do Player {GetPlayerDamage()}");
        //print($"Cooldown do Ataque do Player {GetPlayerAttackCooldown()}");
        //print($"Vida base do Player {GetPlayerMaxHearts()}");
    }

    private void Update()
    {
        // Atualiza a lista de debuffs temporarios, checando se algum deles expirou e removendo eles da lista caso sim!
        for (int i = tempModifiers.Count - 1; i >= 0; i--)
        {
            // Checa se tem duração!!
            if (tempModifiers[i].duration > 0)
            {
                tempModifiers[i].duration -= Time.deltaTime;    // Se tiver, espera ela

                if (tempModifiers[i].duration <= 0)     // Quando a duração acabar ele remove!!
                {
                    string expiredName = tempModifiers[i].statName;     // Salva o nome antes de remover ele da lista, usado em DEBUG!!!!
                    if (tempModifiers[i] == modificadorPocaoPoder)
                        GameEffects.Instance.Popup(transform.position + Vector3.up * 0.8f, "O poder acabou...", CorPocaoPoder);
                    tempModifiers.RemoveAt(i);
                    print($"Modificador {expiredName} expirou e foi removido!");
                }
            }
        }
    }

    // Recebe o valor atual do stat base e o tipo do stat, e retorna o valor final considerando os modificadores aplicados

    public float GetPlayerWalkSpeed()
    {
        return CalculateStat(basePlayerSpeed, StatType.Speed);
    }

    public float GetPlayerDamage()
    {
        return CalculateStat(basePlayerDamage, StatType.Damage);
    }

    public float GetPlayerMaxHearts()
    {
        float hp = CalculateStat(basePlayerMaxHearts, StatType.Health);
        if (hp > maxPossibleHealth)
        {
            hp = maxPossibleHealth;
        }
        return hp;
    }

    public float GetPlayerAttackCooldown()
    {
        return CalculateStat(basePlayerAttackCooldown, StatType.AttackCooldown);
    }

    // Calcula o valor final do stat considerando todos os modificadores aplicados - Permanentes
    private float CalculateStat(float baseValue, StatType type) 
    {
        float finalValue = baseValue;

        //-----------------------Permanentes-----------------------

        // Calculo de valores flat permanentes!! (Somas simples e etc)
        foreach (var mod in modifiers)
        {
            if (mod.statType == type && mod.modifierType == ModifierType.Flat)
            {
                finalValue += mod.value;
            }
        }

        // Calculo de valores percentuais permanentes!!
        foreach (var mod in modifiers)
        {
            if (mod.statType == type && mod.modifierType == ModifierType.Percent)
            {
                finalValue *= (1f + mod.value);
            }
        }

        //-----------------------Temporarios-----------------------

        // Calculo de valores flat temporarios!! (Somas simples e etc)
        foreach (var mod in tempModifiers)
        {
            if (mod.statType == type && mod.modifierType == ModifierType.Flat)
            {
                finalValue += mod.value;
            }
        }

        // Calculo de valores percentuais temporarios!!
        foreach (var mod in tempModifiers)
        {
            if (mod.statType == type && mod.modifierType == ModifierType.Percent)
            {
                finalValue *= (1f + mod.value);
            }
        }

        return finalValue;
    }
}
