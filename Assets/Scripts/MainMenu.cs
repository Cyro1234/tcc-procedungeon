using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject optionsMenu;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        optionsMenu.SetActive(false);
    }

    public void IniciarJogo()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1f;
        }

        SceneManager.LoadSceneAsync(1); // Carrega a cena no index X em Build Profiles => Scene List
    }

    public void AbrirOpcoes()
    {
        mainMenu.SetActive(false);
        optionsMenu.SetActive(true);
    }

    public void RetornarMenu()
    {
        optionsMenu.SetActive(false);
        mainMenu.SetActive(true);
    }

    public void SairJogo()
    {
        Application.Quit();

    #if UNITY_EDITOR
       UnityEditor.EditorApplication.isPlaying = false;
    #else
       Application.Quit();
    #endif

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
