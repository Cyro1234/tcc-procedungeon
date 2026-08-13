using System.Collections.Generic;
using UnityEngine;

public static class CorridorGenerator
{
    public static HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomsCenters, DoorSpawner doorSpawner, RoomDetector roomDetector)
    {
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        // Copia a lista para nao interferir na original
        List<Vector2Int> centers = new List<Vector2Int>(roomsCenters);

        var currentRoomCenter = centers[Rng.DungeonRange(0, centers.Count)];
        centers.Remove(currentRoomCenter);

        while (centers.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, centers);
            centers.Remove(closest);
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest, doorSpawner, roomDetector);
            currentRoomCenter = closest;
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private static HashSet<Vector2Int> CreateCorridor(Vector2Int current, Vector2Int destination, DoorSpawner doorSpawner, RoomDetector roomDetector)
    {
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        var position = current;
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
            doorSpawner.CheckAndAddDoor(position, roomDetector);
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
            doorSpawner.CheckAndAddDoor(position, roomDetector);
        }
        return corridor;
    }

    private static Vector2Int FindClosestPointTo(Vector2Int current, List<Vector2Int> centers)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;

        foreach (var position in centers)
        {
            float currentDistance = Vector2.Distance(position, current);
            if (currentDistance < distance)
            {
                distance = currentDistance;
                closest = position;
            }
        }

        return closest;
    }
}