using System.Collections;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{

    public float moveSpeed = 2f;
    Rigidbody2D rb;
    Transform target;
    Vector2 moveDirection;
    private Animator animator;

    [SerializeField] int viewDistance = 12;

    // TODO: Colocar a vida do inimigo em um script separado para deixar mais organizado
    [SerializeField] float maxHealth = 3f;
    private float health;

    private bool isKnockedBack = false;

    [SerializeField] float strength = 4f;
    [SerializeField] float knockbackDuration = 0.15f;

    [SerializeField] private AudioClip damageSound;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        target = GameObject.Find("Player").transform;
        animator = GetComponent<Animator>();

        health = maxHealth;

    }

    // Update is called once per frame
    void Update() // Calcula pra onde o inimigo deve andar
    {
        if (target) // Se tiver um jogador para seguir
        {
            if (Vector3.Distance(target.position, transform.position) < viewDistance) // Verifica se o jogador esta na distancia de visao do inimigo
            { // Mover em direcao ao jogador
                // Animacao walk
                animator.SetBool("isWalking", true);
                Vector3 direction = (target.position - transform.position).normalized;
                moveDirection = direction;

                animator.SetFloat("InputX", moveDirection.x);
                animator.SetFloat("InputY", moveDirection.y);
            }
            else
            { // Fica parado
                // Animacao idle
                animator.SetBool("isWalking", false);
                animator.SetFloat("LastInputX", moveDirection.x);
                animator.SetFloat("LastInputY", moveDirection.y);

                Vector3 direction = Vector3.zero.normalized;
                moveDirection = direction;

            }

            // Roda o inimigo. NAO UTILIZADO NESSES SPRITES
            // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // rb.rotation = angle;

        }
    }

    private void FixedUpdate() // Move o inimigo para o local calculado em Update() guardado na variavel moveDirection
    {
        if (isKnockedBack) return; // Se nao tiver isso, a movimentacao do inimigo ira cancelar o knockback
        if (target)
        {
            rb.linearVelocity = new Vector2(moveDirection.x, moveDirection.y) * moveSpeed;
        }
    }

    public void takeDamage(float damage, GameObject sender)
    {
        health -= damage;
        StartCoroutine(Knockback(sender));

        // Som
        AudioSource.PlayClipAtPoint(damageSound, transform.position, 1f);

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Knockback(GameObject sender)
    {
        isKnockedBack = true;

        rb.linearVelocity = Vector2.zero;

        Vector2 direction = ((Vector2)transform.position - (Vector2)sender.transform.position).normalized; // Calcula pra onde fazer o knockback dependendo da posicao do dano
        rb.AddForce(direction * strength, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration); // Espera alguns frames para que tenha o efeito de knockback, impedindo tomar mais de 1 knockback com o mesmo dano

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }


}
