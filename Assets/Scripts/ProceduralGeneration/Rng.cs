using UnityEngine;

public static class Rng
{
    private static int baseSeed;

    private static System.Random dungeonRng;
    private static System.Random enemyRng;
    
    public static void Init(int seed)
    {
        baseSeed = seed;

        dungeonRng = new System.Random(seed);
        enemyRng = new System.Random(seed + 1);
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

}
