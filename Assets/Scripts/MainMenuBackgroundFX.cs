using UnityEngine;
using UnityEngine.UI;

public class MenuBackgroundFX : MonoBehaviour
{
    [Header("Camadas (RawImage)")]
    public RawImage gradient;
    public RawImage clouds;

    [Header("Gradiente")]
    public Color centerColor = new Color(0.55f, 0.10f, 0.55f);
    public Color edgeColor = new Color(0.10f, 0.02f, 0.13f);

    [Header("Nuvens - cores")]
    public Color shadowColor = new Color(0.10f, 0.12f, 0.30f); // miolo escuro
    public Color highlightColor = new Color(0.62f, 0.48f, 0.85f); // bordas lavanda
    [Range(0f, 1f)] public float cloudOpacity = 0.9f;

    [Header("Nuvens - forma")]
    public int cloudSize = 256;                    // 512 = mais suave, mas demora mais pra gerar
    [Range(2, 8)] public int baseFrequency = 3;    // menor = nuvens maiores
    [Range(1, 6)] public int octaves = 5;          // mais = mais detalhe fino
    [Range(0f, 2f)] public float warp = 0.8f;      // distorção: deixa com cara de fumaça
    [Range(0f, 1f)] public float cloudMin = 0.42f; // abaixo disso = sem nuvem
    [Range(0f, 1f)] public float cloudMax = 0.70f; // acima disso = nuvem cheia
    public int seed = 7;

    [Header("Movimento")]
    public Vector2 tiling = new Vector2(1.78f, 1f);
    public Vector2 scrollSpeed = new Vector2(0.01f, 0.003f);

    Vector2 offset;

    void Start()
    {
        gradient.texture = MakeGradient(256);
        clouds.texture = MakeClouds(cloudSize);
    }

    void Update()
    {
        offset += scrollSpeed * Time.unscaledDeltaTime;
        offset.x = Mathf.Repeat(offset.x, 1f);
        offset.y = Mathf.Repeat(offset.y, 1f);
        clouds.uvRect = new Rect(offset, tiling);
    }

    Texture2D MakeGradient(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x / (size - 1f) - 0.5f;
                float dy = y / (size - 1f) - 0.5f;
                float t = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / 0.6f);
                px[y * size + x] = Color.Lerp(centerColor, edgeColor, t);
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    Texture2D MakeClouds(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat; // rola sem emenda
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                // distorção: desloca as coordenadas com outro ruído
                float wx = Fbm(u, v, 2, 3, seed + 101) - 0.5f;
                float wy = Fbm(u, v, 2, 3, seed + 202) - 0.5f;
                float n = Fbm(u + wx * warp, v + wy * warp, baseFrequency, octaves, seed);

                // t = quanto "tem nuvem" naquele pixel (0 a 1, com borda macia)
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cloudMin, cloudMax, n));

                var c = Color.Lerp(highlightColor, shadowColor, t); // borda clara -> miolo escuro
                c.a = t * cloudOpacity;
                px[y * size + x] = c;
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    // Ruído fractal que se repete a cada 1.0 em u e v
    float Fbm(float u, float v, int baseFreq, int oct, int s)
    {
        float sum = 0f, amp = 0.5f, norm = 0f;
        int f = baseFreq;
        for (int o = 0; o < oct; o++)
        {
            sum += ValueNoise(u * f, v * f, f, s + o * 17) * amp;
            norm += amp;
            amp *= 0.5f;
            f *= 2;
        }
        return sum / norm;
    }

    float ValueNoise(float x, float y, int period, int s)
    {
        int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
        float fx = x - x0, fy = y - y0;
        float u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f); // interpolação suave
        float v = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);

        float a = Hash(x0, y0, period, s);
        float b = Hash(x0 + 1, y0, period, s);
        float c = Hash(x0, y0 + 1, period, s);
        float d = Hash(x0 + 1, y0 + 1, period, s);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    float Hash(int x, int y, int period, int s)
    {
        x = ((x % period) + period) % period; // é isso que faz repetir sem emenda
        y = ((y % period) + period) % period;
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 1442695041;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}