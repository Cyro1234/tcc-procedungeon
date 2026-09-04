using UnityEngine;

public class BossRoomSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bossRoomFlorestaPrefab;
    [SerializeField] private GameObject bossRoomCavernaPrefab;
    [SerializeField] private GameObject bossRoomAbismoPrefab;
    [SerializeField] private GameObject bossRoomDesertoPrefab;
    [SerializeField] private GameObject bossRoomInfinitoPrefab;

    private GameObject instanciaSala = null;
    private GameObject prefabByBioma(TileMapVisualizer.Biomas bioma)
    {
        switch (bioma)
        {
            case TileMapVisualizer.Biomas.Floresta:
                return bossRoomFlorestaPrefab;
            case TileMapVisualizer.Biomas.Caverna:
                return bossRoomCavernaPrefab;
            case TileMapVisualizer.Biomas.Deserto:
                return bossRoomDesertoPrefab;
            case TileMapVisualizer.Biomas.Abismo:
                return bossRoomAbismoPrefab;
            case TileMapVisualizer.Biomas.Infinito:
                return bossRoomInfinitoPrefab;
            default:
                return bossRoomInfinitoPrefab;
        }
    }

    // Spawna bossRoom em 1000,0
    public void spawnBossRoom(TileMapVisualizer.Biomas biomaAtual, AbstractDungeonGenerator generator, TileMapVisualizer tileMap)
    {
        if (instanciaSala != null)
        {
            Destroy(instanciaSala);
            instanciaSala = null;
        }

        // Instanciar prefab boss
        GameObject bossRoomPrefab = prefabByBioma(biomaAtual);
        instanciaSala = Instantiate(bossRoomPrefab, new Vector3(1000, 0, 0), Quaternion.identity);

        // Instanciar escada
        Vector2Int pos = new Vector2Int(1005, 0); // Mudar para algo melhor 
        tileMap.PaintExit(pos, generator);
    }
}
