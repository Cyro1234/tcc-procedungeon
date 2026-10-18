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
    [SerializeField] private Color corIndicador = new Color(1f, 0.85f, 0.4f, 0.8f);
    [SerializeField, Min(0.01f)] private float espessura = 0.06f;
    [Tooltip("Quantas voltas a lamina do indicador da durante um golpe.")]
    [SerializeField, Min(0f)] private float voltas = 1f;
    [Tooltip("Opcional. Vazio usa o material padrao de sprites.")]
    [SerializeField] private Material materialIndicador;

    private const int Segmentos = 48;

    private PlayerStatsHandler stats;
    private LineRenderer circulo;
    private LineRenderer lamina;
    private Material materialCriado;
    private ContactFilter2D filtro;
    private readonly List<Collider2D> resultados = new List<Collider2D>();
    private readonly HashSet<EnemyMovement> atingidos = new HashSet<EnemyMovement>();
    private float inicio;
    private float duracao;

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
        lamina = CriarLinha("AxeIndicator_Lamina", 2, false);
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
        inicio = Time.time;
        duracao = Mathf.Max(0.01f, duracaoGolpe);
        atingidos.Clear();
        Girando = true;
        circulo.enabled = true;
        lamina.enabled = true;
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
                enemy.takeDamage(stats.GetPlayerDamage() * multiplicadorDano, gameObject);
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
        float angulo = progresso * voltas * Mathf.PI * 2f;
        Vector3 dir = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo));
        lamina.SetPosition(0, centro + dir * raio * 0.45f);
        lamina.SetPosition(1, centro + dir * raio);

        Color corCirculo = corIndicador;
        corCirculo.a *= 1f - progresso * 0.6f; // vai sumindo ate o fim do golpe
        circulo.startColor = circulo.endColor = corCirculo;
        lamina.startColor = lamina.endColor = corIndicador;
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