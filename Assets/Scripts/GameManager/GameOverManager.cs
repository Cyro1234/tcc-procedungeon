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
        GameFlow.Pause(GameFlow.State.GameOver);
        gameOverPanel.SetActive(true);

        if (playerMovement != null) playerMovement.ForcarParada();

        playerInput.enabled = false;
        playerMovement.enabled = false;
        pauseManager.enabled = false;
        AudioManager.Instance.PlayMusic("GameOver", false);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RetornarMenu()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
