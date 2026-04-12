using System;
using System.Collections.Generic;
using Unity.Cinemachine;
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

    [SerializeField] private bool randomWalkRooms = false;

    //[SerializeField] private GameObject enemyPrefab;

    [SerializeField] private List<GameObject> ListenemyPrefab;

    [SerializeField] private int maxEnemiesPerRoom = 3;

    private List<GameObject> enemies = new List<GameObject>();

    protected override void RunProceduralGeneration()
    {
        tileMapVisualizer.Clear();
        CreateRooms();
    }

    private void Start()
    {
        RunProceduralGeneration();
    }

    private void CreateRooms()
    {
        // Limpa inimigos antes de tudo
        foreach (var enemy in enemies) 
        {
            Destroy(enemy);
        }
        enemies.Clear();

        // Obtem todas posicoes das salas geradas
        var roomList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPostion, new Vector3Int(dungeonWidth, dungeonHeight, 0)), minRoomWidth, minRoomHeight);
        
        // Coloca os offsets para que as salas fiquem um pouco distantes entre as outras
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        floor = CreateSimpleRooms(roomList);

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

    private void SpawnEnemies(List<BoundsInt> roomsList)
    {
        // Nao spawna inimigos no spawn do jogador, por isso i = 1
        for (int i = 1; i < roomsList.Count; i++)
        {
            int rng = Random.Range(0, maxEnemiesPerRoom + 1); // Quantidade de inimigos na sala
            for (int j = 0; j < rng; j++) 
            {
                // Posicao do inimigo a spawnar
                int randomX = Random.Range(roomsList[i].xMin + 5, roomsList[i].xMax - 5); // +-5 para nao spawnar na parede, pensar em uma solucao melhor eh ideal
                int randomY = Random.Range(roomsList[i].yMin + 5, roomsList[i].yMax - 5);

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
        int index = Random.Range(0, ListenemyPrefab.Count);
        Debug.Log(index);
        return ListenemyPrefab[index];
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
        var currentRoomCenter = roomsCenters[Random.Range(0, roomsCenters.Count)];
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
        }
        return corridor;
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
}
