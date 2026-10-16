using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] private int maxEnemiesPerRoom = 3;
    private List<GameObject> enemies = new List<GameObject>();
    [SerializeField] private WeightedTable<GameObject> enemyTable;
    private void OnValidate()
    {
        WeightedTable<GameObject>.ValidateInEditor(enemyTable, this, nameof(enemyTable));
    }
    public void SpawnEnemies(SpawnPlacement placement)
    {
        // A primeira sala fica livre de inimigos para o jogador comecar.
        for (int i = 1; i < placement.RoomCount; i++)
        {
            int enemyCount = Rng.EnemyRange(0, maxEnemiesPerRoom + 1);
            for (int j = 0; j < enemyCount; j++)
            {
                if (!placement.TryTakeRandomPosition(i, Rng.enemyRng, out Vector2Int position)) break;
                GameObject enemy = Instantiate(getRandomEnemy(), new Vector3(position.x, position.y, 0), Quaternion.identity);
                InitializeEnemy(enemy);
                enemies.Add(enemy);
            }
        }
        Debug.Log("SPAWNOU " + enemies.Count);
    }

    // Inicializa conteudo e drops antes dos callbacks Start dos prefabs.
    public static void InitializeEnemy(GameObject enemy)
    {
        var movements = enemy.GetComponentsInChildren<EnemyMovement>(true);
        for (int i = 0; i < movements.Length; i++) movements[i].InitializeRandom();
        var regular = enemy.GetComponentsInChildren<EnemyModularBuilder>(true);
        for (int i = 0; i < regular.Length; i++)
            regular[i].Initialize(Rng.NewEnemyRandom());
        var fast = enemy.GetComponentsInChildren<FastEnemyModularBuilder>(true);
        for (int i = 0; i < fast.Length; i++)
            fast[i].Initialize(Rng.NewEnemyRandom());
        foreach (var owl in enemy.GetComponentsInChildren<Coruja>(true)) owl.InitializeRandom();
        foreach (var witch in enemy.GetComponentsInChildren<Bruxinha>(true)) witch.InitializeRandom();
    }

    private GameObject getRandomEnemy()
    {
        return enemyTable.getRandom(Rng.enemyRng);
    }

    public void ClearEnemies()
    {
        // Limpa inimigos antes de tudo
        foreach (var enemy in enemies)
        {
            Destroy(enemy);
        }
        enemies.Clear();
    }
}
