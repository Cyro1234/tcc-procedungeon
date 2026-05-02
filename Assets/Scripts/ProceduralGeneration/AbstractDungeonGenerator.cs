using UnityEngine;

public abstract class AbstractDungeonGenerator : MonoBehaviour
{
    [SerializeField] protected TileMapVisualizer tileMapVisualizer;
    [SerializeField] protected Vector2 startPosition = Vector2.zero;


    public void GenerateDungeon() 
    { 
        tileMapVisualizer.Clear();
        RunProceduralGeneration();
    }

    public abstract void Setup();

    protected abstract void RunProceduralGeneration();
}
