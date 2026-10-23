using UnityEngine;

// Pena da Fenix: vida extra
public static class PenaFenix
{
    public const string Nome = "Pena da Fênix";
    public static readonly Color Cor = new Color(1f, 0.55f, 0.2f);

    private static Sprite icone;
    private static AudioClip somReviver;
    private static AudioClip vozOriginal;
    private static AudioClip vozInvertida;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Resetar()
    {
        icone = null;
        somReviver = null;
        vozOriginal = null;
        vozInvertida = null;
    }

    public static Sprite Icone
    {
        get
        {
            if (icone != null) return icone;
            var arte = Resources.LoadAll<Sprite>("Itens/pena-fenix");
            if (arte.Length > 0) return icone = arte[0];

            const int tamanho = 32;
            var textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var vermelho = new Color(0.85f, 0.18f, 0.12f);
            var laranja = new Color(1f, 0.55f, 0.15f);
            var amarelo = new Color(1f, 0.86f, 0.3f);
            var brilho = new Color(1f, 0.97f, 0.78f);
            var haste = new Color(1f, 0.93f, 0.72f);
            var contorno = new Color(0.33f, 0.07f, 0.05f);

            Vector2 baseDaPena = new Vector2(7f, 4f);
            Vector2 eixo = new Vector2(26f, 28f) - baseDaPena;
            float comprimento = eixo.magnitude;
            Vector2 direcao = eixo / comprimento;
            Vector2 normal = new Vector2(-direcao.y, direcao.x);

            var pixels = new Color[tamanho * tamanho];
            for (int y = 0; y < tamanho; y++)
                for (int x = 0; x < tamanho; x++)
                {
                    Vector2 p = new Vector2(x + .5f, y + .5f) - baseDaPena;
                    float t = Vector2.Dot(p, direcao) / comprimento; // 0 = base, 1 = ponta
                    float lado = Vector2.Dot(p, normal);
                    Color pixel = Color.clear;

                    if (t >= .2f && t <= 1f)
                    {
                        float u = (t - .2f) / .8f;
                        float largura = 5f * Mathf.Sin(Mathf.PI * Mathf.Pow(u, .85f));
                        float distancia = Mathf.Abs(lado);
                        // Dois cortes inclinados nas barbas pra parecer pena e nao folha
                        bool corte = (lado > 1.2f && Mathf.Abs(u - .40f - .03f * distancia) < .035f) ||
                                     (lado < -1.2f && Mathf.Abs(u - .58f - .03f * distancia) < .035f);
                        if (distancia < largura && !corte)
                        {
                            pixel = u < .5f ? Color.Lerp(vermelho, laranja, u / .5f)
                                            : Color.Lerp(laranja, amarelo, (u - .5f) / .5f);
                            if (lado > 0f && lado < largura * .45f) pixel = Color.Lerp(pixel, brilho, .35f);
                        }
                    }
                    if (t >= 0f && t <= .97f && Mathf.Abs(lado) < .75f) pixel = haste;
                    pixels[y * tamanho + x] = pixel;
                }

            // Contorno: pixel vazio encostado num pixel pintado
            var final = (Color[])pixels.Clone();
            for (int y = 0; y < tamanho; y++)
                for (int x = 0; x < tamanho; x++)
                {
                    if (pixels[y * tamanho + x].a > 0f) continue;
                    bool vizinho = (x > 0 && pixels[y * tamanho + x - 1].a > 0f) ||
                                   (x < tamanho - 1 && pixels[y * tamanho + x + 1].a > 0f) ||
                                   (y > 0 && pixels[(y - 1) * tamanho + x].a > 0f) ||
                                   (y < tamanho - 1 && pixels[(y + 1) * tamanho + x].a > 0f);
                    if (vizinho) final[y * tamanho + x] = contorno;
                }
            textura.SetPixels(final);
            textura.Apply();
            icone = Sprite.Create(textura, new Rect(0, 0, tamanho, tamanho), Vector2.one * .5f, tamanho);
            icone.name = "PenaFenix_Icone";
            return icone;
        }
    }

    // Copia do audio tocada de tras pra frente, sem o silencio das pontas
    public static AudioClip Invertido(AudioClip original)
    {
        if (original == null) return null;
        if (original == vozOriginal && vozInvertida != null) return vozInvertida;
        if (original.loadState != AudioDataLoadState.Loaded) original.LoadAudioData();

        int canais = original.channels;
        var dados = new float[original.samples * canais];
        if (!original.GetData(dados, 0))
        {
            Debug.LogWarning("Nao deu pra ler o audio pra inverter: " + original.name);
            return null;
        }

        int primeiro = 0, ultimo = original.samples - 1;
        while (primeiro < ultimo && Silencioso(dados, primeiro, canais)) primeiro++;
        while (ultimo > primeiro && Silencioso(dados, ultimo, canais)) ultimo--;

        int quadros = ultimo - primeiro + 1;
        var invertido = new float[quadros * canais];
        for (int i = 0; i < quadros; i++)
            for (int c = 0; c < canais; c++)
                invertido[i * canais + c] = dados[(ultimo - i) * canais + c];

        var clip = AudioClip.Create(original.name + " (invertido)", quadros, canais, original.frequency, false);
        clip.SetData(invertido, 0);
        vozOriginal = original;
        vozInvertida = clip;
        return clip;
    }

    private static bool Silencioso(float[] dados, int quadro, int canais)
    {
        for (int c = 0; c < canais; c++)
            if (Mathf.Abs(dados[quadro * canais + c]) > .01f) return false;
        return true;
    }

    // Som de reviver: Assets/CustomAssets/Resources/Sons/reviver.mp3
    public static AudioClip SomReviver
    {
        get
        {
            if (somReviver != null) return somReviver;
            somReviver = Resources.Load<AudioClip>("Sons/reviver");
            if (somReviver == null) Debug.LogWarning("Som nao encontrado: Resources/Sons/reviver");
            return somReviver;
        }
    }
}

/// <summary>Quantas penas da fenix o jogador tem (so aparece quando tem pelo menos uma).</summary>
public class PenaFenixHUD : MonoBehaviour
{
    private HeartSystem vida;
    private GameObject painel;
    private UnityEngine.UI.Text quantidade;
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

        painel = new GameObject("Pena da Fenix - quantidade", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        painel.transform.SetParent(canvas.transform, false);
        var rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-85, -305);
        rect.sizeDelta = new Vector2(80, 80);
        var icone = painel.GetComponent<UnityEngine.UI.Image>();
        icone.sprite = PenaFenix.Icone;
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        var texto = new GameObject("Quantidade", typeof(RectTransform), typeof(UnityEngine.UI.Text), typeof(UnityEngine.UI.Outline));
        texto.transform.SetParent(painel.transform, false);
        var textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchoredPosition = new Vector2(34, -30);
        textoRect.sizeDelta = new Vector2(60, 36);
        quantidade = texto.GetComponent<UnityEngine.UI.Text>();
        quantidade.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quantidade.fontSize = 26;
        quantidade.fontStyle = FontStyle.Bold;
        quantidade.alignment = TextAnchor.MiddleCenter;
        quantidade.color = Color.white;
        quantidade.raycastTarget = false;
        var contorno = texto.GetComponent<UnityEngine.UI.Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(2, -2);
        painel.SetActive(false);
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (painel == null || vida == null) return;
        int valor = vida.Penas;
        if (valor == ultimoValor) return;
        ultimoValor = valor;
        painel.SetActive(valor > 0);
        quantidade.text = "x" + valor;
    }

    private void OnDestroy()
    {
        if (painel != null) Destroy(painel);
    }
}
