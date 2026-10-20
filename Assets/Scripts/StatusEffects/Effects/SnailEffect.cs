using UnityEngine;

// Caracol Imortal: um caracol bem lento te persegue pelo mapa pelo resto da run.

public class SnailEffect : StatusEffect
{
    private SnailChaser caracol;

    public SnailEffect(float duration = 0f)
    {
        Duration = duration;
    }

    public override void OnApply(PlayerContext ctx)
    {
        if (ctx.Movement == null) return;
        caracol = SnailChaser.Spawn(ctx.Movement.transform);
    }

    public override void OnRemove(PlayerContext ctx)
    {
        if (caracol != null) Object.Destroy(caracol.gameObject);
    }
}

// Executa depois do CinemachineBrain (ordem 0), que move a camera no LateUpdate
[DefaultExecutionOrder(1000)]
public class SnailChaser : MonoBehaviour
{
    [Tooltip("Unidades por segundo. O jogador anda a 10, entao da pra fugir, mas ele nunca para")]
    [SerializeField, Min(0f)] private float velocidade = 1.5f;
    [Tooltip("Distancia do jogador onde o caracol aparece (ao escolher o debuff e a cada andar novo)")]
    [SerializeField, Min(1f)] private float distanciaInicial = 22f;
    [Tooltip("Distancia que conta como encostar no jogador.")]
    [SerializeField, Min(0.05f)] private float raioToque = 0.45f;

    [Header("Icone na borda da tela")]
    [Tooltip("Tamanho do icone quando o caracol esta longe.")]
    [SerializeField, Min(0.05f)] private float tamanhoLonge = 0.45f;
    [Tooltip("Tamanho do icone logo antes do caracol entrar na tela.")]
    [SerializeField, Min(0.05f)] private float tamanhoPerto = 1.4f;
    [Tooltip("A partir de quantos tiles fora da tela o icone fica no tamanho minimo.")]
    [SerializeField, Min(0.1f)] private float tilesLonge = 20f;
    [Tooltip("Abaixo de quantos tiles fora da tela o icone comeca a pulsar (crescer e encolher).")]
    [SerializeField, Min(0f)] private float tilesPerto = 6f;
    [Tooltip("Quantas vezes por segundo o icone cresce e volta.")]
    [SerializeField, Min(0.1f)] private float pulsosPorSegundo = 1.2f;
    [Tooltip("Quanto o icone cresce no pulso (0.2 = 20% maior).")]
    [SerializeField, Min(0f)] private float forcaPulso = 0.2f;
    [Tooltip("Orthographic Size da camera em que os tamanhos do icone valem. Com zoom (corredores usam 4), o icone diminui junto e fica do mesmo tamanho na tela.")]
    [SerializeField, Min(0.01f)] private float zoomReferencia = 10f;
    [Tooltip("Distancia da seta ate o centro do icone (1 = largura do icone).")]
    [SerializeField, Min(0f)] private float distanciaSeta = 0.8f;

    // Se o jogador "pular" mais que isso num frame, foi teleporte (andar novo, sala do chefe)
    private const float SaltoTeleporte = 5f;
    private static readonly Color CorAviso = new Color(0.6f, 0.95f, 0.45f);

    private Transform alvo;
    private HeartSystem vida;
    private SpriteRenderer sr;
    private SpriteRenderer srIndicador;
    private SpriteRenderer srSeta;
    private Material materialIndicador;
    private System.Random rng;
    private Vector3 ultimaPosAlvo;
    private float proximoRastro;
    private bool avisou;
    private bool pegou;

