using UnityEngine;

public class Chest : MonoBehaviour
{
    // NOVO: Tipos de itens atualizados com as qualidades dos escudos
    public enum ItemType { None, ShieldSmall, ShieldMedium, ShieldLarge, ShieldLegendary, LongSword, Dagger, SpeedBoots, HealthPotion, LegendarySpeedBoots, PowerPotion }

    [Header("Configurações do Baú")]
    // Cada Prefab de baú terá sua própria tabela de loot configurada no Inspector
    [SerializeField] private WeightedTable<ItemType> itemTable;

    [Header("Apenas para depuração (Não alterar)")]
    public ItemType itemInside;

    private bool isOpen = false;
    private Animator animator;

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
        itemPreConfigurado = true;
    }

    public void ConfigurarItem(ItemType item)
    {
        itemInside = item;
        itemPreConfigurado = true; // Ativa a trava
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
            itemInside == ItemType.HealthPotion || itemInside == ItemType.PowerPotion;
        if (vaiParaInventario && (InventoryManager.Instance == null ||
            !InventoryManager.Instance.AdicionarItem(itemInside)))
        {
            GameEffects.Instance.Popup(transform.position + Vector3.up * 0.9f, "Inventário cheio!", new Color(1f, 0.5f, 0.4f));
            return;
        }

        isOpen = true;
        if (animator != null) animator.SetTrigger("Open");

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
                player.GetComponent<PlayerStatsHandler>()?.EquiparBota();
                GameEffects.Instance.ItemObtained(transform.position, "Bota da Velocidade!", new Color(0.6f, 1f, 0.6f));
                break;
            case ItemType.LegendarySpeedBoots:
                player.GetComponent<PlayerStatsHandler>()?.EquiparBotaLendaria();
                GameEffects.Instance.ItemObtained(transform.position, "Bota Lendária da Velocidade!", new Color(1f, 0.7f, 0.2f));
                break;


            // Armas e pocoes vão para o Inventário!
            case ItemType.LongSword:
            case ItemType.Dagger:
            case ItemType.HealthPotion:
            case ItemType.PowerPotion:
                Debug.Log(itemInside + " adicionado ao inventário!");
                string nome = itemInside switch
                {
                    ItemType.LongSword => "Espada Longa",
                    ItemType.Dagger => "Adaga",
                    ItemType.HealthPotion => "Poção de Vida",
                    _ => "Poção de Poder"
                };
                Color cor = itemInside switch
                {
                    ItemType.HealthPotion => new Color(1f, 0.45f, 0.55f),
                    ItemType.PowerPotion => PlayerStatsHandler.CorPocaoPoder,
                    _ => WeaponColor
                };
                int slot = InventoryManager.Instance.UltimoSlotAdicionado + 1;
                GameEffects.Instance.ItemObtained(transform.position, slot > 0 ? $"{nome}! [{slot}]" : $"{nome}!", cor);
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
        hs.EquipShield(amount);

        string texto = antes >= amount ? $"{nome} (já tem um melhor)" : $"{nome}! +{amount}";
        GameEffects.Instance.ItemObtained(transform.position, texto, color);
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
