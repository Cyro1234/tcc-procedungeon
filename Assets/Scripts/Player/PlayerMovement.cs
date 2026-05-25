using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerStatsHandler stats;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    public Transform Aim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        stats = GetComponent<PlayerStatsHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = moveInput * stats.GetPlayerWalkSpeed();

        if (moveInput != Vector2.zero)
        {
            animator.SetBool("isWalking", true);
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);

            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);

            Vector3 vector3 = Vector3.left * moveInput.x + Vector3.down * moveInput.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    // Movimentacao do jogador
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void ForcarParada()
    {
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("isWalking", false);
    }
}
