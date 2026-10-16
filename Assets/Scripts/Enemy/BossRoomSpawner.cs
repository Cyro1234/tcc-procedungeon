using UnityEngine;
using UnityEngine.Tilemaps;

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
        PrepareWallColliders(instanciaSala);
        EnemySpawner.InitializeEnemy(instanciaSala);

        // Instanciar escada
        Vector2Int pos = new Vector2Int(1005, 0); // Mudar para algo melhor 
        tileMap.PaintExit(pos, generator);
    }

    private static void PrepareWallColliders(GameObject room)
    {
        foreach (TilemapCollider2D wall in room.GetComponentsInChildren<TilemapCollider2D>(true))
        {
            Tilemap map = wall.GetComponent<Tilemap>();
            // O atlas pode remover o contorno fisico do sprite no executavel.
            // As paredes ocupam a celula inteira, como no restante da dungeon.
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                if (map.HasTile(cell)) map.SetColliderType(cell, Tile.ColliderType.Grid);
            wall.ProcessTilemapChanges();
        }
    }
}
