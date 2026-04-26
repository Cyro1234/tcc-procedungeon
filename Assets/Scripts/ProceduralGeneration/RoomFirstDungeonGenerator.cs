using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

// Utilizao o BSP para gerar as salas
public class RoomFirstDungeonGenerator : SimpleRandomWalkMapGenerator
{

    [SerializeField] private int minRoomWidth = 4;
    [SerializeField] private int minRoomHeight = 4;

    [SerializeField] private int dungeonWidth = 20;
    [SerializeField] private int dungeonHeight = 20;

    [SerializeField] private int offset = 1;
    public int Offset => offset;
    [SerializeField] private int subOffset = 1;

    [SerializeField] private bool randomWalkRooms = false;
    [SerializeField] private bool subBSPRooms = false;

    //[SerializeField] private GameObject enemyPrefab;

    //[SerializeField] private List<GameObject> ListenemyPrefab;
    [SerializeField] private WeightedTable<GameObject> enemyTable;

    [SerializeField] private int maxEnemiesPerRoom = 3;

    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    [SerializeField] private RoomDetector roomDetector;

    private List<GameObject> enemies = new List<GameObject>();
    private HashSet<Vector2Int> roomEntrances = new HashSet<Vector2Int>(); // guarda a posicao das entradas da sala
    private bool salaTrancada = false;

    protected override void RunProceduralGeneration()
    {
        

        tileMapVisualizer.Clear();
        CreateRooms();
    }

    private void Start()
    {
        if (useRandomSeed)
        {
            seed = GenerateRandomSeed();
        }

        Rng.Init(seed);

        Debug.Log("SEED: " + seed);

        RunProceduralGeneration();
    }

    private void CreateRooms()
    {
        roomEntrances.Clear();
        salaTrancada = false;
        // Limpa inimigos antes de tudo
        foreach (var enemy in enemies) 
        {
            Destroy(enemy);
        }
        enemies.Clear();

        // Obtem todas posicoes das salas geradas
        var roomList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPostion, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        if (roomDetector != null) roomDetector.SetRooms(roomList, offset);

        // Coloca os offsets para que as salas fiquem um pouco distantes entre as outras
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        if (subBSPRooms)
        {
            floor = CreateSubBSPRooms(roomList, subOffset, minRoomWidth, minRoomHeight); // deixei o offset no 0 pra ficar grudado
        }
        else
        {
            floor = CreateSimpleRooms(roomList);
        }

        //floor = CreateSimpleRooms(roomList);

        // Obtem o ponto central das salas
        List<Vector2Int> roomsCenters = new List<Vector2Int>();
        foreach (var room in roomList)
        {
            roomsCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }

        // Spawn do jogador e saida. O spawn eh a primeira sala e a saida a ultima sala gerada.
        PlaceSpawnAndExit(roomsCenters);
        
        SpawnEnemies(roomList);

        // Conectar salas com corredores
        HashSet<Vector2Int> corridors = ConnectRooms(roomsCenters);
        floor.UnionWith(corridors);

        // Coloca o chao e paredes
        tileMapVisualizer.PaintFloorTiles(floor);
        WallGenerator.CreateWalls(floor, tileMapVisualizer);

    }

    private HashSet<Vector2Int> CreateSubBSPRooms(List<BoundsInt> roomList, int offset, int minRoomWidth, int minRoomHeight)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>(); // guarda as posicoes do chao
        //for (int i = 0; i < roomList.Count; i++) { // percorre todas as salas menos as subs
        //    var roomBounds = roomList[i]; // limites
        //    var subRooms = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt(new Vector3Int(roomBounds.xMin + offset, roomBounds.yMin + offset, 0), new Vector3Int(roomBounds.size.x - offset * 2, roomBounds.size.y - offset * 2, 0)), Mathf.Max(2, minRoomWidth / 2), Mathf.Max(2, minRoomHeight / 2)); // gera as subsalas

