using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class DoorSpawner : MonoBehaviour
{

    [SerializeField] private AudioClip somFecharPorta;
    [SerializeField] private AudioClip somAbrirPorta;
    private int offset = 1;
    private AudioSource audioSource;

    public void setOffset(int newOffset) {  offset = newOffset; }
    public void setAudioSource(AudioSource newAudioSource) {  audioSource = newAudioSource; }

    public HashSet<Vector2Int> roomEntrances = new HashSet<Vector2Int>(); // guarda a posicao das entradas da sala
    private bool salaTrancada = false;
    private BoundsInt? currentBounds = null;

    public bool getSalaTrancada() { return salaTrancada; }
    public void setSalaTrancada(bool newSalaTrancada) { salaTrancada = newSalaTrancada; }
    public BoundsInt? getCurrentBounds() { return currentBounds; }
    public void setCurrentBounds(BoundsInt? newCurrentBounds) { currentBounds = newCurrentBounds; }

    public void FecharPortasDaSala(TileMapVisualizer tileMapVisualizer)
    {
        if (currentBounds == null) return;

        if (somFecharPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somFecharPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.PaintDoorTile(pos);
                Debug.Log("FECHANDO: X: " + pos.x + "  -  Y: " + pos.y);
            }
        }
    }

    public void AbrirPortasDaSala(TileMapVisualizer tileMapVisualizer)
    {
        if (currentBounds == null) return;

        if (somAbrirPorta != null && audioSource != null)
        {
            audioSource.PlayOneShot(somAbrirPorta, 0.3f);
        }

        foreach (var pos in roomEntrances)
        {
            // verifica se a posicao das salas usa o limite real das
            if (pos.x >= currentBounds.Value.xMin && pos.x < currentBounds.Value.xMax &&
                pos.y >= currentBounds.Value.yMin && pos.y < currentBounds.Value.yMax)
            {
                tileMapVisualizer.ClearTile(pos);
                //Debug.Log("LIMPANDO: X: " + pos.x + "  -  Y: " + pos.y);
            }
        }
        //Debug.Log("Sala limpa! Portas removidas e chão restaurado.");
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
