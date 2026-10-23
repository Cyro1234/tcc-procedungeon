using UnityEngine;
using UnityEngine.UI;

public class PocaoForcaHUD : MonoBehaviour
{
    private HeartSystem vida;
    private PlayerStatsHandler atributos;
    private GameObject painel;
    private RectTransform rect;
    private Image icone;
    private Text tempo;

    public void Inicializar(HeartSystem sistema, PlayerStatsHandler stats)
    {
        vida = sistema;
        atributos = stats;
        Canvas canvas = null;
        foreach (var coracao in vida.hearts)
        {
            if (coracao == null) continue;
            canvas = coracao.GetComponentInParent<Canvas>();
            if (canvas != null) break;
        }
        if (canvas == null || atributos == null) return;

        painel = new GameObject("Pocao de forca - icone", typeof(RectTransform), typeof(Image));
        painel.transform.SetParent(canvas.transform, false);
        rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1, 1);
        rect.sizeDelta = new Vector2(80, 80);
        icone = painel.GetComponent<Image>();
        icone.sprite = Cannon.CarregarArte("espada-heroi");
        icone.preserveAspect = true;
        icone.raycastTarget = false;

        var texto = new GameObject("Tempo restante", typeof(RectTransform), typeof(Text), typeof(Outline));
        texto.transform.SetParent(painel.transform, false);
        var textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchoredPosition = new Vector2(30, -30);
        textoRect.sizeDelta = new Vector2(70, 36);
        tempo = texto.GetComponent<Text>();
        tempo.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tempo.fontSize = 24;
        tempo.fontStyle = FontStyle.Bold;
        tempo.alignment = TextAnchor.MiddleCenter;
        tempo.color = PlayerStatsHandler.CorPocaoPoder;
        tempo.raycastTarget = false;
        var contorno = texto.GetComponent<Outline>();
        contorno.effectColor = Color.black;
        contorno.effectDistance = new Vector2(2, -2);
        painel.SetActive(false);
    }

    private void LateUpdate()
    {
        if (painel == null) return;
        float restante = atributos.TempoPocaoPoder;
        bool ativo = restante > 0f && !vida.IsDead;
        if (painel.activeSelf != ativo) painel.SetActive(ativo);
        if (!ativo) return;

        float y = !atributos.PossuiBotasAtivas ? -170f : vida.Penas > 0 ? -400f : -305f;
        rect.anchoredPosition = new Vector2(-85, y);
        tempo.text = Mathf.CeilToInt(restante) + "s";
        float alfa = restante <= 3f ? .35f + .65f * Mathf.PingPong(Time.time * 4f, 1f) : 1f;
        icone.color = new Color(1f, 1f, 1f, alfa);
    }

    private void OnDestroy()
    {
        if (painel != null) Destroy(painel);
    }
}