        //    foreach (var subRoom in subRooms) { // percorre as subsalas
        //        for (int x = subRoom.xMin; x < subRoom.xMax; x++) // limites da sub-sala da posicao x
        //        {                
        //            for (int y = subRoom.yMin; y < subRoom.yMax; y++) // limites da sub-sala da posicao y
        //            {
        //                floor.Add(new Vector2Int(x, y));
        //            }
        //        }
        //    }
        //}
        foreach (var roomBounds in roomList) // percorre todas as salas geradas pelo BSP principal
        {
            HashSet<Vector2Int> roomFloor = new HashSet<Vector2Int>(); // guarda as posicoes de uma unica sala para processar os cortes

            // cria uma area base para a sala, com offset para nao ficar grudada nas outras salas
            for (int x = roomBounds.xMin + offset; x < roomBounds.xMax - offset; x++) // limites da sala na posicao x
            {
                for (int y = roomBounds.yMin + offset; y < roomBounds.yMax - offset; y++) // limites da sala na posicao y
                {
                    roomFloor.Add(new Vector2Int(x, y));
                }
            }


            // escolhe o tamanho dos cortes para a sala baseado no tamanho minimo
            int cutSizeX = Rng.DungeonRange(1, Mathf.Max(2, minRoomWidth / 3));
            int cutSizeY = Rng.DungeonRange(1, Mathf.Max(2, minRoomHeight / 3));

            // lista de cantos pra remover, 0 inferior esquerdo, 1 inferior direito, 2 superior esquerdo, 3 superior direito
            for (int i = 0; i < 4; i++) // percorre os 4 cantos da sala
            {
                // 50% de chance de cortar o canto para variar os formatos
                if (Rng.DungeonValue() > 0.5f) continue;

                for (int x = 0; x < cutSizeX; x++) // largura do corte
                {
                    for (int y = 0; y < cutSizeY; y++) // altura do corte
                    {
                        Vector2Int posToRemove = Vector2Int.zero;
                        if (i == 0) posToRemove = new Vector2Int(roomBounds.xMin + offset + x, roomBounds.yMin + offset + y);
                        if (i == 1) posToRemove = new Vector2Int(roomBounds.xMax - offset - 1 - x, roomBounds.yMin + offset + y);
                        if (i == 2) posToRemove = new Vector2Int(roomBounds.xMin + offset + x, roomBounds.yMax - offset - 1 - y);
                        if (i == 3) posToRemove = new Vector2Int(roomBounds.xMax - offset - 1 - x, roomBounds.yMax - offset - 1 - y);

                        roomFloor.Remove(posToRemove); // remove a posicao do chao dessa sala
                    }
                }
            }

            // garante que o centro e os eixos principais existam evitando q a sala suma
            Vector2Int center = (Vector2Int)Vector3Int.RoundToInt(roomBounds.center); // obtem o ponto central
            roomFloor.Add(center); // adiciona o centro de volta caso ele tenha sido removido

            floor.UnionWith(roomFloor);
        }
        return floor;
    }


    private void SpawnEnemies(List<BoundsInt> roomsList)
    {
        // Nao spawna inimigos no spawn do jogador, por isso i = 1
        for (int i = 1; i < roomsList.Count; i++)
        {
            int rng = Rng.EnemyRange(0, maxEnemiesPerRoom + 1); // Quantidade de inimigos na sala
            for (int j = 0; j < rng; j++) 
            {
                // Posicao do inimigo a spawnar
                int randomX = Rng.EnemyRange(roomsList[i].xMin + 5, roomsList[i].xMax - 5); // +-5 para nao spawnar na parede, pensar em uma solucao melhor eh ideal
                int randomY = Rng.EnemyRange(roomsList[i].yMin + 5, roomsList[i].yMax - 5);

                Vector3 spawnPos = new Vector3(randomX, randomY, 0);

                // Instancia o inimigo na posicao
                GameObject enemy = Instantiate(getRandomEnemy(), spawnPos, Quaternion.identity);
                enemies.Add(enemy); // Guarda em uma lista para que possa limpar os inimigos ao concluir a fase
            }
        }
        Debug.Log("SPAWNOU " + enemies.Count); // Quantidade de inimigos spawnadas
    }

    private GameObject getRandomEnemy() // TODO: USAR WEIGHTEDLIST
    {
        return enemyTable.getRandom(Rng.enemyRng);
    }

    // Coloca o jogador no spawn e cria a saida da fase
    private void PlaceSpawnAndExit(List<Vector2Int> roomsCenters)
    {
        // Coloca o jogador na primeira Sala
        GameObject player = GameObject.FindWithTag("Player");
        player.transform.position = new Vector3(roomsCenters[0].x, roomsCenters[0].y, 0);

        // Cria a escada da ultima sala
        tileMapVisualizer.PaintExit(roomsCenters[roomsCenters.Count - 1], this);
        
    }

    // Conecta as salas com um corredor
    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomsCenters)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        var currentRoomCenter = roomsCenters[Rng.DungeonRange(0, roomsCenters.Count)];
        roomsCenters.Remove(currentRoomCenter);

        while (roomsCenters.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, roomsCenters);
            roomsCenters.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    // Cria o corredor entre as salas. Funcao auxiliar de ConnectRooms
    private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = currentRoomCenter;
        corridor.Add(position);

        // A partir da posicao inicial
        while (position.y != destination.y) // Vai subindo ou descendo ate chegar no Y da sala de destino
        {
            if (destination.y > position.y) 
            {
                position += Vector2Int.up;
            }
            else if (destination.y < position.y) 
            {
                position += Vector2Int.down;
            }
            corridor.Add(position);
            CheckAndAddDoor(position);
        }
        while (position.x != destination.x) // Vai andando pros lados ate chegar no x da sala de destino
        {
            if (destination.x > position.x) 
            {
                position += Vector2Int.right;
            }
            else if (destination.x < position.x)
            {
                position += Vector2Int.left;
            }
            corridor.Add(position);
            CheckAndAddDoor(position);
        }
        return corridor;
    }

    // Função auxiliar para identificar se a posição é uma conexão
    private void CheckAndAddDoor(Vector2Int pos)
    {
        foreach (var room in roomDetector.GetRoomsList())
        {
            // define os limites onde as paredes da sala realmente existem (tava spawnando deslocado)
            int left = room.xMin + offset - 1;
            int right = room.xMax - offset;
            int bottom = room.yMin + offset - 1;
            int top = room.yMax - offset;

            bool naBordaVertical = (pos.x == left || pos.x == right) && (pos.y >= room.yMin + offset && pos.y < room.yMax - offset);
            bool naBordaHorizontal = (pos.y == bottom || pos.y == top) && (pos.x >= room.xMin + offset && pos.x < room.xMax - offset);

            if (naBordaVertical || naBordaHorizontal)
            {
                roomEntrances.Add(pos);
                // Debug.Log($"Porta registada em: {pos}");
            }
        }
    }

    // Acha a sala mais perto da sala atual. Funcao auxiliar de ConnectRooms
    private Vector2Int FindClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomsCenters)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;

        foreach (var position in roomsCenters) 
        {
            float currentDistance = Vector2.Distance(position, currentRoomCenter);
            if (currentDistance < distance) 
            { 
                distance = currentDistance;
                closest = position;
            }
        }

        return closest;
    }

    // Coloca os offsets das salas para nao ficar todas coladas
    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomList)
    {
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        foreach (var room in roomList)
        {
            for (int col = offset; col < room.size.x - offset; col++) 
            { 
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    private int GenerateRandomSeed()
    {
        return System.DateTime.Now.GetHashCode();
    }


    void Update()
    {
        if (roomDetector != null && roomDetector.jogadorSala) // verifica se o jogador esta na sala
        {
            if (roomDetector.inimigoSala && !salaTrancada) // se passar e tiver inimigos, tranca a sala
            {
                FecharPortasDaSala();
                salaTrancada = true;
            }
            else if (!roomDetector.inimigoSala && salaTrancada) // se não houver inimigos e a sala estiver trancada, abre a sala
            {
                AbrirPortasDaSala();
                salaTrancada = false;
            }
        }
    }

    private void FecharPortasDaSala()
    {
        BoundsInt? currentBounds = roomDetector.GetCurrentRoomBounds();
        if (currentBounds == null) return;

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.PaintDoorTile(pos);
            }
        }
    }

    private void AbrirPortasDaSala()
    {
        BoundsInt? currentBounds = roomDetector.GetCurrentRoomBounds();
        if (currentBounds == null) return;

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.ClearTile(pos);
            }
        }
        Debug.Log("Sala limpa! Portas removidas e chão restaurado.");
    }
}
