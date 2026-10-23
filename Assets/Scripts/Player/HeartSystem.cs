using TMPro;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Audio;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    public GameObject Escudo;
    private PlayerStatsHandler stats;
    int life;

    // Variável para controlar se o jogador tem o escudo
    //public bool hasShield = false;

    //Instancia da vida do escudo inicial
    public int shieldHealth = 0;

    [Header("Game effects")]
    [Tooltip("Tempo de invencibilidade depois de tomar dano (o jogador pisca nesse periodo).")]
    [SerializeField] private float invincibilityTime = 0.9f;
    [Tooltip("Forca do empurrao que o jogador leva ao tomar dano.")]
    [SerializeField] private float knockbackForce = 12f;
    [Tooltip("Tempo de invencibilidade depois de reviver com a Pena da Fenix.")]
    [SerializeField] private float invencivelAoReviver = 2.5f;

    private GameOverManager gameOverManager;
    private AudioSource audioSource;
    private PlayerMovement movement;
    private SpriteRenderer spriteRenderer;

    private float invincibleUntil;
    private bool dead;

    public bool IsDead => dead;
    public bool IsFullHealth => life >= MaxLife;
    public int Life => life;
    public int Penas { get; private set; } // Pena da Fenix: cada uma e uma vida extra
    public bool IsInvulnerable => dead || Time.time < invincibleUntil;
    private int MaxLife => stats != null ? Mathf.Max(0, (int)stats.GetPlayerMaxHearts()) : 3;

    //[SerializeField] private AudioClip hurtSound;
    // Opcional: Adicione um som para quando o escudo quebrar!
    //[SerializeField] private AudioClip shieldBreakSound;

    private void Start()
    {
        stats = GetComponent<PlayerStatsHandler>();
        life = (int)stats.GetPlayerMaxHearts();       // O jogador inicia com a vida máxima definida no PlayerStatsHandler.cs
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        audioSource = GetComponent<AudioSource>();
        movement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        AtualizarVidaMaxima();
        gameObject.AddComponent<ShieldCounterHUD>().Inicializar(this);
        gameObject.AddComponent<BootBonusHUD>().Inicializar(this, stats);
        gameObject.AddComponent<PenaFenixHUD>().Inicializar(this);
        gameObject.AddComponent<PocaoForcaHUD>().Inicializar(this, stats);
    }

    void Update()
    {
        AtualizarVidaMaxima();

        // Decrementa os containers de vida na UI
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < life)
                hearts[i].SetActive(true);
            else
                hearts[i].SetActive(false);
        }

        // Pisca enquanto estiver invencivel depois de levar dano
        if (spriteRenderer != null && !dead)
        {
            bool blinking = Time.time < invincibleUntil;
            spriteRenderer.enabled = !blinking || Mathf.Repeat(Time.time * 14f, 1f) > 0.5f;
        }
    }

    public void updateMaxLife()
    {
        if (dead) return;
        life = MaxLife;
        AtualizarVidaMaxima();
    }

    // Reduzir a vida maxima a zero encerra a partida, mesmo com escudo
    // ou invencibilidade. Reducoes nao letais nao recuperam vida perdida.
    public void AtualizarVidaMaxima()
    {
        if (dead) return;
        life = Mathf.Clamp(life, 0, MaxLife);
        if (life == 0) Die();
    }

    // Usado pelos coracoes que os inimigos soltam (HeartPickup)
    public void Heal(int amount)
    {
        AtualizarVidaMaxima();
        if (dead || amount <= 0) return;
        life = Mathf.Min(life + amount, MaxLife);
        GameEffects.Instance.PlayerHealed(transform.position, amount);
    }

    public void RestaurarVidaInteira()
    {
        Heal(Mathf.Max(0, MaxLife - life));
    }

    public void takeDamage(int damage)
    {
        takeDamage(damage, null);
    }

    // source: quem causou o dano (usado pra empurrar o jogador na direcao contraria)
    public void takeDamage(int damage, Transform source)
    {
        AtualizarVidaMaxima();
        if (!GameFlow.CanPlay || damage <= 0 || IsInvulnerable) return;

        invincibleUntil = Time.time + invincibilityTime;
        ApplyKnockback(source);

        if (shieldHealth > 0)
        {
            int absorbed = Mathf.Min(shieldHealth, damage);
            shieldHealth -= absorbed;
            damage -= absorbed;
            Debug.Log("O Escudo absorveu o dano! Resistência restante: " + shieldHealth);

            GameEffects.Instance.PlayerShieldHit(transform.position);

            if (shieldHealth == 0)
            {
                if (Escudo != null) Escudo.SetActive(false);
                AudioManager.Instance.PlaySFX("EscudoQuebrou");
                invincibleUntil = Time.time + invincibilityTime;
                return;
            }

            if (damage == 0) return; // O escudo absorveu o impacto inteiro, mesmo se quebrou.
        }

        // Apenas o dano que nao foi absorvido chega aos coracoes.
        life = Mathf.Max(life - damage, 0);
        GameEffects.Instance.PlayerHurt(transform.position);
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);

        if (life <= 0)
        {
            Die();
            return;
        }

        else
        {
            AudioManager.Instance.PlaySFX("PlayerTomouDano");
        }
    }

    private void ApplyKnockback(Transform source)
    {
        if (source == null || movement == null) return;

        Vector2 dir = (Vector2)(transform.position - source.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Rng.CombatInsideUnitCircle();
        movement.OverrideVelocity(dir.normalized * knockbackForce, 0.15f, true);
    }

    // Função que será chamada pelo Baú para dar o escudo
    public void EquipShield(int shieldAmount)
    {
        shieldHealth = Mathf.Max(0, shieldAmount);
        if (Escudo != null) Escudo.SetActive(shieldHealth > 0);
        Debug.Log($"Escudo Equipado! Proteção total: {shieldHealth} de dano.");
        //hasShield = true;
        //Debug.Log("Escudo Equipado! Você tem uma vida extra.");
        // Opcional: Atualizar a UI para mostrar o icone do escudo na tela
    }

    // Morte na hora: ignora escudo e invencibilidade (usado pelo Caracol Imortal)
    public void InstaKill()
    {
        if (dead) return;
        GameEffects.Instance.PlayerHurt(transform.position);
        Die();
    }

    public void GanharPena() => Penas++;

    private void Die()
    {
        if (dead) return;
        dead = true;
        life = 0;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        AudioManager.Instance.PlaySFX("PlayerMorreu");

        // Pena da Fenix: so salva se ainda existe vida maxima pra voltar (debuff pode ter zerado)
        if (Penas > 0 && MaxLife > 0 && gameOverManager != null)
        {
            Penas--;
            gameOverManager.ReviverJogador(this);
            return;
        }

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver(transform);
        }
    }

    // Chamado pelo GameOverManager no fim da animacao de reviver
    public void Reviver()
    {
        dead = false;
        life = MaxLife;
        invincibleUntil = Time.time + invencivelAoReviver;
    }
}

