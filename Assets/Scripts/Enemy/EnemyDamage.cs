using UnityEngine;
using System.Collections;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] int damage = 1;

    private Rigidbody2D rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("IREI DAR DANO AGORA");
        HeartSystem heartSystem = collision.gameObject.GetComponent<HeartSystem>();
        if (heartSystem != null)
        {
            Debug.Log("DANDO DANO");
            heartSystem.takeDamage(damage);
        }
    }
}
