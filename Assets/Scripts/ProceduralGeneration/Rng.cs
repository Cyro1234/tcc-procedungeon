using System.Collections.Generic;
using UnityEngine;

public static class Rng
{
    public static int baseSeed { get; private set; }
    public static int CurrentFloor { get; private set; }

    // Mesma ideia original: cada sistema usa a seed com um deslocamento.
    public static System.Random dungeonRng;
    public static System.Random enemyRng;
    public static System.Random chestRng;
    public static System.Random debuffRng;
    private static System.Random combatRng;
    private static System.Random effectsRng = new System.Random(0);
    private static System.Random biomeRng;

    public static void Init(int seed)
    {
        baseSeed = seed;
        biomeRng = new System.Random(unchecked(seed + 6));
        BeginFloor(0);
    }

    public static void BeginFloor(int floor)
    {
        CurrentFloor = floor;

        // Exemplo: seed 100, andar 1 => seed do andar 200.
        // O unchecked permite usar tambem seeds nos limites de int.
        unchecked
        {
            int floorSeed = baseSeed + floor * 100;
            dungeonRng = new System.Random(floorSeed);
            enemyRng = new System.Random(floorSeed + 1);
            chestRng = new System.Random(floorSeed + 2);
            debuffRng = new System.Random(floorSeed + 3);
            combatRng = new System.Random(floorSeed + 4);
            effectsRng = new System.Random(floorSeed + 5);
        }
    }

    // Cada entidade recebe seu proprio Random ao nascer. Seus sorteios depois
    // disso nao gastam os numeros usados para gerar o restante do mapa.
    public static System.Random NewEnemyRandom() => new System.Random(enemyRng.Next());
    public static System.Random NewChestRandom() => new System.Random(chestRng.Next());
    public static System.Random NewCombatRandom() => new System.Random(combatRng.Next());

    // Ordenar as posicoes evita depender da ordem interna de um HashSet.
    public static List<Vector2Int> OrderPositions(IEnumerable<Vector2Int> positions)
    {
        var ordered = new List<Vector2Int>(positions);
        ordered.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return ordered;
    }

    // Vetor aleatorio
    private static Vector2 InsideUnitCircle(System.Random rng)
    {
        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
        float radius = Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    public static Vector2 EffectsInsideUnitCircle() => InsideUnitCircle(effectsRng);
    public static Vector2 CombatInsideUnitCircle() => InsideUnitCircle(combatRng);
    public static float EffectsRange(float min, float max) => Mathf.Lerp(min, max, (float)effectsRng.NextDouble());
    public static int BiomeRange(int min, int max) => biomeRng.Next(min, max);
    public static int DungeonRange(int min, int max) => dungeonRng.Next(min, max);
    public static float DungeonValue() => (float)dungeonRng.NextDouble();
    public static int EnemyRange(int min, int max) => enemyRng.Next(min, max);
    public static float EnemyValue() => (float)enemyRng.NextDouble();
    public static int ChestRange(int min, int max) => chestRng.Next(min, max);
    public static float ChestValue() => (float)chestRng.NextDouble();
    public static int DebuffRange(int min, int max) => debuffRng.Next(min, max);
    public static float DebuffValue() => (float)debuffRng.NextDouble();
}