/// <summary>Quantidade atual de escudo, independente da qualidade do item coletado.</summary>
public class ShieldCounterHUD : MonoBehaviour
{
    private HeartSystem vida;
    private UnityEngine.UI.Image icone;
    private UnityEngine.UI.Text quantidade;
    private GameObject painel;
    private int ultimoValor = -1;

    public void Inicializar(HeartSystem sistema)
    {
        vida = sistema;
        Canvas canvas = null;
        foreach (var coracao in vida.hearts)
        {
            if (coracao == null) continue;
            canvas = coracao.GetComponentInParent<Canvas>();
            if (canvas != null) break;
        }
        if (canvas == null) return;
        painel = new GameObject("Escudo - quantidade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        painel.transform.SetParent(canvas.transform, false);
        var rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-85, -80);
        rect.sizeDelta = new Vector2(100, 100);
        icone = painel.GetComponent<UnityEngine.UI.Image>();
        icone.sprite = ItemSprites.Obter(Chest.ItemType.ShieldSmall);
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        var texto = new GameObject("Quantidade", typeof(RectTransform), typeof(UnityEngine.UI.Text), typeof(UnityEngine.UI.Outline));
        texto.transform.SetParent(painel.transform, false);
        var textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchorMin = new Vector2(.12f, .2f);
        textoRect.anchorMax = new Vector2(.88f, .8f);
        textoRect.offsetMin = textoRect.offsetMax = Vector2.zero;
        quantidade = texto.GetComponent<UnityEngine.UI.Text>();
        quantidade.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quantidade.fontSize = 32;
        quantidade.fontStyle = FontStyle.Bold;
        quantidade.resizeTextForBestFit = true;
        quantidade.resizeTextMinSize = 10;
        quantidade.resizeTextMaxSize = 32;
        quantidade.alignment = TextAnchor.MiddleCenter;
        quantidade.color = Color.white;
        quantidade.raycastTarget = false;
        var contorno = texto.GetComponent<UnityEngine.UI.Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(2, -2);
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (quantidade == null || vida == null) return;
        int valor = Mathf.Max(0, vida.shieldHealth);
        if (ultimoValor == valor) return;
        ultimoValor = valor;
        quantidade.text = valor.ToString();
        icone.color = new Color(1, 1, 1, valor > 0 ? 1f : .4f);
    }

    private void OnDestroy()
    {
        if (painel != null) Destroy(painel);
    }
}

