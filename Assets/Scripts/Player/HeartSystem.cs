using UnityEngine;
using UnityEngine.Audio;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    private PlayerStatsHandler stats;
    int life;

    private GameOverManager gameOverManager;

    private AudioSource audioSource;

    [SerializeField] private AudioClip hurtSound;

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
        life -= damage;

        life = Mathf.Max(life, 0); // No maximo fica com 0 vidas
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);
        if (life <= 0)
        {
            Die();
        }
        else
        {
            audioSource.PlayOneShot(hurtSound, 0.3f); // TODO: tirar valores hardcoded de volume em todos os campos de volume
        }
    }

    private void Die()
    {

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
        }
    }
}
