using UnityEngine;
using System.Collections;

public class Chest : MonoBehaviour
{
    public enum ItemType { None, ShieldSmall, ShieldMedium, ShieldLarge, ShieldLegendary, LongSword, Dagger, SpeedBoots, HealthPotion, LegendarySpeedBoots, PowerPotion, Axe, BasicSword, Cannon, PenaFenix }

    [Header("Configurações do Baú")]
    // Cada Prefab de baú terá sua própria tabela de loot configurada no Inspector
    [SerializeField] private WeightedTable<ItemType> itemTable;
    [Header("Aparencia animada")]
    [SerializeField] private string aparenciaBau;
    private Sprite[] quadrosAbertura;
    private SpriteRenderer renderBau;
    private static readonly System.Collections.Generic.Dictionary<string, Sprite[]> animacoesBaus =
        new System.Collections.Generic.Dictionary<string, Sprite[]>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarAnimacoes() => animacoesBaus.Clear();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        renderBau = GetComponent<SpriteRenderer>();
        if (string.IsNullOrEmpty(aparenciaBau) || renderBau == null) return;
        if (!animacoesBaus.TryGetValue(aparenciaBau, out quadrosAbertura))
        {
            quadrosAbertura = Resources.LoadAll<Sprite>("Baus/" + aparenciaBau);
            System.Array.Sort(quadrosAbertura, (a, b) => string.CompareOrdinal(a.name, b.name));
            animacoesBaus[aparenciaBau] = quadrosAbertura;
        }
        if (quadrosAbertura.Length == 0)
        {
            Debug.LogWarning("Animacao do bau nao encontrada: Baus/" + aparenciaBau, this);
            return;
        }
        // Os arquivos possuem quatro quadros de 100ms; o primeiro e o bau fechado.
        if (animator != null) animator.enabled = false;
        renderBau.sprite = quadrosAbertura[0];
    }

    private IEnumerator AnimarAbertura()
    {
        foreach (var quadro in quadrosAbertura)
        {
            renderBau.sprite = quadro;
            yield return new WaitForSeconds(.1f);
        }
        // Mantem o ultimo quadro aberto ate o fade existente terminar.
    }

    [Header("Apenas para depuração (Não alterar)")]
    public ItemType itemInside;

    private bool isOpen = false;
    private Animator animator;
    [SerializeField, Min(0f)] private float tempoAntesDeSumir = 1.5f;
    [SerializeField, Min(.1f)] private float duracaoFade = .8f;

    private bool itemPreConfigurado = false; // NOVO: Trava para não sobrescrever o item manual

    private void OnValidate()
    {
        WeightedTable<ItemType>.ValidateInEditor(itemTable, this, nameof(itemTable));
    }

    void Start()
    {
        animator = GetComponent<Animator>();

        if (!itemPreConfigurado)
            InicializarConteudo(Rng.NewChestRandom());
    }

    public void InicializarConteudo(System.Random rng)
    {
        if (itemPreConfigurado) return;
        itemInside = itemTable.getRandom(rng);
        // A tabela do bau decide se vem uma arma; qual arma e sorteada por igual entre todas (5 armas = 20% cada), pra nenhuma ser mais rara que as outras
        if (System.Array.IndexOf(ArmasDoBau, itemInside) >= 0)
            itemInside = ArmasDoBau[rng.Next(ArmasDoBau.Length)];
        itemPreConfigurado = true;
    }

    // Armas que podem sair de bau. A espada inicial tambem: como o jogador ja tem ela, conta como copia (+dano)
    public static readonly ItemType[] ArmasDoBau = { ItemType.BasicSword, ItemType.LongSword, ItemType.Dagger, ItemType.Axe, ItemType.Cannon };

    public void ConfigurarItem(ItemType item)
    {
        itemInside = item;
        itemPreConfigurado = true; 
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isOpen && collision.CompareTag("Player"))
        {
            OpenChest(collision.gameObject);
        }
    }

    private void OpenChest(GameObject player)
    {
        // Um inventário cheio não deve fazer o item desaparecer dentro de um baú aberto.
        bool vaiParaInventario = itemInside == ItemType.LongSword || itemInside == ItemType.Dagger ||
            itemInside == ItemType.HealthPotion || itemInside == ItemType.PowerPotion || itemInside == ItemType.Axe ||
            itemInside == ItemType.Cannon || itemInside == ItemType.BasicSword;
        if (vaiParaInventario && (InventoryManager.Instance == null ||
            !InventoryManager.Instance.AdicionarItem(itemInside)))
        {
            GameEffects.Instance.Popup(transform.position + Vector3.up * 0.9f, "Inventário cheio!", new Color(1f, 0.5f, 0.4f));
            return;
        }

        isOpen = true;
        if (quadrosAbertura != null && quadrosAbertura.Length > 0) StartCoroutine(AnimarAbertura());
        else if (animator != null) animator.SetTrigger("Open");
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        StartCoroutine(Sumir());

        switch (itemInside)
        {
            // Escudos continuam indo direto para o HeartSystem (Passivos)
            case ItemType.ShieldSmall:
                GiveShield(player, 1, "Escudo Pequeno", ShieldColor);
                break;
            case ItemType.ShieldMedium:
                GiveShield(player, 3, "Escudo Médio", ShieldColor);
                break;
            case ItemType.ShieldLarge:
                GiveShield(player, 5, "Escudo Grande", ShieldColor);
                break;
            case ItemType.ShieldLegendary:
                GiveShield(player, 10, "Escudo Lendário", new Color(1f, 0.7f, 0.2f));
                break;

            case ItemType.SpeedBoots:
            case ItemType.LegendarySpeedBoots:
                var atributos = player.GetComponent<PlayerStatsHandler>();
                bool lendaria = itemInside == ItemType.LegendarySpeedBoots;
                if (lendaria) atributos?.EquiparBotaLendaria();
                else atributos?.EquiparBota();
                string textoBota = lendaria ? "Bota Lendária da Velocidade!" : "Bota da Velocidade!";
                // Avisa quando a velocidade ja chegou no limite (hard cap) e a bota nao soma mais
                if (atributos != null && atributos.BotasNoMaximo) textoBota += " (velocidade no máximo)";
                GameEffects.Instance.ItemObtained(transform.position, textoBota,
                    lendaria ? new Color(1f, 0.7f, 0.2f) : new Color(0.6f, 1f, 0.6f), itemInside);
                break;

            // Vida extra: passivo, vai direto pro HeartSystem
            case ItemType.PenaFenix:
                HeartSystem vida = player.GetComponent<HeartSystem>();
                if (vida != null) vida.GanharPena();
                int penas = vida != null ? vida.Penas : 1;
                string textoPena = penas > 1 ? $"{PenaFenix.Nome}! ({penas} vidas extras)" : $"{PenaFenix.Nome}! Você revive uma vez";
                GameEffects.Instance.ItemObtained(transform.position, textoPena, PenaFenix.Cor, itemInside);
                break;


            // Armas e pocoes vão para o Inventário
            case ItemType.LongSword:
            case ItemType.Dagger:
            case ItemType.HealthPotion:
            case ItemType.PowerPotion:
            case ItemType.Axe:
            case ItemType.Cannon:
            case ItemType.BasicSword:
                Debug.Log(itemInside + " adicionado ao inventário!");
                string nome = itemInside switch
                {
                    ItemType.LongSword => "Espada Longa",
                    ItemType.Dagger => "Adaga",
                    ItemType.HealthPotion => "Poção de Vida",
                    ItemType.PowerPotion => "Poção de Poder",
                    ItemType.Axe => "Machado",
                    ItemType.Cannon => "Canhão",
                    ItemType.BasicSword => "Espada Inicial",
                    _ => itemInside.ToString()
                };
                Color cor = itemInside switch
                {
                    ItemType.HealthPotion => new Color(1f, 0.45f, 0.55f),
                    ItemType.PowerPotion => PlayerStatsHandler.CorPocaoPoder,
                    _ => WeaponColor
                };
                var inventario = InventoryManager.Instance;
                int repeticoes = inventario.ObterRepeticoes(itemInside);
                float bonusCopia = inventario.ObterBonusUltimaCopia(itemInside);
                string mensagem = repeticoes == 0 ? $"{nome}!"
                    : bonusCopia <= 0f ? $"{nome}! (dano no máximo: {InventoryManager.TextoPorcentagem(inventario.ObterBonusDano(itemInside))})"
                    : $"{nome}! {InventoryManager.TextoPorcentagem(bonusCopia)} dano " +
                      $"(total {InventoryManager.TextoPorcentagem(inventario.ObterBonusDano(itemInside))})";
                GameEffects.Instance.ItemObtained(transform.position, mensagem, cor, itemInside);
                break;

                // Armas agora vão para o Inventário!
                //case ItemType.LongSword:
                //case ItemType.Dagger:
                //case ItemType.HealthPotion:
                //    Debug.Log(itemInside + " adicionado ao inventário!");
                //    string nome = itemInside == ItemType.LongSword ? "Espada Longa"
                //        : itemInside == ItemType.Dagger ? "Adaga" : "Poção de Vida";
                //    Color cor = itemInside == ItemType.HealthPotion ? new Color(1f, 0.45f, 0.55f) : WeaponColor;
                //    int slot = InventoryManager.Instance.UltimoSlotAdicionado + 1;
                //    GameEffects.Instance.ItemObtained(transform.position, slot > 0 ? $"{nome}! [{slot}]" : $"{nome}!", cor);
                //    break;
        }
    }

    private static readonly Color ShieldColor = new Color(0.55f, 0.85f, 1f);
    private static readonly Color WeaponColor = new Color(1f, 0.85f, 0.4f);

    private void GiveShield(GameObject player, int amount, string nome, Color color)
    {
        HeartSystem hs = player.GetComponent<HeartSystem>();
        if (hs == null) return;

        int antes = hs.shieldHealth;
        if (antes < amount) hs.EquipShield(amount);

        string texto = antes >= amount ? $"{nome} (já tem um melhor)" : $"{nome}! +{amount}";
        GameEffects.Instance.ItemObtained(transform.position, texto, color, itemInside);
    }

    private IEnumerator Sumir()
    {
        yield return new WaitForSeconds(tempoAntesDeSumir);
        if (animator != null) animator.enabled = false;
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        var cores = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) cores[i] = sprites[i].color;
        for (float t = 0; t < duracaoFade; t += Time.deltaTime)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                var cor = cores[i];
                cor.a *= 1f - t / duracaoFade;
                sprites[i].color = cor;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    //private void OpenChest(GameObject player)
    //{
    //    isOpen = true;

    //    if (animator != null)
    //    {
    //        animator.SetTrigger("Open");
    //    }

    //    // Entrega o item sorteado com os respectivos atributos
    //    switch (itemInside)
    //    {
    //        case ItemType.ShieldSmall:
    //            player.GetComponent<HeartSystem>()?.EquipShield(1);
    //            break;
    //        case ItemType.ShieldMedium:
    //            player.GetComponent<HeartSystem>()?.EquipShield(3);
    //            break;
    //        case ItemType.ShieldLarge:
    //            player.GetComponent<HeartSystem>()?.EquipShield(5);
    //            break;
    //        case ItemType.ShieldLegendary:
    //            player.GetComponent<HeartSystem>()?.EquipShield(10);
    //            break;
    //        case ItemType.LongSword:
    //            player.GetComponent<Attack>()?.EquipWeapon("LongSword");
    //            break;
    //        case ItemType.Dagger:
    //            player.GetComponent<Attack>()?.EquipWeapon("Dagger");
    //            break;
    //    }
    //}
}
