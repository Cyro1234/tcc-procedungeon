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

    [SerializeField] float maxHealth = 3f;
    private float health;

    private bool isKnockedBack = false;

    [SerializeField] float strength = 4f;
    [SerializeField] float knockbackDuration = 0.15f;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = GameObject.Find("Player").transform;
        animator = GetComponent<Animator>();

        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (target)
        {
            if (Vector3.Distance(target.position, transform.position) < viewDistance)
            {
                animator.SetBool("isWalking", true);
                Vector3 direction = (target.position - transform.position).normalized;
                moveDirection = direction;

                animator.SetFloat("InputX", moveDirection.x);
                animator.SetFloat("InputY", moveDirection.y);
            }
            else
            {
                animator.SetBool("isWalking", false);
                animator.SetFloat("LastInputX", moveDirection.x);
                animator.SetFloat("LastInputY", moveDirection.y);

                Vector3 direction = Vector3.zero.normalized;
                moveDirection = direction;


            }

            // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // rb.rotation = angle;

        }
    }

    private void FixedUpdate()
    {
        if (isKnockedBack) return;
        if (target)
        {
            rb.linearVelocity = new Vector2(moveDirection.x, moveDirection.y) * moveSpeed;
        }
    }

    public void takeDamage(float damage, GameObject sender)
    {
        health -= damage;
        StartCoroutine(Knockback(sender));
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator Knockback(GameObject sender)
    {
        isKnockedBack = true;

        rb.linearVelocity = Vector2.zero;

        Vector2 direction = ((Vector2)transform.position - (Vector2)sender.transform.position).normalized;
        rb.AddForce(direction * strength, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }


}
