using UnityEngine;

public class GameManager : MonoBehaviour
{
    private RoomFirstDungeonGenerator generator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generator = GetComponent<RoomFirstDungeonGenerator>();
        generator.Setup();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
