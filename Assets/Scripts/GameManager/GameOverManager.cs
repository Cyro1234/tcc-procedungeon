using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PauseManager pauseManager;

    public void Start()
    {

    }

    // Mostra a tela de game over e pausa o jogo
    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // pausa o jogo

        playerInput.enabled = false;
        playerMovement.enabled = false;
        pauseManager.enabled = false;
        AudioManager.Instance.PlayMusic("GameOver", false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // despausa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RetornarMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
