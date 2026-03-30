using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ScriptsProcedural
{
    public static HashSet<Vector2Int> RandomWalk(Vector2Int startPosition, int walkLenght)
    {
        HashSet<Vector2Int> path = new HashSet<Vector2Int>();

        path.Add(startPosition);
        var previousPosition = startPosition;

        for(int i = 0; i < walkLenght; i++)
        {
            var newPosition = previousPosition + Direction2D.GetRandomDirection();
            path.Add(newPosition);
            previousPosition = newPosition;
        }
        return path;
    }
}

public class Direction2D
{
    public static List<Vector2Int> cardinalDirectionList = new List<Vector2Int>
    {
        new Vector2Int(0, 1), // cima
        new Vector2Int(1, 0), // direita
        new Vector2Int(0, -1), // baixo
        new Vector2Int(-1, 0), // esquerda

    };

    // escolhe aleatoriamente pra onde vai andar
    public static Vector2Int GetRandomDirection()
    {
        return cardinalDirectionList[Random.Range(0, cardinalDirectionList.Count)];
    }
}