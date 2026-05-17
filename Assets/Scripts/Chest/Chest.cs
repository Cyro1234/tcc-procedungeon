using UnityEngine;

public class Chest : MonoBehaviour
{
    // NOVO: Tipos de itens atualizados com as qualidades dos escudos
    public enum ItemType { ShieldSmall, ShieldMedium, ShieldLarge, ShieldLegendary, LongSword, Dagger }

    [Header("Configurações do Baú")]
    // Cada Prefab de baú terá sua própria tabela de loot configurada no Inspector
    [SerializeField] private WeightedTable<ItemType> itemTable;

    [Header("Apenas para depuração (Não alterar)")]
    public ItemType itemInside;

    private bool isOpen = false;
    private Animator animator;

    private bool itemPreConfigurado = false; // NOVO: Trava para não sobrescrever o item manual

    void Start()
    {
        animator = GetComponent<Animator>();

        // O próprio baú sorteia o item de sua tabela assim que é instanciado
        // Só sorteia se o Gerador da Masmorra não tiver forçado um item antes!
        if (!itemPreConfigurado && itemTable != null && itemTable.items.Count > 0)
        {
            System.Random rng = new System.Random(System.Guid.NewGuid().GetHashCode());
            itemInside = itemTable.getRandom(rng);
        }
        //if (itemTable != null && itemTable.items.Count > 0)
        //{
        //    System.Random rng = new System.Random(System.Guid.NewGuid().GetHashCode());
        //    itemInside = itemTable.getRandom(rng);
        //}
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
        isOpen = true;

        if (animator != null)
        {
            animator.SetTrigger("Open");
        }

        // Entrega o item sorteado com os respectivos atributos
        switch (itemInside)
        {
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
            case ItemType.LongSword:
                player.GetComponent<Attack>()?.EquipWeapon("LongSword");
                break;
            case ItemType.Dagger:
                player.GetComponent<Attack>()?.EquipWeapon("Dagger");
                break;
        }
    }
}