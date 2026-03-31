using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ProceduralGenerationAlgorithms
{

    /*
        Usa o algoritmo Random Walk para andar no mapa e gerando posicoes que foi andadas para gerar o mapa futuramente
        Usa HashSet para remover itens duplicados
        Vector2Int vetor 2D que utiliza int ao inves de float.
     */
    public static HashSet<Vector2Int> SimpleRandomWalk(Vector2Int startPosition, int walkLength)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();

        path.Add(startPosition);
        var previousPosition = startPosition;

        for (int i = 0; i < walkLength; i++) // Iteracoes para a quantidade de passos
        {
            var newPosition = previousPosition + Direction2D.GetRandomCardinalDirection(); // Anda para uma nova posicao aleatoria
            path.Add(newPosition);
            previousPosition = newPosition;
        }

        return path; // Hash com as posicoes que foram andadas
    }


    public static List<BoundsInt> BinarySpacePartitioning(BoundsInt spaceToSplit, int minWidth, int minHeight)
    {
        // BoundsInt guarda as posicoes x,y dos cantos das salas para formar o retangulo
        Queue<BoundsInt> roomsQueue = new Queue<BoundsInt>(); // Salas que estao esperando para serem divididas
        List<BoundsInt> roomsList = new List<BoundsInt>(); // Salas feitas
        roomsQueue.Enqueue(spaceToSplit);

        while (roomsQueue.Count > 0) // Enquanto ainda tem salas possiveis de serem divididas
        {
            var room = roomsQueue.Dequeue();
            if (room.size.y >= minHeight && room.size.x >= minWidth) // Se a sala possui tamanho minimo especificado. Se nao tiver, a sala eh descartada
            {
                if (Random.value < 0.5f) // 50% de chance
                {
                    // Precisa de espaco para dividir e as duas salas criadas estejam dentro do minWidth e minHeight, por isso verifica se tem o dobro do tamanho
                    if (room.size.y >= minHeight * 2)
                    {
                        SplitHorizontally(minHeight, roomsQueue, room);
                    }
                    else if (room.size.x >= minWidth * 2) // Se nao deu pra dividir horizontalmente, tenta verticalmente
                    {
                        SplitVertically(minWidth, roomsQueue, room);
                    }
                    else // Eh uma sala porem nao eh possivel dividir 
                    { 
                        roomsList.Add(room);
                    }
                }
                else
                {
                    // Precisa de espaco para dividir e as duas salas criadas estejam dentro do minWidth e minHeight, por isso verifica se tem o dobro do tamanho
                    if (room.size.x >= minWidth * 2)
                    {
                        SplitVertically(minWidth, roomsQueue, room);
                    }
                    else if (room.size.y >= minHeight * 2) // Se nao deu pra dividir verticalmente, tenta horizontalmente
                    {
                        SplitHorizontally(minHeight, roomsQueue, room);
                    }
                    else // Eh uma sala porem nao eh possivel dividir 
                    {
                        roomsList.Add(room);
                    }
                }
            }
        }
        return roomsList; // Salas criadas
    }

    private static void SplitVertically(int minWidth, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var xSplit = Random.Range(1, room.size.x);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(xSplit, room.size.y, room.size.z));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.min.x + xSplit, room.min.y, room.min.z), new Vector3Int(room.size.x - xSplit, room.size.y, room.size.z));
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }

    private static void SplitHorizontally(int minHeight, Queue<BoundsInt> roomsQueue, BoundsInt room)
    {
        var ySplit = Random.Range(1, room.size.y);
        BoundsInt room1 = new BoundsInt(room.min, new Vector3Int(room.size.x, ySplit, room.size.z));
        BoundsInt room2 = new BoundsInt(new Vector3Int(room.min.x, room.min.y + ySplit, room.min.z), new Vector3Int(room.size.x, room.size.y - ySplit, room.size.z));
        roomsQueue.Enqueue(room1);
        roomsQueue.Enqueue(room2);
    }
}

// Usado no random walk e para criar as paredes das salas
public static class Direction2D
{
    public static List<Vector2Int> cardinalDirections = new List<Vector2Int>
    {
        new Vector2Int(0, 1), // UP
        new Vector2Int(1, 0), // RIGHT
        new Vector2Int(0, -1), // DOWN
        new Vector2Int(-1, 0) // LEFT
    };

    public static Vector2Int GetRandomCardinalDirection()
    {
        return cardinalDirections[Random.Range(0, cardinalDirections.Count)];
    }

}
