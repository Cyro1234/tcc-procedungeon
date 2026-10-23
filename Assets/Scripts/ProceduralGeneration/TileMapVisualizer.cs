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

    private void Awake()
    {
        ConfigurarColisaoDasParedes();
    }

    private void OnValidate()
    {
        WeightedTable<List<TileBase>>.ValidateInEditor(floorTilesNivelFloresta, this, nameof(floorTilesNivelFloresta));
        WeightedTable<List<TileBase>>.ValidateInEditor(floorTilesNivelDeserto, this, nameof(floorTilesNivelDeserto));
        WeightedTable<List<TileBase>>.ValidateInEditor(floorTilesNivelCaverna, this, nameof(floorTilesNivelCaverna));
        WeightedTable<List<TileBase>>.ValidateInEditor(floorTilesNivelAbismo, this, nameof(floorTilesNivelAbismo));
        WeightedTable<List<TileBase>>.ValidateInEditor(floorTilesNivelInfinito, this, nameof(floorTilesNivelInfinito));
    }

    private void ConfigurarColisaoDasParedes()
    {
        if (wallTileMap == null) return;
        var tiles = wallTileMap.GetComponent<TilemapCollider2D>();
        if (tiles == null) return;

        var corpo = wallTileMap.GetComponent<Rigidbody2D>();
        if (corpo == null) corpo = wallTileMap.gameObject.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Static;

        var paredes = wallTileMap.GetComponent<CompositeCollider2D>();
        if (paredes == null) paredes = wallTileMap.gameObject.AddComponent<CompositeCollider2D>();
        paredes.geometryType = CompositeCollider2D.GeometryType.Polygons;
        paredes.generationType = CompositeCollider2D.GenerationType.Synchronous;
        paredes.vertexDistance = 0.005f;

        // Unir tiles vizinhos elimina as arestas internas que prendem o jogador.
        // Uma extrusão mínima fecha pequenas diferenças entre os contornos dos tiles.
        tiles.extrusionFactor = 0.001f;
        tiles.compositeOperation = Collider2D.CompositeOperation.Merge;
    }

    // PREFAB DA ESCADA
    [SerializeField] private GameObject exitPrefab;
    [SerializeField] private GameObject teleportPrefab;

    // LISTA DE TILES DE CADA DIFICULDADE.
    // Primeiro tile: chao base. Ultimo tile: parede. Entre eles: detalhes opcionais.

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
    private List<TileBase> floorTilesNoise = null;
    [Serializable]
    public class ConfiguracaoNoise
    {
        public bool ativo = true;
        [Min(0.01f), Tooltip("Tamanho das manchas de detalhe.")]
        public float escala = 8f;
        [Range(0f, 1f), Tooltip("Quanto maior, menos detalhes aparecem. 1 remove os detalhes.")]
        public float limite = 0.65f;
        public ConfiguracaoNoise(float limite) { this.limite = limite; }
    }

    [Header("Noise por bioma")]
    [SerializeField] private ConfiguracaoNoise noiseFloresta = new ConfiguracaoNoise(0.5f);
    [SerializeField] private ConfiguracaoNoise noiseDeserto = new ConfiguracaoNoise(0.65f);
    [SerializeField] private ConfiguracaoNoise noiseCaverna = new ConfiguracaoNoise(0.65f);
    [SerializeField] private ConfiguracaoNoise noiseAbismo = new ConfiguracaoNoise(0.65f);
    [SerializeField] private ConfiguracaoNoise noiseInfinito = new ConfiguracaoNoise(0.65f);
    private ConfiguracaoNoise noiseAtual;
    private readonly Dictionary<Vector2Int, TileBase> chaoPintado = new Dictionary<Vector2Int, TileBase>();
    private bool UsaNoise => noiseAtual != null && noiseAtual.ativo && floorTilesNoise != null && floorTilesNoise.Count > 1;
    private bool EhDetalhe(Vector2Int pos) => noiseAtual.limite < 1f && Mathf.PerlinNoise(pos.x / Mathf.Max(0.01f, noiseAtual.escala), pos.y / Mathf.Max(0.01f, noiseAtual.escala)) >= noiseAtual.limite;
    // Estados de conexão dos vizinhos (bitwise)
    private const int UP = 1;      // Vizinho acima
    private const int DOWN = 2;    // Vizinho abaixo
    private const int LEFT = 4;    // Vizinho esquerda
    private const int RIGHT = 8;   // Vizinho direita

    private GameObject currentLadder;
    private GameObject currentTeleport;

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
        // Todos os biomas: chao base, detalhes opcionais e parede por ultimo.
        floorTileEscolhido = itemEscolhido[0];
        wallTileEscolhido = itemEscolhido[itemEscolhido.Count - 1];
        floorTilesNoise = new List<TileBase>(itemEscolhido);
        floorTilesNoise.RemoveAt(floorTilesNoise.Count - 1);
        switch (bioma)
        {
            case Biomas.Floresta: noiseAtual = noiseFloresta; break;
            case Biomas.Deserto: noiseAtual = noiseDeserto; break;
            case Biomas.Caverna: noiseAtual = noiseCaverna; break;
            case Biomas.Abismo: noiseAtual = noiseAbismo; break;
            default: noiseAtual = noiseInfinito; break;
        }
    }
    // Pinta o chao inteiro
    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions, Biomas bioma)
    {
        if (UsaNoise)
        {
            PaintFloorTilesWithNoise(floorPositions);
        }
        else
        {
            PaintTiles(floorPositions, floorTileMap, floorTileEscolhido);
        }
    }

    // Pinta o chao usando o noise configurado para o bioma.
    private void PaintFloorTilesWithNoise(IEnumerable<Vector2Int> floorPositions)
    {
        if (floorTilesNoise == null || floorTilesNoise.Count == 0)
            return;

        // Primeiro passo: pintar todos os tiles base e determinar quais são "caminhos"
        HashSet<Vector2Int> pathPositions = new HashSet<Vector2Int>();
        List<Vector2Int> positionsList = new List<Vector2Int>(floorPositions);

        foreach (var position in positionsList)
        {


            if (!EhDetalhe(position))
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
        // Os novos biomas possuem um detalhe unico; a floresta possui o conjunto de 16 conexoes.
        if (floorTilesNoise.Count < 17) return floorTilesNoise[1];
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
        if (tileMap == floorTileMap) chaoPintado[position] = tile;
        if (tileMap == floorTileMap && tile != null)
        {
            // Fecha fissuras de rasterizacao com zoom/posicao fracionarios da camera.
            // A sobreposicao e apenas visual (0,2% por borda); o grid continua com celulas de 1 tile.
            tileMap.RemoveTileFlags(tilePosition, TileFlags.LockTransform);
            tileMap.SetTransformMatrix(tilePosition, Matrix4x4.Scale(new Vector3(1.004f, 1.004f, 1f)));
        }

        // Paredes e portas ocupam a celula inteira. O contorno do sprite pode
        // nao estar disponivel no Player quando a textura usa um atlas.
        if (tileMap == wallTileMap && tile != null)
        {
            tileMap.SetColliderType(tilePosition, Tile.ColliderType.Grid);
        }
    }

    // Limpa todos os tilemaps. Chamar antes de pintar o proximo andar
    public void Clear()
    {
        chaoPintado.Clear();
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
        PaintSingleTile(position, wallTileMap, doorTile);
    }

    // Remove a porta e pinta o chao no lugar da porta novamente
    public void ClearTile(Vector2Int position) 
    {
        Vector3Int tilePosition = wallTileMap.WorldToCell((Vector3Int)position);

        // remove a porta
        wallTileMap.SetTile(tilePosition, null);

        // Restaura exatamente o detalhe que estava sob a porta.
        TileBase tile;
        if (!chaoPintado.TryGetValue(position, out tile))
            tile = UsaNoise && EhDetalhe(position) ? floorTilesNoise[1] : floorTileEscolhido;
        PaintSingleTile(position, floorTileMap, tile);
    }
    public void PaintTeleport(Vector2Int position)
    {
        if (currentTeleport != null)
        {
            Destroy(currentTeleport);
        }

        currentTeleport = Instantiate(teleportPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
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
