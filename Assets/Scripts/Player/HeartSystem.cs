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
    private int MaxLife => stats != null ? (int)stats.GetPlayerMaxHearts() : 3;

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
    }

    void Update()
    {
        // Se um downgrade diminuiu a vida maxima, a vida atual acompanha
        if (!dead && life > MaxLife) life = MaxLife;

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
        life = (int)stats.GetPlayerMaxHearts();
    }

    // Usado pelos coracoes que os inimigos soltam (HeartPickup)
    public void Heal(int amount)
    {
        if (dead || amount <= 0) return;
        life = Mathf.Min(life + amount, MaxLife);
        GameEffects.Instance.PlayerHealed(transform.position, amount);
    }

    public void takeDamage(int damage)
    {
        takeDamage(damage, null);
    }

    // source: quem causou o dano (usado pra empurrar o jogador na direcao contraria)
    public void takeDamage(int damage, Transform source)
    {
        if (IsInvulnerable) return;

        invincibleUntil = Time.time + invincibilityTime;
        ApplyKnockback(source);

        if (shieldHealth > 0)
        {
            shieldHealth -= damage;
            Debug.Log("O Escudo absorveu o dano! Resistência restante: " + shieldHealth);

            AudioManager.Instance.PlaySFX("EscudoQuebrou");
            GameEffects.Instance.PlayerShieldHit(transform.position);

            if (shieldHealth < 0)
            {
                // O dano quebrou o escudo e sobrou um pouco. Subtrai a sobra da vida.
                life += shieldHealth; // shieldHealth ficou negativo, então isso subtrai da vida
                shieldHealth = 0;
                Escudo.SetActive(false);
                GameEffects.Instance.PlayerHurt(transform.position);
                Debug.Log("O escudo quebrou e o jogador sofreu o impacto!");
            }
            else
            {
                return; // O escudo aguentou todo o impacto. Fim da função.
            }
        }
        else
        {
            // Se não tinha escudo, tira da vida normalmente
            life -= damage;
            GameEffects.Instance.PlayerHurt(transform.position);
        }

        life = Mathf.Max(life, 0); // No maximo fica com 0 vidas
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);

        if (life <= 0)
        {
            dead = true;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            Die();
            AudioManager.Instance.PlaySFX("PlayerMorreu");
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
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitCircle;
        movement.OverrideVelocity(dir.normalized * knockbackForce, 0.15f, true);
    }

    // Função que será chamada pelo Baú para dar o escudo
    public void EquipShield(int shieldAmount)
    {
        shieldHealth = shieldAmount;
        Escudo.SetActive(true);
        Debug.Log($"Escudo Equipado! Proteção total: {shieldHealth} de dano.");
        //hasShield = true;
        //Debug.Log("Escudo Equipado! Você tem uma vida extra.");
        // Opcional: Atualizar a UI para mostrar o icone do escudo na tela
    }

    private void Die()
    {
        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
        }
    }
}
