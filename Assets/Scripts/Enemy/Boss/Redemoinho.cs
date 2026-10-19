using UnityEngine;

public class Redemoinho : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float waitTime = 2f;
    [Header("Visual e vento")]
    [SerializeField, Min(0f)] private float velocidadeGiro = 540f;
    [SerializeField, Min(.02f)] private float intervaloVento = .06f;

    private Rigidbody2D rb;
    private Transform player;
    private bool avante;
    private bool lancado;
    private float timeAtaque;
    private float proximoVento;
    private Vector2 direction;
    private Transform visual;
    private SpriteRenderer renderVento;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var original = GetComponent<SpriteRenderer>();
        var sprites = Resources.LoadAll<Sprite>("Efeitos/redemoinho");
        if (sprites.Length == 0)
        {
            Debug.LogWarning("Sprite Efeitos/redemoinho nao encontrado.", this);
            return;
        }
        var objeto = new GameObject("Redemoinho - visual giratorio");
        visual = objeto.transform;
        visual.SetParent(transform, false);
        renderVento = objeto.AddComponent<SpriteRenderer>();
        renderVento.sprite = sprites[0];
        if (original != null)
        {
            renderVento.sharedMaterial = original.sharedMaterial;
            renderVento.sortingLayerID = original.sortingLayerID;
            renderVento.sortingOrder = original.sortingOrder;
            // Preserva o tamanho anterior sem mudar o collider ou a velocidade.
            float tamanho = original.sprite != null
                ? Mathf.Max(original.sprite.bounds.size.x, original.sprite.bounds.size.y) : .64f;
            float tamanhoArte = Mathf.Max(sprites[0].bounds.size.x, sprites[0].bounds.size.y);
            visual.localScale = Vector3.one * (tamanho / Mathf.Max(.001f, tamanhoArte));
            original.enabled = false;
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime + waitTime);
    }

    public void Launch(Transform newPlayer)
    {
        player = newPlayer;
        avante = true;
        lancado = false;
        timeAtaque = Time.time + waitTime;
    }

    private void FixedUpdate()
    {
        if (!avante || Time.time < timeAtaque) return;
        // Mira uma vez no lancamento, preservando o comportamento do projetil.
        direction = player != null ? (Vector2)(player.position - transform.position).normalized : Vector2.down;
        rb.linearVelocity = direction * speed;
        avante = false;
        lancado = true;
        EmitirVento(10);
    }

    private void Update()
    {
        if (!GameFlow.CanPlay || Time.deltaTime <= 0f || !lancado) return;
        // So a arte gira; o collider e a trajetoria continuam estaveis.
        if (visual != null) visual.Rotate(0, 0, -velocidadeGiro * Time.deltaTime, Space.Self);
        if (Time.time < proximoVento) return;
        EmitirVento(3);
        proximoVento = Time.time + intervaloVento;
    }

    private void EmitirVento(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            Vector2 radial = Rng.EffectsInsideUnitCircle();
            Vector2 tangenteHoraria = new Vector2(radial.y, -radial.x);
            var objeto = new GameObject("Vento - quadradinho");
            objeto.transform.position = transform.position + (Vector3)(radial * .6f);
            var sprite = objeto.AddComponent<SpriteRenderer>();
            sprite.sprite = GameEffects.WhiteSprite;
            float tom = Rng.EffectsRange(.7f, .95f);
            sprite.color = new Color(tom * .9f, tom * .96f, tom, .8f);
            // Acima do piso, como a fumaca de invocacao.
            sprite.sortingLayerName = "UI";
            sprite.sortingOrder = 390;
            if (renderVento != null) sprite.sharedMaterial = renderVento.sharedMaterial;
            objeto.AddComponent<FxParticle>().Init(
                tangenteHoraria * Rng.EffectsRange(1f, 2.5f) - direction * .8f,
                Rng.EffectsRange(.25f, .55f), Rng.EffectsRange(.6f, 1.5f));
            objeto.transform.rotation = Quaternion.identity;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boss")) return;
        if (collision.CompareTag("Bullet")) return;
        if (collision.CompareTag("Melee")) return;
        if (collision.CompareTag("Ladder")) return;
        if (lancado) EmitirVento(8);
        Destroy(gameObject);
    }
}
