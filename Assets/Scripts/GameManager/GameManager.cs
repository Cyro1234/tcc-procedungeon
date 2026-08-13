using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private RoomFirstDungeonGenerator generator;
    [SerializeField] private TMP_Text seedText;
    
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generator = GetComponent<RoomFirstDungeonGenerator>();

        int seedToUse = seed;

        if (useRandomSeed)
        {
            seedToUse = System.DateTime.Now.GetHashCode();
        }

        // 1. Atualiza a UI (Interface separada da Lógica de Negócios)
        if (seedText != null)
        {
            seedText.text = seedToUse.ToString();
        }

        // 2. Manda o gerador trabalhar usando a seed definida
        generator.Setup(seedToUse);
    }
}
