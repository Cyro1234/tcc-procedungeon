using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChestSpawner : MonoBehaviour
{

    //Baús com pesos de raridades diferentes baseados no nivel do andar
    [Header("Chest Settings (Probabilidade por Nível)")]
    [SerializeField] private WeightedTable<GameObject> chestTableBaixo;
    [SerializeField] private WeightedTable<GameObject> chestTableMedio;
    [SerializeField] private WeightedTable<GameObject> chestTableAlto;

    [Header("Starting Chest Settings")]
    [SerializeField] public bool randomStartingChest = false;
    [SerializeField] private GameObject manualStartingChestPrefab; // Escolhe qual Prefab de baú aparece na sala 1
    [SerializeField] public bool forceStartingItem = true;
    [SerializeField] private Chest.ItemType startingChestItem = Chest.ItemType.LongSword;


    //Lista para guardar e limpar todos os baús do andar
    private List<GameObject> spawnedChests = new List<GameObject>();


    public void CleanChest()
    {
        foreach (var chest in spawnedChests)
        {
            if (chest != null) Destroy(chest);
        }
        spawnedChests.Clear();
    }

    // NOVO: Função para pegar a tabela de baús correta dependendo do andar
    private WeightedTable<GameObject> GetChestTablePorNivel(TileMapVisualizer.Niveis nivel)
    {
        switch (nivel)
        {
            case TileMapVisualizer.Niveis.Baixo: return chestTableBaixo;
            case TileMapVisualizer.Niveis.Medio: return chestTableMedio;
            case TileMapVisualizer.Niveis.Alto: return chestTableAlto;
            default: return chestTableBaixo;
        }
    }


    public void SpawnProceduralChests(List<HashSet<Vector2Int>> roomsList, TileMapVisualizer.Niveis nivelAtual, int andar, int seed)
    {
        // Pega a tabela de acordo com o nível atual
        var chestTable = GetChestTablePorNivel(nivelAtual);

        if (roomsList.Count <= 1 || chestTable.items.Count == 0) return;

        int qtdBaus = Rng.ChestRange(0, 3);

        for (int i = 0; i < qtdBaus; i++)
        {
            int roomIndex = Rng.ChestRange(1, roomsList.Count);
            var roomTiles = roomsList[roomIndex];

            List<Vector2Int> availablePositions = new List<Vector2Int>();
            foreach (var pos in roomTiles)
            {
                if (!EhParede(pos, roomTiles)) availablePositions.Add(pos);
            }

            if (availablePositions.Count > 0)
            {
                int posIndex = Rng.ChestRange(0, availablePositions.Count);
                Vector2Int pos = availablePositions[posIndex];

                // Sorteia o Prefab do baú e instancia
                GameObject prefabToSpawn = chestTable.getRandom(Rng.chestRng);
                if (prefabToSpawn != null)
                {
                    GameObject chest = Instantiate(prefabToSpawn, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                    spawnedChests.Add(chest);
                }
            }
        }
    }


    // Usado quando NÃO está gerando com SubBSP (Salas Simples)
    public void SpawnProceduralChests(List<BoundsInt> roomsList, TileMapVisualizer.Niveis nivelAtual, int andar, int seed)
    {
        var chestTable = GetChestTablePorNivel(nivelAtual);

        if (roomsList.Count <= 1 || chestTable.items.Count == 0) return;

        int qtdBaus = Rng.ChestRange(0, 3);

        for (int i = 0; i < qtdBaus; i++)
        {
            int roomIndex = Rng.ChestRange(1, roomsList.Count);
            BoundsInt room = roomsList[roomIndex];

            int randomX = Rng.ChestRange(room.xMin + 2, room.xMax - 2);
            int randomY = Rng.ChestRange(room.yMin + 2, room.yMax - 2);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GameObject prefabToSpawn = chestTable.getRandom(Rng.chestRng);
            if (prefabToSpawn != null)
            {
                GameObject chest = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
                spawnedChests.Add(chest);
            }
        }
    }
    private bool EhParede(Vector2Int pos, HashSet<Vector2Int> floor)
    {
        return !floor.Contains(pos + Vector2Int.up) ||
               !floor.Contains(pos + Vector2Int.down) ||
               !floor.Contains(pos + Vector2Int.left) ||
               !floor.Contains(pos + Vector2Int.right);
    }


    public void SpawnaBauInicial(TileMapVisualizer.Niveis nivelAtual, int andar, List<Vector2Int> roomsCenters)
    {
        // Instancia o Baú na primeira sala (com um offset de +1 no X para não nascer em cima do jogador)
        // Decide se usa uma tabela sorteada ou o prefab manual
        GameObject prefabToSpawn = manualStartingChestPrefab;
        if (randomStartingChest)
        {
            var chestTable = GetChestTablePorNivel(nivelAtual);
            if (chestTable.items.Count > 0)
            {
                prefabToSpawn = chestTable.getRandom(Rng.chestRng);
            }
        }

        if (prefabToSpawn != null)
        {
            Vector3 chestPosition = new Vector3(roomsCenters[0].x + 1.5f, roomsCenters[0].y, 0);
            GameObject initialChest = Instantiate(prefabToSpawn, chestPosition, Quaternion.identity);
            // Força o item escolhido se a opção estiver marcada
            Chest chestScript = initialChest.GetComponent<Chest>();
            if (chestScript != null && forceStartingItem)
            {
                chestScript.ConfigurarItem(startingChestItem);
            }
            spawnedChests.Add(initialChest);
            Debug.Log("Baú instanciado na sala inicial.");
        }
    }

}
