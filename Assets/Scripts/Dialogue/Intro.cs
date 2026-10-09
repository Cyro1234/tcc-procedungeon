using System.Collections;
using TMPro;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class Intro : MonoBehaviour
{
    [SerializeField] private GameObject painelDialogo; // Para esconder dialogo durante a animacao
    [SerializeField] private CanvasGroup fadePanel;
    private float duracaoFade = 3.0f;

    [SerializeField] private PlayableDirector animacaoMataBoss;
    private int mataIndex; // Index de quando sumir o dialogo para matar o boss e dps continuar o dialogo

    [SerializeField] private TextMeshProUGUI text;
    private string[] lines;
    [SerializeField] private float textspeed;
    private int index = 0;

    private int[] trocas;
    private int index_troca = 0;
    private bool alexFalando = true;

    private bool aguardandoAnimacao = false;

    [SerializeField] private Image imagem;
    [SerializeField] private Sprite[] sprites; // Colocar 0-> Alex. 1-> Olho

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = string.Empty;
        SetupDialogo();
        StartCoroutine(FadeIn());
        StartDialogue();
    }

    // Update is called once per frame
    void Update()
    {
        if (aguardandoAnimacao)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (text.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                text.text = lines[index];
            }
        }
    }

    void StartDialogue()
    {
        index = 0;
        imagem.sprite = sprites[0];
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            text.text += c;
            yield return new WaitForSeconds(textspeed);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            if (index == mataIndex)
            {
                aguardandoAnimacao = true;

                StartCoroutine(EsperarAnimacao());
                return;
            }

            AtualizarDialogo();
        }
        else // Acabou os dialogos
        {
            //gameObject.SetActive(false);
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator EsperarAnimacao()
    {
        bool animacaoTerminou = false;

        animacaoMataBoss.stopped += AoTerminarAnimacao;
        animacaoMataBoss.Play();

        yield return new WaitUntil(() => animacaoTerminou);

        animacaoMataBoss.stopped -= AoTerminarAnimacao;

        painelDialogo.SetActive(true);
        aguardandoAnimacao = false;

        AtualizarDialogo();

        void AoTerminarAnimacao(PlayableDirector director)
        {
            animacaoTerminou = true;
        }

    }


    private IEnumerator FadeIn()
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 1f;

        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            fadePanel.alpha = 1f - Mathf.Clamp01(tempo / duracaoFade);

            yield return null;
        }

        fadePanel.alpha = 0f;
    }



    private IEnumerator FadeOut()
    {
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float tempo = 0f;

        while (tempo < duracaoFade)
        {
            tempo += Time.deltaTime;
            fadePanel.alpha = Mathf.Clamp01(tempo / duracaoFade);

            yield return null;
        }

        fadePanel.alpha = 1f;
        // TODO: Vai para o game agora
        SceneManager.LoadSceneAsync(1); // Carrega a cena no index X em File >> Build Profiles >> Scene List
    }

    private void AtualizarDialogo()
    {
        if (index == trocas[index_troca]) //Troca a imagem
        {
            index_troca++;
            if (alexFalando)
            {
                alexFalando = false;
                imagem.sprite = sprites[1];
            }
            else
            {
                alexFalando = true;
                imagem.sprite = sprites[0];
            }
        }
        text.text = string.Empty;
        StartCoroutine(TypeLine());
    }

    void SetupDialogo() 
    {
        const int n_lines = 10; // Deixar o numero total de linhas
        const int n_trocas = 5; // Numero de trocas de imagem

        trocas = new int[n_trocas];

        lines = new string[n_lines];

        lines[0] = "Olho...";
        trocas[0] = 1;

        lines[1] = "ALEX SEU MALDITO";
        lines[2] = "VOCE SENTIRA A IRA DE UM DEUS";
        lines[3] = "TOMAAAA!!!!!!!";

        trocas[1] = 4;

        lines[4] = "Você morrerá agora...";

        mataIndex = 5;
        trocas[2] = 5;

        lines[5] = "!!!!!!!!!!!!!!!!!!!!!!!!!!!";
        lines[6] = "Eu morrerei porem não será em vão!";
        lines[7] = "VOCÊ SERÁ AMALDIÇOADO!";

        trocas[3] = 8;

        lines[8] = "......";
        lines[9] = "Hora de voltar para casa...";
    }
}
