using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.UI;

// A tela de teclado mantém seu remapeamento. Com controle conectado,
// mostra um guia dos comandos de jogo com os ícones da família do dispositivo.
public class ConnectedGamepadControls : MonoBehaviour
{
    [Serializable]
    private class Comando
    {
        public string nome;
        public Sprite xbox;
        public Sprite playstation;
        public Sprite alternativaXbox;
        public Sprite alternativaPlaystation;
    }

    [SerializeField] private GameObject[] conteudoTeclado;
    [SerializeField] private TMP_FontAsset fonte;
    [SerializeField] private Comando[] comandos;

    private bool[] estadosOriginais;
    private GameObject guia;
    private TextMeshProUGUI titulo;
    private Image[] icones;
    private Image[] alternativas;
    private Gamepad controleAnterior;
    private bool atualizado;

    private void Awake()
    {
        estadosOriginais = new bool[conteudoTeclado.Length];
        for (int i = 0; i < conteudoTeclado.Length; i++)
            estadosOriginais[i] = conteudoTeclado[i] != null && conteudoTeclado[i].activeSelf;
        CriarGuia();
    }

    private void OnEnable()
    {
        atualizado = false;
        AtualizarDispositivo();
        InputSystem.onDeviceChange += AoMudarDispositivo;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= AoMudarDispositivo;
    }

    private void AoMudarDispositivo(InputDevice dispositivo, InputDeviceChange mudanca)
    {
        if (dispositivo is Gamepad) atualizado = false;
    }

    private void Update() => AtualizarDispositivo();

    private void AtualizarDispositivo()
    {
        Gamepad controle = Gamepad.current;
        if (controle != null && (!controle.added || !controle.enabled)) controle = null;
        if (controle == null)
            foreach (Gamepad candidato in Gamepad.all)
                if (candidato.added && candidato.enabled) controle = candidato;
        if (atualizado && controle == controleAnterior) return;
        controleAnterior = controle;
        atualizado = true;

        bool conectado = controle != null;
        // Desativar a tela de teclado encerra o remapeamento no OnDisable do próprio componente.
        for (int i = 0; i < conteudoTeclado.Length; i++)
            if (conteudoTeclado[i] != null)
                conteudoTeclado[i].SetActive(!conectado && estadosOriginais[i]);
        guia.SetActive(conectado);
        if (!conectado) return;

        bool playstation = controle is DualShockGamepad;
        titulo.text = playstation ? "Controle PlayStation" : "Controle Xbox / Gamepad";
        for (int i = 0; i < comandos.Length; i++)
        {
            icones[i].sprite = playstation ? comandos[i].playstation : comandos[i].xbox;
            alternativas[i].sprite = playstation ? comandos[i].alternativaPlaystation : comandos[i].alternativaXbox;
            alternativas[i].gameObject.SetActive(alternativas[i].sprite != null);
        }
    }

    private void CriarGuia()
    {
        RectTransform raiz = CriarRetangulo("Comandos do controle", transform,
            new Vector2(0.12f, 0.22f), new Vector2(0.88f, 0.72f));
        guia = raiz.gameObject;
        titulo = CriarTexto("Dispositivo", raiz, new Vector2(0f, 0.87f), Vector2.one,
            string.Empty, 34f, TextAlignmentOptions.Center);
        icones = new Image[comandos.Length];
        alternativas = new Image[comandos.Length];
        int linhas = Mathf.Max(1, Mathf.CeilToInt(comandos.Length / 2f));
        for (int i = 0; i < comandos.Length; i++)
        {
            int coluna = i % 2;
            int linha = i / 2;
            float altura = 0.72f / linhas;
            float esquerda = coluna * 0.52f;
            float cima = 0.84f - linha * altura;
            RectTransform faixa = CriarRetangulo(comandos[i].nome, raiz,
                new Vector2(esquerda, cima - altura + 0.02f), new Vector2(esquerda + 0.48f, cima));
            CriarTexto("Ação", faixa, Vector2.zero, new Vector2(0.65f, 1f),
                comandos[i].nome, 30f, TextAlignmentOptions.MidlineLeft);
            icones[i] = CriarIcone("Botão", faixa, 0.68f);
            alternativas[i] = CriarIcone("Alternativa", faixa, 0.86f);
        }
        CriarTexto("Ajuda", raiz, Vector2.zero, new Vector2(1f, 0.1f),
            "Inventário: marque um item, escolha o destino e confirme a troca.", 24f,
            TextAlignmentOptions.Center);
        guia.SetActive(false);
    }

    private Image CriarIcone(string nome, Transform pai, float esquerda)
    {
        RectTransform retangulo = CriarRetangulo(nome, pai, new Vector2(esquerda, 0.05f),
            new Vector2(esquerda + 0.14f, 0.95f));
        var imagem = retangulo.gameObject.AddComponent<Image>();
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        return imagem;
    }

    private TextMeshProUGUI CriarTexto(string nome, Transform pai, Vector2 minimo,
        Vector2 maximo, string conteudo, float tamanho, TextAlignmentOptions alinhamento)
    {
        RectTransform retangulo = CriarRetangulo(nome, pai, minimo, maximo);
        var texto = retangulo.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) texto.font = fonte;
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 18f;
        texto.fontSizeMax = tamanho;
        texto.alignment = alinhamento;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }

    private static RectTransform CriarRetangulo(string nome, Transform pai, Vector2 minimo, Vector2 maximo)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = pai.gameObject.layer;
        var retangulo = objeto.GetComponent<RectTransform>();
        retangulo.SetParent(pai, false);
        retangulo.anchorMin = minimo;
        retangulo.anchorMax = maximo;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
        return retangulo;
    }
}
