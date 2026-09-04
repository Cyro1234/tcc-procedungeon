using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menu que aparece depois da fase terminar!!! O jogador escolhe 1 downgrade entre N sorteados.
public class DowngradeMenuManager : MonoBehaviour
{
    public static DowngradeMenuManager Instance;

    [SerializeField] private PlayerStatsHandler playerStats;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private GameObject downgradePanel;

    [SerializeField] private List<DowngradeOption> downgradePool;

    [Header("UI")]
    [SerializeField] private List<Button> choiceButtons;
    [SerializeField] private List<TMP_Text> choiceLabels;

    private AbstractDungeonGenerator pendingGenerator;
    private readonly List<DowngradeOption> currentChoices = new List<DowngradeOption>();
    private readonly List<float> currentRolledValues = new List<float>();

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
        currentRolledValues.Clear();

        List<int> availableIndexes = new List<int>();
        for (int i = 0; i < downgradePool.Count; i++) availableIndexes.Add(i);

        int amount = Mathf.Min(choiceButtons.Count, availableIndexes.Count);

        for (int i = 0; i < amount; i++)
        {
            int pick = Rng.DebuffRange(0, availableIndexes.Count);
            int optionIndex = availableIndexes[pick];
            availableIndexes.RemoveAt(pick);

            DowngradeOption option = downgradePool[optionIndex];
            float magnitude = Mathf.Lerp(option.minMagnitude, option.maxMagnitude, Rng.DebuffValue());

            currentChoices.Add(option);
            currentRolledValues.Add(magnitude);

            int buttonIndex = i; // captura local pro listener
            choiceButtons[i].gameObject.SetActive(true);
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => SelectChoice(buttonIndex));

            if (i < choiceLabels.Count)
            {
                choiceLabels[i].text = BuildLabel(option, magnitude);
            }
        }

        // Esconde botoes sobrando caso o pool tenha menos opcoes que botoes
        for (int i = amount; i < choiceButtons.Count; i++)
        {
            choiceButtons[i].gameObject.SetActive(false);
        }
    }

    private string BuildLabel(DowngradeOption option, float magnitude)
    {
        string sign = option.modifierSource == ModifierSource.Buff ? "+" : "-";

        string valueText = option.modifierType == ModifierType.Percent
            ? $"{sign}{magnitude * 100f:0}%"
            : $"{sign}{magnitude:0.#}";

        string statName = GetStatDisplayName(option.statType);

        return $"{option.displayName}\n{statName} ({valueText})";
    }

    // Nome do stat em portugues pra exibir no label do botao
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
        DowngradeOption option = currentChoices[index];
        float magnitude = currentRolledValues[index];
        float signedValue = option.modifierSource == ModifierSource.Buff ? magnitude : -magnitude;

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
