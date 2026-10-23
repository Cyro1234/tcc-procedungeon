using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Header("Animação de entrada")]
    [SerializeField, Min(0f)] private float duracaoFade = 0.2f;
    [SerializeField, Min(0.01f)] private float duracaoPop = 0.25f;
    [SerializeField, Min(0f)] private float intervaloEntreCartas = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float escalaInicial = 0.6f;

    public bool IsOpen => downgradePanel != null && downgradePanel.activeSelf;

    private Coroutine animacaoEntrada;
    private bool animando;
    private Vector3[] escalasOriginais;

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
            return;
        }

        // Entra no sorteio por codigo, sem precisar editar a lista salva na cena
        GarantirNoSorteio(StatusEffectKind.Snail, "Caracol Imortal");
    }

    private void GarantirNoSorteio(StatusEffectKind kind, string nome)
    {
        if (gameStatusEffectDebuffPool == null) gameStatusEffectDebuffPool = new List<StatusEffectDowngradeOption>();
        foreach (var opcao in gameStatusEffectDebuffPool)
            if (opcao.statusEffectKind == kind) return;

        gameStatusEffectDebuffPool.Add(new StatusEffectDowngradeOption
        {
            displayName = nome,
            statusEffectKind = kind,
            hasDuration = false // permanente
        });
    }

    // hotkey pra que as teclas 1, 2, 3 escolhem a opcao correspondente no menu de downgrade
    private void Update()
    {
        if (!IsOpen || animando || Keyboard.current == null) return;

        for (int i = 0; i < currentChoices.Count && i < 9; i++)
        {
            Key key = Key.Digit1 + i;
            if (Keyboard.current[key].wasPressedThisFrame && choiceButtons[i].gameObject.activeSelf)
            {
                SelectChoice(i);
                return;
            }
        }
    }

    private void OnEnable() => GameFlow.Changed += AoAlterarEstado;

    private void OnDisable()
    {
        GameFlow.Changed -= AoAlterarEstado;
        FinalizarAnimacao();
        pendingGenerator = null;
        if (downgradePanel != null) downgradePanel.SetActive(false);
        GameFlow.Resume(GameFlow.State.Downgrade);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void AoAlterarEstado()
    {
        if (GameFlow.Current == GameFlow.State.Downgrade) return;
        if ((downgradePanel != null && downgradePanel.activeSelf) || pendingGenerator != null)
        {
            pendingGenerator = null;
            if (downgradePanel != null) downgradePanel.SetActive(false);
            GameFlow.Resume(GameFlow.State.Downgrade);
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
        if (GameFlow.Current == GameFlow.State.GameOver || GameFlow.Current == GameFlow.State.Downgrade) return;
        pendingGenerator = generator;

        RollChoices();

        if (playerMovement != null) playerMovement.ForcarParada();
        GameFlow.Pause(GameFlow.State.Downgrade);
        downgradePanel.SetActive(true);

        if (animacaoEntrada != null) StopCoroutine(animacaoEntrada);
        animacaoEntrada = StartCoroutine(AnimarEntrada());
    }

    private System.Collections.IEnumerator AnimarEntrada()
    {
        if (escalasOriginais == null)
        {
            escalasOriginais = new Vector3[choiceButtons.Count];
            for (int i = 0; i < choiceButtons.Count; i++)
                escalasOriginais[i] = choiceButtons[i] != null ? choiceButtons[i].transform.localScale : Vector3.one;
        }

        var grupo = downgradePanel.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = downgradePanel.AddComponent<CanvasGroup>();
        animando = true;
        grupo.interactable = false;
        grupo.alpha = 0f;
        for (int i = 0; i < choiceButtons.Count; i++)
            if (choiceButtons[i] != null) choiceButtons[i].transform.localScale = escalasOriginais[i] * escalaInicial;

        float total = duracaoFade + intervaloEntreCartas * Mathf.Max(0, choiceButtons.Count - 1) + duracaoPop;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = duracaoFade > 0f ? Mathf.Clamp01(t / duracaoFade) : 1f;
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (choiceButtons[i] == null) continue;
                float p = Mathf.Clamp01((t - duracaoFade * 0.5f - intervaloEntreCartas * i) / duracaoPop);
                choiceButtons[i].transform.localScale = escalasOriginais[i] * Mathf.LerpUnclamped(escalaInicial, 1f, VoltaComSobra(p));
            }
            yield return null;
        }

        animacaoEntrada = null;
        FinalizarAnimacao();
    }

    private void FinalizarAnimacao()
    {
        if (animacaoEntrada != null) StopCoroutine(animacaoEntrada);
        animacaoEntrada = null;
        animando = false;
        if (downgradePanel != null && downgradePanel.TryGetComponent(out CanvasGroup grupo))
        {
            grupo.alpha = 1f;
            grupo.interactable = true;
        }
        if (escalasOriginais == null) return;
        for (int i = 0; i < choiceButtons.Count && i < escalasOriginais.Length; i++)
            if (choiceButtons[i] != null) choiceButtons[i].transform.localScale = escalasOriginais[i];
    }

    private static float VoltaComSobra(float p)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = p - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
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
                choiceLabels[i].text = $"<size=70%>[{i + 1}]</size>\n" + BuildLabel(choice);
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
            string descricao = StatusEffectFactory.Describe(choice.statusEffectOption.statusEffectKind);
            if (!string.IsNullOrEmpty(descricao)) descricao = $"\n<size=65%>{descricao}</size>";

            return choice.statusEffectOption.hasDuration
                ? $"{choice.statusEffectOption.displayName}\n({choice.magnitude:0.#}s){descricao}"
                : $"{choice.statusEffectOption.displayName}\n(Permanente){descricao}";
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
        if (GameFlow.Current != GameFlow.State.Downgrade || !downgradePanel.activeSelf || animando ||
            index < 0 || index >= currentChoices.Count) return;
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
        var health = playerStats.GetComponent<HeartSystem>();
        if (health != null)
        {
            health.AtualizarVidaMaxima();
            if (health.IsDead)
            {
                pendingGenerator = null;
                GameFlow.Resume(GameFlow.State.Downgrade);
                return;
            }
        }

        if (playerMovement != null) playerMovement.ForcarParada();

        var generator = pendingGenerator;
        pendingGenerator = null;
        if (generator != null) generator.GenerateDungeon();
        GameFlow.Resume(GameFlow.State.Downgrade);
    }
}
