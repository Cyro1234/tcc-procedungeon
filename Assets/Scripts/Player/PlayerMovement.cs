using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerStatsHandler stats;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    public Transform Aim;

    // Debuffs de complexos de movimento (bebum, perneta, etc) se registram aqui em vez deste script precisar saber que eles existem.
    public readonly ModifierPipeline<IMovementModifier> MovementModifiers = new ModifierPipeline<IMovementModifier>();

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

        // moveInput cru continua guiando animacao e a mira, so a velocidade real é modificada quando um debuff ou buff e ativado, assim nao muda pra onde o jogador mira.
        Vector2 moveDirection = moveInput;
        foreach (var modifier in MovementModifiers.Modifiers)
        {
            moveDirection = modifier.ModifyDirection(moveDirection, Time.deltaTime);
        }

        rb.linearVelocity = moveDirection * stats.GetPlayerWalkSpeed();

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
