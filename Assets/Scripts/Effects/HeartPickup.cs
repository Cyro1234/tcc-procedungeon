using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Coracao que cai de inimigos. Flutua, e puxado pelo jogador quando ele chega perto
// e cura 1 de vida. Usa o mesmo sprite dos coracoes do HUD (HeartSystem.hearts[0]),
// entao nao precisa de prefab nenhum.
public class HeartPickup : MonoBehaviour
{
    private const float WorldSize = 0.6f;
    private const float Lifetime = 25f;
    private const float MagnetRadius = 2.5f;

    private static readonly List<HeartPickup> active = new List<HeartPickup>();

    private HeartSystem heartSystem;
    private SpriteRenderer sr;
    private Vector3 basePosition;
    private Vector2 popVelocity;
    private float age;
    private float baseScale;
    private bool collected;

    public static void Spawn(Vector3 position)
    {
        HeartSystem hs = FindAnyObjectByType<HeartSystem>();
        if (hs == null || hs.hearts == null || hs.hearts.Length == 0 || hs.hearts[0] == null) return;

        Image image = hs.hearts[0].GetComponent<Image>();
        Sprite sprite = image != null ? image.sprite : null;
        if (sprite == null) return;

        var go = new GameObject("HeartPickup");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        SpriteRenderer playerSr = hs.GetComponent<SpriteRenderer>();
        if (playerSr != null)
        {
            sr.sortingLayerID = playerSr.sortingLayerID;
            sr.sortingOrder = playerSr.sortingOrder - 1;
            sr.sharedMaterial = playerSr.sharedMaterial;
        }

        float spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
        float scale = WorldSize / spriteHeight;
        go.transform.localScale = Vector3.one * scale;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f / scale;

        var pickup = go.AddComponent<HeartPickup>();
        pickup.heartSystem = hs;
        pickup.sr = sr;
        pickup.baseScale = scale;
        pickup.basePosition = position;
        pickup.popVelocity = Random.insideUnitCircle.normalized * 2.5f;
    }

    public static void ClearAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (active[i] != null) Destroy(active[i].gameObject);
        }
        active.Clear();
    }

    private void OnEnable() { active.Add(this); }
    private void OnDisable() { active.Remove(this); }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        // "Pulinho" inicial ao cair do inimigo
        basePosition += (Vector3)(popVelocity * dt);
        popVelocity *= 1f - Mathf.Clamp01(5f * dt);

        // Ima: se o jogador precisa de vida e esta perto, o coracao vai ate ele
        if (heartSystem != null && !heartSystem.IsFullHealth)
        {
            Vector3 toPlayer = heartSystem.transform.position - basePosition;
            float dist = toPlayer.magnitude;
            if (dist < MagnetRadius && dist > 0.01f)
            {
                float pull = Mathf.Lerp(9f, 2f, dist / MagnetRadius);
                basePosition += toPlayer / dist * pull * dt;
            }
        }

        float bob = Mathf.Sin(age * 5f) * 0.08f;
        transform.position = basePosition + new Vector3(0f, bob, 0f);
        transform.localScale = Vector3.one * baseScale * (1f + Mathf.Sin(age * 8f) * 0.06f);

        // Pisca nos ultimos segundos antes de sumir
        if (sr != null && age > Lifetime - 4f)
        {
            sr.enabled = Mathf.Repeat(age * 8f, 1f) > 0.35f;
        }

        if (age >= Lifetime) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) { TryCollect(other); }
    private void OnTriggerStay2D(Collider2D other) { TryCollect(other); }

    private void TryCollect(Collider2D other)
    {
        if (collected || heartSystem == null) return;
        if (!other.CompareTag("Player")) return;
        if (heartSystem.IsFullHealth || heartSystem.IsDead) return;

        collected = true;
        heartSystem.Heal(1);
        Destroy(gameObject);
    }
}
