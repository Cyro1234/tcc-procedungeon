using UnityEngine;
using UnityEngine.Audio;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    int life = 3;

    private GameOverManager gameOverManager;

    private AudioSource audioSource;

    [SerializeField] private AudioClip hurtSound;

    private void Start()
    {
        gameOverManager = FindFirstObjectByType<GameOverManager>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < life)
                hearts[i].SetActive(true);
            else
                hearts[i].SetActive(false);
        }
    }

    public void takeDamage(int damage) 
    {

        life -= damage;
        life = Mathf.Max(life, 0);
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);
        if (life <= 0)
        {
            Die();
        }
        else
        {
            audioSource.PlayOneShot(hurtSound, 0.3f);
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
