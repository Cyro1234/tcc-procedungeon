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

    [SerializeField] private WeightedTable<TileBase> floorTilesNivelBaixo;
    [SerializeField] private WeightedTable<TileBase> floorTilesNivelMedio;
    [SerializeField] private WeightedTable<TileBase> floorTilesNivelAlto;

    [SerializeField] private TileBase doorTile;

    // Usado no random walk e para criar as paredes das salas
    

    private GameObject currentLadder;

    public enum Niveis
    {
        Baixo,
        Medio,
        Alto
    }

    private WeightedTable<TileBase> GetTilePorNivel(Niveis nivel)
    {
        switch(nivel) 
        {
            case Niveis.Baixo:
                return floorTilesNivelBaixo;
            case Niveis.Medio:
                return floorTilesNivelMedio;
            case Niveis.Alto:
                return floorTilesNivelAlto;
            default:
                return floorTilesNivelBaixo; // NAO DEVERIA ACONTECER MAS POR SEGURANCA
        }
    }

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions, Niveis nivel)
    {
        WeightedTable<TileBase> tableTiles = GetTilePorNivel(nivel);
        TileBase floorTileEscolhido = tableTiles.getRandom(Rng.dungeonRng);

        PaintTiles(floorPositions, floorTileMap, floorTileEscolhido);
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

    public void PaintDoorTile(Vector2Int position)
    {
        var tilePosition = wallTileMap.WorldToCell((Vector3Int)position);

        // remove o tile do chao
        floorTileMap.SetTile(tilePosition, null);

        // pinta a porta na camada de paredes
        var tilePos = wallTileMap.WorldToCell((Vector3Int)position);
        wallTileMap.SetTile(tilePos, doorTile);
    }

    public void ClearTile(Vector2Int position)
    {
        Vector3Int tilePosition = wallTileMap.WorldToCell((Vector3Int)position);

        // remove a porta
        wallTileMap.SetTile(tilePosition, null);

        // pinta o chao
        floorTileMap.SetTile(tilePosition, floorTile);
    }

    public void PaintExit(Vector2Int position, AbstractDungeonGenerator generator) 
    {


        if (currentLadder != null)
        {
            Destroy(currentLadder);
        }

        currentLadder = Instantiate(exitPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);

        LadderNextDungeon ladderScript = currentLadder.GetComponent<LadderNextDungeon>();

        if (ladderScript == null)
        {
            return;
        }

        if (generator == null)
        {
            return;
        }


        ladderScript.SetDungeonGenerator(generator);

    }
}
