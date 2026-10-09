using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Intro : MonoBehaviour
{
    private enum Estado { Entrada, Digitando, AguardandoClique, Animando, Finalizando }
    private enum Personagem { Alex, Olho }

    [Serializable]
    private class Fala
    {
        [TextArea(2, 5)] public string texto;
        public Personagem personagem;
        public bool animarAntes;

        public Fala(string texto, Personagem personagem, bool animarAntes = false)
        {
            this.texto = texto;
            this.personagem = personagem;
            this.animarAntes = animarAntes;
        }
    }

    [Header("Interface")]
    [SerializeField] private GameObject painelDialogo;
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Image imagem;
    [Tooltip("0: Alex; 1: Olho.")]
    [SerializeField] private Sprite[] sprites;

    [Header("Sequência")]
    [SerializeField] private PlayableDirector animacaoMataBoss;
    [SerializeField, Min(0f)] private float textspeed = 0.05f;
    [SerializeField, Min(0f)] private float duracaoFade = 3f;
    [SerializeField] private string cenaJogo = "InGame";
    [SerializeField] private Fala[] falas =
    {
        new Fala("Olho...", Personagem.Alex),
        new Fala("ALEX SEU MALDITO", Personagem.Olho),
        new Fala("VOCE SENTIRA A IRA DE UM DEUS", Personagem.Olho),
        new Fala("TOMAAAA!!!!!!!", Personagem.Olho),
        new Fala("Você morrerá agora...", Personagem.Alex),
        new Fala("!!!!!!!!!!!!!!!!!!!!!!!!!!!", Personagem.Olho, true),
        new Fala("Eu morrerei porem não será em vão!", Personagem.Olho),
        new Fala("VOCÊ SERÁ AMALDIÇOADO!", Personagem.Olho),
        new Fala("......", Personagem.Alex),
        new Fala("Hora de voltar para casa...", Personagem.Alex)
    };

    private CanvasGroup grupoDialogo;
    private Estado estado = Estado.Entrada;
    private Coroutine digitacao;
    private PlayableDirector diretorEmExecucao;
    private bool animacaoTerminou;
    private bool inicializado;
    private int indice;
    private InputSystem_Actions controles;
    private int ultimoFrameDeAvanco = -1;

    private void Awake()
    {
        controles = new InputSystem_Actions();
    }

    private void Start()
    {
        if (!ValidarConfiguracao())
        {
            enabled = false;
            return;
        }

        grupoDialogo = painelDialogo.GetComponent<CanvasGroup>();
        if (grupoDialogo == null)
            grupoDialogo = painelDialogo.AddComponent<CanvasGroup>();
        inicializado = true;
        StartCoroutine(IniciarIntro());
    }

    private void OnEnable()
    {
        controles.UI.Click.performed += AoAvancarDialogo;
        controles.UI.Submit.performed += AoAvancarDialogo;
        controles.UI.Click.Enable();
        controles.UI.Submit.Enable();
        if (inicializado)
            StartCoroutine(IniciarIntro());
    }

    private void AoAvancarDialogo(InputAction.CallbackContext context)
    {
        if (estado != Estado.Digitando && estado != Estado.AguardandoClique)
            return;
        // Click é PassThrough: também dispara ao soltar o botão.
        // Dois dispositivos no mesmo frame devem produzir apenas um avanço.
        if (!context.action.IsPressed() || ultimoFrameDeAvanco == Time.frameCount)
            return;
        ultimoFrameDeAvanco = Time.frameCount;
        if (estado == Estado.Digitando)
        {
            if (digitacao != null)
                StopCoroutine(digitacao);
            digitacao = null;
            text.maxVisibleCharacters = text.textInfo.characterCount;
            estado = Estado.AguardandoClique;
        }
        else if (++indice < falas.Length)
        {
            StartCoroutine(ApresentarFala());
        }
        else
        {
            estado = Estado.Finalizando;
            StartCoroutine(FinalizarIntro());
        }
    }

    private IEnumerator IniciarIntro()
    {
        estado = Estado.Entrada;
        indice = 0;
        ultimoFrameDeAvanco = -1;
        text.text = string.Empty;
        MostrarDialogo(false);
        yield return Fade(1f, 0f);
        yield return ApresentarFala();
    }

    private IEnumerator ApresentarFala()
    {
        Fala fala = falas[indice];
        if (fala.animarAntes)
        {
            estado = Estado.Animando;
            MostrarDialogo(false);
            diretorEmExecucao = animacaoMataBoss;
            animacaoTerminou = false;
            diretorEmExecucao.stopped += AoTerminarAnimacao;
            diretorEmExecucao.time = 0;
            diretorEmExecucao.Play();
            yield return new WaitUntil(() => animacaoTerminou);
            RemoverInscricaoAnimacao();
        }
        imagem.sprite = sprites[(int)fala.personagem];
        text.text = fala.texto;
        text.maxVisibleCharacters = 0;
        text.ForceMeshUpdate();
        MostrarDialogo(true);
        estado = Estado.Digitando;
        digitacao = StartCoroutine(DigitarFala());
    }

    private IEnumerator DigitarFala()
    {
        int quantidade = text.textInfo.characterCount;
        if (textspeed > 0f)
        {
            var intervalo = new WaitForSecondsRealtime(textspeed);
            for (int visiveis = 1; visiveis <= quantidade; visiveis++)
            {
                text.maxVisibleCharacters = visiveis;
                yield return intervalo;
            }
        }
        text.maxVisibleCharacters = quantidade;
        estado = Estado.AguardandoClique;
        digitacao = null;
    }

    private IEnumerator FinalizarIntro()
    {
        MostrarDialogo(false);
        yield return Fade(fadePanel.alpha, 1f);
        SceneManager.LoadSceneAsync(cenaJogo);
    }

    private IEnumerator Fade(float origem, float destino)
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;
        fadePanel.alpha = origem;
        float tempo = 0f;
        while (tempo < duracaoFade)
        {
            tempo += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(origem, destino, Mathf.Clamp01(tempo / duracaoFade));
            yield return null;
        }
        fadePanel.alpha = destino;
        fadePanel.blocksRaycasts = destino > 0f;
    }

    private void MostrarDialogo(bool mostrar)
    {
        grupoDialogo.alpha = mostrar ? 1f : 0f;
        grupoDialogo.interactable = mostrar;
        grupoDialogo.blocksRaycasts = mostrar;
    }

    private void AoTerminarAnimacao(PlayableDirector diretor)
    {
        if (diretor == diretorEmExecucao)
            animacaoTerminou = true;
    }

    private void RemoverInscricaoAnimacao()
    {
        if (diretorEmExecucao != null)
            diretorEmExecucao.stopped -= AoTerminarAnimacao;
        diretorEmExecucao = null;
    }

    private void OnDisable()
    {
        controles.UI.Click.performed -= AoAvancarDialogo;
        controles.UI.Submit.performed -= AoAvancarDialogo;
        controles.UI.Click.Disable();
        controles.UI.Submit.Disable();
        estado = Estado.Entrada;
        StopAllCoroutines();
        digitacao = null;
        PlayableDirector diretor = diretorEmExecucao;
        RemoverInscricaoAnimacao();
        if (diretor != null)
            diretor.Stop();
    }

    private void OnDestroy()
    {
        controles?.Dispose();
    }

    private bool ValidarConfiguracao()
    {
        if (painelDialogo == null || fadePanel == null || text == null || imagem == null ||
            sprites == null || sprites.Length < 2 || sprites[0] == null || sprites[1] == null ||
            falas == null || falas.Length == 0)
        {
            Debug.LogError("Intro: configure a interface, os dois retratos e as falas no Inspector.", this);
            return false;
        }
        foreach (Fala fala in falas)
        {
            if (fala == null || fala.texto == null ||
                (fala.personagem != Personagem.Alex && fala.personagem != Personagem.Olho))
            {
                Debug.LogError("Intro: existe uma fala inválida no Inspector.", this);
                return false;
            }
            if (fala.animarAntes && (animacaoMataBoss == null || animacaoMataBoss.playableAsset == null ||
                animacaoMataBoss.extrapolationMode != DirectorWrapMode.None))
            {
                Debug.LogError("Intro: configure uma Timeline com Wrap Mode None para finalizar a animação.", this);
                return false;
            }
        }
        if (string.IsNullOrWhiteSpace(cenaJogo) || !Application.CanStreamedLevelBeLoaded(cenaJogo))
        {
            Debug.LogError("Intro: a cena de jogo precisa estar habilitada na lista de cenas do build.", this);
            return false;
        }
        return true;
    }
}
