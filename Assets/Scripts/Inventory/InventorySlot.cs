using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    public Image itemIcon;
    public Image highlightImage;

    private Transform originalParent;
    private Vector3 originalPosition;

    // NOVO: Referência segura para o Canvas principal
    private Canvas canvas;

    private void Start()
    {
        // Busca o Canvas onde a UI está renderizada assim que o jogo começa
        canvas = GetComponentInParent<Canvas>();
    }

    public void AtualizarSlot(Chest.ItemType tipoItem, Sprite icone, bool selecionado)
    {
        if (tipoItem == Chest.ItemType.None)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        else
        {
            itemIcon.sprite = icone;
            itemIcon.enabled = true;
        }

        highlightImage.enabled = selecionado;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!InventoryManager.Instance.inventarioAberto || itemIcon.sprite == null) return;

        originalParent = itemIcon.transform.parent;
        originalPosition = itemIcon.transform.position;

        // NOVO: Movemos o ícone para o Canvas de forma segura (true mantém a escala original)
        itemIcon.transform.SetParent(canvas.transform, true);

        // Coloca a imagem como a última da lista para renderizar por cima de tudo
        itemIcon.transform.SetAsLastSibling();
        itemIcon.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!InventoryManager.Instance.inventarioAberto || itemIcon.sprite == null) return;

        // NOVO: A forma matemática correta e à prova de falhas de mover UI no Unity
        Vector3 globalMousePos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out globalMousePos))
        {
            itemIcon.transform.position = globalMousePos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!InventoryManager.Instance.inventarioAberto || itemIcon.sprite == null) return;

        // Devolve o item para o botão de origem (true mantém a escala original)
        itemIcon.transform.SetParent(originalParent, true);
        itemIcon.transform.position = originalPosition;
        itemIcon.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryManager.Instance.inventarioAberto) return;

        InventorySlot slotArrastado = eventData.pointerDrag.GetComponent<InventorySlot>();

        // NOVO: Adicionado um teste de segurança (slotArrastado != this) para evitar que ele troque o item com ele mesmo
        if (slotArrastado != null && slotArrastado != this)
        {
            InventoryManager.Instance.TrocarItens(slotArrastado.slotIndex, this.slotIndex);

            // Toca um som ou efeito opcional ao soltar, se desejar.
            Debug.Log($"Trocou o item do slot {slotArrastado.slotIndex} para o {this.slotIndex}!");
        }
    }
}