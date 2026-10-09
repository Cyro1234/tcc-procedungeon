using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    //[SerializeField] GameObject pauseManager;
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject controlsPanel;

    [SerializeField] private PlayerMovement playerMovement;

    public static bool pausado = false;
    private static int i = 1;
    private InputSystem_Actions controles;

    private void OnEnable()
    {
        controles = new InputSystem_Actions();
        InputAction pausa = controles.FindAction("Player/Pause", true);
        pausa.performed += AoPausar;
        pausa.Enable();
        controles.UI.Cancel.performed += AoVoltar;
        controles.UI.Cancel.Enable();
    }

    private void OnDisable()
    {
        controles?.Disable();
        controles?.Dispose();
        controles = null;
    }

    void Start()
    {
        // Força a variável a resetar toda vez que uma fase iniciar
        pausado = false;

        // Garante que o tempo do jogo comece normal, caso retornar pro menu com o jogo pausado
        Time.timeScale = 1f;

        if (playerMovement == null)
            playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
    }

    private void AoPausar(InputAction.CallbackContext context)
    {
        if (pausePanel.activeSelf || optionsPanel.activeSelf || controlsPanel.activeSelf)
            ResumirJogo();
        else if (Time.timeScale > 0f)
            PausarJogo();
    }

    private void AoVoltar(InputAction.CallbackContext context)
    {
        // Escape é tratado por Pause; Cancel no controle volta um nível do menu.
        if (!(context.control.device is Gamepad)) return;
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventarioAberto)
            return;
        if (controlsPanel.activeSelf) VoltarParaOpcoes();
        else if (optionsPanel.activeSelf) VoltarParaPause();
        else if (pausePanel.activeSelf) ResumirJogo();
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