    private static Sprite sprite;
    private static Sprite spriteSeta;
    private static Sprite spriteGosma;
    private static bool gosmaProcurada; // evita procurar o arquivo a cada gota quando ele nao existe

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        sprite = null;
        spriteSeta = null;
        spriteGosma = null;
        gosmaProcurada = false;
    }

    public static SnailChaser Spawn(Transform alvo)
    {
        var go = new GameObject("Caracol Imortal");
        var caracol = go.AddComponent<SnailChaser>();
        caracol.Init(alvo);
        return caracol;
    }

    private void Init(Transform alvo)
    {
        this.alvo = alvo;
        vida = alvo.GetComponent<HeartSystem>();
        rng = Rng.NewCombatRandom();

        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = Arte;
        var srJogador = alvo.GetComponent<SpriteRenderer>();
        if (srJogador != null)
        {
            // Mesma camada e material do jogador, assim o caracol recebe a mesma iluminacao
            sr.sortingLayerID = srJogador.sortingLayerID;
            sr.sortingOrder = srJogador.sortingOrder;
            sr.sharedMaterial = srJogador.sharedMaterial;
        }

        // Icone na borda da tela apontando onde o caracol esta quando ele sai da camera
        // Fora da hierarquia do caracol pra nao herdar o "rastejar" (estica e encolhe)
        var indicador = new GameObject("Indicador do caracol");
        srIndicador = indicador.AddComponent<SpriteRenderer>();
        srIndicador.sprite = Arte;
        materialIndicador = new Material(Shader.Find("Sprites/Default"));
        srIndicador.sharedMaterial = materialIndicador;
        srIndicador.sortingLayerName = "UI";
        srIndicador.sortingOrder = 450;
        srIndicador.enabled = false;

        // Seta em volta do icone apontando pro caracol. Filha do icone: cresce e pulsa junto
        var seta = new GameObject("Seta do caracol");
        seta.transform.SetParent(indicador.transform, false);
        srSeta = seta.AddComponent<SpriteRenderer>();
        srSeta.sprite = ArteSeta;
        srSeta.sharedMaterial = materialIndicador;
        srSeta.sortingLayerName = "UI";
        srSeta.sortingOrder = 451;
        srSeta.enabled = false;

        Reposicionar();
    }

    private void OnDestroy()
    {
        if (srIndicador != null) Destroy(srIndicador.gameObject);
        if (materialIndicador != null) Destroy(materialIndicador);
    }

    private void Update()
    {
        if (alvo == null) { Destroy(gameObject); return; }

        // Andar novo ou teleporte pro chefe: o caracol recomeca longe, mas continua vindo
        if ((alvo.position - ultimaPosAlvo).sqrMagnitude > SaltoTeleporte * SaltoTeleporte) Reposicionar();
        ultimaPosAlvo = alvo.position;

        if (!GameFlow.CanPlay || pegou || (vida != null && vida.IsDead)) return;

        Vector3 antes = transform.position;
        transform.position = Vector3.MoveTowards(antes, alvo.position, velocidade * Time.deltaTime);

        // A arte olha pra esquerda; vira quando anda pra direita
        float dx = alvo.position.x - antes.x;
        if (Mathf.Abs(dx) > 0.01f) sr.flipX = dx > 0f;

        // Rastejando: estica e encolhe devagar
        float onda = Mathf.Sin(Time.time * 6f) * 0.08f;
        transform.localScale = new Vector3(1f + onda, 1f - onda, 1f);

        DeixarRastro();

        if (Vector2.Distance(transform.position, alvo.position) <= raioToque) Pegar();
    }

    private void Pegar()
    {
        pegou = true;
        GameEffects.Instance.Shake(0.8f);
        if (vida != null) vida.InstaKill();
    }

    private void Reposicionar()
    {
        double angulo = rng.NextDouble() * System.Math.PI * 2.0;
        Vector3 direcao = new Vector3((float)System.Math.Cos(angulo), (float)System.Math.Sin(angulo), 0f);
        transform.position = alvo.position + direcao * distanciaInicial;
        ultimaPosAlvo = alvo.position;

    }

    // Gosma: quadradinhos verdes que ficam no chao e somem devagar
    private void DeixarRastro()
    {
        if (Time.time < proximoRastro) return;
        proximoRastro = Time.time + 0.25f;

        var go = new GameObject("Gosma");
        go.transform.position = transform.position + Vector3.down * 0.3f;
        var gosma = go.AddComponent<SpriteRenderer>();
        // Com o arquivo, a cor ja vem dele; sem, pinta o quadradinho branco de verde translucido
        Sprite arteGosma = ArteGosma;
        gosma.sprite = arteGosma != null ? arteGosma : GameEffects.WhiteSprite;
        gosma.color = arteGosma != null ? Color.white : new Color(0.6f, 0.9f, 0.5f, 0.45f);
        gosma.sortingLayerID = sr.sortingLayerID;
        gosma.sortingOrder = sr.sortingOrder - 1;
        gosma.sharedMaterial = sr.sharedMaterial;
        // Nao usa FxParticle: o Game Over apaga todas as FxParticle, e a gosma deve ficar no chao
        go.AddComponent<GosmaCaracol>().Init(3f, 1.4f);
    }

    // Roda depois do Cinemachine mover a camera (ver DefaultExecutionOrder na classe);
    // no Update o icone ficaria um frame atrasado e tremeria na borda
    private void LateUpdate()
    {
        if (alvo != null) AtualizarIndicador(Camera.main);
    }

    private void AtualizarIndicador(Camera cam)
    {
        // Continua aparecendo no Game Over, pra o jogador ver de onde o caracol vinha
        bool mostrar = cam != null;
        Vector3 vp = mostrar ? cam.WorldToViewportPoint(transform.position) : Vector3.zero;

        Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
        float maior = Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y));

        // Quantos tiles o caracol esta fora da tela (0 = encostado na borda). Em tiles e nao em "telas":
        // a camera da zoom nos corredores, e uma tela de corredor e bem menor que uma de sala
        float fora = 0f;
        if (mostrar)
        {
            Vector3 local = cam.transform.InverseTransformPoint(transform.position);
            float meiaAltura = cam.orthographic ? cam.orthographicSize : 10f;
            float meiaLargura = meiaAltura * cam.aspect;
            fora = Mathf.Max(Mathf.Abs(local.x) - meiaLargura, Mathf.Abs(local.y) - meiaAltura);
        }

        // Some so quando algum pedaco do caracol aparece na tela do jogador
        if (mostrar) mostrar = !NaTela(cam);
        srIndicador.enabled = mostrar;
        srSeta.enabled = mostrar;
        if (!mostrar) return;

        // 0 = bem longe, 1 = logo antes de entrar na tela
        float proximidade = 1f - Mathf.InverseLerp(0f, tilesLonge, fora);

        // Leva o ponto ate a borda da tela, na direcao do caracol. A posicao nunca oscila.
        // 0.4 deixa espaco pra seta nao sair da tela quando o icone esta grande
        float k = 0.4f / Mathf.Max(maior, 0.0001f);
        Vector2 borda = new Vector2(0.5f, 0.5f) + d * k;
        Vector3 mundo = cam.ViewportToWorldPoint(new Vector3(borda.x, borda.y, -cam.transform.position.z));
        mundo.z = 0f;
        srIndicador.transform.position = mundo;

        // Olha pro centro da tela (a arte olha pra esquerda). So vira quando o caracol esta
        // claramente de um lado, pra nao ficar virando quando ele esta bem em cima ou embaixo
        if (Mathf.Abs(d.x) > 0.1f) srIndicador.flipX = d.x < 0f;

        // Seta do lado do icone virado pro caracol, apontando pra ele
        Vector2 paraCaracol = ((Vector2)transform.position - (Vector2)mundo).normalized;
        srSeta.transform.localPosition = paraCaracol * distanciaSeta;
        srSeta.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(paraCaracol.y, paraCaracol.x) * Mathf.Rad2Deg);

        // Longe = pequeno, perto = grande
        float tamanho = Mathf.Lerp(tamanhoLonge, tamanhoPerto, proximidade);

        // Muito perto: o icone cresce um pouco e volta, num ritmo calmo (in and out)
        if (fora < tilesPerto)
        {
            // Comeca o pulso suave em vez de dar um pulo ao entrar na zona de perto
            float entrada = Mathf.Clamp01((tilesPerto - fora) / (tilesPerto * 0.3f + 0.0001f));
            // Tempo do jogo: o pulso congela junto com o resto quando o jogo pausa (Game Over, pause)
            float onda = (1f - Mathf.Cos(Time.time * pulsosPorSegundo * Mathf.PI * 2f)) * 0.5f; // 0 -> 1 -> 0
            tamanho *= 1f + onda * forcaPulso * entrada;
        }
        // A camera da zoom nos corredores; escala junto pra o icone ter sempre o mesmo tamanho na tela
        if (cam.orthographic) tamanho *= cam.orthographicSize / Mathf.Max(0.01f, zoomReferencia);
        srIndicador.transform.localScale = new Vector3(tamanho, tamanho, 1f);

        srIndicador.color = new Color(1f, 1f, 1f, 0.85f);
        srSeta.color = srIndicador.color;
    }

    // Verdadeiro se qualquer parte do sprite do caracol esta dentro da camera
    private bool NaTela(Camera cam)
    {
        Bounds b = sr.bounds;
        Vector3 min = cam.WorldToViewportPoint(b.min);
        Vector3 max = cam.WorldToViewportPoint(b.max);
        return max.x > 0f && min.x < 1f && max.y > 0f && min.y < 1f;
    }

    // Arte editavel em Assets/CustomAssets/Resources/Caracol (.png, ou .aseprite com o mesmo nome).
    // Sem o arquivo, cada sprite cai no desenho por codigo logo abaixo
    private static Sprite CarregarArte(string nome)
    {
        var sprites = Resources.LoadAll<Sprite>("Caracol/" + nome);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    // Quadradinho da gosma (Resources/Caracol/caracol-gosma). Null se o arquivo nao existir
    private static Sprite ArteGosma
    {
        get
        {
            if (!gosmaProcurada)
            {
                gosmaProcurada = true;
                spriteGosma = CarregarArte("caracol-gosma");
            }
            return spriteGosma;
        }
    }

    // Setinha 8x8 apontando pra direita (a rotacao vira ela pro caracol)
    private static Sprite ArteSeta
    {
        get
        {
            if (spriteSeta != null) return spriteSeta;
            spriteSeta = CarregarArte("caracol-seta");
            if (spriteSeta != null) return spriteSeta;
            const int lado = 8;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Point;
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    bool cheio = x >= 1 && Mathf.Abs(y - 3.5f) <= (7 - x) * 0.55f;
                    textura.SetPixel(x, y, cheio ? Color.white : Color.clear);
                }
            textura.Apply();
            spriteSeta = Sprite.Create(textura, new Rect(0, 0, lado, lado), Vector2.one * .5f, 16);
            spriteSeta.name = "Caracol_Seta";
            return spriteSeta;
        }
    }

    // Caracol 16x16 olhando pra esquerda (Resources/Caracol/caracol)
    private static Sprite Arte
    {
        get
        {
            if (sprite != null) return sprite;
            sprite = CarregarArte("caracol");
            if (sprite != null) return sprite;
            const int lado = 16;
            var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Point;
            Color corpo = new Color(0.62f, 0.72f, 0.55f);
            Color concha = new Color(0.78f, 0.5f, 0.3f);
            Color espiral = new Color(0.5f, 0.28f, 0.16f);
            Color olho = new Color(0.12f, 0.1f, 0.1f);
            for (int y = 0; y < lado; y++)
                for (int x = 0; x < lado; x++)
                {
                    Color pixel = Color.clear;
                    bool base_ = y >= 2 && y <= 4 && x >= 1 && x <= 14;
                    bool cabeca = x >= 1 && x <= 4 && y >= 4 && y <= 7;
                    bool antena = (x == 2 || x == 4) && (y == 8 || y == 9);
                    if (base_ || cabeca || antena) pixel = corpo;
                    if ((x == 2 || x == 4) && y == 10) pixel = olho;

                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(9.5f, 8.5f));
                    if (d < 5.3f) pixel = (d > 2.2f && d < 3.1f) || d < 1.1f ? espiral : concha;
                    textura.SetPixel(x, y, pixel);
                }
            textura.Apply();
            sprite = Sprite.Create(textura, new Rect(0, 0, lado, lado), Vector2.one * .5f, lado);
            sprite.name = "Caracol";
            return sprite;
        }
    }
}

// Gosma do rastro: encolhe e some devagar. Usa Time.deltaTime, entao congela no lugar
// quando o jogo pausa (inclusive no Game Over, que deixa o timeScale em 0)
public class GosmaCaracol : MonoBehaviour
{
    private float vida;
    private float idade;
    private float escala;
    private SpriteRenderer sr;
    private Color corInicial;

    public void Init(float vida, float escala)
    {
        this.vida = Mathf.Max(0.05f, vida);
        this.escala = escala;
        transform.localScale = Vector3.one * escala;
        transform.rotation = Quaternion.Euler(0f, 0f, Rng.EffectsRange(0f, 90f));
        sr = GetComponent<SpriteRenderer>();
        corInicial = sr.color;
    }

    private void Update()
    {
        idade += Time.deltaTime;
        float t = idade / vida;
        transform.localScale = Vector3.one * Mathf.Lerp(escala, 0f, t * t);
        Color c = corInicial;
        c.a = Mathf.Lerp(corInicial.a, 0f, t);
        sr.color = c;
        if (idade >= vida) Destroy(gameObject);
    }
}
