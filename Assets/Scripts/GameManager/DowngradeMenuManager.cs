using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Menu que aparece depois da fase terminar!!! O jogador escolhe 1 downgrade entre N sorteados.
public class DowngradeMenuManager : MonoBehaviour
{
    public static DowngradeMenuManager Instance;

    [SerializeField] private PlayerStatsHandler playerStats;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private StatusEffectManager statusEffects;
    [SerializeField] private GameObject downgradePanel;
    [SerializeField] private Sprite buttonSprite;

    [FormerlySerializedAs("downgradePool")]
    [SerializeField] private List<DowngradeOption> playerStatusDebuffPool;
    [SerializeField] private List<StatusEffectDowngradeOption> gameStatusEffectDebuffPool;

    [Header("UI")]
    [SerializeField] private List<Button> choiceButtons;
    [SerializeField] private List<TMP_Text> choiceLabels;

    private AbstractDungeonGenerator pendingGenerator;
    private readonly List<RolledChoice> currentChoices = new List<RolledChoice>();

    // Uma escolha ja sorteada e mostrada no menu ou embrulha um DowngradeOption
    // (com a magnitude ja sorteada), ou um StatusEffectDowngradeOption.
    private class RolledChoice
    {
        public DowngradeOption statOption;
        public StatusEffectDowngradeOption statusEffectOption;
        public float magnitude;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Debug pro botao abrir qnd eu quiser
    //public void OpenMenuForTesting()
    //{
    //    OpenMenu(null);
    //}

    // Chamado pela escada em vez de gerar a proxima dungeon direto
    public void OpenMenu(AbstractDungeonGenerator generator)
    {
        pendingGenerator = generator;

        RollChoices();

        if (playerMovement != null) playerMovement.ForcarParada();
        downgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void RollChoices()
    {
        currentChoices.Clear();

        List<int> availableStatIndexes = new List<int>();
        for (int i = 0; i < playerStatusDebuffPool.Count; i++)
        {
            if (playerStatusDebuffPool[i].isActive) availableStatIndexes.Add(i);
        }

        List<int> availableStatusEffectIndexes = new List<int>();
        for (int i = 0; i < gameStatusEffectDebuffPool.Count; i++)
        {
            if (gameStatusEffectDebuffPool[i].isActive) availableStatusEffectIndexes.Add(i);
        }

        int amount = Mathf.Min(choiceButtons.Count, availableStatIndexes.Count + availableStatusEffectIndexes.Count);

        for (int i = 0; i < amount; i++)
        {
            // Sorteia um slot dentro dos dois pools juntos, pra debuffs simples e complexos aparecerem misturados no menu com a mesma chance
            int pick = Rng.DebuffRange(0, availableStatIndexes.Count + availableStatusEffectIndexes.Count);

            RolledChoice choice;
            if (pick < availableStatIndexes.Count)
            {
                int optionIndex = availableStatIndexes[pick];
                availableStatIndexes.RemoveAt(pick);

                DowngradeOption option = playerStatusDebuffPool[optionIndex];
                float magnitude = option.useRange
                    ? Mathf.Lerp(option.minMagnitude, option.maxMagnitude, Rng.DebuffValue())
                    : option.fixedMagnitude;

                choice = new RolledChoice { statOption = option, magnitude = magnitude };
            }
            else
            {
                int statusEffectIndex = pick - availableStatIndexes.Count;
                int optionIndex = availableStatusEffectIndexes[statusEffectIndex];
                availableStatusEffectIndexes.RemoveAt(statusEffectIndex);

                StatusEffectDowngradeOption seOption = gameStatusEffectDebuffPool[optionIndex];
                float duration = 0f;
                if (seOption.hasDuration)
                {
                    duration = seOption.isRange
                        ? Mathf.Lerp(seOption.minDuration, seOption.maxDuration, Rng.DebuffValue())
                        : seOption.fixedDuration;
                }

                choice = new RolledChoice { statusEffectOption = seOption, magnitude = duration };
            }

            currentChoices.Add(choice);

            int buttonIndex = i; // captura local pro listener
            choiceButtons[i].gameObject.SetActive(true);
            Button btn = choiceButtons[i];
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectChoice(buttonIndex));

            Image btnImage = btn.GetComponent<Image>();

            if (buttonSprite != null)
            {
                btnImage.sprite = buttonSprite;
            }
            //else if (defaultSpriteFrame != null)
            //{
            //    btnImage.sprite = defaultSpriteFrame;
            //}

            if (i < choiceLabels.Count)
            {
                choiceLabels[i].text = BuildLabel(choice);
            }
        }

        // Esconde botoes sobrando caso os pools tenham menos opcoes que botoes
        for (int i = amount; i < choiceButtons.Count; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }
    }

    private string BuildLabel(RolledChoice choice)
    {
        if (choice.statusEffectOption != null)
        {
            return choice.statusEffectOption.hasDuration
                ? $"{choice.statusEffectOption.displayName}\n({choice.magnitude:0.#}s)"
                : $"{choice.statusEffectOption.displayName}\n(Permanente)";
        }

        DowngradeOption option = choice.statOption;
        float magnitude = choice.magnitude;

        string sign = option.modifierSource == ModifierSource.Buff ? "+" : "-";

        string valueText = option.modifierType == ModifierType.Percent
            ? $"{sign}{magnitude * 100f:0}%"
            : $"{sign}{magnitude:0.#}";

        string statName = GetStatDisplayName(option.statType);

        return $"{option.displayName}\n{statName} ({valueText})";
    }

    private string GetStatDisplayName(StatType statType)
    {
        switch (statType)
        {
            case StatType.Health: return "Vida";
            case StatType.Speed: return "Velocidade";
            case StatType.Damage: return "Dano";
            case StatType.AttackCooldown: return "Recarga de Ataque";
            default: return statType.ToString();
        }
    }

    public void SelectChoice(int index)
    {
        RolledChoice choice = currentChoices[index];

        if (choice.statusEffectOption != null)
        {
            StatusEffect effect = StatusEffectFactory.Create(choice.statusEffectOption.statusEffectKind, choice.magnitude);
            if (effect != null && statusEffects != null)
            {
                statusEffects.ApplyEffect(effect);
            }

            CloseMenuAndAdvance();
            return;
        }

        DowngradeOption option = choice.statOption;
        float signedValue = option.modifierSource == ModifierSource.Buff ? choice.magnitude : -choice.magnitude;

        StatModifier modifier = new StatModifier(
            option.displayName,
            option.statType,
            option.modifierSource,
            option.modifierType,
            signedValue
        );

        playerStats.AddSimpleModifier(modifier);

        CloseMenuAndAdvance();
    }

    private void CloseMenuAndAdvance()
    {
        downgradePanel.SetActive(false);
        Time.timeScale = 1f;
        if (playerMovement != null) playerMovement.ForcarParada();

        if (pendingGenerator != null)
        {
            pendingGenerator.GenerateDungeon();
            pendingGenerator = null;
        }
    }
}
