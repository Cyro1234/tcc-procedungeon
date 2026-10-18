using UnityEngine;

// efeito do dash que fica pra tras durante o dash e some rapidinho
public class AfterImage : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color startColor;
    private float lifetime = 0.25f;
    private float age;

    public static void Spawn(SpriteRenderer source, Color tint, float lifetime)
    {
        if (source == null || source.sprite == null) return;

        var go = new GameObject("AfterImage");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder - 1;
        sr.color = tint;

        var ai = go.AddComponent<AfterImage>();
        ai.sr = sr;
        ai.startColor = tint;
        ai.lifetime = Mathf.Max(0.05f, lifetime);
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = age / lifetime;
        Color c = startColor;
        c.a = Mathf.Lerp(startColor.a, 0f, t);
        sr.color = c;
        if (age >= lifetime) Destroy(gameObject);
    }
}
