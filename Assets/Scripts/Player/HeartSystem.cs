using UnityEngine;
using UnityEngine.Audio;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    private PlayerStatsHandler stats;
    int life;

    // Variável para controlar se o jogador tem o escudo
    //public bool hasShield = false;

    //Instancia da vida do escudo inicial
    public int shieldHealth = 0;

    private GameOverManager gameOverManager;
    private AudioSource audioSource;

    //[SerializeField] private AudioClip hurtSound;
    // Opcional: Adicione um som para quando o escudo quebrar!
    //[SerializeField] private AudioClip shieldBreakSound;

    private void Start()
    {
        stats = GetComponent<PlayerStatsHandler>();
        life = (int)stats.GetPlayerMaxHearts();       // O jogador inicia com a vida máxima definida no PlayerStatsHandler.cs
        gameOverManager = FindAnyObjectByType<GameOverManager>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Decrementa os containers de vida na UI
        // Nao sei se fui eu que buguei ele mas a contagem de coração não sobe mais de 3 - isso nao impacta o jogo!!!
        
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < life)
                hearts[i].SetActive(true);
            else
                hearts[i].SetActive(false);
        }
    }

    public void updateMaxLife()
    {
        life = (int)stats.GetPlayerMaxHearts();
    }

    public void takeDamage(int damage)
    {
        // PASSO NOVO: Verifica se o jogador tem o escudo ANTES de tirar a vida
        //if (hasShield)
        //{
        //    hasShield = false; // O escudo quebra
        //    Debug.Log("O Escudo absorveu o dano!");

        //    if (shieldBreakSound != null)
        //    {
        //        audioSource.PlayOneShot(shieldBreakSound, 0.5f);
        //    }

        //    // Opcional: Aqui você pode colocar um código para atualizar a UI do Escudo no futuro
        //    return; // O 'return' faz a função parar aqui, protegendo a vida do jogador.
        //}

        if (shieldHealth > 0)
        {
            shieldHealth -= damage;
            Debug.Log("O Escudo absorveu o dano! Resistência restante: " + shieldHealth);

            // consertar audio depois
            // if (shieldBreakSound != null)
            // {
            //     audioSource.PlayOneShot(shieldBreakSound, 0.5f);
            // }
            AudioManager.Instance.PlaySFX("EscudoQuebrou");

            if (shieldHealth < 0)
            {
                // O dano quebrou o escudo e sobrou um pouco. Subtrai a sobra da vida.
                life += shieldHealth; // shieldHealth ficou negativo, então isso subtrai da vida
                shieldHealth = 0;
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
        }

        life = Mathf.Max(life, 0); // No maximo fica com 0 vidas
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);

        if (life <= 0)
        {
            Die();
            AudioManager.Instance.PlaySFX("PlayerMorreu");
            return;
        }

        else
        {
            AudioManager.Instance.PlaySFX("PlayerTomouDano");
        }
    }

    // Função que será chamada pelo Baú para dar o escudo
    public void EquipShield(int shieldAmount)
    {
        shieldHealth = shieldAmount;
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