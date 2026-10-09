using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Mantém um alvo de navegação nos menus, inclusive após trocar de painel
// ou clicar no fundo com o mouse. A entrada continua no InputSystemUIInputModule.
public class GamepadMenuFocus : MonoBehaviour
{
    private MenuSelectionOutline borda;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Inicializar()
    {
        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        ConfigurarCena();
    }

    private static void AoCarregarCena(Scene cena, LoadSceneMode modo) => ConfigurarCena();

    private static void ConfigurarCena()
    {
        var sistema = EventSystem.current;
        if (sistema != null && sistema.GetComponent<GamepadMenuFocus>() == null)
            sistema.gameObject.AddComponent<GamepadMenuFocus>();
    }

    private void LateUpdate()
    {
        ExcluirInventarioDaNavegacao();
        GarantirSelecao();
        AtualizarBorda();
    }

    private static bool EhInventario(Selectable alvo)
    {
        if (alvo == null) return false;
        if (alvo.GetComponentInParent<InventorySlot>(true) != null) return true;
        var inventario = InventoryManager.Instance;
        return inventario != null && inventario.hotbarPanel != null &&
            alvo.transform.IsChildOf(inventario.hotbarPanel);
    }

    private static void ExcluirInventarioDaNavegacao()
    {
        // A navegação automática do Unity também procura botões fora do menu.
        // Mode.None remove a hotbar dessa busca sem impedir cliques ou arrastar itens.
        foreach (Selectable alvo in Selectable.allSelectablesArray)
        {
            if (!EhInventario(alvo) || alvo.navigation.mode == Navigation.Mode.None) continue;
            Navigation navegacao = alvo.navigation;
            navegacao.mode = Navigation.Mode.None;
            alvo.navigation = navegacao;
        }
    }

    private void AtualizarBorda()
    {
        var sistema = GetComponent<EventSystem>();
        GameObject selecionado = sistema != null ? sistema.currentSelectedGameObject : null;
        var alvo = selecionado != null ? selecionado.GetComponent<Selectable>() : null;
        bool mostrar = alvo != null && !EhInventario(alvo) && alvo.IsActive() && alvo.IsInteractable() &&
            (InventoryManager.Instance == null || !InventoryManager.Instance.inventarioAberto);
        if (!mostrar)
        {
            if (borda != null) borda.gameObject.SetActive(false);
            return;
        }

        if (borda == null)
        {
            var objeto = new GameObject("Borda de seleção", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(MenuSelectionOutline), typeof(LayoutElement));
            borda = objeto.GetComponent<MenuSelectionOutline>();
            borda.raycastTarget = false;
            borda.color = new Color(1f, 0.85f, 0.15f, 1f);
            objeto.GetComponent<LayoutElement>().ignoreLayout = true;
        }

        RectTransform retangulo = borda.rectTransform;
        if (retangulo.parent != alvo.transform)
        {
            retangulo.SetParent(alvo.transform, false);
            retangulo.anchorMin = Vector2.zero;
            retangulo.anchorMax = Vector2.one;
            retangulo.offsetMin = new Vector2(-6f, -6f);
            retangulo.offsetMax = new Vector2(6f, 6f);
        }
        retangulo.SetAsLastSibling();
        borda.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (borda != null) borda.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (borda != null) Destroy(borda.gameObject);
    }

    private void GarantirSelecao()
    {
        var sistema = GetComponent<EventSystem>();
        if (sistema == null || !sistema.isActiveAndEnabled) return;
        if (InventoryManager.Instance != null && InventoryManager.Instance.inventarioAberto) return;

        GameObject selecionado = sistema.currentSelectedGameObject;
        if (selecionado != null && selecionado.activeInHierarchy)
        {
            var alvo = selecionado.GetComponent<Selectable>();
            if (alvo != null && !EhInventario(alvo) && alvo.IsInteractable() &&
                alvo.navigation.mode != Navigation.Mode.None) return;
        }

        sistema.SetSelectedGameObject(null);
        Button primeiro = null;
        foreach (Selectable alvo in Selectable.allSelectablesArray)
        {
            // Barras de vida também são Sliders; somente botões iniciam a seleção.
            if (alvo is Button && !EhInventario(alvo) && alvo.IsActive() && alvo.IsInteractable() &&
                alvo.navigation.mode != Navigation.Mode.None)
            {
                var botao = (Button)alvo;
                if (primeiro == null) primeiro = botao;
                for (int i = 0; i < botao.onClick.GetPersistentEventCount(); i++)
                {
                    string metodo = botao.onClick.GetPersistentMethodName(i);
                    if (metodo == "IniciarJogo" || metodo == "ResumirJogo" || metodo == "RestartGame")
                    {
                        sistema.SetSelectedGameObject(botao.gameObject);
                        return;
                    }
                }
            }
        }
        if (primeiro != null) sistema.SetSelectedGameObject(primeiro.gameObject);
    }
}
