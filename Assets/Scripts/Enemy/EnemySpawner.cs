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
    public void SpawnEnemies(List<HashSet<Vector2Int>> roomsList) // NOVO SPAWN DE INIMIGOS PARA O SUB BSP
    {
        for (int i = 1; i < roomsList.Count; i++)
        {
            var roomTiles = roomsList[i];

            // Filtra posicoes que nao estejam na parede para impedir spawnar inimigos dentro de paredes
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            foreach (var pos in roomTiles)
            {
                if (!EhParede(pos, roomTiles))
                {
                    availablePositions.Add(pos);
                }
            }

            availablePositions = Rng.OrderPositions(availablePositions);
            int enemyCount = Rng.EnemyRange(0, maxEnemiesPerRoom + 1); // Quantidade de inimigos na sala

            for (int j = 0; j < enemyCount && availablePositions.Count > 0; j++)
            {
                int index = Rng.EnemyRange(0, availablePositions.Count);
                Vector2Int pos = availablePositions[index];

                availablePositions.RemoveAt(index); // evita repetir posição

                GameObject enemy = Instantiate(getRandomEnemy(), new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                InitializeEnemy(enemy);
                enemies.Add(enemy);
            }
        }

        Debug.Log("SPAWNOU " + enemies.Count); // Quantidade de inimigos spawnadas
    }


    public void SpawnEnemies(List<BoundsInt> roomsList) // USADO QUANDO NAO TEM SUBBSP
    {
        // Nao spawna inimigos no spawn do jogador, por isso i = 1
        for (int i = 1; i < roomsList.Count; i++)
        {
            int rng = Rng.EnemyRange(0, maxEnemiesPerRoom + 1); // Quantidade de inimigos na sala
            for (int j = 0; j < rng; j++)
            {
                // Posicao do inimigo a spawnar
                int randomX = Rng.EnemyRange(roomsList[i].xMin + 5, roomsList[i].xMax - 5); // +-5 para nao spawnar na parede, pensar em uma solucao melhor eh ideal
                int randomY = Rng.EnemyRange(roomsList[i].yMin + 5, roomsList[i].yMax - 5);

                Vector3 spawnPos = new Vector3(randomX, randomY, 0);

                // Instancia o inimigo na posicao
                GameObject enemy = Instantiate(getRandomEnemy(), spawnPos, Quaternion.identity);
                InitializeEnemy(enemy);
                enemies.Add(enemy); // Guarda em uma lista para que possa limpar os inimigos ao concluir a fase
            }
        }
        Debug.Log("SPAWNOU " + enemies.Count); // Quantidade de inimigos spawnadas
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

    private bool EhParede(Vector2Int pos, HashSet<Vector2Int> floor)
    {
        return !floor.Contains(pos + Vector2Int.up) ||
               !floor.Contains(pos + Vector2Int.down) ||
               !floor.Contains(pos + Vector2Int.left) ||
               !floor.Contains(pos + Vector2Int.right);
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
