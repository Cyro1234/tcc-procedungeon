using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    //[SerializeField] GameObject pauseManager;
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject controlsPanel;

    [SerializeField] private PlayerMovement playerMovement;

    public static bool pausado = false;
    private static int i = 1;

    void Start()
    {
        // Força a variável a resetar toda vez que uma fase iniciar
        pausado = false;

        // Garante que o tempo do jogo comece normal, caso retornar pro menu com o jogo pausado
        Time.timeScale = 1f;

        if (playerMovement == null)
            playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
    }

    // Ao apertar a tecla ESC, o jogo pausa/despausa e exibe/oculta o menu de pausa
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!pausado)
                PausarJogo();
            else
                ResumirJogo();
        }   
    }

    //private void Start() // APENAS PARA TESTAR AS SEEDS. DEIXAR COMENTADO CASO NAO FOR TESTAR
    //{
    //    StartCoroutine(TesteDeSeeds());
    //}


    public void PausarJogo()
    {
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        if (playerMovement != null) playerMovement.ForcarParada();

        Time.timeScale = 0;
        pausado = true;
    }

    public void ResumirJogo()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        if (playerMovement != null) playerMovement.ForcarParada();

        Time.timeScale = 1;
        pausado = false;
    }

    public void AbrirOpcoes()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void AbrirControles()
    {
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }
    public void VoltarParaPause()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
    public void VoltarParaOpcoes()
    {
        controlsPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    
    private void RestartGame()
    {
        Time.timeScale = 1f; // despausa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator TesteDeSeeds() {
        float delay = 0.01f;

        yield return new WaitForSeconds(delay);

        string folderPath =
        Application.persistentDataPath + "/SeedsTests/";

        System.IO.Directory.CreateDirectory(folderPath);

        string path = folderPath + $"Seed_{i}.png";

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log("PRINT SALVO EM: " + path);

        i++;

        yield return new WaitForSeconds(delay);

        RestartGame();
    }
}