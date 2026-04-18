using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class RoomDetector : MonoBehaviour
{

    private List<BoundsInt> roomsList;
    private BoundsInt? currentRoom = null;
    public Transform playerTransform;
    private List<Vector3> listaDeCentros = new List<Vector3>();
    public CinemachineCamera virtualCamera;


    public void SetRooms(List<BoundsInt> rooms) // recebe a lista do parametro e volta pro roomslist
    {
        roomsList = rooms;
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
        if (roomsList == null || playerTransform == null) return;

        Vector3Int playerPos = new Vector3Int(
            Mathf.FloorToInt(playerTransform.position.x),
            Mathf.FloorToInt(playerTransform.position.y),
            0
        );

        bool jogadorSala = false;

        foreach (var room in roomsList)
        {
            // checar se o player esta dentro da sala
            if (playerPos.x >= room.xMin && playerPos.x < room.xMax &&
                playerPos.y >= room.yMin && playerPos.y < room.yMax)
            {
                jogadorSala = true;
                if (currentRoom != room)
                {
                    currentRoom = room;
                    virtualCamera.Lens.OrthographicSize = room.size.y / 2.2f; // ajusta o zoom para caber a sala
                    Debug.Log("Jogador entrou na sala: " + room);
                    Debug.Log("tamanho da sala: " + room.size);
                }
                return;
            }
        }
        if (!jogadorSala && currentRoom != null)
        {
            currentRoom = null;
            virtualCamera.Lens.OrthographicSize = 4f;
            Debug.Log("Jogador saiu das salas (Corredor)");
        }
    }
}
