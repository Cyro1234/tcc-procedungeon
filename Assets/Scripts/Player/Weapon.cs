using UnityEngine;

public class Weapon : MonoBehaviour
{
    private GameObject player;
    public float damage = 5f;

    private void Awake()
    {
        player = transform.root.gameObject;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyMovement enemy = collision.GetComponent<EnemyMovement>();
        if (enemy != null) 
        {
            enemy.takeDamage(damage, player);
        }
    }

}
