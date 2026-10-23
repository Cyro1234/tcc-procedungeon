using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PauseManager pauseManager;

    [Header("Morte do jogador")]
    [Tooltip("Quadros da explosão, na ordem (Assets/_Sprites/Effects/ExplosaoMorte).")]
    [SerializeField] private Sprite[] quadrosExplosao;
    [SerializeField, Min(1f)] private float quadrosPorSegundo = 12f;
    [SerializeField, Min(0f)] private float tamanhoExplosao = 1f;
    [Tooltip("Tempo depois da explosão antes da tela de Game Over aparecer.")]
    [SerializeField, Min(0f)] private float esperaAposExplosao = 0.75f;
    [Tooltip("Duração do fade in da tela de Game Over.")]
    [SerializeField, Min(0f)] private float duracaoFade = 1f;

    [Header("Reviver (Pena da Fênix)")]
    [Tooltip("Pausa entre o fim da morte e o começo da volta.")]
    [SerializeField, Min(0f)] private float pausaAntesDeVoltar = 0.3f;
    [Tooltip("Limites da duração da explosão ao contrário. Dentro deles ela acompanha a voz invertida.")]
    [SerializeField, Min(0.1f)] private float duracaoMinimaVolta = 0.8f;
    [SerializeField, Min(0.1f)] private float duracaoMaximaVolta = 2.5f;

    private bool mostrandoGameOver;
    private bool revivendo;

    public void Start()
    {

    }

    // Pausa o jogo, explode o jogador e depois mostra a tela de game over com fade in
    public void ShowGameOver(Transform jogador = null)
    {
        if (mostrandoGameOver) return;
        mostrandoGameOver = true;
        GameFlow.Pause(GameFlow.State.GameOver);

        if (playerMovement != null) playerMovement.ForcarParada();

        playerInput.enabled = false;
        playerMovement.enabled = false;
        pauseManager.enabled = false;
        StartCoroutine(SequenciaGameOver(jogador));
    }

    // O jogo fica com timeScale 0 no Game Over, então tudo aqui usa tempo real.
    private IEnumerator SequenciaGameOver(Transform jogador)
    {
        if (jogador != null && quadrosExplosao != null && quadrosExplosao.Length > 0)
        {
            yield return Explodir(jogador);
            yield return new WaitForSecondsRealtime(esperaAposExplosao);
        }

        AudioManager.Instance.PlayMusic("GameOver", false);
        yield return MostrarPainel();
    }

    private IEnumerator Explodir(Transform jogador)
    {
        EsconderJogador(jogador);
        yield return TocarExplosao(jogador, false, quadrosPorSegundo);
    }

    // Esconde o jogador e devolve as partes que estavam visiveis, pra mostrar de novo ao reviver
    private System.Collections.Generic.List<Renderer> EsconderJogador(Transform jogador)
    {
        var escondidas = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer parte in jogador.GetComponentsInChildren<Renderer>())
        {
            if (!parte.enabled) continue;
            parte.enabled = false;
            escondidas.Add(parte);
        }
        // As partículas do último dano congelariam com o jogo pausado.
        foreach (FxParticle particula in FindObjectsByType<FxParticle>(FindObjectsSortMode.None))
            Destroy(particula.gameObject);
        return escondidas;
    }

    private IEnumerator TocarExplosao(Transform jogador, bool aoContrario, float qps)
    {
        var referencia = jogador.GetComponent<SpriteRenderer>();
        var explosao = new GameObject(aoContrario ? "Explosão ao contrário" : "Explosão da morte");
        explosao.transform.position = jogador.position;
        explosao.transform.localScale = Vector3.one * tamanhoExplosao;
        var sprite = explosao.AddComponent<SpriteRenderer>();
        if (referencia != null)
        {
            sprite.sortingLayerID = referencia.sortingLayerID;
            sprite.sortingOrder = referencia.sortingOrder + 1;
        }

        var intervalo = new WaitForSecondsRealtime(1f / qps);
        for (int i = 0; i < quadrosExplosao.Length; i++)
        {
            sprite.sprite = quadrosExplosao[aoContrario ? quadrosExplosao.Length - 1 - i : i];
            yield return intervalo;
        }
        Destroy(explosao);
    }

    // Pena da Fênix: jogador explode, depois a explosão ocorre de forma reversa e então ele revive
    public void ReviverJogador(HeartSystem vida)
    {
        if (mostrandoGameOver || revivendo) return;
        revivendo = true;
        StartCoroutine(SequenciaReviver(vida));
    }

    // O jogo fica pausado (timeScale 0) durante a sequência, então tudo usa tempo real
    private IEnumerator SequenciaReviver(HeartSystem vida)
    {
        Transform jogador = vida.transform;
        GameFlow.Pause(GameFlow.State.Revive);
        if (playerMovement != null) playerMovement.ForcarParada();
        float inicio = Time.realtimeSinceStartup;

        var escondidas = EsconderJogador(jogador);
        bool temQuadros = quadrosExplosao != null && quadrosExplosao.Length > 0;
        if (temQuadros) yield return TocarExplosao(jogador, false, quadrosPorSegundo);

        // Espera a voz da morte acabar antes de tocar ela ao contrário
        AudioClip vozDaMorte = AudioManager.Instance.ObterSFX("PlayerMorreu");
        float fimDaVoz = vozDaMorte != null ? inicio + vozDaMorte.length : 0f;
        float espera = Mathf.Max(pausaAntesDeVoltar, fimDaVoz - Time.realtimeSinceStartup);
        yield return new WaitForSecondsRealtime(espera);

        AudioClip vozInvertida = PenaFenix.Invertido(vozDaMorte);
        float duracaoVoz = vozInvertida != null ? vozInvertida.length : 0f;
        float duracaoVolta = temQuadros
            ? Mathf.Clamp(duracaoVoz > 0f ? duracaoVoz : quadrosExplosao.Length / quadrosPorSegundo,
                duracaoMinimaVolta, Mathf.Max(duracaoMinimaVolta, duracaoMaximaVolta))
            : 0f;
        if (vozInvertida != null) AudioManager.Instance.PlaySFX(vozInvertida);

        // A explosão ao contrário termina junto com a voz invertida
        if (duracaoVoz > duracaoVolta) yield return new WaitForSecondsRealtime(duracaoVoz - duracaoVolta);
        if (temQuadros) yield return TocarExplosao(jogador, true, quadrosExplosao.Length / duracaoVolta);

        foreach (var parte in escondidas)
            if (parte != null) parte.enabled = true;
        vida.Reviver();

        // O caracol imortal tinha pegado o jogador: recomeça longe
        foreach (var caracol in FindObjectsByType<SnailChaser>(FindObjectsSortMode.None))
            caracol.RecomecarLonge();

        revivendo = false;
        GameFlow.Resume(GameFlow.State.Revive);

        AudioManager.Instance.PlaySFX(PenaFenix.SomReviver);
        GameEffects.Instance.Burst(jogador.position, PenaFenix.Cor, 24, 5f, null);
        GameEffects.Instance.FlashScreen(new Color(1f, 0.75f, 0.35f, 0.35f), 0.5f);
        GameEffects.Instance.Popup(jogador.position + Vector3.up * 1.2f, "Reviveu!", PenaFenix.Cor, 1.3f, 1.2f);
    }

    private void ColocarPainelPorCima() => CanvasPorCima.Mover("[Game Over]", 1000, gameOverPanel);

    private IEnumerator MostrarPainel()
    {
        var grupo = gameOverPanel.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameOverPanel.AddComponent<CanvasGroup>();

        // Os botões só respondem depois que o painel termina de aparecer
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        ColocarPainelPorCima();
        gameOverPanel.SetActive(true);

        for (float tempo = 0f; tempo < duracaoFade; tempo += Time.unscaledDeltaTime)
        {
            grupo.alpha = tempo / duracaoFade;
            yield return null;
        }

        grupo.alpha = 1f;
        grupo.interactable = true;
        grupo.blocksRaycasts = true;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RetornarMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
