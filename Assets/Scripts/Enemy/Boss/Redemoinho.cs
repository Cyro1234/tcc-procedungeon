using UnityEngine;

public class Redemoinho : MonoBehaviour
{

    [SerializeField] private float speed = 3f;
    [SerializeField] private float lifeTime = 10f;
    [SerializeField] private float waitTime = 2f;

    private Rigidbody2D rb;

    private Transform player;
    private bool avante = false;
    private float timeAtaque;

    private Vector2 direction;

    private void FixedUpdate()
    {
        if (avante && Time.time >= timeAtaque)
        {
            // Calcula a direção EXATAMENTE na hora que vai se mover
            if (player != null)
            {
                direction = (player.position - this.transform.position).normalized;
            }
            else
            {
                direction = Vector2.down;
            }

            rb.linearVelocity = direction * speed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            avante = false;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime + waitTime);
    }

    public void Launch(Transform newPlayer)
    {
        player = newPlayer;
        avante = true;
        timeAtaque = Time.time + waitTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Boss")) return;
        if (collision.CompareTag("Bullet")) return;
        if (collision.CompareTag("Melee")) return;

        Destroy(gameObject); // destroi ao bater na parede ou no jogador
    }
}