/// <summary>Mostra o bonus acumulado das botas, sem outros efeitos de velocidade.</summary>
public class BootBonusHUD : MonoBehaviour
{
    private PlayerStatsHandler atributos;
    private GameObject painel;
    private UnityEngine.UI.Image icone;
    private UnityEngine.UI.Text legenda;
    private float ultimoBonus = -1f;
    private bool ultimaLendaria;

    public void Inicializar(HeartSystem vida, PlayerStatsHandler stats)
    {
        atributos = stats;
        Canvas canvas = null;
        foreach (var coracao in vida.hearts)
        {
            if (coracao == null) continue;
            canvas = coracao.GetComponentInParent<Canvas>();
            if (canvas != null) break;
        }
        if (canvas == null || atributos == null) return;
        painel = new GameObject("Botas - bonus de velocidade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        painel.transform.SetParent(canvas.transform, false);
        var rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-85, -170);
        rect.sizeDelta = new Vector2(80, 80);
        icone = painel.GetComponent<UnityEngine.UI.Image>();
        icone.preserveAspect = true;
        icone.raycastTarget = false;
        var texto = new GameObject("Bonus acumulado", typeof(RectTransform), typeof(UnityEngine.UI.Text), typeof(UnityEngine.UI.Outline));
        texto.transform.SetParent(painel.transform, false);
        var textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchoredPosition = new Vector2(-10, -66);
        textoRect.sizeDelta = new Vector2(150, 50);
        legenda = texto.GetComponent<UnityEngine.UI.Text>();
        legenda.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        legenda.fontSize = 20;
        legenda.fontStyle = FontStyle.Bold;
        legenda.alignment = TextAnchor.MiddleCenter;
        legenda.color = Color.white;
        legenda.raycastTarget = false;
        legenda.resizeTextForBestFit = true;
        legenda.resizeTextMinSize = 12;
        legenda.resizeTextMaxSize = 20;
        var contorno = texto.GetComponent<UnityEngine.UI.Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(1, -1);
        painel.SetActive(false);
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (painel == null || atributos == null) return;
        bool visivel = atributos.PossuiBotasAtivas;
        if (painel.activeSelf != visivel) painel.SetActive(visivel);
        if (!visivel) return;
        float bonus = atributos.BonusTotalBotas;
        bool lendaria = atributos.TemBotaLendaria;
        if (ultimoBonus == bonus && ultimaLendaria == lendaria) return;
        ultimoBonus = bonus;
        ultimaLendaria = lendaria;
        icone.sprite = ItemSprites.Obter(lendaria ? Chest.ItemType.LegendarySpeedBoots : Chest.ItemType.SpeedBoots);
        legenda.text = $"+{bonus * 100f:0.#}%\nVelocidade";
    }

    private void OnDestroy()
    {
        if (painel != null) Destroy(painel);
    }
}

// O prazo vem do proprio modificador: pausa, renovacao e fim do encanto ficam sincronizados.
public class InvertedControlsFeedback : MonoBehaviour
{
    private PlayerStatsHandler stats;
    private HeartSystem vida;
    private SpriteRenderer personagem;
    private Color corOriginal;
    private bool tingido;
    private GameObject interfaceEfeito, painel;
    private TextMeshProUGUI texto;
    private Image barra;
    private float maiorPrazo = 5f;
    private static readonly Color Roxo = new Color(.75f, .24f, 1f);
    private GameObject painelPoder;
    private TextMeshProUGUI textoPoder;
    private Image barraPoder;

