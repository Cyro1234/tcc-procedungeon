using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TileMapVisualizer;

public class TileMapVisualizer : MonoBehaviour
{

    [SerializeField] private Tilemap floorTileMap;
    [SerializeField] private Tilemap wallTileMap;
    [SerializeField] private Tilemap walkInFrontTileMap;

    [SerializeField] private GameObject exitPrefab;

    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelBaixo;
    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelMedio;
    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelAlto;

    [SerializeField] private TileBase doorTile;

    private WeightedTable<List<TileBase>> tableTiles = null;
    private TileBase floorTileEscolhido = null;
    private TileBase wallTileEscolhido = null;

    // Usado no random walk e para criar as paredes das salas


    private GameObject currentLadder;

    public enum Niveis
    {
        Baixo,
        Medio,
        Alto
    }

    private WeightedTable<List<TileBase>> GetTilePorNivel(Niveis nivel)
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

    public void Setup(Niveis nivel)  // Faz as escolhas do chao e parede para pintar o nivel. CHAMAR SETUP ANTES DE PINTAR TODA VEZ QUE MUDAR DE ANDAR OU PRIMEIRO ANDAR.
    {
        WeightedTable<List<TileBase>> tableTiles = GetTilePorNivel(nivel);
        var itemEscolhido = tableTiles.getRandom(Rng.dungeonRng);
        floorTileEscolhido = itemEscolhido[0]; // CHAO
        wallTileEscolhido = itemEscolhido[1]; // PAREDE
        Debug.Log("SETUP CONCLUIDO");
    }

    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions, Niveis nivel)
    {
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
        PaintSingleTile(position, wallTileMap, wallTileEscolhido);
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
        floorTileMap.SetTile(tilePosition, floorTileEscolhido);
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
