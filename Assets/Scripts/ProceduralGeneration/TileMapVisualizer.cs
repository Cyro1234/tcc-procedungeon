using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TileMapVisualizer;

public class TileMapVisualizer : MonoBehaviour
{
    // TILEMAPS
    [SerializeField] private Tilemap floorTileMap;
    [SerializeField] private Tilemap wallTileMap;
    [SerializeField] private Tilemap walkInFrontTileMap;

    // PREFAB DA ESCADA
    [SerializeField] private GameObject exitPrefab;

    // LISTA DE TILES DE CADA DIFICULDADE.
    // tile[0] eh o chao
    // tile[1] eh a parede

    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelFloresta;
    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelDeserto;
    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelCaverna;
    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelAbismo;

    [SerializeField] private WeightedTable<List<TileBase>> floorTilesNivelInfinito;

    private Biomas biomaAtual = Biomas.Infinito;

    // Tile da porta quando fecha. Se quiser usar o mesmo da parede do nivel, usar wallTileEscolhido
    [SerializeField] private TileBase doorTile;

    // Usado para guardar as informacoes do chao e parede atual
    private WeightedTable<List<TileBase>> tableTiles = null;
    private TileBase floorTileEscolhido = null;
    private TileBase wallTileEscolhido = null;


    private GameObject currentLadder;

    // Dificuldade de cada nivel. Se quiser adicionar mais, completar na funcao GetTilePorNivel e adicionar mais WeightedTable<List<TileBase>>
    public enum Niveis
    {
        Baixo,
        Medio,
        Alto
    }

    public enum Biomas
    {
        Deserto,
        Floresta,
        Caverna,
        Abismo,
        Infinito // Endless. Usado quando passou por todos os outros biomas
    }

    private WeightedTable<List<TileBase>> GetTilePorBioma(Biomas bioma)
    {
        switch (bioma)
        {
            case Biomas.Deserto:
                return floorTilesNivelDeserto;
            case Biomas.Floresta:
                return floorTilesNivelFloresta;
            case Biomas.Caverna:
                return floorTilesNivelCaverna;
            case Biomas.Abismo:
                return floorTilesNivelAbismo;
            default:
                return floorTilesNivelInfinito; // NAO DEVERIA ACONTECER MAS POR SEGURANCA
        }
    }

    //// Pega os possiveis Tiles do nivel atual
    //private WeightedTable<List<TileBase>> GetTilePorNivel(Niveis nivel)
    //{
    //    switch(nivel) 
    //    {
    //        case Niveis.Baixo:
    //            return floorTilesNivelBaixo;
    //        case Niveis.Medio:
    //            return floorTilesNivelMedio;
    //        case Niveis.Alto:
    //            return floorTilesNivelAlto;
    //        default:
    //            return floorTilesNivelBaixo; // NAO DEVERIA ACONTECER MAS POR SEGURANCA
    //    }
    //}

    public void Setup(Biomas bioma)  // Faz as escolhas do chao e parede para pintar o nivel. CHAMAR SETUP ANTES DE PINTAR TODA VEZ QUE MUDAR DE ANDAR OU PRIMEIRO ANDAR.
    {

        tableTiles = GetTilePorBioma(bioma);
        List<TileBase> itemEscolhido = tableTiles.getRandom(Rng.dungeonRng);
        floorTileEscolhido = itemEscolhido[0]; // CHAO
        wallTileEscolhido = itemEscolhido[1]; // PAREDE    
    }


    // Pinta o chao inteiro
    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions, Biomas bioma)
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

    // Limpa todos os tilemaps. Chamar antes de pintar o proximo andar
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

    // Remove a porta e pinta o chao no lugar da porta novamente
    public void ClearTile(Vector2Int position) 
    {
        Vector3Int tilePosition = wallTileMap.WorldToCell((Vector3Int)position);

        // remove a porta
        wallTileMap.SetTile(tilePosition, null);

        // pinta o chao
        floorTileMap.SetTile(tilePosition, floorTileEscolhido);
    }

    // Gera a escada no final do nivel
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
