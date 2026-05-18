using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class SimpleRandomWalkMapGenerator : AbstractDungeonGenerator
{

    [SerializeField] protected Vector2Int startPostion = Vector2Int.zero;

    [SerializeField] private SimpleRandomWalkData randomWalkParameters;
    protected override void RunProceduralGeneration()
    {
        HashSet<Vector2Int> floorPositon = RunRandomWalk();
        tileMapVisualizer.Clear();
        tileMapVisualizer.PaintFloorTiles(floorPositon, TileMapVisualizer.Biomas.Infinito);
        WallGenerator.CreateWalls(floorPositon, tileMapVisualizer);
    }

    protected HashSet<Vector2Int> RunRandomWalk()
    {
        var currentPosition = startPostion;
        HashSet<Vector2Int> floorPosition = new HashSet<Vector2Int>();
        for (int i = 0; i < randomWalkParameters.iterations; i++)
        {
            var path = ProceduralGenerationAlgorithms.SimpleRandomWalk(currentPosition, randomWalkParameters.walkLength);
            floorPosition.UnionWith(path);
            if (randomWalkParameters.startRandomEachIteration)
            {
                currentPosition = floorPosition.ElementAt(Rng.DungeonRange(0, floorPosition.Count));
            }
        }
        return floorPosition;
    }

    public override void Setup()
    {
        return;
    }
}
