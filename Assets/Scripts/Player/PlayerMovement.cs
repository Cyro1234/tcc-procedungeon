using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private PlayerStatsHandler stats;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    public Transform Aim;

    // Ultima direcao para onde o jogador olhou, usado pelo debuff de recuo
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    // Debuffs de complexos de movimento (bebum, perneta, etc) se registram aqui em vez deste script precisar saber que eles existem.
    public readonly ModifierPipeline<IMovementModifier> MovementModifiers = new ModifierPipeline<IMovementModifier>();

    // Velocidade forcada por um curto periodo (knockback ao tomar dano)
    // Enquanto estiver ativa, o input normal nao controla o rigidbody
    private Vector2 overrideVelocity;
    private float overrideStart;
    private float overrideEnd;
    private bool overrideDecay;

    // "Empurrao" somado ao input ANTES dos modificadores de movimento (ex: Recuo do ataque)
    // Como passa pelo pipeline, os debuffs interagem com ele: o Bebum entorta o empurrao,
    // o Sonambulo continua deslizando depois dele, os Pes Escorregadios viram a direcao, etc
    private Vector2 push;
    [SerializeField] private float pushDecay = 14f; // quanto o empurrao perde por segundo

    // Direcao final depois dos modificadores que e usada pela animacao
    private Vector2 effectiveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() => GameFlow.Changed += ForcarParada;
    private void OnDisable()
    {
        GameFlow.Changed -= ForcarParada;
        ForcarParada();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        stats = GetComponent<PlayerStatsHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameFlow.CanPlay || Time.timeScale == 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (moveInput.sqrMagnitude > 0.0001f) FacingDirection = moveInput.normalized;
        UpdateAnimationAndAim();
    }

    private void FixedUpdate()
    {
        if (!GameFlow.CanPlay || Time.timeScale == 0f) return;

        // moveInput cru continua guiando a mira, so a velocidade real e modificada quando um debuff ou buff e ativado, assim nao muda pra onde o jogador mira!!
        Vector2 moveDirection = moveInput + push;
        push = Vector2.MoveTowards(push, Vector2.zero, pushDecay * Time.fixedDeltaTime);
        foreach (var modifier in MovementModifiers.Modifiers)
        {
            moveDirection = modifier.ModifyDirection(moveDirection, Time.fixedDeltaTime);
        }
        effectiveDirection = moveDirection;

        if (Time.time < overrideEnd)
        {
            float k = 1f;
            if (overrideDecay)
            {
                k = 1f - Mathf.InverseLerp(overrideStart, overrideEnd, Time.time);
            }
            rb.linearVelocity = overrideVelocity * k;
            return;
        }

        rb.linearVelocity = moveDirection * stats.GetPlayerWalkSpeed();
    }

    // A mira continua seguindo so o input cru A animacao usa o movimento real, assim o
    // Sonambulo e os Pes Escorregadios continuam "andando" mesmo sem tecla apertada
    private void UpdateAnimationAndAim()
    {
        if (moveInput != Vector2.zero)
        {
            Vector3 vector3 = Vector3.left * moveInput.x + Vector3.down * moveInput.y;
            Aim.rotation = Quaternion.LookRotation(Vector3.forward, vector3);
        }

        bool moving = moveInput != Vector2.zero || effectiveDirection.sqrMagnitude > 0.0001f;
        animator.SetBool("isWalking", moving);
        if (!moving) return;

        // Sendo empurrado pelo recuo: continua olhando pra frente, fazendo a animacao fazer o alex andar de costas wow
        Vector2 animDirection = moveInput != Vector2.zero ? moveInput
            : (push.sqrMagnitude > 0.01f ? FacingDirection : effectiveDirection.normalized);

        animator.SetFloat("InputX", animDirection.x);
        animator.SetFloat("InputY", animDirection.y);
        animator.SetFloat("LastInputX", animDirection.x);
        animator.SetFloat("LastInputY", animDirection.y);
    }

    // Empurra o jogador "como se fosse input". Passa pelos modificadores de movimento!!
    public void AddPush(Vector2 amount)
    {
        push += amount;
    }

    // Forca uma velocidade por "duration" segundos. decay = true faz ela diminuir ate zero (bom pra knockback).
    public void OverrideVelocity(Vector2 velocity, float duration, bool decay)
    {
        overrideVelocity = velocity;
        overrideStart = Time.time;
        overrideEnd = Time.time + duration;
        overrideDecay = decay;
    }

    // Movimentacao do jogador
    public void Move(InputAction.CallbackContext context)
    {
        if (!GameFlow.CanPlay) { ForcarParada(); return; }
        moveInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
    }
    public void ForcarParada()
    {
        moveInput = Vector2.zero;
        push = Vector2.zero;
        effectiveDirection = Vector2.zero;
        overrideEnd = 0f;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("isWalking", false);
    }
}
