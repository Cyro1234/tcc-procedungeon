using System.Collections.Generic;
using UnityEngine;

// Golpe do machado: o jogador gira e acerta tudo num circulo ao redor dele.
// Enquanto o golpe dura, um circulo e uma "lamina" girando mostram a area atingida.
// O Attack.cs chama Comecar() e Parar(), esse script nao le input nenhum.
public class AxeSpin : MonoBehaviour
{
    [Header("Golpe")]
    [Tooltip("Raio do circulo de dano, em unidades (1 tile = 1).")]
    [SerializeField, Min(0.1f)] private float raio = 1.75f;
    [Tooltip("Multiplica o dano do jogador (ja com pocoes e debuffs). 1 = mesmo dano da espada, 2 = dobro.")]
    [SerializeField, Min(0f)] private float multiplicadorDano = 2f;
    [Tooltip("Marque se algum inimigo nao estiver levando dano (inimigos com collider marcado como Is Trigger).")]
    [SerializeField] private bool incluirTriggers = false;

    [Header("Indicador")]
    [Tooltip("Ajuste da orientacao da arte do machado durante o giro, em graus.")]
    [SerializeField] private float anguloSprite = 45f;
    [SerializeField] private Color corIndicador = new Color(1f, 0.85f, 0.4f, 0.8f);
    [SerializeField, Min(0.01f)] private float espessura = 0.06f;
    [Tooltip("Quantas voltas a lamina do indicador da durante um golpe.")]
    [SerializeField, Min(0f)] private float voltas = 1f;
    [Tooltip("Opcional. Vazio usa o material padrao de sprites.")]
    [SerializeField] private Material materialIndicador;

    private const int Segmentos = 48;

    private PlayerStatsHandler stats;
    private LineRenderer circulo;
    private SpriteRenderer lamina;
    private Material materialCriado;
    private ContactFilter2D filtro;
    private readonly List<Collider2D> resultados = new List<Collider2D>();
    private readonly HashSet<EnemyMovement> atingidos = new HashSet<EnemyMovement>();
    private float inicio;
    private float duracao;
    private float bonusArma = 1f;

    public bool Girando { get; private set; }

    private void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();

        if (materialIndicador == null)
        {
            materialCriado = new Material(Shader.Find("Sprites/Default"));
            materialIndicador = materialCriado;
        }
        circulo = CriarLinha("AxeIndicator_Circulo", Segmentos, true);
        var objetoLamina = new GameObject("Machado girando");
        objetoLamina.transform.SetParent(transform, false);
        lamina = objetoLamina.AddComponent<SpriteRenderer>();
        lamina.sprite = ItemSprites.Obter(Chest.ItemType.Axe);
        var jogador = GetComponent<SpriteRenderer>();
        if (jogador != null)
        {
            lamina.sortingLayerID = jogador.sortingLayerID;
            lamina.sortingOrder = jogador.sortingOrder + 2;
            lamina.sharedMaterial = jogador.sharedMaterial;
        }
        EsconderIndicador();
    }

    private void OnDestroy()
    {
        if (materialCriado != null) Destroy(materialCriado);
    }

    private void OnDisable() => Parar();

    // Chamado pelo Attack quando o golpe comeca
    public void Comecar(float duracaoGolpe)
    {
        bonusArma = InventoryManager.Instance != null
            ? InventoryManager.Instance.ObterMultiplicadorDano(Chest.ItemType.Axe) : 1f;
        inicio = Time.time;
        duracao = Mathf.Max(0.01f, duracaoGolpe);
        atingidos.Clear();
        Girando = true;
        circulo.enabled = true;
        lamina.enabled = lamina.sprite != null;
        Desenhar(0f);
        AcertarInimigos();
    }

    // Chamado pelo Attack quando o golpe termina ou e cancelado (pause, menu, etc)
    public void Parar()
    {
        Girando = false;
        EsconderIndicador();
    }

    private void Update()
    {
        if (Girando) Desenhar(Mathf.Clamp01((Time.time - inicio) / duracao));
    }

    private void FixedUpdate()
    {
        if (Girando && GameFlow.CanPlay) AcertarInimigos();
    }

    private void AcertarInimigos()
    {
        filtro.NoFilter();
        filtro.useTriggers = incluirTriggers;
        Physics2D.OverlapCircle(transform.position, raio, filtro, resultados);

        foreach (Collider2D col in resultados)
        {
            if (col.CompareTag("Bullet")) continue;
            EnemyMovement enemy = col.GetComponentInParent<EnemyMovement>();
            // Varios colliders do mesmo inimigo nao contam como golpes extras.
            if (enemy != null && atingidos.Add(enemy))
            {
                enemy.takeDamage(stats.GetPlayerDamage() * multiplicadorDano * bonusArma, gameObject);
            }
        }
    }

    // Em espaco de mundo para o desenho bater exatamente com o OverlapCircle, mesmo se o Player tiver escala.
    private void Desenhar(float progresso)
    {
        Vector3 centro = transform.position;
        for (int i = 0; i < Segmentos; i++)
        {
            float a = i * Mathf.PI * 2f / Segmentos;
            circulo.SetPosition(i, centro + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * raio);
        }

        // A lamina comeca longe do centro pra nao cobrir o sprite do jogador
        float angulo = -progresso * voltas * Mathf.PI * 2f;
        Vector3 dir = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo));
        if (lamina.sprite != null)
        {
            float comprimento = raio * .65f;
            float escala = comprimento / Mathf.Max(.001f, lamina.sprite.bounds.size.y);
            Vector3 escalaPai = transform.lossyScale;
            lamina.transform.localScale = new Vector3(
                escala / Mathf.Max(.001f, Mathf.Abs(escalaPai.x)),
                escala / Mathf.Max(.001f, Mathf.Abs(escalaPai.y)), 1f);
            Quaternion rotacao = Quaternion.Euler(0, 0, angulo * Mathf.Rad2Deg - 90f + anguloSprite);
            lamina.transform.rotation = rotacao;
            // Centraliza a arte independentemente do pivot importado e alinha a ponta ao alcance.
            lamina.transform.position = centro + dir * (raio - comprimento * .5f)
                - rotacao * (lamina.sprite.bounds.center * escala);
        }

        Color corCirculo = corIndicador;
        corCirculo.a *= 1f - progresso * 0.6f; // vai sumindo ate o fim do golpe
        circulo.startColor = circulo.endColor = corCirculo;
        lamina.color = Color.white;
    }

    private void EsconderIndicador()
    {
        if (circulo != null) circulo.enabled = false;
        if (lamina != null) lamina.enabled = false;
    }

    private LineRenderer CriarLinha(string nome, int pontos, bool fechada)
    {
        var go = new GameObject(nome);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = fechada;
        lr.positionCount = pontos;
        lr.widthMultiplier = espessura;
        lr.numCapVertices = 2;
        lr.sharedMaterial = materialIndicador;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            lr.sortingLayerID = sr.sortingLayerID;
            lr.sortingOrder = sr.sortingOrder + 1;
        }
        return lr;
    }

    // Mostra o raio na Scene view com o Player selecionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = corIndicador;
        Gizmos.DrawWireSphere(transform.position, raio);
    }
}
