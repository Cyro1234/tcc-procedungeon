using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    private AudioSource audioSource;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioManager audioManager;

    public void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void ShowGameOver()
    {
        audioManager.PlayGameOverMusic();
        audioSource.PlayOneShot(deathSound, 1f);
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // pausa o jogo
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // despausa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
