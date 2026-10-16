using UnityEngine;

public class Chest : MonoBehaviour
{
    // NOVO: Tipos de itens atualizados com as qualidades dos escudos
    public enum ItemType { None, ShieldSmall, ShieldMedium, ShieldLarge, ShieldLegendary, LongSword, Dagger, SpeedBoots, HealthPotion }

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
            itemInside == ItemType.SpeedBoots || itemInside == ItemType.HealthPotion;
        if (vaiParaInventario && (InventoryManager.Instance == null ||
            !InventoryManager.Instance.AdicionarItem(itemInside))) return;

        isOpen = true;
        if (animator != null) animator.SetTrigger("Open");

        switch (itemInside)
        {
            // Escudos continuam indo direto para o HeartSystem (Passivos)
            case ItemType.ShieldSmall:
                player.GetComponent<HeartSystem>()?.EquipShield(1);
                break;
            case ItemType.ShieldMedium:
                player.GetComponent<HeartSystem>()?.EquipShield(3);
                break;
            case ItemType.ShieldLarge:
                player.GetComponent<HeartSystem>()?.EquipShield(5);
                break;
            case ItemType.ShieldLegendary:
                player.GetComponent<HeartSystem>()?.EquipShield(10);
                break;

            // Armas agora vão para o Inventário!
            case ItemType.LongSword:
            case ItemType.Dagger:
            case ItemType.SpeedBoots:
            case ItemType.HealthPotion:
                Debug.Log(itemInside + " adicionado ao inventário!");
                break;
        }
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
