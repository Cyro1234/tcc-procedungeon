using UnityEngine;
using UnityEngine.UI;

// Desenha apenas a moldura, sem alterar a imagem ou a transição do botão.
public class MenuSelectionOutline : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        Rect area = rectTransform.rect;
        float espessura = Mathf.Min(4f, area.width / 2f, area.height / 2f);
        AdicionarFaixa(vertices, area.xMin, area.yMin, area.xMax, area.yMin + espessura);
        AdicionarFaixa(vertices, area.xMin, area.yMax - espessura, area.xMax, area.yMax);
        AdicionarFaixa(vertices, area.xMin, area.yMin + espessura, area.xMin + espessura, area.yMax - espessura);
        AdicionarFaixa(vertices, area.xMax - espessura, area.yMin + espessura, area.xMax, area.yMax - espessura);
    }

    private void AdicionarFaixa(VertexHelper vertices, float esquerda, float baixo, float direita, float cima)
    {
        int inicio = vertices.currentVertCount;
        vertices.AddVert(new Vector3(esquerda, baixo), color, Vector2.zero);
        vertices.AddVert(new Vector3(esquerda, cima), color, Vector2.zero);
        vertices.AddVert(new Vector3(direita, cima), color, Vector2.zero);
        vertices.AddVert(new Vector3(direita, baixo), color, Vector2.zero);
        vertices.AddTriangle(inicio, inicio + 1, inicio + 2);
        vertices.AddTriangle(inicio, inicio + 2, inicio + 3);
    }
}
