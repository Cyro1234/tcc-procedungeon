using UnityEngine;
using UnityEngine.Audio;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    public GameObject Escudo;
    private PlayerStatsHandler stats;
    int life;

    // Variável para controlar se o jogador tem o escudo
    //public bool hasShield = false;

    //Instancia da vida do escudo inicial
    public int shieldHealth = 0;

    [Header("Game effects")]
    [Tooltip("Tempo de invencibilidade depois de tomar dano (o jogador pisca nesse periodo).")]
    [SerializeField] private float invincibilityTime = 0.9f;
    [Tooltip("Forca do empurrao que o jogador leva ao tomar dano.")]
    [SerializeField] private float knockbackForce = 12f;

    private GameOverManager gameOverManager;
    private AudioSource audioSource;
    private PlayerMovement movement;
    private SpriteRenderer spriteRenderer;

    private float invincibleUntil;
    private bool dead;

    public bool IsDead => dead;
    public bool IsFullHealth => life >= MaxLife;
    public int Life => life;
    public bool IsInvulnerable => dead || Time.time < invincibleUntil;
    private int MaxLife => stats != null ? Mathf.Max(0, (int)stats.GetPlayerMaxHearts()) : 3;

    //[SerializeField] private AudioClip hurtSound;
    // Opcional: Adicione um som para quando o escudo quebrar!
    //[SerializeField] private AudioClip shieldBreakSound;

    private void Start()
    {
        stats = GetComponent<PlayerStatsHandler>();
        life = (int)stats.GetPlayerMaxHearts();       // O jogador inicia com a vida máxima definida no PlayerStatsHandler.cs
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        audioSource = GetComponent<AudioSource>();
        movement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        AtualizarVidaMaxima();
    }

    void Update()
    {
        AtualizarVidaMaxima();

        // Decrementa os containers de vida na UI
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < life)
                hearts[i].SetActive(true);
            else
                hearts[i].SetActive(false);
        }

        // Pisca enquanto estiver invencivel depois de levar dano
        if (spriteRenderer != null && !dead)
        {
            bool blinking = Time.time < invincibleUntil;
            spriteRenderer.enabled = !blinking || Mathf.Repeat(Time.time * 14f, 1f) > 0.5f;
        }
    }

    public void updateMaxLife()
    {
        if (dead) return;
        life = MaxLife;
        AtualizarVidaMaxima();
    }

    // Reduzir a vida maxima a zero encerra a partida, mesmo com escudo
    // ou invencibilidade. Reducoes nao letais nao recuperam vida perdida.
    public void AtualizarVidaMaxima()
    {
        if (dead) return;
        life = Mathf.Clamp(life, 0, MaxLife);
        if (life == 0) Die();
    }

    // Usado pelos coracoes que os inimigos soltam (HeartPickup)
    public void Heal(int amount)
    {
        AtualizarVidaMaxima();
        if (dead || amount <= 0) return;
        life = Mathf.Min(life + amount, MaxLife);
        GameEffects.Instance.PlayerHealed(transform.position, amount);
    }

    public void RestaurarVidaInteira()
    {
        Heal(Mathf.Max(0, MaxLife - life));
    }

    public void takeDamage(int damage)
    {
        takeDamage(damage, null);
    }

    // source: quem causou o dano (usado pra empurrar o jogador na direcao contraria)
    public void takeDamage(int damage, Transform source)
    {
        AtualizarVidaMaxima();
        if (damage <= 0 || IsInvulnerable) return;

        invincibleUntil = Time.time + invincibilityTime;
        ApplyKnockback(source);

        if (shieldHealth > 0)
        {
            int absorbed = Mathf.Min(shieldHealth, damage);
            shieldHealth -= absorbed;
            damage -= absorbed;
            Debug.Log("O Escudo absorveu o dano! Resistência restante: " + shieldHealth);

            GameEffects.Instance.PlayerShieldHit(transform.position);

            if (shieldHealth == 0)
            {
                if (Escudo != null) Escudo.SetActive(false);
                AudioManager.Instance.PlaySFX("EscudoQuebrou");
            }

            if (damage == 0) return; // O escudo absorveu o impacto inteiro, mesmo se quebrou.
        }

        // Apenas o dano que nao foi absorvido chega aos coracoes.
        life = Mathf.Max(life - damage, 0);
        GameEffects.Instance.PlayerHurt(transform.position);
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);

        if (life <= 0)
        {
            Die();
            return;
        }

        else
        {
            AudioManager.Instance.PlaySFX("PlayerTomouDano");
        }
    }

    private void ApplyKnockback(Transform source)
    {
        if (source == null || movement == null) return;

        Vector2 dir = (Vector2)(transform.position - source.position);
        if (dir.sqrMagnitude < 0.0001f) dir = Rng.CombatInsideUnitCircle();
        movement.OverrideVelocity(dir.normalized * knockbackForce, 0.15f, true);
    }

    // Função que será chamada pelo Baú para dar o escudo
    public void EquipShield(int shieldAmount)
    {
        shieldHealth = Mathf.Max(0, shieldAmount);
        if (Escudo != null) Escudo.SetActive(shieldHealth > 0);
        Debug.Log($"Escudo Equipado! Proteção total: {shieldHealth} de dano.");
        //hasShield = true;
        //Debug.Log("Escudo Equipado! Você tem uma vida extra.");
        // Opcional: Atualizar a UI para mostrar o icone do escudo na tela
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        life = 0;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver(transform);
        }
        AudioManager.Instance.PlaySFX("PlayerMorreu");
    }
}
