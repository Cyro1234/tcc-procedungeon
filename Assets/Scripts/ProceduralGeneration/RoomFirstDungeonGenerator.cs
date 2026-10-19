using System.Collections.Generic;
using UnityEngine;


// Utilizao o BSP para gerar as salas
public class RoomFirstDungeonGenerator : SimpleRandomWalkMapGenerator
{

    [SerializeField] private int minRoomWidth = 4;
    [SerializeField] private int minRoomHeight = 4;

    [SerializeField] private int dungeonWidth = 20;
    [SerializeField] private int dungeonHeight = 20;

    [SerializeField] private int offset = 1;
    public int Offset => offset;

    [SerializeField] private bool subBSPRooms = false;

    [SerializeField] private RoomDetector roomDetector;
    [SerializeField] private bool focarCentroSala = true;
    public bool FocarCentroSala => focarCentroSala;

    [SerializeField] private LevelManager levelManager;

    // SPAWNERS
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private ChestSpawner chestSpawner;
    [SerializeField] private DoorSpawner doorSpawner;
    [SerializeField] private BossRoomSpawner bossRoomSpawner;

    [SerializeField] private Transform playerTransform;


    protected override void RunProceduralGeneration()
    {
        levelManager.passarAndar();
        Rng.BeginFloor(levelManager.GetAndar());
        Debug.Log("ANDAR: " + levelManager.GetAndar());
        tileMapVisualizer.Clear();
        tileMapVisualizer.Setup(levelManager.GetBioma());
        CreateRooms();

        // Fade + banner "Andar X - Bioma" (so visual, nao usa o Rng da seed)
        GameEffects.Instance.OnNewFloor(levelManager.GetAndar(), levelManager.GetBiomaAtual());
    }

    public void Setup(int seed)
    {
        Rng.Init(seed);
        GameManager.clean();
        levelManager.setup();
        RunProceduralGeneration();
    }

    private void CreateRooms()
    {
        // Limpeza
        doorSpawner.CleanDoor();
        enemySpawner.ClearEnemies();
        chestSpawner.CleanChest();


        // Obtem todas posicoes das salas geradas
        var roomList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPostion, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        if (roomDetector != null) roomDetector.SetRooms(roomList, offset);

        // Coloca os offsets para que as salas fiquem um pouco distantes entre as outras
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        List<HashSet<Vector2Int>> salas = subBSPRooms
            ? CreateSubBSPRooms(roomList, offset, minRoomWidth, minRoomHeight)
            : CreateSimpleRooms(roomList);
        foreach (var sala in salas) floor.UnionWith(sala);

        // Obtem o ponto central das salas
        List<Vector2Int> roomsCenters = new List<Vector2Int>();
        foreach (var room in roomList)
        {
            roomsCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }

        // Os spawners precisam conhecer as passagens antes de ocupar o piso.
        HashSet<Vector2Int> corridors = CorridorGenerator.ConnectRooms(roomsCenters, doorSpawner, roomDetector);
        floor.UnionWith(corridors);
        var placement = new SpawnPlacement(salas, corridors, doorSpawner.roomEntrances,
            roomsCenters[0], roomsCenters[roomsCenters.Count - 1]);

        // Spawn do jogador e saida. O spawn eh a primeira sala e a saida a ultima sala gerada.
        PlaceSpawnAndExit(roomsCenters);
        chestSpawner.SpawnaBauInicial(levelManager.GetNivelAtual(), placement, roomsCenters[0]);
        enemySpawner.SpawnEnemies(placement);
        chestSpawner.SpawnProceduralChests(placement, levelManager.GetNivelAtual());

        // Coloca o chao e paredes
        tileMapVisualizer.PaintFloorTiles(floor, levelManager.GetBiomaAtual());
        WallGenerator.CreateWalls(floor, tileMapVisualizer);

        SpawnBossRoom();
        enemySpawner.PrepararRecompensas(roomDetector.GetRoomsList());

    }

    private List<HashSet<Vector2Int>> CreateSubBSPRooms(List<BoundsInt> roomList, int offset, int minRoomWidth, int minRoomHeight)
    {
        List<HashSet<Vector2Int>> salas = new List<HashSet<Vector2Int>>(); // lista que contem as posicoes de cada sala separadas por hash 

        HashSet<Vector2Int> floor = new HashSet<Vector2Int>(); // guarda as posicoes do chao

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

            // calcula o tamanho real do chao da sala para definir os cortes, considerando o offset
            int larguraUtil = roomBounds.size.x - (offset * 2);
            int alturaUtil = roomBounds.size.y - (offset * 2);


            // se a sala for muito pequena, nao aplica os cortes de canto para evitar que a sala suma ou fique muito estranha. O corte é baseado em 1/3 do tamanho util da sala, mas só é aplicado se a sala tiver pelo menos 3 tiles de largura e altura util para garantir que o corte faça sentido
            int maxCutX = larguraUtil >= 3 ? larguraUtil / 3 : 0;
            int maxCutY = alturaUtil >= 3 ? alturaUtil / 3 : 0;

            // Só aplicamos os cortes de canto se a sala útil for maior que 10x10 tiles
            if (maxCutX > 0 && maxCutY > 0)
            {

                // escolhe o tamanho dos cortes para a sala baseado no tamanho minimo
                int cutSizeX = Rng.DungeonRange(1, maxCutX + 1);
                int cutSizeY = Rng.DungeonRange(1, maxCutY + 1);

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
            }

            // garante que o centro e os eixos principais existam evitando q a sala suma
            Vector2Int center = (Vector2Int)Vector3Int.RoundToInt(roomBounds.center); // obtem o ponto central
            roomFloor.Add(center); // adiciona o centro de volta caso ele tenha sido removido

            floor.UnionWith(roomFloor);

            salas.Add(roomFloor);
        }
        return salas;
    }

    // Coloca o jogador no spawn e cria a saida da fase
    private void PlaceSpawnAndExit(List<Vector2Int> roomsCenters)
    {
        // Coloca o jogador na primeira Sala
        playerTransform.position = new Vector3(roomsCenters[0].x, roomsCenters[0].y, 0);

        // Cria o teleport na ultima sala
        //tileMapVisualizer.PaintExit(roomsCenters[roomsCenters.Count - 1], this); 
        tileMapVisualizer.PaintTeleport(roomsCenters[roomsCenters.Count - 1]);

    }

    // Coloca os offsets das salas para nao ficar todas coladas
    private List<HashSet<Vector2Int>> CreateSimpleRooms(List<BoundsInt> roomList)
    {
        var rooms = new List<HashSet<Vector2Int>>();
        foreach (var room in roomList)
        {
            var floor = new HashSet<Vector2Int>();
            for (int col = offset; col < room.size.x - offset; col++)
            {
                for (int row = offset; row < room.size.y - offset; row++)
                {
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    floor.Add(position);
                }
            }
            rooms.Add(floor);
        }
        return rooms;
    }

    private void SpawnBossRoom() 
    {
        bossRoomSpawner.spawnBossRoom(levelManager.GetBiomaAtual(), this, tileMapVisualizer);
    }

}
