using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapVisualizer : MonoBehaviour
{

    [SerializeField] private Tilemap floorTileMap;
    [SerializeField] private Tilemap wallTileMap;
    [SerializeField] private Tilemap walkInFrontTileMap;

    [SerializeField] private TileBase floorTile;
    [SerializeField] private TileBase wallTile;
    [SerializeField] private GameObject exitPrefab;

    private GameObject currentLadder;

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions)
    {
        PaintTiles(floorPositions, floorTileMap, floorTile);
    }

    private void PaintTiles(IEnumerable<Vector2Int> positions, Tilemap tileMap, TileBase tile)
    {
        foreach (var position in positions)
        {
            PaintSingleTile(position, tileMap, tile);
        }
    }

    private void PaintSingleTile(Vector2Int position, Tilemap tileMap, TileBase tile)
    {
        var tilePosition = tileMap.WorldToCell((Vector3Int)position);
        tileMap.SetTile(tilePosition, tile);
    }

    public void Clear()
    {
        floorTileMap.ClearAllTiles();
        wallTileMap.ClearAllTiles();
        walkInFrontTileMap.ClearAllTiles();
    }

    internal void PaintWallTile(Vector2Int position)
    {
        PaintSingleTile(position, wallTileMap, wallTile);
    }

    public void PaintExit(Vector2Int position, AbstractDungeonGenerator generator) 
    {

        Debug.Log("[PAINTEXIT] Fui chamado");

        if (currentLadder != null)
        {
            Debug.Log("[PAINTEXIT] Destruindo escada antiga");
            Destroy(currentLadder);
        }

        currentLadder = Instantiate(exitPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
        Debug.Log("[PAINTEXIT] Escada instanciada: " + currentLadder.name);

        LadderNextDungeon ladderScript = currentLadder.GetComponent<LadderNextDungeon>();

        if (ladderScript == null)
        {
            Debug.LogError("[PAINTEXIT] NÃO achei LadderNextDungeon no objeto raiz da escada!");
            return;
        }
        Debug.Log("[PAINTEXIT] Achei LadderNextDungeon no objeto: " + ladderScript.gameObject.name);

        if (generator == null)
        {
            Debug.LogError("[PAINTEXIT] generator veio NULL!");
            return;
        }


        Debug.Log("[PAINTEXIT] Vou chamar SetDungeonGenerator agora...");
        ladderScript.SetDungeonGenerator(generator);
        Debug.Log("[PAINTEXIT] Chamei SetDungeonGenerator");

    }
}
