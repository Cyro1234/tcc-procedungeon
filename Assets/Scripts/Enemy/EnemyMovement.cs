using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    // Bosses como o Coco controlam a propria velocidade durante seus ataques.
    public bool ExternalMovement { get; set; }

    public float moveSpeed = 2f;
    Rigidbody2D rb;
    Transform target;
    Vector2 moveDirection;
    private Animator animator;

    [SerializeField] int viewDistance = 12;

    [SerializeField] float maxHealth = 3f;
    private float health;

    private bool isKnockedBack = false;

    [SerializeField] float strength = 4f;
    [SerializeField] float knockbackDuration = 0.15f;

    [SerializeField] private GameObject healthBarPrefab = null;
    private HealthBar healthBar;
    private bool hpVisivel = false;
    private GameObject barInstance;

    [Header("Game effects")]
    [Tooltip("Tempo que o inimigo 'leva um susto' ao ver o jogador pela primeira vez antes de comecar a perseguir.")]
    [SerializeField] private float reactionTime = 0.35f;
    [Tooltip("Chance (0-1) de soltar um coracao ao morrer.")]
    [SerializeField] private float heartDropChance = 0.1f;
    [Tooltip("Chance (0-1) de soltar um coracao ao morrer quando o jogador esta com 1 de vida.")]
    [SerializeField] private float heartDropChanceLowHealth = 0.3f;

    private bool noticedPlayer = false;
    private float chaseStartTime = 0f;
    private bool dead = false;
    private System.Random lootRng;

    public void InitializeRandom()
    {
        lootRng = Rng.NewCombatRandom();
    }

    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Vector3 baseScale;
    private Coroutine flashRoutine;

    private bool IsBoss => CompareTag("Boss");

    private void Awake()
    {
        if (healthBarPrefab != null)
        {
            barInstance = Instantiate(healthBarPrefab);
            barInstance.SetActive(false);
            healthBar = barInstance.GetComponentInChildren<HealthBar>();
        }
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDestroy()
    {
        // A barra fica fora da hierarquia do inimigo e deve acompanhar qualquer destruicao dele.
        if (barInstance != null) Destroy(barInstance);
    }

    void Start()
    {
        if (lootRng == null) InitializeRandom();
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null) target = playerObj.transform;
        animator = GetComponent<Animator>();
        health = maxHealth;

        if (IsBoss && healthBar != null) { healthBar.SetMaxHealth(health); }

        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        baseScale = transform.localScale;
    }

    void Update() // Calcula pra onde o inimigo deve andar
    {
        if (target) // Se tiver um jogador para seguir
        {
            if (Vector3.Distance(target.position, transform.position) < viewDistance) // Viu o jogador
            {
                if (IsBoss && hpVisivel == false && barInstance != null)
                {
                    hpVisivel = true;
                    barInstance.SetActive(true);
                }

                // Primeira vez que viu o jogador: ele faz o coiso do metal gear e uma pequena pausa antes de ir no jogador
                if (!noticedPlayer)
                {
                    noticedPlayer = true;
                    chaseStartTime = Time.time + (IsBoss ? 0f : reactionTime);
                    if (!IsBoss) GameEffects.Instance.EnemyNoticed(transform.position);
                }

                // Mover em direcao ao jogador
                Vector3 direction = (target.position - transform.position).normalized;
                moveDirection = Time.time >= chaseStartTime ? (Vector2)direction : Vector2.zero;

                // Se o inimigo tiver um animator, atualiza os parametros de animacao.
                if (animator != null && !ExternalMovement)
                {

                    // Animacao walk
                    animator.SetBool("isWalking", true);
                    animator.SetFloat("InputX", direction.x);
                    animator.SetFloat("InputY", direction.y);
                }
            }
            else
            {   // Fica parado
                moveDirection = Vector2.zero;

                // Se o inimigo tiver um animator, atualiza os parametros de animacao.
                if (animator != null && !ExternalMovement)
                {
                    animator.SetBool("isWalking", false);
                    animator.SetFloat("LastInputX", moveDirection.x);
                    animator.SetFloat("LastInputY", moveDirection.y);
                }
            }

            // Roda o inimigo. NAO UTILIZADO NESSES SPRITES
            // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // rb.rotation = angle;

        }
    }

    private void FixedUpdate() // Move o inimigo para o local calculado em Update() guardado na variavel moveDirection
    {
        if (isKnockedBack || ExternalMovement) return; // Preserva knockback e movimento controlado pelo boss.
        if (target)
        {
            rb.linearVelocity = new Vector2(moveDirection.x, moveDirection.y) * moveSpeed;
        }
    }

    public void takeDamage(float damage, GameObject sender)
    {
        if (dead || !GameFlow.CanPlay) return;

        health -= damage;
        StartCoroutine(Knockback(sender));
        AudioManager.Instance.PlaySFX("InimigoTomouDano");

        // Se o inimigo levar dano, ele ja persegue o jogador pra nao ficar travado
        noticedPlayer = true;
        chaseStartTime = Mathf.Min(chaseStartTime, Time.time + knockbackDuration);

        if (healthBar != null)
        {
            healthBar.SetHealth(health);
        }

        SpriteRenderer mainRenderer = renderers != null && renderers.Length > 0 ? renderers[0] : null;

        if (health <= 0)
        {
            dead = true;
            bool boss = IsBoss;
            RunStats.AddKill(boss);
            GameEffects.Instance.EnemyKilled(transform.position, boss, mainRenderer);
            DropLoot(boss);

            if (boss)
            {
                GameManager.setBossMorreu(true);
                GetComponentInParent<FinalBossSequence>()?.BossDefeated();
            }
            Destroy(gameObject);
            return;
        }

        GameEffects.Instance.EnemyHit(transform.position, mainRenderer);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
    }

    private void DropLoot(bool boss)
    {
        float chance = heartDropChance;
        HeartSystem hs = target != null ? target.GetComponent<HeartSystem>() : null;
        if (hs != null && hs.Life <= 1) chance = heartDropChanceLowHealth;

        // Chefe sempre solta um coracao
        if (boss || lootRng.NextDouble() < chance)
        {
            HeartPickup.Spawn(transform.position);
        }
    }

    // Pisca em vermelho/branco e da uma squishada ao tomar dano
    private IEnumerator HitFlash()
    {
        const float duration = 0.12f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;

            Color flash = Color.Lerp(new Color(1f, 0.25f, 0.25f, 1f), Color.white, k);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = baseColors[i] * flash;
                c.a = baseColors[i].a;
                renderers[i].color = c;
            }

            float squash = Mathf.Sin(k * Mathf.PI) * 0.2f;
            transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = baseColors[i];
        }
        transform.localScale = baseScale;
        flashRoutine = null;
    }

    private IEnumerator Knockback(GameObject sender)
    {
        if (ExternalMovement) yield break;
        isKnockedBack = true;

        rb.linearVelocity = Vector2.zero;

        Vector2 direction = ((Vector2)transform.position - (Vector2)sender.transform.position).normalized; // Calcula pra onde fazer o knockback dependendo da posicao do dano
        rb.AddForce(direction * strength, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration); // Espera alguns frames para que tenha o efeito de knockback, impedindo tomar mais de 1 knockback com o mesmo dano

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }

    public void ApplyModularStats(float speedFlat, float speedMult, float healthFlat, float healthMult, float attackFlat, float attackMult)
    {
        // Aplica os bonus de velocidade
        moveSpeed = (moveSpeed + speedFlat) * speedMult;

        // Aplica os bonus de vida
        maxHealth = (maxHealth + healthFlat) * healthMult;
        health = maxHealth; // Garante que o inimigo nasça com a vida cheia ajustada

        // Aplica os bonus de ataque
        strength = (strength + attackFlat) * attackMult;
    }
}
