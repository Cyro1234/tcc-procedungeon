using UnityEngine;
using UnityEngine.InputSystem;

// Nao tem invencibilidade!!! Se o jogador for atingido no meio, o empurrao do dano interrompe o dash.

// O dash passa pelo pipeline de modificadores de movimento, entao os downgrades mudam ele!!!!!!
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [Tooltip("Velocidade do dash em relacao a velocidade de andar.")]
    [SerializeField] private float speedMultiplier = 2.6f;
    [SerializeField] private float duration = 0.11f;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private Color afterImageColor = new Color(0.55f, 0.85f, 1f, 0.6f);

    private PlayerMovement movement;
    private SpriteRenderer sr;

    private Vector2 lastDirection = Vector2.down;
    private float motionStart;
    private float motionEnd;
    private float nextDashTime;
    private float nextGhostTime;

    // Momento em que o jogador esta de fato se movendo no dash (pode atrasar com o Lag)
    public bool IsDashing => Time.time >= motionStart && Time.time < motionEnd;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        sr = GetComponent<SpriteRenderer>();

        // Evita atravessar paredes finas em alta velocidade
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        if (!GameFlow.CanPlay || Time.timeScale <= 0f || movement == null || !movement.enabled || PauseManager.pausado) return;

        // Com arma de fogo, R2 usa o ataque normal (incluindo sua recarga).
        // Nenhum dos atalhos de dash pode esquivar enquanto ela estiver equipada.
        bool armaDeFogo = InventoryManager.Instance != null &&
            InventoryManager.Instance.ObterItemSelecionado() == Chest.ItemType.Cannon;
        if (armaDeFogo)
        {
            Gamepad controle = Gamepad.current;
            if (controle != null && controle.rightTrigger.wasPressedThisFrame)
                GetComponent<Attack>()?.OnAttack();
            return;
        }

        Vector2 input = movement.MoveInput;
        if (input.sqrMagnitude > 0.01f) lastDirection = input.normalized;

        if (DashPressed() && Time.time >= nextDashTime)
        {
            StartDash();
        }

        if (IsDashing && Time.time >= nextGhostTime)
        {
            nextGhostTime = Time.time + 0.03f;
            AfterImage.Spawn(sr, afterImageColor, 0.22f);
        }
    }

    private void StartDash()
    {
        float delay = movement.InputDelay;

        motionStart = Time.time + delay;
        motionEnd = motionStart + duration;
        nextDashTime = motionEnd + cooldown;
        nextGhostTime = motionStart;

        // A velocidade final vem do PlayerMovement (velocidade de andar com buffs/debuffs x esta direcao)
        movement.BeginDash(lastDirection * speedMultiplier, duration);
        GameEffects.Instance.OnDash(transform.position);
    }

    private static bool DashPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame)) return true;

        Gamepad gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame)) return true;

        return false;
    }
}
