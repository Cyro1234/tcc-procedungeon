using UnityEngine;
using UnityEngine.UIElements;

public class RaioSeguro : MonoBehaviour
{
    private SpriteRenderer mesh;
    private Color currentColor;
    private float alpha = 1.0f;
    private bool subindo = true;

    private void Start()
    {
        mesh = GetComponent<SpriteRenderer>();
        currentColor = mesh.material.color;
    }

    private void FixedUpdate()
    {
        currentColor.a = alpha;
        mesh.material.color = currentColor;

        if (subindo)
        {
            alpha += 0.05f;
            if (alpha >= 1.0f) 
            { 
                alpha = 1.0f;
                subindo = false;
            }
        }
        else
        {
            alpha -= 0.05f;
            if (alpha <= 0.0f)
            {
                alpha = 0.0f;
                subindo = true;
            }
        }

    }
}
