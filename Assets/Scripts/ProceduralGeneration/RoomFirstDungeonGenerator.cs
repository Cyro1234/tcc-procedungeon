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

    [SerializeField] private bool subBSPRooms = false;

    //[SerializeField] private GameObject enemyPrefab;

    //[SerializeField] private List<GameObject> ListenemyPrefab;
    [SerializeField] private WeightedTable<GameObject> enemyTable;

    [SerializeField] private int maxEnemiesPerRoom = 3;

    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 0;

    [SerializeField] private RoomDetector roomDetector;
    [SerializeField] private bool focarCentroSala = true;
    public bool FocarCentroSala => focarCentroSala;

    [SerializeField] private AudioClip somFecharPorta;
    [SerializeField] private AudioClip somAbrirPorta;
    private AudioSource audioSource;

    [SerializeField] private int BaixoNivel = 1;
    [SerializeField] private int MedioNivel = 2;

    [Header("Chest Settings")]
    [SerializeField] private GameObject chestPrefab;
    [SerializeField] private bool randomStartingChest = false; // Define se o baú inicial é sorteado
    [SerializeField] private Chest.ItemType startingChestItem = Chest.ItemType.Shield; // Escolha do item manual

    //Tabela de pesos para os itens do baú
    [SerializeField] private WeightedTable<Chest.ItemType> itemTable;

    private List<GameObject> enemies = new List<GameObject>();
    private HashSet<Vector2Int> roomEntrances = new HashSet<Vector2Int>(); // guarda a posicao das entradas da sala
    private bool salaTrancada = false;
    private BoundsInt? currentBounds = null;

    private int andar = 0; // Andar que o jogador esta presente

    //private GameObject currentChest;
    //Lista para guardar e limpar todos os baús do andar
    private List<GameObject> spawnedChests = new List<GameObject>();

    protected override void RunProceduralGeneration()
    {
        andar++;
        Debug.Log("ANDAR: " + andar);
        tileMapVisualizer.Clear();
        tileMapVisualizer.Setup(GetNivelAtual());
        CreateRooms();
    }

    //private void Start()
    //{
    //    Debug.Log("START RODOU");
    //    if (useRandomSeed)
    //    {
    //        seed = GenerateRandomSeed();
    //    }

    //    Rng.Init(seed);

    //    Debug.Log("SEED: " + seed);

    //    RunProceduralGeneration();
    //}

    public override void Setup()
    {
        andar = 0;
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

        // Limpa o baú da fase anterior
        //if (currentChest != null)
        //{
        //    Destroy(currentChest);
        //}

        //Limpa todos os baús do andar anterior
        foreach (var chest in spawnedChests)
        {
            if (chest != null) Destroy(chest);
        }
        spawnedChests.Clear();

        // Obtem todas posicoes das salas geradas
        var roomList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPostion, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);

        if (roomDetector != null) roomDetector.SetRooms(roomList, offset);

        // Coloca os offsets para que as salas fiquem um pouco distantes entre as outras
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();

        List<HashSet<Vector2Int>> salas = new List<HashSet<Vector2Int>>();

        if (subBSPRooms)
        {
            salas = CreateSubBSPRooms(roomList, offset, minRoomWidth, minRoomHeight); // deixei o offset no 0 pra ficar grudado
            foreach (var sala in salas)
            {
                floor.UnionWith(sala);
            }
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

        if (subBSPRooms)
        {
            SpawnEnemies(salas);
            SpawnProceduralChests(salas);
        }
        else
        {
            SpawnEnemies(roomList);
            SpawnProceduralChests(roomList);
        }

        // Conectar salas com corredores
        HashSet<Vector2Int> corridors = ConnectRooms(roomsCenters);
        floor.UnionWith(corridors);

        // Coloca o chao e paredes
        tileMapVisualizer.PaintFloorTiles(floor, GetNivelAtual());
        WallGenerator.CreateWalls(floor, tileMapVisualizer);

    }

    private TileMapVisualizer.Niveis GetNivelAtual()
    {
        if (andar <= BaixoNivel)
        {
            return TileMapVisualizer.Niveis.Baixo;
        }
        else if (andar <= MedioNivel)
        {
            return TileMapVisualizer.Niveis.Medio;
        }
        else
        {
            return TileMapVisualizer.Niveis.Alto;
        }
    }

    private List<HashSet<Vector2Int>> CreateSubBSPRooms(List<BoundsInt> roomList, int offset, int minRoomWidth, int minRoomHeight)
    {
        List<HashSet<Vector2Int>> salas = new List<HashSet<Vector2Int>>(); // lista que contem as posicoes de cada sala separadas por hash 

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

            salas.Add(roomFloor);
        }
        return salas;
    }

    bool EhParede(Vector2Int pos, HashSet<Vector2Int> floor)
    {
        return !floor.Contains(pos + Vector2Int.up) ||
               !floor.Contains(pos + Vector2Int.down) ||
               !floor.Contains(pos + Vector2Int.left) ||
               !floor.Contains(pos + Vector2Int.right);
    }

    private void SpawnEnemies(List<HashSet<Vector2Int>> roomsList) // NOVO SPAWN DE INIMIGOS PARA O SUB BSP
    {
        for (int i = 1; i < roomsList.Count; i++)
        {
            var roomTiles = roomsList[i];

            // Filtra posicoes que nao estejam na parede para impedir spawnar inimigos dentro de paredes
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            foreach (var pos in roomTiles)
            {
                if (!EhParede(pos, roomTiles))
                {
                    availablePositions.Add(pos);
                }
            }

            int enemyCount = Rng.EnemyRange(0, maxEnemiesPerRoom + 1); // Quantidade de inimigos na sala

            for (int j = 0; j < enemyCount && availablePositions.Count > 0; j++)
            {
                int index = Rng.EnemyRange(0, availablePositions.Count);
                Vector2Int pos = availablePositions[index];

                availablePositions.RemoveAt(index); // evita repetir posição

                GameObject enemy = Instantiate(getRandomEnemy(), new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                enemies.Add(enemy);
            }
        }

        Debug.Log("SPAWNOU " + enemies.Count); // Quantidade de inimigos spawnadas
    }


    private void SpawnEnemies(List<BoundsInt> roomsList) // USADO QUANDO NAO TEM SUBBSP
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

    private GameObject getRandomEnemy()
    {
        return enemyTable.getRandom(Rng.enemyRng);
    }

    //SISTEMA DE GERAÇÃO PROCEDURAL DE BAÚS
    // Usado quando está gerando com SubBSP
    private void SpawnProceduralChests(List<HashSet<Vector2Int>> roomsList)
    {
        if (roomsList.Count <= 1 || chestPrefab == null) return; // Só tem a sala inicial ou falta prefab

        int qtdBaus = Rng.DungeonRange(0, 3); // Decide se terá 0, 1 ou 2 baús espalhados
        System.Random chestRng = new System.Random(seed + andar); // Semente para a tabela de pesos

        for (int i = 0; i < qtdBaus; i++)
        {
            // Escolhe uma sala aleatória (começa do 1 para ignorar a sala de Spawn)
            int roomIndex = Rng.DungeonRange(1, roomsList.Count);
            var roomTiles = roomsList[roomIndex];

            // Filtra posicoes que não sejam paredes
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            foreach (var pos in roomTiles)
            {
                if (!EhParede(pos, roomTiles)) availablePositions.Add(pos);
            }

            if (availablePositions.Count > 0)
            {
                int posIndex = Rng.DungeonRange(0, availablePositions.Count);
                Vector2Int pos = availablePositions[posIndex];

                GameObject chest = Instantiate(chestPrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
                Chest chestScript = chest.GetComponent<Chest>();

                if (chestScript != null && itemTable.items.Count > 0)
                {
                    // Rola o dado na tabela de pesos e injeta o item no baú!
                    Chest.ItemType sortedItem = itemTable.getRandom(chestRng);
                    chestScript.ConfigurarItem(sortedItem);
                }

                spawnedChests.Add(chest); // Registra para destruir depois
            }
        }
    }

    // Usado quando NÃO está gerando com SubBSP (Salas Simples)
    private void SpawnProceduralChests(List<BoundsInt> roomsList)
    {
        if (roomsList.Count <= 1 || chestPrefab == null) return;

        int qtdBaus = Rng.DungeonRange(0, 3); // 0, 1 ou 2
        System.Random chestRng = new System.Random(seed + andar);

        for (int i = 0; i < qtdBaus; i++)
        {
            int roomIndex = Rng.DungeonRange(1, roomsList.Count);
            BoundsInt room = roomsList[roomIndex];

            // Garante que o baú não nasce colado na parede (+2 e -2 de margem)
            int randomX = Rng.DungeonRange(room.xMin + 2, room.xMax - 2);
            int randomY = Rng.DungeonRange(room.yMin + 2, room.yMax - 2);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0);

            GameObject chest = Instantiate(chestPrefab, spawnPos, Quaternion.identity);
            Chest chestScript = chest.GetComponent<Chest>();

            if (chestScript != null && itemTable.items.Count > 0)
            {
                Chest.ItemType sortedItem = itemTable.getRandom(chestRng);
                chestScript.ConfigurarItem(sortedItem);
            }

            spawnedChests.Add(chest);
        }
    }

    // Coloca o jogador no spawn e cria a saida da fase
    private void PlaceSpawnAndExit(List<Vector2Int> roomsCenters)
    {
        // Coloca o jogador na primeira Sala
        GameObject player = GameObject.FindWithTag("Player");
        player.transform.position = new Vector3(roomsCenters[0].x, roomsCenters[0].y, 0);

        // Instancia o Baú na primeira sala (com um offset de +1 no X para não nascer em cima do jogador)
        if (chestPrefab != null)
        {
            Vector3 chestPosition = new Vector3(roomsCenters[0].x + 1.5f, roomsCenters[0].y, 0);
            //currentChest = Instantiate(chestPrefab, chestPosition, Quaternion.identity);
            GameObject initialChest = Instantiate(chestPrefab, chestPosition, Quaternion.identity);

            // Pega o script do baú que acabou de nascer para configurá-lo
            //Chest chestScript = initialChest.GetComponent<Chest>();
            //if (chestScript != null)
            //{
            //    chestScript.isRandomItem = randomStartingChest;
            //    // Se não for aleatório, coloca o item que você escolheu no Inspector
            //    if (!randomStartingChest)
            //    {
            //        chestScript.itemInside = startingChestItem;
            //    }
            //}
            //Debug.Log("Baú instanciado na sala inicial.");

            Chest chestScript = initialChest.GetComponent<Chest>();
            if (chestScript != null)
            {
                if (!randomStartingChest)
                {
                    chestScript.ConfigurarItem(startingChestItem); // Item Manual
                }
                else if (itemTable.items.Count > 0)
                {
                    // Item Sorteado pela Tabela de Pesos!
                    System.Random rng = new System.Random(seed + andar);
                    chestScript.ConfigurarItem(itemTable.getRandom(rng));
                }
            }

            spawnedChests.Add(initialChest); // Adiciona na lista de limpeza
            Debug.Log("Baú instanciado na sala inicial.");

        }

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
        if (roomDetector == null) return;


        if (roomDetector.jogadorSala && roomDetector.inimigoSala && !salaTrancada) // se passar e tiver inimigos, tranca a sala
        {
            currentBounds = roomDetector.GetCurrentRoomBounds();
            FecharPortasDaSala();
            salaTrancada = true;
        }
        else if (!roomDetector.inimigoSala && salaTrancada) // se não houver inimigos e a sala estiver trancada, abre a sala
        {
            AbrirPortasDaSala();
            salaTrancada = false;
            currentBounds = null;
        }
    }


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Se não houver um AudioSource no objeto, adiciona um automaticamente
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void FecharPortasDaSala()
    {
        if (currentBounds == null) return;

        if (somFecharPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somFecharPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.PaintDoorTile(pos);
                Debug.Log("FECHANDO: X: " + pos.x + "  -  Y: " + pos.y);
            }
        }
    }

    private void AbrirPortasDaSala()
    {
        if (currentBounds == null) return;

        if (somAbrirPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somAbrirPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.ClearTile(pos);
                //Debug.Log("LIMPANDO: X: " + pos.x + "  -  Y: " + pos.y);
            }
        }
        //Debug.Log("Sala limpa! Portas removidas e chão restaurado.");
    }
}
