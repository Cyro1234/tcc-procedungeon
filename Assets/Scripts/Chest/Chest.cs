using UnityEngine;

public class Chest : MonoBehaviour
{
    // Criamos a nossa lista de itens possíveis
    public enum ItemType { Shield, LongSword, Dagger }

    [Header("Configurações do Baú")]
    public bool isRandomItem = true; // Se for true, sorteia. Se for false, usa o item abaixo.
    public ItemType itemInside;      // Qual item está aqui dentro (aparece como menu no Unity!)

    private bool isOpen = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Se o baú for aleatório, fazemos o sorteio logo que ele nasce
        if (isRandomItem)
        {
            // Pega um número aleatório de 0 até a quantidade de itens na nossa lista (3)
            int randomIndex = Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);
            itemInside = (ItemType)randomIndex; // Transforma o número de volta em um ItemType
        }
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

        // Verifica qual item tem dentro do baú e entrega pro jogador
        switch (itemInside)
        {
            case ItemType.Shield:
                HeartSystem heartSystem = player.GetComponent<HeartSystem>();
                if (heartSystem != null) heartSystem.EquipShield();
                break;

            case ItemType.LongSword:
                Attack attackSword = player.GetComponent<Attack>();
                if (attackSword != null) attackSword.EquipWeapon("LongSword");
                break;

            case ItemType.Dagger:
                Attack attackDagger = player.GetComponent<Attack>();
                if (attackDagger != null) attackDagger.EquipWeapon("Dagger");
                break;
        }
    }
}