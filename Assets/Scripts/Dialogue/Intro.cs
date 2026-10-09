using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
public class Intro : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI text;
    private string[] lines;
    [SerializeField] private float textspeed;
    private int index = 0;

    private int[] trocas;
    private int index_troca = 0;
    private bool alexFalando = true;

    [SerializeField] private Image imagem;
    [SerializeField] private Sprite[] sprites; // Colocar 0-> Alex. 1-> Olho

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = string.Empty;
        SetupDialogo();
        StartDialogue();
    }

    // Update is called once per frame
    void Update()
    {
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
        else
        {
            gameObject.SetActive(false);
        }
    }

    void SetupDialogo() 
    {
        const int n_lines = 5; // Deixar o numero total de linhas
        const int n_trocas = 2;

        trocas = new int[n_trocas];

        lines = new string[n_lines];

        lines[0] = "OLA JAIME";
        trocas[0] = 1;

        lines[1] = "ALEX SEU MALDITO";
        lines[2] = "VOCE SENTIRA A IRA DOS JUDEUS";
        lines[3] = "POR NETANYAHU!!!!!!!";

        trocas[1] = 4;

        lines[4] = "Oy Vey";
    }
}
