using UnityEngine;
using UnityEngine.InputSystem;

// Canhao: dispara uma bala em linha reta pra onde o jogador esta olhando.
// Dano alto, mas cada tiro empurra o jogador pra tras. A lentidao de segurar o canhao
// fica no PesoDasArmas, junto com a das outras armas pesadas.
// O Attack.cs chama Disparar(); o analogico direito define a mira.
public class Cannon : MonoBehaviour
{
    [Header("Tiro")]
    [Tooltip("Multiplica o dano do jogador (ja com pocoes e debuffs). Espadas e adaga = 1; o canhao usa o mesmo do machado (2).")]
    [SerializeField, Min(0f)] private float multiplicadorDano = 4f;
    [SerializeField, Min(0.1f)] private float velocidadeBala = 16f;
    [Tooltip("A bala voa ate sair da tela. Isto e so um limite de seguranca, em segundos.")]
    [SerializeField, Min(0.1f)] private float tempoMaximoBala = 6f;
    [SerializeField, Min(0.05f)] private float raioBala = 0.25f;

    [Header("Desvantagens")]
    [Tooltip("Empurrao pra tras a cada tiro. O debuff de Recuo usa 2.2.")]
    [SerializeField, Min(0f)] private float forcaRecuo = 3f;

    private PlayerStatsHandler stats;
    private PlayerMovement movement;
    private SpriteRenderer spriteJogador;

    [Header("Mira")]
    [SerializeField, Min(0.1f)] private float alcanceMira = 7f;
    [SerializeField, Range(0.05f, 0.9f)] private float deadzoneMira = 0.2f;
    [SerializeField] private Color corMira = new Color(1f, 0.12f, 0.12f, 1f);

    private Vector2 direcaoMira = Vector2.down;
    private bool mirandoComAnalogico;
    private Transform guiaMira;
    private Sprite spriteTraco;
    private const int QuantidadeTracos = 18;
    private readonly Transform[] tracos = new Transform[QuantidadeTracos];

    private static Sprite icone;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => icone = null;

    private void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
        movement = GetComponent<PlayerMovement>();
        spriteJogador = GetComponent<SpriteRenderer>();
    }

    private bool EstaEquipado => InventoryManager.Instance != null &&
        InventoryManager.Instance.ObterItemSelecionado() == Chest.ItemType.Cannon;

    public bool TentarObterDirecaoMira(out Vector2 direcao)
    {
        if (!EstaEquipado)
        {
            mirandoComAnalogico = false;
            direcao = Vector2.zero;
            return false;
        }
        direcao = AtualizarDirecaoMira();
        return true;
    }

    private Vector2 AtualizarDirecaoMira()
    {
        Gamepad controle = Gamepad.current;
        Vector2 analogico = controle != null ? controle.rightStick.ReadValue() : Vector2.zero;
        if (analogico.sqrMagnitude > deadzoneMira * deadzoneMira)
        {
            direcaoMira = analogico.normalized;
            mirandoComAnalogico = true;
        }
        else if (!mirandoComAnalogico || controle == null)
        {
            direcaoMira = movement != null ? movement.FacingDirection : Vector2.down;
            mirandoComAnalogico = false;
        }
        return direcaoMira;
    }

    private void LateUpdate()
    {
        bool equipado = EstaEquipado;
        if (!equipado) mirandoComAnalogico = false;
        bool visivel = equipado && GameFlow.CanPlay && Time.timeScale > 0f &&
            movement != null && movement.enabled;
        if (guiaMira != null) guiaMira.gameObject.SetActive(visivel);
        if (!visivel) return;

        Vector2 direcao = AtualizarDirecaoMira();
        if (guiaMira == null) CriarGuiaMira();
        Vector2 boca = (Vector2)transform.position + direcao * 0.6f;
        Quaternion rotacao = Quaternion.Euler(0f, 0f, Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg);
        float passo = alcanceMira / QuantidadeTracos;
        for (int i = 0; i < tracos.Length; i++)
        {
            tracos[i].SetPositionAndRotation(boca + direcao * (passo * (i + 0.3f)), rotacao);
            // Compensa a escala do jogador para manter tracos finos no mundo.
            Vector3 escala = guiaMira.lossyScale;
            tracos[i].localScale = new Vector3(
                passo * 0.4f / Mathf.Max(Mathf.Abs(escala.x), 0.0001f),
                0.045f / Mathf.Max(Mathf.Abs(escala.y), 0.0001f), 1f);
        }
    }

    private void CriarGuiaMira()
    {
        guiaMira = new GameObject("Mira tracejada").transform;
        guiaMira.SetParent(transform, false);
        spriteTraco = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            Vector2.one * 0.5f, Texture2D.whiteTexture.width);
        for (int i = 0; i < tracos.Length; i++)
        {
            var traco = new GameObject("Traco " + i);
            tracos[i] = traco.transform;
            tracos[i].SetParent(guiaMira, false);
            var render = traco.AddComponent<SpriteRenderer>();
            render.sprite = spriteTraco;
            render.color = corMira;
            if (spriteJogador != null)
            {
                render.sortingLayerID = spriteJogador.sortingLayerID;
                render.sortingOrder = spriteJogador.sortingOrder + 4;
            }
        }
    }

    private void OnDisable()
    {
        if (guiaMira != null) guiaMira.gameObject.SetActive(false);
        mirandoComAnalogico = false;
    }

    private void OnDestroy()
    {
        if (spriteTraco != null) Destroy(spriteTraco);
    }

    // Chamado pelo Attack quando o jogador ataca com o canhao na mao
    public void Disparar()
    {
        Vector2 direcao = AtualizarDirecaoMira();
        if (direcao.sqrMagnitude < 0.0001f) direcao = Vector2.down;
        direcao.Normalize();

        float bonusArma = InventoryManager.Instance != null
            ? InventoryManager.Instance.ObterMultiplicadorDano(Chest.ItemType.Cannon) : 1f;
        float dano = stats.GetPlayerDamage() * multiplicadorDano * bonusArma;

        // A bala nasce na frente do jogador pra nao sair de dentro do sprite
        Vector2 boca = (Vector2)transform.position + direcao * 0.6f;
        CannonBall.Spawn(boca, direcao * velocidadeBala, tempoMaximoBala, raioBala, dano, gameObject, spriteJogador);

        // Passa pelo pipeline de movimento, entao combina com os debuffs igual ao Recuo
        if (movement != null) movement.AddPush(-direcao * forcaRecuo);

        GameEffects.Instance.CannonFired(boca);
    }

    // Arte em Assets/CustomAssets/Resources/Armas (cannon.ase, cannonball.ase).
    // Sem o arquivo, cai no desenho por codigo
    public static Sprite CarregarArte(string nome)
    {
        var sprites = Resources.LoadAll<Sprite>("Armas/" + nome);
        return sprites.Length > 0 ? sprites[0] : null;
    }

    // Icone do canhao (HUD, bau, inventario)
    public static Sprite Icone
    {
        get
        {
            if (icone != null) return icone;
            icone = CarregarArte("cannon");
            if (icone != null) return icone;
            var textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Point;
            Color ferro = new Color(.32f, .34f, .40f);
            Color brilho = new Color(.55f, .58f, .66f);
            Color madeira = new Color(.55f, .36f, .20f);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    Color pixel = Color.clear;
                    bool cano = x >= 7 && x <= 25 && y >= 14 && y <= 21;
                    bool boca = x >= 24 && x <= 28 && y >= 12 && y <= 23;
                    bool traseira = Vector2.Distance(new Vector2(x, y), new Vector2(7, 17.5f)) < 4.5f;
                    float distRoda = Vector2.Distance(new Vector2(x, y), new Vector2(12, 10));
                    if (cano || boca || traseira) pixel = y >= 20 ? brilho : ferro;
                    if (distRoda < 6.5f) pixel = distRoda < 2f ? ferro : madeira;
                    textura.SetPixel(x, y, pixel);
                }
            textura.Apply();
            icone = Sprite.Create(textura, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
            icone.name = "Cannon_Icone";
            return icone;
        }
    }
}

