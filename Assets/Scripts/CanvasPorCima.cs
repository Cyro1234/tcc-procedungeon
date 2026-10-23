using UnityEngine;
using UnityEngine.UI;

// Move paineis pra um Canvas proprio em Screen Space - Overlay. O Unity desenha esse tipo de
// Canvas depois de tudo que a camera desenha (sprites, HUD, icone do caracol, textos flutuantes),
// e entre Canvas Overlay vence quem tem o sortingOrder maior. Assim o painel cobre a barra de
// vida dos bosses e a hotbar sem depender de camadas de ordenacao.
public static class CanvasPorCima
{
    // O fundo passa um pouco das bordas da tela: encostado exatamente nelas, sobrava uma fileira de
    // pixels sem cobrir (dava pra ver pedacos dos inimigos nas beiradas, por exemplo)
    private const float MargemForaDaTela = 8f;

    public static void Mover(string nomeCanvas, int ordem, params GameObject[] paineis)
    {
        Transform raiz = null;
        foreach (var painel in paineis)
        {
            if (painel == null) continue;
            Transform pai = painel.transform.parent;
            if (pai != null && pai.name == nomeCanvas) continue; // ja foi movido

            if (raiz == null) raiz = CriarCanvas(nomeCanvas, ordem, painel.GetComponentInParent<Canvas>(true));
            painel.transform.SetParent(raiz, false);

            var rect = (RectTransform)painel.transform;
            if (rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one)
            {
                rect.offsetMin = new Vector2(-MargemForaDaTela, -MargemForaDaTela);
                rect.offsetMax = new Vector2(MargemForaDaTela, MargemForaDaTela);
            }
        }
    }

    private static Transform CriarCanvas(string nome, int ordem, Canvas original)
    {
        var raiz = new GameObject(nome, typeof(RectTransform));
        var canvas = raiz.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;

        // Mesma escala do Canvas de onde o painel veio, pra os botoes e textos nao mudarem de tamanho
        var escala = raiz.AddComponent<CanvasScaler>();
        var escalaOriginal = original != null ? original.rootCanvas.GetComponent<CanvasScaler>() : null;
        if (escalaOriginal != null)
        {
            escala.uiScaleMode = escalaOriginal.uiScaleMode;
            escala.referenceResolution = escalaOriginal.referenceResolution;
            escala.screenMatchMode = escalaOriginal.screenMatchMode;
            escala.matchWidthOrHeight = escalaOriginal.matchWidthOrHeight;
            escala.scaleFactor = escalaOriginal.scaleFactor;
            escala.referencePixelsPerUnit = escalaOriginal.referencePixelsPerUnit;
            escala.physicalUnit = escalaOriginal.physicalUnit;
            escala.fallbackScreenDPI = escalaOriginal.fallbackScreenDPI;
            escala.defaultSpriteDPI = escalaOriginal.defaultSpriteDPI;
            escala.dynamicPixelsPerUnit = escalaOriginal.dynamicPixelsPerUnit;
        }

        // Sem GraphicRaycaster os botoes nao respondem ao mouse
        raiz.AddComponent<GraphicRaycaster>();
        return raiz.transform;
    }
}
