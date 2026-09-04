using UnityEngine;

public class Teleporter : MonoBehaviour
{
    private bool used = false;
    private void OnTriggerEnter2D(Collider2D collision)
    {

        // Se a escada ja foi usada, nao faz nada para evitar criar multiplas dungeons em uma escada
        if (used) return;

        // Se quem entrou na colisao foi o jogador
        if (collision.CompareTag("Player"))
        {
            used = true;

            collision.transform.position = new Vector2(1000, 0);
        }
    }
}
