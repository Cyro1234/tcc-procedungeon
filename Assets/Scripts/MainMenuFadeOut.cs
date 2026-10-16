using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SceneFader : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float duration = 1f;

    public static bool isFading = false;

    void Start()
    {
        isFading = false;
    }

    public void FadeAndLoad(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;

        // desativa o EventSystem para bloquear qualquer input de Teclado/Gamepad nos botões da UI
        if (EventSystem.current != null)
        {
            EventSystem.current.enabled = false;
        }

        yield return Fade(0f, 1f);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator Fade(float from, float to)
    {
        fadeGroup.blocksRaycasts = true;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fadeGroup.alpha = to;
        fadeGroup.blocksRaycasts = to > 0.5f;
    }
}