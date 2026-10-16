using UnityEngine;

// Estatisticas da run atual (mostradas na tela de game over). Estatico porque a cena
// e recarregada a cada nova tentativa e o GameManager chama Reset() no Start.
// IMPORTANTE: nada aqui usa o Rng da seed, entao nao interfere na geracao procedural.
public static class RunStats
{
    private const string BestFloorKey = "BestFloor";

    public static int Floor { get; private set; }
    public static int Kills { get; private set; }
    public static int BossesKilled { get; private set; }
    public static float StartTime { get; private set; }

    public static int BestFloor => PlayerPrefs.GetInt(BestFloorKey, 0);

    public static void Reset()
    {
        Floor = 0;
        Kills = 0;
        BossesKilled = 0;
        StartTime = Time.time;
    }

    public static void SetFloor(int floor)
    {
        Floor = floor;
        if (floor > BestFloor)
        {
            PlayerPrefs.SetInt(BestFloorKey, floor);
        }
    }

    public static void AddKill(bool boss)
    {
        Kills++;
        if (boss) BossesKilled++;
    }

    public static string ElapsedText()
    {
        int seconds = Mathf.Max(0, Mathf.FloorToInt(Time.time - StartTime));
        return $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
