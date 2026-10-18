using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
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

    [Header("Avanço automático")]
    [SerializeField] private bool avancoAutomatico = true;
    [Tooltip("Tempo de espera após a fala terminar de ser digitada.")]
    [SerializeField, Min(0f)] private float esperaAutoAvanco = 1.5f;
    [Tooltip("Tempo extra por caractere, para falas longas ficarem mais tempo na tela.")]
    [SerializeField, Min(0f)] private float esperaPorCaractere = 0.04f;

    [Header("Pular introdução")]
    [SerializeField, Min(0.2f)] private float tempoParaPular = 1.5f;
    [SerializeField] private CanvasGroup indicadorPular;
    [SerializeField] private TextMeshProUGUI textoPular;
    [SerializeField] private RectTransform barraProgresso;

    private CanvasGroup grupoDialogo;
    private Estado estado = Estado.Entrada;
    private Coroutine digitacao;
    private PlayableDirector diretorEmExecucao;
    private bool animacaoTerminou;
    private bool inicializado;
    private int indice;
    private InputSystem_Actions controles;
    private int ultimoFrameDeAvanco = -1;
    private ButtonControl botaoSegurado;
    private float inicioPressao;
    private bool entradaLiberada;
    private float prazoAutoAvanco;
    private const string DicaPular = "Segure o botão de avançar para pular";

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
        entradaLiberada = false;
        CancelarPulo();
        controles.UI.Click.performed += AoAvancarDialogo;
        controles.UI.Submit.performed += AoAvancarDialogo;
        controles.UI.Click.Enable();
        controles.UI.Submit.Enable();
        if (inicializado)
            StartCoroutine(IniciarIntro());
    }

    private void AoAvancarDialogo(InputAction.CallbackContext context)
    {
        if (!inicializado || !entradaLiberada || estado == Estado.Finalizando ||
            !(context.control is ButtonControl botao) || !botao.isPressed)
            return;

        // Uma pressao continua pertence a um unico botao/dispositivo.
        if (botaoSegurado != null && botaoSegurado.device.added &&
            botaoSegurado.device.enabled && botaoSegurado.isPressed) return;
        botaoSegurado = botao;
        inicioPressao = Time.unscaledTime;
        string nome = botao.device is Mouse ? "clique" : botao.shortDisplayName ?? botao.displayName;
        if (botao.device is DualShockGamepad pad && botao == pad.buttonSouth) nome = "X";
        textoPular.text = $"Segure {nome} para pular";

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
            AguardarClique();
        }
        else
        {
            AvancarFala();
        }
    }

    private void AvancarFala()
    {
        if (++indice < falas.Length)
            StartCoroutine(ApresentarFala());
        else
            EncerrarIntro(false);
    }

    private void AguardarClique()
    {
        estado = Estado.AguardandoClique;
        prazoAutoAvanco = Time.unscaledTime + esperaAutoAvanco +
                          esperaPorCaractere * text.textInfo.characterCount;
    }

    private void Update()
    {
        if (!inicializado || estado == Estado.Finalizando) return;

        if (avancoAutomatico && estado == Estado.AguardandoClique && Time.unscaledTime >= prazoAutoAvanco)
            AvancarFala();

        // Nao aproveitar um botao que veio segurado do menu anterior.
        if (!entradaLiberada)
        {
            entradaLiberada = !AcaoPressionada(controles.UI.Click) && !AcaoPressionada(controles.UI.Submit);
            return;
        }

        if (botaoSegurado == null) return;
        if (!botaoSegurado.device.added || !botaoSegurado.device.enabled || !botaoSegurado.isPressed)
        {
            CancelarPulo();
            return;
        }

        float progresso = Mathf.Clamp01((Time.unscaledTime - inicioPressao) / Mathf.Max(0.2f, tempoParaPular));
        barraProgresso.anchorMax = new Vector2(progresso, 1f);
        indicadorPular.alpha = 1f;
        if (progresso >= 1f) EncerrarIntro(true);
    }

    private static bool AcaoPressionada(InputAction acao)
    {
        foreach (var controle in acao.controls)
            if (controle is ButtonControl botao && botao.isPressed) return true;
        return false;
    }

    private void CancelarPulo()
    {
        botaoSegurado = null;
        inicioPressao = 0f;
        if (barraProgresso != null) barraProgresso.anchorMax = new Vector2(0f, 1f);
        if (textoPular != null) textoPular.text = DicaPular;
        if (indicadorPular != null) indicadorPular.alpha = estado == Estado.Finalizando ? 0f : 0.75f;
    }

    private void OnApplicationFocus(bool foco)
    {
        if (foco) return;
        CancelarPulo();
        entradaLiberada = false;
    }

    private void EncerrarIntro(bool pulada)
    {
        if (estado == Estado.Finalizando) return;
        estado = Estado.Finalizando;
        CancelarPulo();
        indicadorPular.alpha = 0f;
        StopAllCoroutines();
        digitacao = null;
        PlayableDirector diretor = diretorEmExecucao;
        RemoverInscricaoAnimacao();
        if (diretor != null) diretor.Stop();
        StartCoroutine(FinalizarIntro(pulada ? Mathf.Min(duracaoFade, 0.35f) : duracaoFade));
    }

    private IEnumerator IniciarIntro()
    {
        estado = Estado.Entrada;
        indice = 0;
        ultimoFrameDeAvanco = -1;
        CancelarPulo();
        text.text = string.Empty;
        MostrarDialogo(false);
        yield return Fade(1f, 0f, duracaoFade);
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
        digitacao = null;
        AguardarClique();
    }

    private IEnumerator FinalizarIntro(float tempoFade)
    {
        MostrarDialogo(false);
        yield return Fade(fadePanel.alpha, 1f, tempoFade);
        SceneManager.LoadSceneAsync(cenaJogo);
    }

    private IEnumerator Fade(float origem, float destino, float duracao)
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.blocksRaycasts = true;
        fadePanel.alpha = origem;
        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            fadePanel.alpha = Mathf.Lerp(origem, destino, Mathf.Clamp01(tempo / duracao));
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
        CancelarPulo();
        if (indicadorPular != null) indicadorPular.alpha = 0f;
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
            indicadorPular == null || textoPular == null || barraProgresso == null ||
            sprites == null || sprites.Length < 2 || sprites[0] == null || sprites[1] == null ||
            falas == null || falas.Length == 0)
        {
            Debug.LogError("Intro: configure a interface, o indicador de pulo, os dois retratos e as falas no Inspector.", this);
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
