using System.Collections.Generic;
using UnityEngine;

public static class CorridorGenerator
{
    public static HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomsCenters, DoorSpawner doorSpawner, RoomDetector roomDetector)
    {
        var corridors = new HashSet<Vector2Int>();
        var centers = new List<Vector2Int>(roomsCenters);
        if (centers.Count == 0) return corridors;
        Vector2Int current = centers[Rng.DungeonRange(0, centers.Count)];
        centers.Remove(current);
        while (centers.Count > 0)
        {
            Vector2Int closest = FindClosestPointTo(current, centers);
            centers.Remove(closest);
            List<Vector2Int> path = CriarCaminho(current, closest, roomDetector.GetRoomsList(), doorSpawner.CorridorOffset);
            foreach (Vector2Int pos in path)
            {
                corridors.Add(pos);
                doorSpawner.CheckAndAddDoor(pos, roomDetector);
            }
            current = closest;
        }
        return corridors;
    }

    // Preserva a rota original sempre que ela não percorre a borda de uma sala.
    internal static List<Vector2Int> CriarCaminho(Vector2Int origem, Vector2Int destino, List<BoundsInt> salas, int offset)
    {
        var path = CaminhoEmL(origem, destino, true);
        if (CaminhoValido(path, salas, offset)) return path;
        path = CaminhoEmL(origem, destino, false);
        if (CaminhoValido(path, salas, offset)) return path;

        // As duas curvas em L podem acompanhar paredes: nesse caso, busca o menor desvio.
        int margem = 2 * offset + 2;
        foreach (BoundsInt sala in salas)
            margem = Mathf.Max(margem, Mathf.Max(sala.size.x, sala.size.y));
        int minX = Mathf.Min(origem.x, destino.x) - margem;
        int maxX = Mathf.Max(origem.x, destino.x) + margem;
        int minY = Mathf.Min(origem.y, destino.y) - margem;
        int maxY = Mathf.Max(origem.y, destino.y) + margem;
        var fila = new Queue<Vector2Int>();
        var anteriores = new Dictionary<Vector2Int, Vector2Int> { { origem, origem } };
        fila.Enqueue(origem);
        Vector2Int[] direcoes = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };
        while (fila.Count > 0)
        {
            Vector2Int pos = fila.Dequeue();
            if (pos == destino)
            {
                path = new List<Vector2Int>();
                for (Vector2Int passo = destino; passo != origem; passo = anteriores[passo]) path.Add(passo);
                path.Add(origem);
                path.Reverse();
                return path;
            }
            foreach (Vector2Int dir in direcoes)
            {
                Vector2Int next = pos + dir;
                if (next.x < minX || next.x > maxX || next.y < minY || next.y > maxY ||
                    anteriores.ContainsKey(next) || !PassoValido(pos, next, salas, offset)) continue;
                anteriores.Add(next, pos);
                fila.Enqueue(next);
            }
        }
        throw new System.InvalidOperationException("Não foi possível conectar as salas sem percorrer suas bordas.");
    }

    private static List<Vector2Int> CaminhoEmL(Vector2Int origem, Vector2Int destino, bool verticalPrimeiro)
    {
        var path = new List<Vector2Int> { origem };
        Vector2Int pos = origem;
        for (int eixo = 0; eixo < 2; eixo++)
        {
            bool vertical = eixo == 0 ? verticalPrimeiro : !verticalPrimeiro;
            while (vertical ? pos.y != destino.y : pos.x != destino.x)
            {
                pos += vertical ? (destino.y > pos.y ? Vector2Int.up : Vector2Int.down)
                    : (destino.x > pos.x ? Vector2Int.right : Vector2Int.left);
                path.Add(pos);
            }
        }
        return path;
    }

    private static bool CaminhoValido(List<Vector2Int> path, List<BoundsInt> salas, int offset)
    {
        for (int i = 1; i < path.Count; i++)
            if (!PassoValido(path[i - 1], path[i], salas, offset)) return false;
        return true;
    }

    private static bool PassoValido(Vector2Int origem, Vector2Int destino, List<BoundsInt> salas, int offset)
    {
        foreach (BoundsInt sala in salas)
        {
            if (!PontoValido(origem, origem, destino, sala, offset) ||
                !PontoValido(destino, origem, destino, sala, offset)) return false;
        }
        return true;
    }

    private static bool PontoValido(Vector2Int pos, Vector2Int origem, Vector2Int destino, BoundsInt sala, int offset)
    {
        int left = sala.xMin + offset - 1, right = sala.xMax - offset;
        int bottom = sala.yMin + offset - 1, top = sala.yMax - offset;
        bool vertical = (pos.x == left || pos.x == right) && pos.y >= bottom && pos.y <= top;
        bool horizontal = (pos.y == bottom || pos.y == top) && pos.x >= left && pos.x <= right;
        return !(vertical && horizontal) && !(vertical && origem.x == destino.x) &&
            !(horizontal && origem.y == destino.y);
    }

    private static Vector2Int FindClosestPointTo(Vector2Int current, List<Vector2Int> centers)
    {
        Vector2Int closest = Vector2Int.zero;
        float distance = float.MaxValue;
        foreach (Vector2Int pos in centers)
        {
            float candidate = Vector2.Distance(pos, current);
            if (candidate >= distance) continue;
            closest = pos;
            distance = candidate;
        }
        return closest;
    }
}
