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

    // Tile da porta quando fecha. Se quiser usar o mesmo da parede do nivel, usar wallTileEscolhido
    [SerializeField] private TileBase doorTile;

    // Usado para guardar as informacoes do chao e parede atual
    private WeightedTable<List<TileBase>> tableTiles = null;
    private TileBase floorTileEscolhido = null;
    private TileBase wallTileEscolhido = null;
    private List<TileBase> floorTilesNoise = null; // Para biomas com noise (Floresta)
    private Biomas biomaAtualSetup = Biomas.Deserto;

    // Parametros de noise para Floresta
    [SerializeField] private float noiseScale = 8f;
    [SerializeField] private float noiseThreshold = 0.7f; // 0.0-1.0: quanto maior, mais o tile base (0) aparece

    // Mapeamento de tiles para autotiling (os índices dos tiles de detalhe)
    // Você vai configurar isso no Inspector com os índices corretos
    private Dictionary<int, TileBase> autoTileMapping = new Dictionary<int, TileBase>();

    // Estados de conexão dos vizinhos (bitwise)
    private const int UP = 1;      // Vizinho acima
    private const int DOWN = 2;    // Vizinho abaixo
    private const int LEFT = 4;    // Vizinho esquerda
    private const int RIGHT = 8;   // Vizinho direita

    private GameObject currentLadder;
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
        biomaAtualSetup = bioma;
        tableTiles = GetTilePorBioma(bioma);
        List<TileBase> itemEscolhido = tableTiles.getRandom(Rng.dungeonRng);

        if (bioma == Biomas.Floresta)
        {
            // Para Floresta, armazenar todos os tiles de chão para usar com noise
            floorTilesNoise = new List<TileBase>(itemEscolhido);
            // O último item é a parede
            if (floorTilesNoise.Count > 0)
            {
                wallTileEscolhido = floorTilesNoise[floorTilesNoise.Count - 1];
                // Remove a parede da lista de tiles de chão
                floorTilesNoise.RemoveAt(floorTilesNoise.Count - 1);
            }
        }
        else
        {
            floorTileEscolhido = itemEscolhido[0]; // CHAO
            wallTileEscolhido = itemEscolhido[1]; // PAREDE
            floorTilesNoise = null;
        }
    }


    // Pinta o chao inteiro
    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions, Biomas bioma)
    {
        if (bioma == Biomas.Floresta && floorTilesNoise != null && floorTilesNoise.Count > 0)
        {
            PaintFloorTilesWithNoise(floorPositions);
        }
        else
        {
            PaintTiles(floorPositions, floorTileMap, floorTileEscolhido);
        }
    }

    // Pinta o chão usando Perlin Noise para variar os tiles (para Floresta)
    private void PaintFloorTilesWithNoise(IEnumerable<Vector2Int> floorPositions)
    {
        if (floorTilesNoise == null || floorTilesNoise.Count == 0)
            return;

        // Primeiro passo: pintar todos os tiles base e determinar quais são "caminhos"
        HashSet<Vector2Int> pathPositions = new HashSet<Vector2Int>();
        List<Vector2Int> positionsList = new List<Vector2Int>(floorPositions);

        foreach (var position in positionsList)
        {
            float noiseValue = Mathf.PerlinNoise(position.x / noiseScale, position.y / noiseScale);

            if (noiseValue < noiseThreshold)
            {
                // Tile base - pintar direto
                PaintSingleTile(position, floorTileMap, floorTilesNoise[0]);
            }
            else
            {
                // Marcar como posição de caminho (vamos processar depois)
                pathPositions.Add(position);
            }
        }

        // Segundo passo: processar os caminhos com autotiling
        foreach (var position in pathPositions)
        {
            // Determinar conexões com vizinhos
            int connectionState = 0;

            // Verificar vizinho acima
            if (pathPositions.Contains(position + Vector2Int.up))
                connectionState |= UP;

            // Verificar vizinho abaixo
            if (pathPositions.Contains(position + Vector2Int.down))
                connectionState |= DOWN;

            // Verificar vizinho esquerda
            if (pathPositions.Contains(position + Vector2Int.left))
                connectionState |= LEFT;

            // Verificar vizinho direita
            if (pathPositions.Contains(position + Vector2Int.right))
                connectionState |= RIGHT;

            // Escolher o tile baseado na conexão
            TileBase selectedTile = GetAutoTile(connectionState, position);
            PaintSingleTile(position, floorTileMap, selectedTile);
        }
    }

    private TileBase GetAutoTile(int connectionState, Vector2Int position)
    {
        // Se não tem vizinhos, é um tile isolado
        if (connectionState == 0)
            return floorTilesNoise[1]; // Isolado

        // Conexão simples: apenas uma direção (pontas)
        if (connectionState == LEFT)
            return floorTilesNoise[3];  // Ponta horizontal esquerda
        if (connectionState == RIGHT)
            return floorTilesNoise[2];  // Ponta horizontal direita
        if (connectionState == DOWN)
            return floorTilesNoise[8];  // Ponta vertical baixo
        if (connectionState == UP)
            return floorTilesNoise[7];  // Ponta vertical cima

        // Conexão dupla: dois lados opostos (retas)
        if (connectionState == (LEFT | RIGHT))
            return floorTilesNoise[4];  // Linha horizontal -
        if (connectionState == (UP | DOWN))
            return floorTilesNoise[5];  // Linha vertical |

        // Cantos (L): dois lados adjacentes
        // L de cima pra baixo (esquerda-direita)
        if (connectionState == (LEFT | DOWN))
            return floorTilesNoise[9];  // Cima pra baixo esquerda→direita
        if (connectionState == (RIGHT | DOWN))
            return floorTilesNoise[10]; // Cima pra baixo direita→esquerda

        // L pra direita/esquerda (cima-baixo)
        if (connectionState == (UP | RIGHT))
            return floorTilesNoise[11]; // Pra direita (cima pra baixo)
        if (connectionState == (UP | LEFT))
            return floorTilesNoise[12]; // Pra esquerda (cima pra baixo)

        // Três conexões (T)
        // T normal (⊢) - conexões: cima, esquerda, direita
        if (connectionState == (UP | LEFT | RIGHT))
            return floorTilesNoise[14]; // T normal ⊢

        // T invertido (⊣) - conexões: baixo, esquerda, direita
        if (connectionState == (DOWN | LEFT | RIGHT))
            return floorTilesNoise[13]; // T invertido ⊣

        // T de lado esquerdo (┤) - conexões: cima, baixo, direita
        if (connectionState == (UP | DOWN | RIGHT))
            return floorTilesNoise[15]; // T lado esquerdo ├

        // T de lado direito (├) - conexões: cima, baixo, esquerda
        if (connectionState == (UP | DOWN | LEFT))
            return floorTilesNoise[16]; // T lado direito ┤

        // Todas as quatro conexões (cruz)
        if (connectionState == (UP | DOWN | LEFT | RIGHT))
            return floorTilesNoise[6]; // Cruz

        // Fallback (não deve acontecer)
        return floorTilesNoise[1];
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
        if (biomaAtualSetup == Biomas.Floresta && floorTilesNoise != null && floorTilesNoise.Count > 0)
        {
            // Para Floresta com noise, usar noise para escolher o tile
            float noiseValue = Mathf.PerlinNoise(position.x / noiseScale, position.y / noiseScale);

            if (noiseValue < noiseThreshold)
            {
                // Tile base
                floorTileMap.SetTile(tilePosition, floorTilesNoise[0]);
            }
            else
            {
                // Para ClearTile, usar o tile de caminho simples (índice 1)
                // Pois não temos contexto de vizinhos aqui
                floorTileMap.SetTile(tilePosition, floorTilesNoise[1]);
            }
        }
        else
        {
            floorTileMap.SetTile(tilePosition, floorTileEscolhido);
        }
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
