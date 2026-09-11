using UnityEngine;

public class Bullet : MonoBehaviour
{

    [SerializeField] private float speed = 4f;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boss")) return;
        if (collision.CompareTag("Melee")) return;

        //if (collision.CompareTag("Player"))
        //{
        //    // Dar dano no jogador
        //}

        Destroy(gameObject); // destroi ao bater na parede ou no jogador
    }
}
