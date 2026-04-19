using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void IniciarJogo()
    {
        if (Time.timeScale == 0)
        {
            Time.timeScale = 1f;
        }

  
        SceneManager.LoadSceneAsync(1); // Carrega a cena no index X em Build Profiles => Scene List
    }

    public void Opcoes()
    {
        SceneManager.LoadSceneAsync(2); 
    }

    public void SairJogo()
    {
        Application.Quit();

    // Pra testar o quit no editor, remover os comentários
    //#if UNITY_EDITOR
    //   UnityEditor.EditorApplication.isPlaying = false;
    //#else
    //   Application.Quit();
    //#endif

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
