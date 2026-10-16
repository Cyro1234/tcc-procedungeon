using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    private PlayerStatsHandler stats;
    private GameObject player;
    private readonly HashSet<EnemyMovement> hitEnemies = new HashSet<EnemyMovement>();

    private void Awake()
    {
        player = transform.root.gameObject;
        stats = player.GetComponent<PlayerStatsHandler>();
    }

    private void OnEnable()
    {
        // A hitbox e reativada a cada golpe. Cada alvo recebe dano uma unica vez nesse golpe.
        hitEnemies.Clear();
    }

    // Quando entra na colisao
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet")) return;

        EnemyMovement enemy = collision.GetComponentInParent<EnemyMovement>();
        // Sair e entrar novamente (ou atingir outro collider do mesmo alvo) nao e um novo golpe.
        if (enemy != null && hitEnemies.Add(enemy))
        {
            enemy.takeDamage(stats.GetPlayerDamage(), player);
        }
    }
}
