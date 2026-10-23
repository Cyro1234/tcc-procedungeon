using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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
    private readonly Dictionary<Chest.ItemType, int> pocoes = new Dictionary<Chest.ItemType, int>();
    private readonly Dictionary<Chest.ItemType, int> repeticoesArmas = new Dictionary<Chest.ItemType, int>();
    public int ObterRepeticoes(Chest.ItemType tipo) => repeticoesArmas.TryGetValue(tipo, out int valor) ? valor : 0;
    // Bonus aditivo por tipo: uma copia extra = 1.05x; duas = 1.1x (no maximo 1.5x).
    public float ObterMultiplicadorDano(Chest.ItemType tipo) => 1f + ObterBonusDano(tipo);

    // Toda arma ganha o mesmo por copia repetida: +5%, +10%, +15%... ate +50% (10 copias)
    private const float BonusPorCopia = 0.05f;
    private const float BonusMaximoCopias = 0.5f;

    public float ObterBonusDano(Chest.ItemType tipo) => BonusComCopias(ObterRepeticoes(tipo));

    // Quanto a ultima copia pega somou (pro texto do bau)
    public float ObterBonusUltimaCopia(Chest.ItemType tipo)
    {
        int copias = ObterRepeticoes(tipo);
        return copias > 0 ? BonusComCopias(copias) - BonusComCopias(copias - 1) : 0f;
    }

    private static float BonusComCopias(int copias) => Mathf.Min(copias * BonusPorCopia, BonusMaximoCopias);

    // "+12.5%", "+18.75%": ate duas casas, sem zeros sobrando
    public static string TextoPorcentagem(float fracao) => "+" + (fracao * 100f).ToString("0.##") + "%";
    public int ObterQuantidade(Chest.ItemType tipo) => pocoes.TryGetValue(tipo, out int valor) ? valor : 0;
    public bool PossuiItem(Chest.ItemType tipo) => tipo != Chest.ItemType.None &&
        (ObterQuantidade(tipo) > 0 || System.Array.IndexOf(itensNoInventario, tipo) >= 0);
    public Chest.ItemType ObterVizinho(int direcao)
    {
        for (int passo = 1; passo < itensNoInventario.Length; passo++)
        {
            int indice = (slotSelecionadoIndex + direcao * passo + itensNoInventario.Length) % itensNoInventario.Length;
            if (itensNoInventario[indice] != Chest.ItemType.None) return itensNoInventario[indice];
        }
        return Chest.ItemType.None;
    }

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
        for (int passo = 1; passo <= itensNoInventario.Length; passo++)
        {
            int indice = (slotSelecionadoIndex + deslocamento * passo + itensNoInventario.Length) % itensNoInventario.Length;
            if (itensNoInventario[indice] == Chest.ItemType.None) continue;
            slotSelecionadoIndex = indice;
            break;
        }
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
    private Sprite spriteEspadaInicial;
    public Sprite spriteAdaga;
    public Sprite spritePocaoVida;
    public Sprite spritePocaoPoder;
    public Sprite spriteMachado;
    private Sprite spriteProvisorio;

    // Itens que sao gastos ao usar em vez de servirem como arma
    public static bool EhConsumivel(Chest.ItemType tipo)
    {
        return tipo == Chest.ItemType.HealthPotion || tipo == Chest.ItemType.PowerPotion;
    }

    public bool UsarItemSelecionado()
    {
        if (!GameFlow.CanPlay || Time.timeScale == 0f) return false;

        return UsarPocao(ObterItemSelecionado());
    }

    public bool UsarPocao(Chest.ItemType item)
    {
        if (!GameFlow.CanPlay || Time.timeScale == 0f || ObterQuantidade(item) <= 0) return false;
        if (item == Chest.ItemType.HealthPotion)
        {
            var vida = FindAnyObjectByType<HeartSystem>();
            if (vida == null || vida.IsDead || vida.IsFullHealth) return false;
            vida.RestaurarVidaInteira();
        }
        else if (item == Chest.ItemType.PowerPotion)
        {
            var atributos = FindAnyObjectByType<PlayerStatsHandler>();
            if (atributos == null || !atributos.BeberPocaoPoder()) return false;
        }
        else return false;

        GameEffects.Instance.ItemConsumed(item == Chest.ItemType.HealthPotion
            ? new Color(1f, .4f, .5f) : PlayerStatsHandler.CorPocaoPoder);
        pocoes[item]--;
        AtualizarUI();
        return true;
    }

    //public bool UsarItemSelecionado()
    //{
    //    if (!GameFlow.CanPlay || Time.timeScale == 0f ||
    //        ObterItemSelecionado() != Chest.ItemType.HealthPotion) return false;
    //    var vida = FindAnyObjectByType<HeartSystem>();
    //    if (vida == null || vida.IsDead || vida.IsFullHealth) return false;
    //    vida.RestaurarVidaInteira();
    //    itensNoInventario[slotSelecionadoIndex] = Chest.ItemType.None;
    //    AtualizarUI();
    //    return true;
    //}

    private void OnDestroy()
    {
        if (spriteProvisorio != null) Destroy(spriteProvisorio);
        if (Instance == this) Instance = null;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        if (spriteMachado == null) spriteMachado = ItemSprites.Obter(Chest.ItemType.Axe);
        if (spritePocaoVida == null) spritePocaoVida = ItemSprites.Obter(Chest.ItemType.HealthPotion);
        if (spritePocaoPoder == null) spritePocaoPoder = ItemSprites.Obter(Chest.ItemType.PowerPotion);

        for (int i = 0; i < itensNoInventario.Length; i++)
        {
            itensNoInventario[i] = Chest.ItemType.None;
            slots[i].slotIndex = i; // Configura o ID de cada slot automaticamente
        }
        AdicionarItem(Chest.ItemType.BasicSword);
    }

    private void Update()
    {
        LidarComInputsDeTeclado();
        if (GameFlow.CanPlay && Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) UsarPocao(Chest.ItemType.HealthPotion);
            if (Gamepad.current.dpad.down.wasPressedThisFrame) UsarPocao(Chest.ItemType.PowerPotion);
        }
        AtualizarUI();
    }

    private void Start()
    {
        var ataque = FindAnyObjectByType<Attack>();
        if (ataque != null && ataque.armaRender != null) spriteEspadaInicial = ataque.armaRender.sprite;
        gameObject.AddComponent<CircularInventoryHUD>().Inicializar(this, hotbarPanel);
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

        if (Keyboard.current.zKey.wasPressedThisFrame) UsarPocao(Chest.ItemType.HealthPotion);
        if (Keyboard.current.xKey.wasPressedThisFrame) UsarPocao(Chest.ItemType.PowerPotion);
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
        UltimoSlotAdicionado = -1;
        if (novoItem == Chest.ItemType.None) return false;
        if (EhConsumivel(novoItem))
        {
            pocoes[novoItem] = ObterQuantidade(novoItem) + 1;
            return true;
        }
        int existente = System.Array.IndexOf(itensNoInventario, novoItem);
        if (existente >= 0)
        {
            repeticoesArmas[novoItem] = ObterRepeticoes(novoItem) + 1;
            UltimoSlotAdicionado = existente;
            return true;
        }
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

    public bool EquiparArmaDebug(Chest.ItemType arma)
    {
        if (GameFlow.Current != GameFlow.State.DebugMenu ||
            System.Array.IndexOf(Chest.ArmasDoBau, arma) < 0) return false;

        int indice = System.Array.IndexOf(itensNoInventario, arma);
        if (indice < 0)
        {
            indice = System.Array.IndexOf(itensNoInventario, Chest.ItemType.None);
            if (indice < 0) indice = slotSelecionadoIndex;
            itensNoInventario[indice] = arma;
        }
        // Selecionar uma arma ja existente nao concede bonus de copia.
        slotSelecionadoIndex = indice;
        AtualizarUI();
        return true;
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
            if (itensNoInventario[i] == Chest.ItemType.PowerPotion && spritePocaoPoder == null)
                cor = PlayerStatsHandler.CorPocaoPoder;
            if (itensNoInventario[i] == Chest.ItemType.Axe && spriteMachado == null)
                cor = new Color(0.65f, 0.7f, 0.75f);
            slots[i].AtualizarSlot(itensNoInventario[i], icone, estaSelecionado, cor);
        }
    }

    public Sprite ObterSprite(Chest.ItemType tipo)
    {
        Sprite arte = ItemSprites.Obter(tipo);
        if (arte != null) return arte;
        if (tipo == Chest.ItemType.BasicSword) return spriteEspadaInicial != null ? spriteEspadaInicial : spriteEspadaLonga;
        if (tipo == Chest.ItemType.LongSword) return spriteEspadaLonga;
        if (tipo == Chest.ItemType.Dagger) return spriteAdaga;
        if (tipo == Chest.ItemType.HealthPotion && spritePocaoVida != null) return spritePocaoVida;
        if (tipo == Chest.ItemType.PowerPotion && spritePocaoPoder != null) return spritePocaoPoder;
        if (tipo == Chest.ItemType.Axe && spriteMachado != null) return spriteMachado;
        if (tipo == Chest.ItemType.Cannon) return Cannon.Icone;
        if (tipo == Chest.ItemType.HealthPotion || tipo == Chest.ItemType.PowerPotion
            || tipo == Chest.ItemType.Axe)
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

/// <summary>HUD circular criado no Canvas existente, sem depender de ajustes manuais na cena.</summary>
public class CircularInventoryHUD : MonoBehaviour
{
    private InventoryManager inventario;
    private RectTransform raiz;
    private UnityEngine.UI.Image centro, anterior, proximo;
    private UnityEngine.UI.Text nome, vida, poder, esquerda, direita;
    private Sprite disco;
    private Texture2D textura;
    private Attack ataqueJogador;
    private UnityEngine.UI.Image coberturaRecarga;
    private UnityEngine.UI.Text tempoRecarga;
    private bool estavaRecarregando;

    // Flash ao trocar/usar items na hotbar
    private readonly Dictionary<UnityEngine.UI.Image, UnityEngine.UI.Image> brilhos = new Dictionary<UnityEngine.UI.Image, UnityEngine.UI.Image>();
    private readonly Dictionary<UnityEngine.UI.Image, Coroutine> piscadas = new Dictionary<UnityEngine.UI.Image, Coroutine>();
    private UnityEngine.UI.Image iconeVida, iconePoder;
    private Chest.ItemType ultimaArma;
    private int ultimaQtdVida, ultimaQtdPoder;

    public void Inicializar(InventoryManager manager, RectTransform painel)
    {
        if (painel == null) return;
        inventario = manager;
        ultimaArma = manager.ObterItemSelecionado();
        ultimaQtdVida = manager.ObterQuantidade(Chest.ItemType.HealthPotion);
        ultimaQtdPoder = manager.ObterQuantidade(Chest.ItemType.PowerPotion);
        // O painel antigo fica disponível somente para reorganizar o inventário.
        var objeto = new GameObject("Seletor circular", typeof(RectTransform));
        raiz = objeto.GetComponent<RectTransform>();
        raiz.SetParent(painel.parent, false);
        raiz.anchorMin = raiz.anchorMax = new Vector2(0, 0);
        raiz.pivot = new Vector2(0, 0);
        raiz.anchoredPosition = new Vector2(24, 24);
        raiz.sizeDelta = new Vector2(250, 280);
        textura = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var pixels = new Color[128 * 128];
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                pixels[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(63.5f - distancia));
            }
        textura.SetPixels(pixels);
        textura.Apply();
        disco = Sprite.Create(textura, new Rect(0, 0, 128, 128), Vector2.one * .5f);
        centro = Bolha("Arma", new Vector2(125, 130), 188);
        anterior = Bolha("Anterior", new Vector2(45, 55), 70);
        proximo = Bolha("Proximo", new Vector2(205, 55), 70);
        ataqueJogador = FindAnyObjectByType<Attack>();
        coberturaRecarga = Imagem("Recarga do canhao", brilhos[centro].transform.parent,
            Vector2.zero, Vector2.one * 183f);
        coberturaRecarga.sprite = disco;
        coberturaRecarga.color = new Color(.4f, .4f, .4f, .78f);
        coberturaRecarga.type = UnityEngine.UI.Image.Type.Filled;
        coberturaRecarga.fillMethod = UnityEngine.UI.Image.FillMethod.Vertical;
        coberturaRecarga.fillOrigin = (int)UnityEngine.UI.Image.OriginVertical.Bottom;
        coberturaRecarga.gameObject.SetActive(false);
        tempoRecarga = Texto(coberturaRecarga.transform.parent, Vector2.zero, new Vector2(155, 42), 28);
        tempoRecarga.color = Color.white;
        var sombra = tempoRecarga.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        sombra.effectColor = Color.black;
        sombra.effectDistance = new Vector2(1f, -1f);
        tempoRecarga.gameObject.SetActive(false);
        nome = Texto(centro.transform.parent, new Vector2(0, -61), new Vector2(155, 26), 15);
        esquerda = Texto(anterior.transform.parent, new Vector2(0, -48), new Vector2(100, 24), 14);
        direita = Texto(proximo.transform.parent, new Vector2(0, -48), new Vector2(100, 24), 14);
        iconeVida = CriarPocao(Chest.ItemType.HealthPotion, new Vector2(158, 235), out vida);
        iconePoder = CriarPocao(Chest.ItemType.PowerPotion, new Vector2(218, 202), out poder);
    }

    private UnityEngine.UI.Image Bolha(string titulo, Vector2 posicao, float tamanho)
    {
        var borda = Imagem(titulo, raiz, posicao, Vector2.one * tamanho);
        borda.sprite = disco;
        borda.color = new Color(.72f, .57f, .31f);
        var fundo = Imagem("Fundo", borda.transform, Vector2.zero, Vector2.one * (tamanho - 5));
        fundo.sprite = disco;
        fundo.color = new Color(.055f, .07f, .10f, .97f);
        var icone = Imagem("Icone", fundo.transform, Vector2.zero, Vector2.one * (tamanho * .56f));
        // Criado depois do fundo para ficar por cima do icone
        var brilho = Imagem("Brilho", borda.transform, Vector2.zero, Vector2.one * tamanho);
        brilho.sprite = disco;
        brilho.color = Color.clear;
        brilhos[icone] = brilho;
        return icone;
    }

    // Acende o disco da bolha na cor pedida e da um "pulo" de escala, depois volta ao normal
    private void Piscar(UnityEngine.UI.Image icone, Color cor)
    {
        if (icone == null || !brilhos.TryGetValue(icone, out var brilho)) return;
        if (piscadas.TryGetValue(icone, out var rotina) && rotina != null) StopCoroutine(rotina);
        piscadas[icone] = StartCoroutine(AnimarPiscada(brilho, cor));
    }

    private IEnumerator AnimarPiscada(UnityEngine.UI.Image brilho, Color cor)
    {
        const float duracao = .35f;
        Transform borda = brilho.transform.parent;
        // Tempo sem escala: o flash nao trava durante hit-stop
        for (float t = 0; t < duracao; t += Time.unscaledDeltaTime)
        {
            float p = t / duracao;
            float suave = 1f - (1f - p) * (1f - p);
            cor.a = Mathf.Lerp(.85f, 0f, suave);
            brilho.color = cor;
            borda.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, suave);
            yield return null;
        }
        brilho.color = Color.clear;
        borda.localScale = Vector3.one;
    }

    private UnityEngine.UI.Image Imagem(string titulo, Transform pai, Vector2 posicao, Vector2 tamanho)
    {
        var go = new GameObject(titulo, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(pai, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = pai == raiz ? Vector2.zero : Vector2.one * .5f;
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        var imagem = go.GetComponent<UnityEngine.UI.Image>();
        imagem.preserveAspect = true;
        imagem.raycastTarget = false;
        return imagem;
    }

    private UnityEngine.UI.Text Texto(Transform pai, Vector2 posicao, Vector2 tamanho, int fonte)
    {
        var go = new GameObject("Legenda", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        go.transform.SetParent(pai, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        var texto = go.GetComponent<UnityEngine.UI.Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.fontSize = fonte;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.color = new Color(.95f, .89f, .73f);
        texto.raycastTarget = false;
        return texto;
    }

    private UnityEngine.UI.Image CriarPocao(Chest.ItemType tipo, Vector2 posicao, out UnityEngine.UI.Text contador)
    {
        var icone = Bolha(tipo.ToString(), posicao, 58);
        var sprite = tipo == Chest.ItemType.HealthPotion ? inventario.spritePocaoVida : inventario.spritePocaoPoder;
        if (sprite != null) icone.sprite = sprite;
        else
        {
            // Frasco desenhado com UI enquanto não há sprite de poção configurado.
            icone.sprite = disco;
            icone.color = tipo == Chest.ItemType.HealthPotion ? new Color(.95f, .22f, .32f) : PlayerStatsHandler.CorPocaoPoder;
            icone.rectTransform.sizeDelta = new Vector2(21, 25);
            var gargalo = Imagem("Gargalo", icone.transform, new Vector2(0, 15), new Vector2(9, 12));
            gargalo.color = icone.color;
            var rolha = Imagem("Rolha", icone.transform, new Vector2(0, 22), new Vector2(13, 5));
            rolha.color = new Color(.76f, .59f, .32f);
        }
        contador = Texto(icone.transform.parent, new Vector2(0, -40), new Vector2(110, 24), 13);
        return icone;
    }

    private void LateUpdate()
    {
        if (raiz == null || inventario == null) return;
        inventario.hotbarPanel.gameObject.SetActive(inventario.inventarioAberto);
        raiz.gameObject.SetActive(!inventario.inventarioAberto);
        Mostrar(centro, inventario.ObterItemSelecionado());
        Mostrar(anterior, inventario.ObterVizinho(-1));
        Mostrar(proximo, inventario.ObterVizinho(1));
        var selecionado = inventario.ObterItemSelecionado();
        int repeticoes = inventario.ObterRepeticoes(selecionado);
        nome.text = Nome(selecionado) + (repeticoes > 0 ? $"\n{InventoryManager.TextoPorcentagem(inventario.ObterBonusDano(selecionado))} dano" : "");
        nome.rectTransform.sizeDelta = new Vector2(155, 44);
        bool controle = Gamepad.current != null;
        esquerda.text = controle ? "L1 / LB" : "1 / Scroll";
        direita.text = controle ? "R1 / RB" : "2 / Scroll";
        vida.text = (controle ? "↑" : "Z") + "  ×" + inventario.ObterQuantidade(Chest.ItemType.HealthPotion);
        poder.text = (controle ? "↓" : "X") + "  ×" + inventario.ObterQuantidade(Chest.ItemType.PowerPotion);
        DetectarMudancas(selecionado);
        AtualizarRecarga(selecionado);
    }

    private void AtualizarRecarga(Chest.ItemType selecionado)
    {
        if (ataqueJogador == null) ataqueJogador = FindAnyObjectByType<Attack>();
        bool recarregando = ataqueJogador != null && ataqueJogador.CannonCooldownRemaining > 0f;
        bool mostrar = selecionado == Chest.ItemType.Cannon && recarregando;
        coberturaRecarga.gameObject.SetActive(mostrar);
        tempoRecarga.gameObject.SetActive(mostrar);
        if (mostrar)
        {
            coberturaRecarga.fillAmount = ataqueJogador.CannonCooldownFraction;
            float segundos = Mathf.Ceil(ataqueJogador.CannonCooldownRemaining * 10f) / 10f;
            tempoRecarga.text = segundos.ToString("0.0") + "s";
        }
        if (estavaRecarregando && !recarregando && selecionado == Chest.ItemType.Cannon)
            Piscar(centro, new Color(.6f, 1f, .6f));
        estavaRecarregando = recarregando;
    }

    // Compara com o frame anterior pro flash aparece de onde a troca vier (scroll, item novo equipado, reorganizar o inventario, etc)
    private void DetectarMudancas(Chest.ItemType selecionado)
    {
        // Com o inventario aberto o HUD fica escondido; a troca e mostrada ao fechar
        if (inventario.inventarioAberto) return;

        if (selecionado != ultimaArma)
        {
            ultimaArma = selecionado;
            Piscar(centro, Color.white);
            GameEffects.Instance.WeaponSwitched(Nome(selecionado));
        }

        int qtdVida = inventario.ObterQuantidade(Chest.ItemType.HealthPotion);
        int qtdPoder = inventario.ObterQuantidade(Chest.ItemType.PowerPotion);
        // So pisca quando a quantidade diminui, ou seja, a pocao foi consumida
        if (qtdVida < ultimaQtdVida) Piscar(iconeVida, new Color(1f, .4f, .5f));
        if (qtdPoder < ultimaQtdPoder) Piscar(iconePoder, PlayerStatsHandler.CorPocaoPoder);
        ultimaQtdVida = qtdVida;
        ultimaQtdPoder = qtdPoder;
    }

    private void Mostrar(UnityEngine.UI.Image imagem, Chest.ItemType tipo)
    {
        imagem.sprite = inventario.ObterSprite(tipo);
        imagem.enabled = tipo != Chest.ItemType.None && imagem.sprite != null;
    }

    private static string Nome(Chest.ItemType tipo) => tipo switch
    {
        Chest.ItemType.BasicSword => "Espada inicial",
        Chest.ItemType.LongSword => "Espada longa",
        Chest.ItemType.Dagger => "Adaga",
        Chest.ItemType.Axe => "Machado",
        Chest.ItemType.Cannon => "Canhão",
        Chest.ItemType.None => "Sem arma",
        _ => tipo.ToString()
    };

    private void OnDestroy()
    {
        if (raiz != null) Destroy(raiz.gameObject);
        if (disco != null) Destroy(disco);
        if (textura != null) Destroy(textura);
    }
}

/// <summary>Sprites dos itens importados pelo Aseprite; todas as qualidades de escudo compartilham o icone.</summary>
public static class ItemSprites
{
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Resetar() => cache.Clear();

    public static Sprite Obter(Chest.ItemType tipo)
    {
        if (tipo == Chest.ItemType.PenaFenix) return PenaFenix.Icone;
        string nome = tipo switch
        {
            Chest.ItemType.Axe => "machado-heroi",
            Chest.ItemType.HealthPotion => "pocao-vida",
            Chest.ItemType.PowerPotion => "pocao-poder",
            Chest.ItemType.SpeedBoots => "bota-velocidade",
            Chest.ItemType.LegendarySpeedBoots => "bota-lendaria",
            Chest.ItemType.ShieldSmall or Chest.ItemType.ShieldMedium or
                Chest.ItemType.ShieldLarge or Chest.ItemType.ShieldLegendary => "escudo-icon",
            _ => null
        };
        if (nome == null) return null;
        if (cache.TryGetValue(nome, out Sprite sprite) && sprite != null) return sprite;
        var sprites = Resources.LoadAll<Sprite>("Itens/" + nome);
        if (sprites.Length == 0)
        {
            Debug.LogWarning("Sprite do item nao encontrado: Itens/" + nome);
            return null;
        }
        cache[nome] = sprites[0];
        return sprites[0];
    }
}
