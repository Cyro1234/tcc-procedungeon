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
    public bool inventarioAberto { get; private set; }
    public bool PodeEditarInventario => inventarioAberto && GameFlow.Current == GameFlow.State.Inventory;
    private InputActionAsset controles;
    private int origemTroca = -1;

    // Indice do ultimo slot que recebeu um item ou -1 se nenhum slot recebeu item recentemente
    public int UltimoSlotAdicionado { get; private set; } = -1;

    private void OnEnable()
    {
        GameFlow.Changed += AoAlterarEstado;
        controles = GameFlow.PlayerActions;
        if (controles != null)
        {
            LigarAcao("Player/Inventory", AoAlternarInventario);
            LigarAcao("Player/Previous", AoSelecionarAnterior);
            LigarAcao("Player/Next", AoSelecionarProximo);
            LigarAcao("Player/Jump", AoConfirmarTroca);
            LigarAcao("UI/Cancel", AoCancelarInventario);
        }
    }

    private void OnDisable()
    {
        GameFlow.Changed -= AoAlterarEstado;
        if (controles != null)
        {
            controles.FindAction("Player/Inventory", true).performed -= AoAlternarInventario;
            controles.FindAction("Player/Previous", true).performed -= AoSelecionarAnterior;
            controles.FindAction("Player/Next", true).performed -= AoSelecionarProximo;
            controles.FindAction("Player/Jump", true).performed -= AoConfirmarTroca;
            controles.FindAction("UI/Cancel", true).performed -= AoCancelarInventario;
        }
        FecharInventario();
        controles = null;
    }

    private void LigarAcao(string nome, System.Action<InputAction.CallbackContext> callback)
    {
        InputAction acao = controles.FindAction(nome, true);
        acao.performed += callback;
        acao.Enable();
    }

    private void AoAlterarEstado()
    {
        if (inventarioAberto && GameFlow.Current != GameFlow.State.Inventory) FecharInventario();
    }

    private void AoAlternarInventario(InputAction.CallbackContext context)
    {
        if (PodeEditarInventario || GameFlow.CanPlay)
            AlternarInventario();
    }

    private void AoSelecionarAnterior(InputAction.CallbackContext context) => SelecionarSlotRelativo(-1);
    private void AoSelecionarProximo(InputAction.CallbackContext context) => SelecionarSlotRelativo(1);

    private void SelecionarSlotRelativo(int deslocamento)
    {
        if (!GameFlow.CanPlay && !PodeEditarInventario) return;
        slotSelecionadoIndex = (slotSelecionadoIndex + deslocamento + itensNoInventario.Length) % itensNoInventario.Length;
    }

    private void AoConfirmarTroca(InputAction.CallbackContext context)
    {
        if (!PodeEditarInventario || !(context.control.device is Gamepad)) return;
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
        if (!PodeEditarInventario || !(context.control.device is Gamepad)) return;
        if (origemTroca >= 0) origemTroca = -1;
        else AlternarInventario();
    }

    [Header("Banco de Sprites")]
    public Sprite spriteEspadaLonga;
    public Sprite spriteAdaga;
    public Sprite spritePocaoVida;
    private Sprite spriteProvisorio;

    public bool UsarItemSelecionado()
    {
        if (!GameFlow.CanPlay || Time.timeScale == 0f ||
            ObterItemSelecionado() != Chest.ItemType.HealthPotion) return false;
        var vida = FindAnyObjectByType<HeartSystem>();
        if (vida == null || vida.IsDead || vida.IsFullHealth) return false;
        vida.RestaurarVidaInteira();
        itensNoInventario[slotSelecionadoIndex] = Chest.ItemType.None;
        AtualizarUI();
        return true;
    }

    private void OnDestroy()
    {
        if (spriteProvisorio != null) Destroy(spriteProvisorio);
        if (Instance == this) Instance = null;
    }

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
        if (Keyboard.current == null || !GameFlow.CanPlay || Time.timeScale == 0f) return;

        // Rodinha do mouse troca de slot
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll > 0.01f) SelecionarSlotRelativo(-1);
            else if (scroll < -0.01f) SelecionarSlotRelativo(1);
        }

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
        if (inventarioAberto) { FecharInventario(); return; }
        if (!GameFlow.CanPlay) return;
        inventarioAberto = true;
        origemTroca = -1;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        hotbarPanel.anchoredPosition = posicaoInventario;
        GameFlow.Pause(GameFlow.State.Inventory);
    }

    public void FecharInventario()
    {
        inventarioAberto = false;
        origemTroca = -1;
        foreach (InventorySlot slot in slots) if (slot != null) slot.CancelarArraste();
        if (hotbarPanel != null) hotbarPanel.anchoredPosition = posicaoJogo;
        GameFlow.Resume(GameFlow.State.Inventory);
    }

    public bool AdicionarItem(Chest.ItemType novoItem)
    {
        // Equipamentos passivos nao ocupam slots, mesmo quando recebidos por este metodo.
        if (novoItem == Chest.ItemType.SpeedBoots || novoItem == Chest.ItemType.LegendarySpeedBoots)
        {
            var atributos = FindAnyObjectByType<PlayerStatsHandler>();
            if (atributos == null) return false;
            if (novoItem == Chest.ItemType.LegendarySpeedBoots) atributos.EquiparBotaLendaria();
            else atributos.EquiparBota();
            UltimoSlotAdicionado = -1;
            return true;
        }
        // Equipamentos passivos nao ocupam slots, mesmo quando recebidos por este metodo.
        //if (novoItem == Chest.ItemType.SpeedBoots)
        //{
        //    var atributos = FindAnyObjectByType<PlayerStatsHandler>();
        //    if (atributos == null) return false;
        //    atributos.EquiparBota();
        //    UltimoSlotAdicionado = -1;
        //    return true;
        //}
        for (int i = 0; i < itensNoInventario.Length; i++)
        {
            if (itensNoInventario[i] == Chest.ItemType.None)
            {
                itensNoInventario[i] = novoItem;
                UltimoSlotAdicionado = i;

                // Se a mao estava vazia, ja equipa o item novo
                if (itensNoInventario[slotSelecionadoIndex] == Chest.ItemType.None || slotSelecionadoIndex == i)
                {
                    slotSelecionadoIndex = i;
                }
                return true; // Adicionado com sucesso
            }
        }
        Debug.Log("Inventário Cheio!");
        return false;
    }

    public void TrocarItens(int indexOrigem, int indexDestino)
    {
        if (!PodeEditarInventario) return;
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
            Color cor = Color.white;
            if (itensNoInventario[i] == Chest.ItemType.HealthPotion && spritePocaoVida == null)
                cor = new Color(1f, 0.2f, 0.3f);
            slots[i].AtualizarSlot(itensNoInventario[i], icone, estaSelecionado, cor);
        }
    }

    private Sprite ObterSprite(Chest.ItemType tipo)
    {
        if (tipo == Chest.ItemType.LongSword) return spriteEspadaLonga;
        if (tipo == Chest.ItemType.Dagger) return spriteAdaga;
        if (tipo == Chest.ItemType.HealthPotion && spritePocaoVida != null) return spritePocaoVida;
        if (tipo == Chest.ItemType.HealthPotion)
        {
            if (spriteProvisorio == null)
                spriteProvisorio = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f));
            return spriteProvisorio;
        }
        return null;
    }
}