// Bala do canhao: anda em linha reta, acerta o primeiro inimigo e explode.
// Paredes e outros colliders solidos tambem fazem ela explodir; triggers (salas, itens, golpes) sao ignorados.
public class CannonBall : MonoBehaviour
{
    private float dano;
    private GameObject dono;
    private bool explodiu;

    private static Sprite disco;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => disco = null;

    public static void Spawn(Vector2 posicao, Vector2 velocidade, float vida, float raio, float dano,
        GameObject dono, SpriteRenderer referencia)
    {
        var go = new GameObject("Bala de canhao");
        go.transform.position = posicao;
        go.transform.localScale = Vector3.one * raio * 2f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Disco;
        if (referencia != null)
        {
            // Mesma camada e material do jogador, assim a bala recebe a mesma iluminacao
            sr.sortingLayerID = referencia.sortingLayerID;
            sr.sortingOrder = referencia.sortingOrder + 5;
            sr.sharedMaterial = referencia.sharedMaterial;
        }

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearVelocity = velocidade;

        var bala = go.AddComponent<CannonBall>();
        bala.dano = dano;
        bala.dono = dono;
        Destroy(go, vida);
    }

    // Cruza a tela inteira: so some quando sai da camera (sem efeito de explosao)
    private void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        const float margem = 0.05f;
        if (vp.x < -margem || vp.x > 1f + margem || vp.y < -margem || vp.y > 1f + margem)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (explodiu) return;
        if (dono != null && collision.transform.IsChildOf(dono.transform)) return;

        EnemyMovement inimigo = collision.GetComponentInParent<EnemyMovement>();
        if (inimigo != null)
        {
            inimigo.takeDamage(dano, gameObject);
            Explodir();
            return;
        }

        if (collision.isTrigger) return;
        Explodir(); // bateu em parede ou obstaculo
    }

    private void Explodir()
    {
        explodiu = true;
        GameEffects.Instance.CannonImpact(transform.position);
        Destroy(gameObject);
    }

    private static Sprite Disco
    {
        get
        {
            if (disco != null) return disco;
            // O tamanho da bala vem de raioBala: a arte precisa ter 1 unidade de largura (16 px a 16 PPU)
            disco = Cannon.CarregarArte("cannonball");
            if (disco != null) return disco;
            var textura = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            textura.filterMode = FilterMode.Point;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    // Pontinho claro em cima pra parecer uma bola de ferro
                    bool reflexo = Vector2.Distance(new Vector2(x, y), new Vector2(5f, 10.5f)) < 1.6f;
                    Color ferro = reflexo ? new Color(.7f, .7f, .75f) : new Color(.18f, .18f, .22f);
                    textura.SetPixel(x, y, d < 7.5f ? ferro : Color.clear);
                }
            textura.Apply();
            disco = Sprite.Create(textura, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
            disco.name = "CannonBall_Disco";
            return disco;
        }
    }
}
