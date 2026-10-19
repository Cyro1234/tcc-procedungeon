using System.Collections.Generic;
using UnityEngine;

// Uma reserva compartilhada por todos os spawners de um unico andar.
public class SpawnPlacement
{
    private readonly List<HashSet<Vector2Int>> rooms;
    private readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();

    public int RoomCount => rooms.Count;
    public int TeleportRoomIndex => rooms.Count - 1;
    private Vector2Int? posicaoInimigoTeleport;

    public SpawnPlacement(List<HashSet<Vector2Int>> rooms, IEnumerable<Vector2Int> corridors,
        IEnumerable<Vector2Int> entrances, Vector2Int playerSpawn, Vector2Int teleport)
    {
        this.rooms = rooms;
        // Mantem livres os caminhos completos, inclusive os trechos dentro das salas.
        foreach (Vector2Int cell in corridors) ReserveArea(cell, 1);
        foreach (Vector2Int cell in entrances) ReserveArea(cell, 2);
        ReserveArea(playerSpawn, 1);
        ReserveArea(teleport, 1);
        // Reserva antes dos baus, para a sala do teleporte nunca ficar sem espaco para combate.
        if (TryTakeClosestPosition(TeleportRoomIndex, teleport + Vector2Int.right * 3, out var posicao))
            posicaoInimigoTeleport = posicao;
        else
        {
            // Salas estreitas podem nao ter o bloco de piso 3x3 exigido pelo posicionamento comum.
            foreach (var candidata in Rng.OrderPositions(rooms[TeleportRoomIndex]))
            {
                if (reserved.Contains(candidata)) continue;
                posicaoInimigoTeleport = candidata;
                ReserveArea(candidata, 1);
                break;
            }
            if (!posicaoInimigoTeleport.HasValue)
                throw new System.InvalidOperationException("Sala do teleporte sem espaco seguro para um inimigo obrigatorio.");
        }
    }

    public bool TryTakeTeleportEnemyPosition(out Vector2Int position)
    {
        position = posicaoInimigoTeleport.GetValueOrDefault();
        if (!posicaoInimigoTeleport.HasValue) return false;
        posicaoInimigoTeleport = null;
        return true;
    }

    public List<Vector2Int> GetAvailablePositions(int roomIndex)
    {
        var available = new List<Vector2Int>();
        HashSet<Vector2Int> floor = rooms[roomIndex];
        foreach (Vector2Int position in floor)
        {
            if (reserved.Contains(position)) continue;
            bool inside = true;
            // O objeto precisa de piso ao redor, nao apenas na sua celula central.
            for (int x = -1; x <= 1 && inside; x++)
                for (int y = -1; y <= 1; y++)
                    if (!floor.Contains(position + new Vector2Int(x, y))) { inside = false; break; }
            if (inside) available.Add(position);
        }
        return Rng.OrderPositions(available);
    }

    public bool TryTakeRandomPosition(int roomIndex, System.Random rng, out Vector2Int position)
    {
        List<Vector2Int> available = GetAvailablePositions(roomIndex);
        if (available.Count == 0) { position = default; return false; }
        position = available[rng.Next(available.Count)];
        ReserveArea(position, 1);
        return true;
    }

    public bool TryTakeClosestPosition(int roomIndex, Vector2Int preferred, out Vector2Int position)
    {
        List<Vector2Int> available = GetAvailablePositions(roomIndex);
        if (available.Count == 0) { position = default; return false; }
        position = available[0];
        int distance = (position - preferred).sqrMagnitude;
        foreach (Vector2Int candidate in available)
        {
            int candidateDistance = (candidate - preferred).sqrMagnitude;
            if (candidateDistance >= distance) continue;
            position = candidate;
            distance = candidateDistance;
        }
        ReserveArea(position, 1);
        return true;
    }

    private void ReserveArea(Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
            for (int y = -radius; y <= radius; y++)
                reserved.Add(center + new Vector2Int(x, y));
    }
}
