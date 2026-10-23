using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Demonstracoes visuais isoladas: nao instanciam PlayerInput, inimigos vivos ou sistemas de combate.
public class Tutorial : MonoBehaviour
{
    [SerializeField] private Sprite controle;
    [SerializeField] private Sprite alex;
    [SerializeField] private Sprite[] caminhada;
    [SerializeField] private Sprite[] paradoFrente;
    [SerializeField] private Sprite[] paradoLateral;
    [SerializeField] private Sprite[] caminhadaEsquerda;
    [SerializeField] private GameObject inimigoModular;
    [SerializeField] private GameObject bruxinha;
    [SerializeField] private string proximaCena = "Cutscene intro";
    [SerializeField, Min(.2f)] private float tempoParaPular = 1.5f;
    [SerializeField, Min(0f)] private float duracaoFade = .6f;

    private readonly string[] titulos = {
        "Mova o Alex", "Aponte o canhão", "Ataque os inimigos", "Use o dash",
        "Troque de arma — a lista é circular!", "Use suas poções", "A fumaça é um aviso", "Cuidado com a bruxinha"
    };
    private readonly string[] acoes = {
        "ANALÓGICO ESQUERDO  •  MOVER", "ANALÓGICO DIREITO  •  MIRAR O CANHÃO",
        "QUADRADO  •  ATACAR", "X / R2  •  DASH", "L1: ANTERIOR  |  R1: PRÓXIMA",
        "CIMA: VIDA  |  BAIXO: PODER", "PREPARE-SE ANTES DO COMBATE", "ROXO = CONTROLES INVERTIDOS"
    };
    private readonly string[] explicacoes = {
        "Use o analógico esquerdo para andar em qualquer direção.\nNo teclado: WASD ou setas.",
        "Com o canhão equipado, use o analógico direito para escolher a direção do tiro.\nCom as outras armas, Alex acompanha a direção do movimento.",
        "Aperte QUADRADO para atacar com a arma selecionada.\nNo teclado e mouse: botão esquerdo do mouse.",
        "Aperte X ou R2 para avançar rapidamente na direção do movimento.\nO dash NÃO dá invencibilidade. Com o CANHÃO equipado, você NÃO pode usar dash: R2 atira.\nNo teclado: Espaço ou Shift.",
        "L1 seleciona a arma anterior. R1 seleciona a próxima. Ao chegar ao fim, a lista recomeça.\nO item grande no centro está equipado; os menores mostram os vizinhos.\nNo teclado e mouse: 1 / 2 ou a rodinha.",
        "Direcional PARA CIMA usa poção de vida. PARA BAIXO usa poção de poder.\nA força dura 15 segundos: Alex pisca laranja e um contador mostra o tempo restante.\nCada uso gasta uma unidade. No teclado: Z para vida e X para poder.",
        "Ao entrar por completo na sala, as portas fecham e surge fumaça nos pontos de spawn.\nDurante o aviso, os inimigos ainda não recebem dano. Prepare-se: eles aparecerão em seguida!\nBaús e portal para o boss só aparecem depois de limpar a sala.",
        "A bruxinha inverte seu movimento por 5 segundos: Alex pisca roxo e o aviso mostra o tempo restante.\nEnquanto durar, mova o analógico no sentido oposto. A inversão termina quando o contador zera.\nNos raios, procure a área segura sinalizada antes do ataque."
    };

