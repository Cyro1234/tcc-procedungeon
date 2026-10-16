using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyMovement))]
public class Coco : MonoBehaviour
{
    private enum Estado { Atirando, PreparandoDash, Dash, Recuperando }

    [Header("Projeteis")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField, Min(0.1f)] private float cooldown = 1.1f;
    [Tooltip("Quantidade de projeteis antes de recarregar.")]
    [SerializeField, Min(1)] private int maxAmmo = 3;
    [SerializeField, Min(0.1f)] private float reloadTime = 3f;

    [Header("Dash")]
    [SerializeField, Min(0.1f)] private float dashCooldown = 6f;
    [SerializeField, Min(0f)] private float dashPreparation = 0.4f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 14f;
    [SerializeField, Min(0.1f)] private float dashDuration = 2.5f;
    [SerializeField, Min(0f)] private float dashRecovery = 0.2f;

    [Header("Dano de contato")]
    [FormerlySerializedAs("dashDamage")]
    [SerializeField, Min(1)] private int contactDamage = 1;

    private const int MaxBounces = 3;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private EnemyMovement movement;
    private Transform player;
    private Estado estado;
    private bool combateIniciado;
    private int ammo;
    private int bounces;
    private float proximoTiro;
    private float proximoDash;
    private float fimEstado;
    private float ultimoQuicar = -1f;
    private Vector2 dashDirection;
    private RigidbodyConstraints2D originalConstraints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalConstraints = rb.constraints;
        bodyCollider = GetComponent<Collider2D>();
        movement = GetComponent<EnemyMovement>();
    }

    private void OnEnable()
    {
        movement.ExternalMovement = true;
        estado = Estado.Atirando;
        combateIniciado = false;
        bounces = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void FixedUpdate()
    {
        if (player == null || !GameManager.getDeuTeleport())
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            combateIniciado = false;
            estado = Estado.Atirando;
            return;
        }

        if (!combateIniciado)
        {
            combateIniciado = true;
            ammo = Mathf.Max(1, maxAmmo);
            proximoTiro = Time.time + reloadTime;
            proximoDash = Time.time + dashCooldown;
        }

        if (estado == Estado.Dash)
        {
            if (Time.time >= fimEstado) FinishDash();
            else rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        rb.linearVelocity = Vector2.zero;
        if (estado == Estado.PreparandoDash)
        {
            if (Time.time >= fimEstado) BeginDash();
            return;
        }
        if (estado == Estado.Recuperando)
        {
            if (Time.time >= fimEstado) estado = Estado.Atirando;
            return;
        }

        if (Time.time >= proximoDash)
        {
            estado = Estado.PreparandoDash;
            fimEstado = Time.time + dashPreparation;
            return;
        }

        if (Time.time < proximoTiro) return;
        if (ammo > 0)
        {
            ammo--;
            Shoot();
            proximoTiro = Time.time + cooldown;
        }
        else
        {
            ammo = Mathf.Max(1, maxAmmo);
            proximoTiro = Time.time + reloadTime;
        }
    }

    private void Shoot()
    {
        if (bulletPrefab == null) return;
        Vector2 origin = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : rb.position;
        Vector2 direction = ((Vector2)player.position - origin).normalized;
        if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
        GameObject bulletObj = Instantiate(bulletPrefab, origin, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null) bullet.Launch(direction);
    }

    private void BeginDash()
    {
        dashDirection = ((Vector2)player.position - rb.worldCenterOfMass).normalized;
        if (dashDirection.sqrMagnitude < 0.001f) dashDirection = Vector2.down;
        bounces = 0;
        ultimoQuicar = -1f;
        estado = Estado.Dash;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        fimEstado = Time.time + dashDuration;
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void FinishDash()
    {
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        estado = Estado.Recuperando;
        fimEstado = Time.time + dashRecovery;
        // O dash encerra a espera anterior de recarga para retomar o ataque rapidamente.
        if (ammo <= 0) ammo = Mathf.Max(1, maxAmmo);
        proximoTiro = fimEstado;
        proximoDash = Time.time + dashCooldown;
    }

    private void OnCollisionEnter2D(Collision2D collision) => HandleContact(collision);
    private void OnCollisionStay2D(Collision2D collision) => HandleContact(collision);

    private void HandleContact(Collision2D collision)
    {
        if (!GameManager.getDeuTeleport()) return;
        HeartSystem health = collision.collider.GetComponentInParent<HeartSystem>();
        if (health != null) health.takeDamage(contactDamage, transform);
        TryBounce(collision);
    }

    private void TryBounce(Collision2D collision)
    {
        if (estado != Estado.Dash || ultimoQuicar == Time.fixedTime) return;

        Collider2D other = collision.collider;
        HeartSystem health = other.GetComponentInParent<HeartSystem>();
        bool wall = other.CompareTag("Wall") || other is TilemapCollider2D || other is CompositeCollider2D ||
            other.attachedRigidbody == null || other.attachedRigidbody.bodyType == RigidbodyType2D.Static;
        if (health == null && !wall) return;

        Vector2 normal = Vector2.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 contactNormal = collision.GetContact(i).normal;
            if (Vector2.Dot(dashDirection, contactNormal) < -0.01f) normal += contactNormal;
        }
        // Contatos que continuam existindo ao se afastar nao sao novos quiques.
        if (normal.sqrMagnitude < 0.001f) return;
        ultimoQuicar = Time.fixedTime;

        // Depois dos tres quiques, o proximo impacto apenas encerra o dash.
        if (bounces >= MaxBounces)
        {
            FinishDash();
            return;
        }
        dashDirection = Vector2.Reflect(dashDirection, normal.normalized).normalized;
        bounces++;
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void OnDisable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = originalConstraints;
        }
        if (movement != null) movement.ExternalMovement = false;
        combateIniciado = false;
        estado = Estado.Atirando;
    }
}
