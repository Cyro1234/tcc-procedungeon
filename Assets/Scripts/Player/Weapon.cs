using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour
{
    [SerializeField, Min(0f)] private float multiplicadorEspadaLonga = 2f;
    [SerializeField, Min(0f)] private float multiplicadorAdaga = 0.6f;

    private PlayerStatsHandler stats;
    private GameObject player;
    private float multiplicadorArma = 1f;
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
        var inventario = InventoryManager.Instance;
        multiplicadorArma = inventario != null ? inventario.ObterMultiplicadorDano(inventario.ObterItemSelecionado()) : 1f;
        if (inventario != null && inventario.ObterItemSelecionado() == Chest.ItemType.LongSword)
            multiplicadorArma *= multiplicadorEspadaLonga;
        else if (inventario != null && inventario.ObterItemSelecionado() == Chest.ItemType.Dagger)
            multiplicadorArma *= multiplicadorAdaga;
    }

    // Quando entra na colisao
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!GameFlow.CanPlay) return;
        if (collision.CompareTag("Bullet")) return;

        EnemyMovement enemy = collision.GetComponentInParent<EnemyMovement>();
        // Sair e entrar novamente (ou atingir outro collider do mesmo alvo) nao e um novo golpe.
        if (enemy != null && hitEnemies.Add(enemy))
        {
            enemy.takeDamage(stats.GetPlayerDamage() * multiplicadorArma, player);
        }
    }
}
