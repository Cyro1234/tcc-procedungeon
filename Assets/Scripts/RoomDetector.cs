using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class RoomDetector : MonoBehaviour
{

    private List<BoundsInt> roomsList;
    private BoundsInt? currentRoom = null;
    public Transform playerTransform;
    public bool jogadorSala = false;
    public bool inimigoSala = false;
    private List<Vector3> listaDeCentros = new List<Vector3>();
    public CinemachineCamera virtualCamera;
    private GameObject[] inimigosNoMapa;
    private bool statusInimigoAnterior = false;
    private int currentOffset;

    [SerializeField] private RoomFirstDungeonGenerator dungeonGenerator;

    public List<BoundsInt> GetRoomsList()
    {
        return roomsList;
    }

    // retorna os limites da sala atual
    public BoundsInt? GetCurrentRoomBounds()
    {
        return currentRoom;
    }

    
    public void SetRooms(List<BoundsInt> rooms, int offset) // recebe a lista do parametro e volta pro roomslist
    {
        this.roomsList = rooms;
        this.currentOffset = offset;
        listaDeCentros.Clear(); // limpa a lista antiga 

        if (roomsList == null) return;

        foreach (var room in roomsList)
        {
            // calcula o centro exato alinhado ao grid
            Vector3 center = new Vector3(room.xMin + room.size.x / 2.0f, room.yMin + room.size.y / 2.0f, 0);

            listaDeCentros.Add(center);
        }
    }

    private void OnDrawGizmos()
    {
        if (roomsList == null || listaDeCentros.Count != roomsList.Count) return;

        for (int i = 0; i < roomsList.Count; i++)
        {
            var room = roomsList[i];
            var center = listaDeCentros[i];

            // mostra vermelho se tiver dentro
            Gizmos.color = (currentRoom == room) ? Color.red : Color.green;

            // Desenha usando o centro
            Gizmos.DrawWireCube(center, (Vector3)room.size);
        }
    }

    // Update is called once per frame
    void Update()
    {
        int offset = dungeonGenerator.Offset;
        if (roomsList == null || playerTransform == null) return;

        Vector3Int playerPos = Vector3Int.FloorToInt(playerTransform.position);

        jogadorSala = false;
        inimigoSala = false;
        inimigosNoMapa = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var room in roomsList)
        {
            // area do jogador para detectar a sala (sem offset) para garantir q ele n acesse um pixel randomico e abra a sala
            if (playerPos.x >= room.xMin && playerPos.x < room.xMax &&
                playerPos.y >= room.yMin && playerPos.y < room.yMax)
            {
                // Define a sala atual para a câmera e referências
                if (currentRoom != room)
                {
                    currentRoom = room;
                    virtualCamera.Lens.OrthographicSize = room.size.y / 2.2f;
                }

                // aqui ele usa offset nos inimigos, pegando o tamanho real
                foreach (GameObject inimigo in inimigosNoMapa)
                {
                    if (inimigo == null) continue;
                    Vector3Int enemyPos = Vector3Int.FloorToInt(inimigo.transform.position);
                    if (enemyPos.x >= room.xMin + offset && enemyPos.x < room.xMax - offset &&
                        enemyPos.y >= room.yMin + offset && enemyPos.y < room.yMax - offset)
                    {
                        inimigoSala = true;
                        break;
                    }
                }

                jogadorSala = (playerPos.x >= room.xMin + offset && playerPos.x < room.xMax - offset &&
                               playerPos.y >= room.yMin + offset && playerPos.y < room.yMax - offset);

                if (inimigoSala != statusInimigoAnterior)
                {
                    statusInimigoAnterior = inimigoSala;
                    if (inimigoSala) Debug.Log($"Inimigos detectados!");
                    else Debug.Log("Sala limpa!");
                }
                return;
            }
        }

        // Se saiu da área total da sala
        if (!jogadorSala && currentRoom != null)
        {
            currentRoom = null;
            virtualCamera.Lens.OrthographicSize = 4f;
        }
    }
}
