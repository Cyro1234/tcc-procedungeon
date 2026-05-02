using UnityEngine;
using System.Collections.Generic;

// Sumario: Oi galera esse arquivo que define como cada inimigo é montado! Ele é bem rudimentar só pra testar a montagem com geração procedural.
// Por enquanto usei sprites proprios beeeeem feinhos e sem animação.

public class EnemyModularBuilder : MonoBehaviour
{
    [Header("BodyParts Renderer")]  // Onde cada sprite vai ser renderizado
    public SpriteRenderer headRenderer;
    public SpriteRenderer torsoRenderer;
    public List<SpriteRenderer> armRenderers;
    public SpriteRenderer legRenderer;

    public enum StatType
    {
        Speed,
        Health,
        Attack,
        Knockback
    }

    public enum BonusType
    {
        Flat,
        Multiplier
    }

    [System.Serializable]
    public struct StatData
    {
        public float bonus;
        public BonusType bonusType;
        public StatType stat;
    }

    [System.Serializable]
    public class EnemyPart
    {
        public Sprite sprite;
        public List<StatData> statMultipliers = new List<StatData>();
    }

    [Header("Sprite List")]     // Listas de sprites para cada parte do corpo (da pra melhorar como ele pega isso)
    public List<EnemyPart> headOptions;
    public List<EnemyPart> torsoOptions;
    public List<EnemyPart> armOptions;
    public List<EnemyPart> legOptions;

    // Metodo que vai montar o inimigo com base na seed, sprites aleatorios
    public void EnemyBuild()
    {
        EnemyMovement movementScript = GetComponent<EnemyMovement>();

        // Listas para pegar as partes que foram roletadas e depois calcular todos os bonus acumulados e tals
        List<EnemyPart> chosenParts = new List<EnemyPart>();

        if (headOptions.Count > 0)
        {
            EnemyPart chosenHead = headOptions[Rng.EnemyRange(0, headOptions.Count)];
            headRenderer.sprite = chosenHead.sprite;
            chosenParts.Add(chosenHead);
        }

        if (torsoOptions.Count > 0)
        {
            EnemyPart chosenTorso = torsoOptions[Rng.EnemyRange(0, torsoOptions.Count)];
            torsoRenderer.sprite = chosenTorso.sprite;
            chosenParts.Add(chosenTorso);
        }

        if (armOptions.Count > 0 && armRenderers != null && armRenderers.Count > 0)
        {
            EnemyPart chosenArm = armOptions[Rng.EnemyRange(0, armOptions.Count)];
            foreach (var arm in armRenderers)
            {
                if (arm != null)
                    arm.sprite = chosenArm.sprite;
            }
            chosenParts.Add(chosenArm);
        }

        if (legOptions.Count > 0)
        {
            EnemyPart chosenLeg = legOptions[Rng.EnemyRange(0, legOptions.Count)];
            legRenderer.sprite = chosenLeg.sprite;
            chosenParts.Add(chosenLeg);
        }

        // Calcula e aplica o bonus do que foi definido no script de movimento eba
        if (movementScript != null)
        {
            CalculateAndApplyStats(chosenParts, movementScript);
        }

        Debug.Log($"{gameObject.name} montado com sucesso via Seed!");
    }

    // Calcula todos os bonus acumulados do que foi sorteado mega-cena e envia pro script de movimento aplicar
    private void CalculateAndApplyStats(List<EnemyPart> parts, EnemyMovement movementScript)
    {
        // Valores iniciais

        // Multiplicadores comecam em 1 pra representar 100%
        float speedFlat = 0f; float speedMult = 1f;
        float healthFlat = 0f; float healthMult = 1f;
        float attackFlat = 0f; float attackMult = 1f;
        float knockbackFlat = 0f; float knockbackMult = 1f;

        // Passa por todas as partes escolhidas
        foreach (EnemyPart part in parts)
        {
            // Passa por todos os modificadores de status de cada parte
            foreach (StatData statData in part.statMultipliers)
            {
                switch (statData.stat)
                {
                    case StatType.Speed:
                        if (statData.bonusType == BonusType.Flat) speedFlat += statData.bonus;
                        else speedMult += statData.bonus;
                        break;

                    case StatType.Health:
                        if (statData.bonusType == BonusType.Flat) healthFlat += statData.bonus;
                        else healthMult += statData.bonus;
                        break;

                    case StatType.Attack:
                        if (statData.bonusType == BonusType.Flat) attackFlat += statData.bonus;
                        else attackMult += statData.bonus;
                        break;

                    case StatType.Knockback:
                        if (statData.bonusType == BonusType.Flat) knockbackFlat += statData.bonus;
                        else knockbackMult += statData.bonus;
                        break;
                }
            }
        }

        // Aplica o bonus pro script da movimentacao ai co ckrr, co ckrr!
        movementScript.ApplyModularStats(speedFlat, speedMult, healthFlat, healthMult, knockbackFlat, knockbackMult);

        // Aplica o bonus de ataque pro script de ataque
        EnemyDamage damageScript = GetComponentInChildren<EnemyDamage>();

        if (damageScript != null)
        {
            damageScript.ApplyDamageBonus(attackFlat, attackMult);
        }
    }

    void Start()
    {
        // Monta o inimigo.
        EnemyBuild();
    }
}