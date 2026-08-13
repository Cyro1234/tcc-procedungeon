using UnityEngine;

public static class Rng
{
    public static int baseSeed { get; private set; }

    public static System.Random dungeonRng;
    public static System.Random enemyRng;
    public static System.Random chestRng;

    public static void Init(int seed)
    {

        baseSeed = seed;
        

        dungeonRng = new System.Random(seed);
        enemyRng = new System.Random(seed + 1);
        chestRng = new System.Random(seed + 2);
    }

    // =================
    // DUNGEON
    // =================

    public static int DungeonRange(int min, int max)
    {
        return dungeonRng.Next(min, max);
    }

    public static float DungeonValue()
    {
        return (float)dungeonRng.NextDouble();
    }

    // =================
    // ENEMIES
    // =================
    public static int EnemyRange(int min, int max)
    {
        return enemyRng.Next(min, max);
    }

    public static float EnemyValue()
    {
        return (float)enemyRng.NextDouble();
    }

    // =================
    // Chest
    // =================

    public static int ChestRange(int min, int max)
    {
        return chestRng.Next(min, max);
    }

    public static float ChestValue()
    {
        return (float)chestRng.NextDouble();
    }

}
