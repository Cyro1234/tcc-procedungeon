using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject pauseManager;

    public static bool pausado = false;
    private static int i = 1;



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

    //private void Start() // APENAS PARA TESTAR AS SEEDS. DEIXAR COMENTADO CASO NAO FOR TESTAR
    //{
    //    StartCoroutine(TesteDeSeeds());
    //}

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

    private void RestartGame()
    {
        Time.timeScale = 1f; // despausa
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator TesteDeSeeds()
    {
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