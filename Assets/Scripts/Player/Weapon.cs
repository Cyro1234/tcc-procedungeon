using UnityEngine;

public class Weapon : MonoBehaviour
{
    private GameObject player;
    public float damage = 5f;

    private void Awake()
    {
        player = transform.root.gameObject;
    }

    // Quando entra na colisao
    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyMovement enemy = collision.GetComponent<EnemyMovement>(); // Tenta pegar o inimigo que entrou na colisao
        if (enemy != null)  // Se foi um inimigo
        {
            enemy.takeDamage(damage, player);
        }
    }

}
