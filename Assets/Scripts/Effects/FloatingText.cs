using TMPro;
using UnityEngine;

// Texto que sobe, da um "pop" de escala e some. Criado pelo GameEffects.Popup().
public class FloatingText : MonoBehaviour
{
    private TextMeshPro tmp;
    private float lifetime = 0.8f;
    private float age;
    private Color baseColor;
    private Vector3 velocity = new Vector3(0f, 1.6f, 0f);

    public void Init(float lifetime)
    {
        this.lifetime = Mathf.Max(0.1f, lifetime);
        tmp = GetComponent<TextMeshPro>();
        if (tmp != null) baseColor = tmp.color;
        transform.localScale = Vector3.one * 0.4f;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;

        // Pop de escala nos primeiros 15% da vida
        float pop = t < 0.15f ? Mathf.Lerp(0.4f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.2f));
        transform.localScale = Vector3.one * pop;

        transform.position += velocity * Time.deltaTime;
        velocity *= 1f - Mathf.Clamp01(4f * Time.deltaTime);

        if (tmp != null && t > 0.6f)
        {
            Color c = baseColor;
            c.a = Mathf.Lerp(baseColor.a, 0f, (t - 0.6f) / 0.4f);
            tmp.color = c;
        }

        if (age >= lifetime) Destroy(gameObject);
    }
}