    private void Start()
    {
        stats = GetComponent<PlayerStatsHandler>();
        vida = GetComponent<HeartSystem>();
        personagem = GetComponent<SpriteRenderer>();
        interfaceEfeito = new GameObject("Aviso - controles invertidos", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        var canvas = interfaceEfeito.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        var scaler = interfaceEfeito.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        painel = new GameObject("Encanto da bruxinha", typeof(RectTransform), typeof(Image));
        painel.transform.SetParent(interfaceEfeito.transform, false);
        var rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -92);
        rect.sizeDelta = new Vector2(650, 92);
        var fundo = painel.GetComponent<Image>();
        fundo.color = new Color(.16f, .035f, .24f, .94f);
        fundo.raycastTarget = false;
        var label = new GameObject("Tempo restante", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(painel.transform, false);
        texto = label.GetComponent<TextMeshProUGUI>();
        texto.rectTransform.sizeDelta = new Vector2(620, 70);
        texto.rectTransform.anchoredPosition = new Vector2(0, 4);
        texto.fontSize = 27;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = new Color(.94f, .8f, 1f);
        texto.raycastTarget = false;
        var linha = new GameObject("Duracao", typeof(RectTransform), typeof(Image));
        linha.transform.SetParent(painel.transform, false);
        barra = linha.GetComponent<Image>();
        barra.color = Roxo;
        barra.raycastTarget = false;
        barra.rectTransform.anchorMin = barra.rectTransform.anchorMax = new Vector2(0, 0);
        barra.rectTransform.pivot = new Vector2(0, 0);
        barra.rectTransform.anchoredPosition = new Vector2(10, 6);
        painel.SetActive(false);
        CriarIndicadorPoder();
    }

    private void CriarIndicadorPoder()
    {
        painelPoder = new GameObject("Pocao de forca - tempo restante", typeof(RectTransform), typeof(Image));
        painelPoder.transform.SetParent(interfaceEfeito.transform, false);
        var rect = painelPoder.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.sizeDelta = new Vector2(650, 92);
        var fundo = painelPoder.GetComponent<Image>();
        fundo.color = new Color(.25f, .1f, .025f, .94f);
        fundo.raycastTarget = false;
        var icone = new GameObject("Pocao", typeof(RectTransform), typeof(Image));
        icone.transform.SetParent(painelPoder.transform, false);
        var imagem = icone.GetComponent<Image>();
        imagem.sprite = ItemSprites.Obter(Chest.ItemType.PowerPotion);
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        imagem.rectTransform.anchoredPosition = new Vector2(-275, 3);
        imagem.rectTransform.sizeDelta = new Vector2(45, 52);
        var label = new GameObject("Tempo de forca restante", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(painelPoder.transform, false);
        textoPoder = label.GetComponent<TextMeshProUGUI>();
        textoPoder.rectTransform.sizeDelta = new Vector2(540, 70);
        textoPoder.rectTransform.anchoredPosition = new Vector2(25, 4);
        textoPoder.fontSize = 27;
        textoPoder.alignment = TextAlignmentOptions.Center;
        textoPoder.color = new Color(1f, .8f, .55f);
        textoPoder.raycastTarget = false;
        var linha = new GameObject("Duracao da forca", typeof(RectTransform), typeof(Image));
        linha.transform.SetParent(painelPoder.transform, false);
        barraPoder = linha.GetComponent<Image>();
        barraPoder.color = PlayerStatsHandler.CorPocaoPoder;
        barraPoder.raycastTarget = false;
        barraPoder.rectTransform.anchorMin = barraPoder.rectTransform.anchorMax = Vector2.zero;
        barraPoder.rectTransform.pivot = Vector2.zero;
        barraPoder.rectTransform.anchoredPosition = new Vector2(10, 6);
        painelPoder.SetActive(false);
    }

    private void LateUpdate()
    {
        if (stats == null || painel == null) return;
        float restante = stats.TempoControlesInvertidos;
        bool ativo = restante > 0 && (vida == null || !vida.IsDead);
        float tempoPoder = stats.TempoPocaoPoder;
        bool poderAtivo = tempoPoder > 0 && (vida == null || !vida.IsDead);
        painel.SetActive(ativo);
        painelPoder.SetActive(poderAtivo);
        if (poderAtivo)
        {
            painelPoder.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, ativo ? -198 : -92);
            textoPoder.text = $"FORÇA ATIVA  •  {tempoPoder:0.0}s\nDano aumentado";
            barraPoder.rectTransform.sizeDelta = new Vector2(630 * Mathf.Clamp01(tempoPoder / PlayerStatsHandler.DuracaoPocaoPoder), 5);
        }
        if (ativo || poderAtivo)
        {
            if (!tingido && personagem != null) { corOriginal = personagem.color; tingido = true; }
            if (personagem != null)
            {
                // Um unico controlador de cor evita que os dois efeitos sobrescrevam um ao outro.
                Color efeito = ativo && (!poderAtivo || Mathf.FloorToInt(Time.time * 3) % 2 == 0)
                    ? Roxo : PlayerStatsHandler.CorPocaoPoder;
                Color cor = Color.Lerp(corOriginal, efeito, .35f + .65f * Mathf.PingPong(Time.time * 4f, 1));
                cor.a = corOriginal.a;
                personagem.color = cor;
            }
        }
        else RestaurarCor();
        if (ativo)
        {
            maiorPrazo = Mathf.Max(maiorPrazo, restante);
            texto.text = $"CONTROLES INVERTIDOS  •  {restante:0.0}s\nEncanto da bruxinha: mova-se no sentido oposto!";
            barra.rectTransform.sizeDelta = new Vector2(630 * Mathf.Clamp01(restante / maiorPrazo), 5);
        }
    }

    private void RestaurarCor()
    {
        if (tingido && personagem != null) personagem.color = corOriginal;
        tingido = false;
        maiorPrazo = 5f;
    }

    private void OnDisable()
    {
        RestaurarCor();
        if (painel != null) painel.SetActive(false);
        if (painelPoder != null) painelPoder.SetActive(false);
    }

    private void OnDestroy()
    {
        RestaurarCor();
        if (interfaceEfeito != null) Destroy(interfaceEfeito);
    }
}