    private Canvas canvas;
    private RectTransform painelControle, palco, personagem, arma, mira, destaque, marcadorAnalogico;
    private Image imagemAlex, imagemArma, brilho, progressoPulo, linhaA, linhaB, pocaoA, pocaoB, fade;
    private Coroutine rotinaFade;
    private TextMeshProUGUI titulo, contador, acao, explicacao, estadoDemo, dica, quantidades, aviso;
    private RectTransform[] inimigos = new RectTransform[2];
    private CanvasGroup[] gruposInimigos = new CanvasGroup[2];
    private Image[] fantasmas = new Image[5], fumaca = new Image[24];
    private Image[] tiros = new Image[4];
    private GameObject visaoBruxa;
    private Texture2D texturaDestaque;
    private Sprite spriteDestaque, espada, machado, canhao;
    private int etapa;
    private float inicioEtapa, inicioPressao, entradaLiberada;
    private ButtonControl pressionado;
    private bool saiuDoBotaoInicial, finalizando;
    private readonly Color Ouro = new Color(.95f, .75f, .35f);
    private readonly Color Roxo = new Color(.78f, .33f, 1f);

    private void Start()
    {
        GameFlow.Reset();
        CriarInterface();
        espada = CarregarSprite("Armas/espada-heroi");
        machado = ItemSprites.Obter(Chest.ItemType.Axe);
        canhao = CarregarSprite("Armas/cannon");
        MudarEtapa(0);
        entradaLiberada = Time.unscaledTime + Mathf.Max(.35f, duracaoFade);
        rotinaFade = StartCoroutine(Fade(1f, 0f, null));
    }

