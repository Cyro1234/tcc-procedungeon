using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject pauseManager;

    public static bool pausado = false;

    // Ao apertar a tecla ESC, o jogo pausa/despausa e exibe/oculta o menu de pausa
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausado)
                ResumirJogo();
            else
                PausarJogo();
        }
    }

    public void PausarJogo()
    {
        pauseManager.SetActive(true);
        Time.timeScale = 0;
        pausado = true;
    }

    public void ResumirJogo()
    {
        pauseManager.SetActive(false);
        Time.timeScale = 1;
        pausado = false;
    }
}