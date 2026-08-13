using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class DoorSpawner : MonoBehaviour
{

    // Setup
    [SerializeField] private RoomDetector roomDetector;
    [SerializeField] private TileMapVisualizer tileMapVisualizer;

    [SerializeField] private AudioClip somFecharPorta;
    [SerializeField] private AudioClip somAbrirPorta;
    private int offset = 1;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // Inscreve no evento do room detector
    private void OnEnable()
    {
        if (roomDetector != null)
        {
            roomDetector.AoEntrarNaSalaComInimigos += FecharPortasDaSala;
            roomDetector.AoLimparSala += AbrirPortasDaSala;
        }
    }

    private void OnDisable()
    {
        if (roomDetector != null)
        {
            roomDetector.AoEntrarNaSalaComInimigos -= FecharPortasDaSala;
            roomDetector.AoLimparSala -= AbrirPortasDaSala;
        }
    }

    public void setOffset(int newOffset) {  offset = newOffset; }

    public HashSet<Vector2Int> roomEntrances = new HashSet<Vector2Int>(); // guarda a posicao das entradas da sala
    private bool salaTrancada = false;
    private BoundsInt? currentBounds = null;

    public bool getSalaTrancada() { return salaTrancada; }
    public void setSalaTrancada(bool newSalaTrancada) { salaTrancada = newSalaTrancada; }
    public BoundsInt? getCurrentBounds() { return currentBounds; }
    public void setCurrentBounds(BoundsInt? newCurrentBounds) { currentBounds = newCurrentBounds; }

    private void FecharPortasDaSala(BoundsInt bounds)
    {
        if (salaTrancada) return; // Se já tá trancada, ignora

        currentBounds = bounds;
        salaTrancada = true;

        if (somFecharPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somFecharPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.PaintDoorTile(pos);
            }
        }
    }

    private void AbrirPortasDaSala()
    {
        if (!salaTrancada || currentBounds == null) return;

        if (somAbrirPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somAbrirPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.ClearTile(pos);
            }
        }

        salaTrancada = false;
        currentBounds = null; // Limpa a sala atual
    }


    public void CheckAndAddDoor(Vector2Int pos, RoomDetector roomDetector)
    {
        foreach (var room in roomDetector.GetRoomsList())
        {
            // define os limites onde as paredes da sala realmente existem (tava spawnando deslocado)
            int left = room.xMin + offset - 1;
            int right = room.xMax - offset;
            int bottom = room.yMin + offset - 1;
            int top = room.yMax - offset;

            bool naBordaVertical = (pos.x == left || pos.x == right) && (pos.y >= room.yMin + offset && pos.y < room.yMax - offset);
            bool naBordaHorizontal = (pos.y == bottom || pos.y == top) && (pos.x >= room.xMin + offset && pos.x < room.xMax - offset);

            if (naBordaVertical || naBordaHorizontal)
            {
                roomEntrances.Add(pos);
                // Debug.Log($"Porta registada em: {pos}");
            }
        }
    }

    public void CleanDoor()
    {
        roomEntrances.Clear();
        salaTrancada = false;
    }
}
