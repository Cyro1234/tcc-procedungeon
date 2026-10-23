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
    private void OnValidate()
    {
        WeightedTable<GameObject>.ValidateInEditor(chestTableBaixo, this, nameof(chestTableBaixo));
        WeightedTable<GameObject>.ValidateInEditor(chestTableMedio, this, nameof(chestTableMedio));
        WeightedTable<GameObject>.ValidateInEditor(chestTableAlto, this, nameof(chestTableAlto));
    }

    [Header("Starting Chest Settings")]
    [SerializeField] public bool randomStartingChest = false;
    [SerializeField] private GameObject manualStartingChestPrefab; // Escolhe qual Prefab de baú aparece na sala 1
    [SerializeField] public bool forceStartingItem = true;
    [SerializeField] private Chest.ItemType startingChestItem = Chest.ItemType.LongSword;

    [Header("Teste")]
    [Tooltip("TESTE: bau inicial sempre traz a pena da fenix.")]
    [SerializeField] private bool penaNoBauInicial = false;


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


    public void SpawnProceduralChests(SpawnPlacement placement, TileMapVisualizer.Niveis nivelAtual)
    {
        var chestTable = GetChestTablePorNivel(nivelAtual);
        if (placement.RoomCount <= 1 || chestTable.items.Count == 0) return;

        int qtdBaus = Rng.ChestRange(0, 3);
        for (int i = 0; i < qtdBaus; i++)
        {
            int roomIndex = Rng.ChestRange(1, placement.RoomCount);
            if (!placement.TryTakeRandomPosition(roomIndex, Rng.chestRng, out Vector2Int position)) continue;
            GameObject prefab = chestTable.getRandom(Rng.chestRng);
            GameObject chest = Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
            InicializarBau(chest);
            spawnedChests.Add(chest);
        }
    }

    private void InicializarBau(GameObject chest)
    {
        var contents = chest.GetComponent<Chest>();
        if (contents != null)
            contents.InicializarConteudo(Rng.NewChestRandom());
    }

    public void SpawnaBauInicial(TileMapVisualizer.Niveis nivelAtual, SpawnPlacement placement, Vector2Int playerSpawn)
    {
        GameObject prefab = manualStartingChestPrefab;
        if (randomStartingChest)
        {
            var chestTable = GetChestTablePorNivel(nivelAtual);
            if (chestTable.items.Count > 0) prefab = chestTable.getRandom(Rng.chestRng);
        }
        if (prefab == null) return;

        // Tenta ficar perto do jogador, mas nunca ocupa uma passagem ou o proprio spawn.
        if (!placement.TryTakeClosestPosition(0, playerSpawn + Vector2Int.right * 2, out Vector2Int position))
        {
            Debug.LogWarning("Nao ha espaco seguro para o bau inicial nesta sala.", this);
            return;
        }
        GameObject initialChest = Instantiate(prefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        Chest chestScript = initialChest.GetComponent<Chest>();
        if (chestScript != null && penaNoBauInicial) chestScript.ConfigurarItem(Chest.ItemType.PenaFenix);
        else if (chestScript != null && forceStartingItem) chestScript.ConfigurarItem(startingChestItem);
        InicializarBau(initialChest);
        spawnedChests.Add(initialChest);
    }

    // Teste (Xiter): cria um bau de raridade aleatoria na posicao pedida. Usa o Random do Unity
    // em vez do Rng da seed, pra nao mudar os baus e o mapa que a partida ja ia gerar
    public GameObject SpawnarBauDeTeste(Vector3 posicao)
    {
        var raridades = new List<GameObject>();
        foreach (var tabela in new[] { chestTableBaixo, chestTableMedio, chestTableAlto })
        {
            if (tabela == null) continue;
            foreach (var entrada in tabela.items)
                if (entrada != null && entrada.item != null && !raridades.Contains(entrada.item)) raridades.Add(entrada.item);
        }
        if (raridades.Count == 0) return null;

        GameObject prefab = raridades[Random.Range(0, raridades.Count)];
        GameObject chest = Instantiate(prefab, posicao, Quaternion.identity);
        var conteudo = chest.GetComponent<Chest>();
        if (conteudo != null) conteudo.InicializarConteudo(new System.Random(Random.Range(int.MinValue, int.MaxValue)));
        spawnedChests.Add(chest);
        return chest;
    }
}
