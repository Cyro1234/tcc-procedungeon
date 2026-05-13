using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerStatsHandler stats;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    public Transform Aim;
    bool isWalking = false;


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
        rb.linearVelocity = moveInput * stats.GetPlayerWalkSpeed();
        if (isWalking) 
        {
            Vector3 vector3 = Vector3.left * moveInput.x + Vector3.down * moveInput.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }
    }

    // Movimentacao do jogador
    public void Move(InputAction.CallbackContext context)
    {
        
        animator.SetBool("isWalking", true);

        if (context.canceled) // Parou de andar. Soltou as teclas de movimentar
        {   // Configura para que as animacoes de idle fiquem congruentes com o movimento
            isWalking = false;
            animator.SetBool("isWalking", false);
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);

            Vector3 vector3 = Vector3.left * moveInput.x + Vector3.down * moveInput.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }
        else if (moveInput.x != 0 || moveInput.y != 0) // Ainda ta apertando teclas de movimento
        {
            isWalking = true;
        }

        moveInput = context.ReadValue<Vector2>();


        animator.SetFloat("InputX", moveInput.x);
        animator.SetFloat("InputY", moveInput.y);
    }
}
