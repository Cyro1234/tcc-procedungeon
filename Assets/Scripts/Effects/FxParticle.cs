using UnityEngine;

// Particula simples (quadradinho) usada pelo GameEffects.Burst(). Nao precisa de ParticleSystem/material.
public class FxParticle : MonoBehaviour
{
    private Vector2 velocity;
    private float lifetime;
    private float age;
    private float startScale;
    private SpriteRenderer sr;
    private Color startColor;

    public void Init(Vector2 velocity, float lifetime, float scale)
    {
        this.velocity = velocity;
        this.lifetime = Mathf.Max(0.05f, lifetime);
        startScale = scale;
        transform.localScale = Vector3.one * scale;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 90f));
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        float t = age / lifetime;

        transform.position += (Vector3)(velocity * dt);
        velocity *= 1f - Mathf.Clamp01(6f * dt);

        transform.localScale = Vector3.one * Mathf.Lerp(startScale, 0f, t * t);
        if (sr != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            sr.color = c;
        }

        if (age >= lifetime) Destroy(gameObject);
    }
}
