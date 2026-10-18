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

    private bool mostrandoGameOver;

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
        var referencia = jogador.GetComponent<SpriteRenderer>();
        foreach (Renderer parte in jogador.GetComponentsInChildren<Renderer>())
            parte.enabled = false;
        // As partículas do último dano congelariam com o jogo pausado.
        foreach (FxParticle particula in FindObjectsByType<FxParticle>(FindObjectsSortMode.None))
            Destroy(particula.gameObject);

        var explosao = new GameObject("Explosão da morte");
        explosao.transform.position = jogador.position;
        explosao.transform.localScale = Vector3.one * tamanhoExplosao;
        var sprite = explosao.AddComponent<SpriteRenderer>();
        if (referencia != null)
        {
            sprite.sortingLayerID = referencia.sortingLayerID;
            sprite.sortingOrder = referencia.sortingOrder + 1;
        }

        var intervalo = new WaitForSecondsRealtime(1f / quadrosPorSegundo);
        foreach (Sprite quadro in quadrosExplosao)
        {
            sprite.sprite = quadro;
            yield return intervalo;
        }
        Destroy(explosao);
    }

    private IEnumerator MostrarPainel()
    {
        var grupo = gameOverPanel.GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameOverPanel.AddComponent<CanvasGroup>();

        // Os botões só respondem depois que o painel termina de aparecer.
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
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
