using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Configurações da UI")]
    public RectTransform hotbarPanel;
    public Vector2 posicaoJogo; // Posição na base da tela
    public Vector2 posicaoInventario; // Posição no centro da tela

    [Header("Slots e Dados")]
    public InventorySlot[] slots = new InventorySlot[8];
    private Chest.ItemType[] itensNoInventario = new Chest.ItemType[8];
    public int slotSelecionadoIndex = 0;
    public bool inventarioAberto = false;
    private InputSystem_Actions controles;
    private int origemTroca = -1;

    private void OnEnable()
    {
        controles = new InputSystem_Actions();
        controles.Player.Inventory.performed += AoAlternarInventario;
        controles.Player.Previous.performed += AoSelecionarAnterior;
        controles.Player.Next.performed += AoSelecionarProximo;
        controles.Player.Jump.performed += AoConfirmarTroca;
        controles.UI.Cancel.performed += AoCancelarInventario;
        controles.Player.Inventory.Enable();
        controles.Player.Previous.Enable();
        controles.Player.Next.Enable();
        controles.Player.Jump.Enable();
        controles.UI.Cancel.Enable();
    }

    private void OnDisable()
    {
        controles?.Disable();
        controles?.Dispose();
        controles = null;
    }

    private void AoAlternarInventario(InputAction.CallbackContext context)
    {
        if (inventarioAberto || Time.timeScale > 0f)
            AlternarInventario();
    }

    private void AoSelecionarAnterior(InputAction.CallbackContext context) => SelecionarSlotRelativo(-1);
    private void AoSelecionarProximo(InputAction.CallbackContext context) => SelecionarSlotRelativo(1);

    private void SelecionarSlotRelativo(int deslocamento)
    {
        if (Time.timeScale == 0f && !inventarioAberto) return;
        slotSelecionadoIndex = (slotSelecionadoIndex + deslocamento + itensNoInventario.Length) % itensNoInventario.Length;
    }

    private void AoConfirmarTroca(InputAction.CallbackContext context)
    {
        if (!inventarioAberto || !(context.control.device is Gamepad)) return;
        if (origemTroca < 0)
        {
            origemTroca = slotSelecionadoIndex;
        }
        else
        {
            TrocarItens(origemTroca, slotSelecionadoIndex);
            origemTroca = -1;
        }
    }

    private void AoCancelarInventario(InputAction.CallbackContext context)
    {
        if (!inventarioAberto || !(context.control.device is Gamepad)) return;
        if (origemTroca >= 0) origemTroca = -1;
        else AlternarInventario();
    }

    [Header("Banco de Sprites")]
    public Sprite spriteEspadaLonga;
    public Sprite spriteAdaga;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        for (int i = 0; i < itensNoInventario.Length; i++)
        {
            itensNoInventario[i] = Chest.ItemType.None;
            slots[i].slotIndex = i; // Configura o ID de cada slot automaticamente
        }
    }

    private void Update()
    {
        LidarComInputsDeTeclado();
        AtualizarUI();
    }

    private void LidarComInputsDeTeclado()
    {
        if (Keyboard.current == null || inventarioAberto || Time.timeScale == 0f) return;

        // Seleção de Slots (Teclas 1 a 8)
        if (Keyboard.current.digit1Key.wasPressedThisFrame) slotSelecionadoIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) slotSelecionadoIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) slotSelecionadoIndex = 2;
        if (Keyboard.current.digit4Key.wasPressedThisFrame) slotSelecionadoIndex = 3;
        if (Keyboard.current.digit5Key.wasPressedThisFrame) slotSelecionadoIndex = 4;
        if (Keyboard.current.digit6Key.wasPressedThisFrame) slotSelecionadoIndex = 5;
        if (Keyboard.current.digit7Key.wasPressedThisFrame) slotSelecionadoIndex = 6;
        if (Keyboard.current.digit8Key.wasPressedThisFrame) slotSelecionadoIndex = 7;
    }

    private void AlternarInventario()
    {
        inventarioAberto = !inventarioAberto;
        origemTroca = -1;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        if (inventarioAberto)
        {
            hotbarPanel.anchoredPosition = posicaoInventario;
            PauseManager.pausado = true;
            Time.timeScale = 0; // Pausa o jogo
        }
        else
        {
            hotbarPanel.anchoredPosition = posicaoJogo;
            PauseManager.pausado = false;
            Time.timeScale = 1; // Retoma o jogo
        }
    }

    public bool AdicionarItem(Chest.ItemType novoItem)
    {
        for (int i = 0; i < itensNoInventario.Length; i++)
        {
            if (itensNoInventario[i] == Chest.ItemType.None)
            {
                itensNoInventario[i] = novoItem;
                return true; // Adicionado com sucesso
            }
        }
        Debug.Log("Inventário Cheio!");
        return false;
    }

    public void TrocarItens(int indexOrigem, int indexDestino)
    {
        Chest.ItemType temp = itensNoInventario[indexDestino];
        itensNoInventario[indexDestino] = itensNoInventario[indexOrigem];
        itensNoInventario[indexOrigem] = temp;
    }

    public Chest.ItemType ObterItemSelecionado()
    {
        return itensNoInventario[slotSelecionadoIndex];
    }

    private void AtualizarUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Sprite icone = ObterSprite(itensNoInventario[i]);
            bool estaSelecionado = (i == slotSelecionadoIndex || i == origemTroca);
            slots[i].AtualizarSlot(itensNoInventario[i], icone, estaSelecionado);
        }
    }

    private Sprite ObterSprite(Chest.ItemType tipo)
    {
        if (tipo == Chest.ItemType.LongSword) return spriteEspadaLonga;
        if (tipo == Chest.ItemType.Dagger) return spriteAdaga;
        return null;
    }
}
