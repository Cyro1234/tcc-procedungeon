using UnityEngine;

public class Weapon : MonoBehaviour
{
    private PlayerStatsHandler stats;
    private GameObject player;

    private void Awake()
    {
        player = transform.root.gameObject;
        stats = player.GetComponent<PlayerStatsHandler>();
    }

    // Quando entra na colisao
    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyMovement enemy = collision.GetComponent<EnemyMovement>(); // Tenta pegar o inimigo que entrou na colisao
        if (enemy != null)  // Se foi um inimigo
        {
            enemy.takeDamage(stats.GetPlayerDamage(), player);
        }
    }
}