    private IEnumerator Fade(float de, float para, System.Action aoTerminar)
    {
        fade.gameObject.SetActive(true);
        for (float t = 0f; t < duracaoFade; t += Time.unscaledDeltaTime)
        {
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(de, para, t / duracaoFade));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, para);
        fade.gameObject.SetActive(para > 0f);
        rotinaFade = null;
        aoTerminar?.Invoke();
    }

    private static Sprite CarregarSprite(string caminho)
    {
        var sprites = Resources.LoadAll<Sprite>(caminho);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    private void Update()
    {
        if (canvas == null) return;
        float t = Time.unscaledTime - inicioEtapa;
        Animar(t);
        if (finalizando) return;
        bool algumSegurado = (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed) ||
            (Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.spaceKey.isPressed)) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);
        if (!saiuDoBotaoInicial)
        {
            if (!algumSegurado && Time.unscaledTime >= entradaLiberada) saiuDoBotaoInicial = true;
            return;
        }
        if (pressionado == null)
        {
            pressionado = BotaoApertado();
            if (pressionado != null) inicioPressao = Time.unscaledTime;
        }
        if (pressionado != null)
        {
            if (!pressionado.device.added || !pressionado.device.enabled)
            {
                pressionado = null;
                progressoPulo.rectTransform.sizeDelta = new Vector2(0, 5);
            }
            else if (pressionado.isPressed)
            {
                float p = (Time.unscaledTime - inicioPressao) / tempoParaPular;
                progressoPulo.rectTransform.sizeDelta = new Vector2(580 * Mathf.Clamp01(p), 5);
                if (p >= 1) Concluir();
            }
            else
            {
                pressionado = null;
                progressoPulo.rectTransform.sizeDelta = new Vector2(0, 5);
                if (etapa == titulos.Length - 1) Concluir();
                else MudarEtapa(etapa + 1);
            }
        }
        if (Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame && etapa > 0)
            MudarEtapa(etapa - 1);
    }

    private static ButtonControl BotaoApertado()
    {
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return Gamepad.current.buttonSouth;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame) return Keyboard.current.enterKey;
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return Keyboard.current.spaceKey;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return Mouse.current.leftButton;
        return null;
    }

    private void Concluir()
    {
        if (finalizando) return;
        finalizando = true;
        float inicio = fade.gameObject.activeSelf ? fade.color.a : 0f;
        if (rotinaFade != null) StopCoroutine(rotinaFade);
        rotinaFade = StartCoroutine(Fade(inicio, 1f, () => SceneManager.LoadSceneAsync(proximaCena)));
    }

    private void MudarEtapa(int indice)
    {
        etapa = indice;
        inicioEtapa = Time.unscaledTime;
        titulo.text = titulos[etapa];
        contador.text = $"GUIA DO ALEX  •  {etapa + 1:00} / {titulos.Length:00}";
        acao.text = acoes[etapa];
        explicacao.text = explicacoes[etapa];
        dica.text = "Toque X para avançar  •  Segure X por 1,5s para pular o tutorial\nTeclado / mouse: Enter, Espaço ou clique  •  Seta esquerda: volta uma etapa";
        imagemAlex.sprite = alex;
        imagemAlex.color = Color.white;
        personagem.anchoredPosition = new Vector2(-80, -25);
        personagem.localScale = Vector3.one;
        imagemArma.sprite = espada;
        imagemArma.enabled = etapa == 1 || etapa == 2 || etapa == 4;
        pocaoA.gameObject.SetActive(etapa == 5);
        pocaoB.gameObject.SetActive(etapa == 5);
        quantidades.gameObject.SetActive(etapa == 5);
        mira.gameObject.SetActive(etapa == 1);
        visaoBruxa.SetActive(etapa == 7);
        for (int i = 0; i < 2; i++) gruposInimigos[i].alpha = etapa == 6 || (etapa == 2 && i == 0) ? 1 : 0;
        for (int i = 0; i < fantasmas.Length; i++) fantasmas[i].gameObject.SetActive(etapa == 3);
        for (int i = 0; i < fumaca.Length; i++) fumaca[i].gameObject.SetActive(etapa == 6);
        for (int i = 0; i < tiros.Length; i++) tiros[i].gameObject.SetActive(etapa == 1);
        painelControle.gameObject.SetActive(etapa < 6);
        aviso.gameObject.SetActive(etapa >= 6);
        aviso.text = etapa == 6 ? "1. ENTRE NA SALA\n|\n2. VEJA A FUMAÇA\n|\n3. PREPARE-SE PARA LUTAR\n\nSó ataque depois que eles aparecerem!" :
            "ENCANTO DA BRUXINHA\n\nAlex piscando ROXO\n+\nCONTADOR NA TELA\n\nMova-se no sentido oposto\naté o tempo acabar.";
        aviso.color = etapa == 7 ? Roxo : Ouro;
        for (int i = 0; i < 2; i++) inimigos[i].anchoredPosition = new Vector2(110 + i * 135, -30);
        estadoDemo.text = string.Empty;
        estadoDemo.color = Ouro;
    }

    private void Animar(float t)
    {
        float pulso = .35f + .65f * Mathf.PingPong(t * 1.8f, 1);
        brilho.color = new Color(1, .78f, .3f, pulso);
        Vector2 alvo = Vector2.zero;
        switch (etapa)
        {
            case 0: alvo = new Vector2(-72, -4); break;
            case 1: alvo = new Vector2(83, -4); break;
            case 2: alvo = new Vector2(127, 90); break;
            case 3: alvo = new Vector2(171, 46); break;
            case 4: alvo = new Vector2(Mathf.FloorToInt(t / 1.5f) % 2 == 0 ? -171 : 171, 189); break;
            case 5: alvo = new Vector2(-176, Mathf.FloorToInt(t / 2) % 2 == 0 ? 117 : 73); break;
        }
        destaque.anchoredPosition = alvo;
        marcadorAnalogico.gameObject.SetActive(etapa == 0 || etapa == 1);
        marcadorAnalogico.anchoredPosition = alvo + new Vector2(Mathf.Cos(t * 1.6f), Mathf.Sin(t * 1.6f)) * 11;
        LigarLinha(linhaA.rectTransform, alvo, new Vector2(alvo.x, -170));
        LigarLinha(linhaB.rectTransform, new Vector2(alvo.x, -170), new Vector2(0, -190));
        personagem.localScale = Vector3.one;
        imagemAlex.color = Color.white;
        imagemArma.color = Color.white;
        arma.localEulerAngles = Vector3.zero;
        arma.anchoredPosition = new Vector2(10, -5);
        if (etapa == 0)
        {
            float x = Mathf.Sin(t * 1.4f) * 150;
            personagem.anchoredPosition = new Vector2(x - 30, -25);
            imagemAlex.sprite = QuadroCaminhada(t, Mathf.Cos(t * 1.4f) < 0);
            imagemAlex.rectTransform.localScale = Vector3.one;
            estadoDemo.text = "Ande e escolha sua posição.";
        }
        else
        {
            imagemAlex.sprite = Quadro(paradoFrente, t, alex);
            imagemAlex.rectTransform.localScale = Vector3.one;
        }
        if (etapa == 1)
        {
            imagemArma.sprite = canhao;
            float a = t * 1.2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            arma.anchoredPosition = personagem.anchoredPosition + dir * 65;
            arma.localEulerAngles = new Vector3(0, 0, a * Mathf.Rad2Deg);
            mira.anchoredPosition = personagem.anchoredPosition + dir * 170;
            for (int i = 0; i < tiros.Length; i++)
                tiros[i].rectTransform.anchoredPosition = personagem.anchoredPosition + dir * (80 + Mathf.Repeat(t * 180 + i * 55, 150));
            estadoDemo.text = "CANHÃO EQUIPADO: SEM DASH\nQuadrado ou R2 atira.";
        }
        if (etapa == 2)
        {
            imagemAlex.sprite = Quadro(paradoLateral, t, alex);
            float p = Mathf.Repeat(t, 1.5f);
            float golpe = Mathf.Clamp01(p / .3f);
            float angulo = Mathf.Lerp(60, -55, golpe) * Mathf.Deg2Rad;
            arma.anchoredPosition = personagem.anchoredPosition + new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * 75;
            arma.localEulerAngles = new Vector3(0, 0, angulo * Mathf.Rad2Deg - 45);
            inimigos[0].anchoredPosition = new Vector2(125 + (p < .4f ? 18 * Mathf.Sin(p / .4f * Mathf.PI) : 0), -30);
            estadoDemo.text = p < .4f ? "ACERTOU!" : "Um golpe pode atingir vários inimigos.";
        }
        if (etapa == 3)
        {
            float p = Mathf.Repeat(t, 2.8f);
            float k = Mathf.Clamp01((p - .75f) / .18f);
            float x = Mathf.Lerp(-190, 145, k);
            personagem.anchoredPosition = new Vector2(x, -25);
            imagemAlex.sprite = QuadroCaminhada(t);
            for (int i = 0; i < fantasmas.Length; i++)
            {
                fantasmas[i].sprite = QuadroCaminhada(Mathf.Max(0, t - (i + 1) * .03f));
                fantasmas[i].rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(-190, 145, Mathf.Clamp01(k - (i + 1) * .1f)), -25);
                fantasmas[i].color = new Color(.5f, .8f, 1, p > .75f && p < 1.4f ? (1 - (p - .75f) / .65f) * .45f : 0);
            }
            estadoDemo.text = "DASH NÃO DÁ INVENCIBILIDADE\nDesvie dos golpes!";
        }
        if (etapa == 4)
        {
            imagemAlex.sprite = Quadro(paradoLateral, t, alex);
            int selecionada = Mathf.FloorToInt(t / 1.5f) % 3;
            imagemArma.sprite = selecionada == 0 ? espada : selecionada == 1 ? machado : canhao;
            arma.anchoredPosition = personagem.anchoredPosition + new Vector2(70, 0);
            arma.localEulerAngles = new Vector3(0, 0, selecionada == 2 ? 0 : -45);
            estadoDemo.text = selecionada == 0 ? "CANHÃO   [ ESPADA ]   MACHADO" :
                selecionada == 1 ? "ESPADA   [ MACHADO ]   CANHÃO" : "MACHADO   [ CANHÃO ]   ESPADA";
        }
        if (etapa == 5)
        {
            bool poder = Mathf.FloorToInt(t / 2) % 2 == 1;
            float p = Mathf.Repeat(t, 2);
            pocaoA.rectTransform.anchoredPosition = new Vector2(80, 100);
            pocaoB.rectTransform.anchoredPosition = new Vector2(215, 100);
            Image ativa = poder ? pocaoB : pocaoA;
            ativa.rectTransform.anchoredPosition = Vector2.Lerp(poder ? new Vector2(215, 100) : new Vector2(80, 100),
                personagem.anchoredPosition + Vector2.up * 35, Mathf.Clamp01((p - .6f) / .5f));
            imagemAlex.color = p > 1.1f ? Color.Lerp(Color.white, poder ? PlayerStatsHandler.CorPocaoPoder : new Color(1, .35f, .4f), pulso) : Color.white;
            quantidades.text = $"VIDA ×{(!poder && p > 1.1f ? 2 : 3)}      PODER ×{(poder && p > 1.1f ? 1 : 2)}";
            estadoDemo.text = poder ? "FORÇA ATIVA: 15s\nAlex pisca laranja durante o efeito." : "Poção de vida: recupera sua vida.";
        }
        if (etapa == 6)
        {
            float p = Mathf.Repeat(t, 4.5f);
            for (int i = 0; i < 2; i++) gruposInimigos[i].alpha = p >= 1.2f ? 1 : 0;
            for (int i = 0; i < fumaca.Length; i++)
            {
                float idade = Mathf.Repeat(p + i * .09f, .7f);
                Vector2 centro = new Vector2(i % 2 == 0 ? 110 : 245, -30);
                fumaca[i].rectTransform.anchoredPosition = centro + new Vector2(Mathf.Sin(i * 7.3f) * 40, idade * 75);
                fumaca[i].color = new Color(.72f, .72f, .75f, p < 1.2f ? 1 - idade / .7f : 0);
            }
            estadoDemo.text = p < 1.2f ? "FUMAÇA: INIMIGOS PRESTES A APARECER" : "AGORA ELES PODEM ATACAR E RECEBER DANO!";
        }
        if (etapa == 7)
        {
            float p = Mathf.Repeat(t, 9);
            bool invertido = p < 5;
            personagem.anchoredPosition = new Vector2(-80 - Mathf.Sin(t * 1.6f) * 55, -25);
            imagemAlex.sprite = QuadroCaminhada(t);
            imagemAlex.color = invertido ? Color.Lerp(Color.white, Roxo, pulso) : Color.white;
            estadoDemo.color = invertido ? Roxo : Ouro;
            estadoDemo.text = invertido ? $"CONTROLES INVERTIDOS  •  {5 - p:0.0}s\nO movimento está ao contrário!" : "EFEITO ENCERRADO\nOs controles voltaram ao normal.";
        }
    }

    private Sprite QuadroCaminhada(float t, bool esquerda = false) =>
        Quadro(esquerda && caminhadaEsquerda != null && caminhadaEsquerda.Length > 0 ? caminhadaEsquerda : caminhada, t, alex);

    private static Sprite Quadro(Sprite[] quadros, float t, Sprite fallback) =>
        quadros != null && quadros.Length > 0 ? quadros[Mathf.FloorToInt(t * 10) % quadros.Length] : fallback;

    private void CriarInterface()
    {
        var go = new GameObject("Tutorial UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        Image fundo = Imagem("Fundo", go.transform, Vector2.zero, new Vector2(5000, 3000), null);
        fundo.color = new Color(.035f, .045f, .065f);
        contador = Texto("Passo", go.transform, new Vector2(0, 445), new Vector2(1700, 50), 25, Ouro);
        titulo = Texto("Titulo", go.transform, new Vector2(0, 365), new Vector2(1740, 100), 48, Color.white);
        painelControle = Retangulo("Controle", go.transform, new Vector2(-440, 10), new Vector2(770, 550));
        Imagem("Controle PlayStation", painelControle, new Vector2(0, 40), new Vector2(620, 400), controle);
        // Marcador acompanha apenas o comando explicado, sem as demais linhas do menu.
        spriteDestaque = CriarAnel();
        brilho = Imagem("Botao piscando", painelControle, Vector2.zero, new Vector2(55, 55), spriteDestaque);
        destaque = brilho.rectTransform;
        marcadorAnalogico = Imagem("Movimento do analogico", painelControle, Vector2.zero, new Vector2(12, 12), null).rectTransform;
        marcadorAnalogico.GetComponent<Image>().color = Ouro;
        linhaA = Imagem("Linha do comando", painelControle, Vector2.zero, Vector2.one, null);
        linhaB = Imagem("Linha ate legenda", painelControle, Vector2.zero, Vector2.one, null);
        linhaA.color = linhaB.color = new Color(.95f, .75f, .35f, .7f);
        acao = Texto("Comando em destaque", painelControle, new Vector2(0, -225), new Vector2(820, 100), 28, Ouro);
        palco = Retangulo("Demonstracao", go.transform, new Vector2(450, 45), new Vector2(700, 450));
        aviso = Texto("Aviso da sala", go.transform, new Vector2(-440, 10), new Vector2(750, 450), 35, Ouro);
        var piso = Imagem("Piso", palco, new Vector2(0, -150), new Vector2(660, 14), null);
        piso.color = new Color(.22f, .25f, .3f);
        for (int i = 0; i < fantasmas.Length; i++)
            fantasmas[i] = Imagem("Rastro do dash", palco, Vector2.zero, new Vector2(110, 140), alex);
        imagemAlex = Imagem("Alex", palco, new Vector2(-80, -25), new Vector2(110, 140), alex);
        personagem = imagemAlex.rectTransform;
        imagemArma = Imagem("Arma", palco, Vector2.zero, new Vector2(100, 100), null);
        arma = imagemArma.rectTransform;
        for (int i = 0; i < 2; i++)
        {
            inimigos[i] = MontarVisual(inimigoModular, palco, new Vector2(110 + 135 * i, -30), 120);
            gruposInimigos[i] = inimigos[i].gameObject.AddComponent<CanvasGroup>();
        }
        visaoBruxa = MontarVisual(bruxinha, palco, new Vector2(220, 80), 120).gameObject;
        mira = Retangulo("Mira", palco, Vector2.zero, new Vector2(28, 28));
        Imagem("Horizontal", mira, Vector2.zero, new Vector2(28, 3), null).color = Ouro;
        Imagem("Vertical", mira, Vector2.zero, new Vector2(3, 28), null).color = Ouro;
        for (int i = 0; i < tiros.Length; i++)
        {
            tiros[i] = Imagem("Tiro demonstrativo", palco, Vector2.zero, new Vector2(10, 10), null);
            tiros[i].color = Ouro;
        }
        pocaoA = Imagem("Vida", palco, new Vector2(80, 100), new Vector2(60, 60), ItemSprites.Obter(Chest.ItemType.HealthPotion));
        pocaoB = Imagem("Poder", palco, new Vector2(215, 100), new Vector2(60, 60), ItemSprites.Obter(Chest.ItemType.PowerPotion));
        quantidades = Texto("Quantidade de pocoes", palco, new Vector2(130, 160), new Vector2(360, 45), 23, Color.white);
        for (int i = 0; i < fumaca.Length; i++)
            fumaca[i] = Imagem("Aviso de spawn", palco, Vector2.zero, Vector2.one * (10 + i % 4 * 3), null);
        estadoDemo = Texto("Estado demonstrado", palco, new Vector2(0, -235), new Vector2(800, 100), 26, Ouro);
        explicacao = Texto("Explicacao", go.transform, new Vector2(0, -320), new Vector2(1660, 150), 29, new Color(.85f, .88f, .93f));
        dica = Texto("Avancar ou pular", go.transform, new Vector2(0, -460), new Vector2(1700, 80), 23, new Color(.66f, .7f, .78f));
        var trilho = Imagem("Trilho para pular", go.transform, new Vector2(0, -515), new Vector2(580, 5), null);
        trilho.color = new Color(.2f, .22f, .27f);
        progressoPulo = Imagem("Progresso para pular", trilho.transform, new Vector2(-290, 0), new Vector2(0, 5), null);
        progressoPulo.rectTransform.pivot = new Vector2(0, .5f);
        progressoPulo.color = Ouro;
        fade = Imagem("Fade", go.transform, Vector2.zero, new Vector2(5000, 3000), null);
        fade.color = Color.black;
    }

    private RectTransform MontarVisual(GameObject prefab, Transform pai, Vector2 pos, float altura)
    {
        RectTransform grupo = Retangulo("Personagem demonstrativo", pai, pos, new Vector2(150, altura));
        if (prefab == null) return grupo;
        var partes = new List<SpriteRenderer>();
        foreach (var parte in prefab.GetComponentsInChildren<SpriteRenderer>(true))
            if (parte.sprite != null && parte.enabled) partes.Add(parte);
        partes.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
        if (partes.Count == 0) return grupo;
        Bounds bounds = partes[0].bounds;
        foreach (var parte in partes) bounds.Encapsulate(parte.bounds);
        float escala = altura / Mathf.Max(.01f, bounds.size.y);
        foreach (var parte in partes)
        {
            var imagem = Imagem(parte.name, grupo, (Vector2)(parte.bounds.center - bounds.center) * escala,
                (Vector2)parte.bounds.size * escala, parte.sprite);
            imagem.color = parte.color;
            imagem.rectTransform.localScale = new Vector3(parte.flipX ? -1 : 1, parte.flipY ? -1 : 1, 1);
        }
        return grupo;
    }

    private static RectTransform Retangulo(string nome, Transform pai, Vector2 pos, Vector2 tamanho)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        var rect = objeto.GetComponent<RectTransform>();
        rect.SetParent(pai, false);
        rect.anchorMin = rect.anchorMax = Vector2.one * .5f;
        rect.anchoredPosition = pos;
        rect.sizeDelta = tamanho;
        return rect;
    }

    private static Image Imagem(string nome, Transform pai, Vector2 pos, Vector2 tamanho, Sprite sprite)
    {
        var imagem = Retangulo(nome, pai, pos, tamanho).gameObject.AddComponent<Image>();
        imagem.sprite = sprite;
        imagem.preserveAspect = sprite != null;
        imagem.raycastTarget = false;
        return imagem;
    }

    private static TextMeshProUGUI Texto(string nome, Transform pai, Vector2 pos, Vector2 tamanho, float fonte, Color cor)
    {
        var texto = Retangulo(nome, pai, pos, tamanho).gameObject.AddComponent<TextMeshProUGUI>();
        texto.fontSize = fonte;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 18;
        texto.fontSizeMax = fonte;
        texto.alignment = TextAlignmentOptions.Center;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.color = cor;
        texto.raycastTarget = false;
        return texto;
    }

    private static void LigarLinha(RectTransform linha, Vector2 inicio, Vector2 fim)
    {
        Vector2 delta = fim - inicio;
        linha.anchoredPosition = (inicio + fim) * .5f;
        linha.sizeDelta = new Vector2(delta.magnitude, 3);
        linha.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Sprite CriarAnel()
    {
        texturaDestaque = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), Vector2.one * 31.5f);
                texturaDestaque.SetPixel(x, y, d < 31 && d > 25 ? Color.white : Color.clear);
            }
        texturaDestaque.Apply();
        return Sprite.Create(texturaDestaque, new Rect(0, 0, 64, 64), Vector2.one * .5f);
    }

    private void OnDestroy()
    {
        if (spriteDestaque != null) Destroy(spriteDestaque);
        if (texturaDestaque != null) Destroy(texturaDestaque);
    }
}
