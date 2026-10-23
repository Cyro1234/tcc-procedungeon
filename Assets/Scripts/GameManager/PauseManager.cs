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

    public static bool pausado => !GameFlow.CanPlay;
    private static int i = 1;
    private InputActionAsset controles;

    private void OnEnable()
    {
        GameFlow.Changed += AoAlterarEstado;
        controles = GameFlow.PlayerActions;
        if (controles == null) return;
        InputAction pausa = controles.FindAction("Player/Pause", true);
        pausa.performed += AoPausar;
        pausa.Enable();
        controles.FindAction("UI/Cancel", true).performed += AoVoltar;
        controles.FindAction("UI/Cancel", true).Enable();
    }

    private void OnDisable()
    {
        GameFlow.Changed -= AoAlterarEstado;
        if (controles != null)
        {
            controles.FindAction("Player/Pause", true).performed -= AoPausar;
            controles.FindAction("UI/Cancel", true).performed -= AoVoltar;
        }
        controles = null;
        ResumirJogo();
    }

    void Start()
    {
        if (playerMovement == null)
            playerMovement = Object.FindAnyObjectByType<PlayerMovement>();

        CanvasPorCima.Mover("[Pause]", 999, pausePanel, optionsPanel, controlsPanel);
    }

    private void AoPausar(InputAction.CallbackContext context)
    {
        if (GameFlow.Current == GameFlow.State.Inventory)
            InventoryManager.Instance?.FecharInventario();
        else if (GameFlow.Current == GameFlow.State.PauseMenu)
        {
            if (context.control.device is Keyboard && controlsPanel.activeSelf) VoltarParaOpcoes();
            else if (context.control.device is Keyboard && optionsPanel.activeSelf) VoltarParaPause();
            else ResumirJogo();
        }
        else if (GameFlow.CanPlay)
            PausarJogo();
    }

    private void AoVoltar(InputAction.CallbackContext context)
    {
        // Escape é tratado por Pause; Cancel no controle volta um nível do menu.
        if (!(context.control.device is Gamepad)) return;
        if (GameFlow.Current != GameFlow.State.PauseMenu) return;
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
        if (!GameFlow.CanPlay) return;
        GameFlow.Pause(GameFlow.State.PauseMenu);
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        if (playerMovement != null) playerMovement.ForcarParada();

    }

    public void ResumirJogo()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        if (playerMovement != null) playerMovement.ForcarParada();

        GameFlow.Resume(GameFlow.State.PauseMenu);
    }

    private void AoAlterarEstado()
    {
        if (GameFlow.Current != GameFlow.State.PauseMenu &&
            ((pausePanel != null && pausePanel.activeSelf) || (optionsPanel != null && optionsPanel.activeSelf) ||
            (controlsPanel != null && controlsPanel.activeSelf))) ResumirJogo();
    }

    public void AbrirOpcoes()
    {
        if (GameFlow.Current != GameFlow.State.PauseMenu) return;
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void AbrirControles()
    {
        if (GameFlow.Current != GameFlow.State.PauseMenu) return;
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }
    public void VoltarParaPause()
    {
        if (GameFlow.Current != GameFlow.State.PauseMenu) return;
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
    public void VoltarParaOpcoes()
    {
        if (GameFlow.Current != GameFlow.State.PauseMenu) return;
        controlsPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    
    private void RestartGame()
    {
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
