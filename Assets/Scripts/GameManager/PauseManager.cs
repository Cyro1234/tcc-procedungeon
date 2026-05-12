using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject controlsPanel;

    public static bool pausado = false;

    void Start()
    {
        // Força a variável a resetar toda vez que uma fase iniciar
        pausado = false;

        // Garante que o tempo do jogo comece normal, caso retornar pro menu com o jogo pausado
        Time.timeScale = 1f;
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

    public void PausarJogo()
    {
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        Time.timeScale = 0;
        pausado = true;
    }

    public void ResumirJogo()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);

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
}