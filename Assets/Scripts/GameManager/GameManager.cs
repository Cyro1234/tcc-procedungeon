using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private RoomFirstDungeonGenerator generator;
    [SerializeField] private TMP_Text seedText;

    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    // Para o Boss
    private static bool deuTeleport = false;
    public static void setDeuTeleport(bool newDeuTeleport) { deuTeleport = newDeuTeleport; }
    public static bool getDeuTeleport() { return deuTeleport; }

    private static bool bossMorreu = false;
    public static void setBossMorreu(bool newBossMorreu) { bossMorreu = newBossMorreu; }
    public static bool getBossMorreu() { return bossMorreu; }

    public static void clean()
    {
        setDeuTeleport(false);
        setBossMorreu(false);
    }

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
