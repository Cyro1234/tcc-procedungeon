using UnityEngine;

public class Chest : MonoBehaviour
{
    // Criamos a nossa lista de itens possíveis
    public enum ItemType { Shield, LongSword, Dagger }

    [Header("Configurações do Baú")]
    public ItemType itemInside;      // Qual item está aqui dentro

    private bool isOpen = false;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // NOVA FUNÇÃO: O Gerador de Dungeon chama essa função para colocar o item sorteado aqui dentro
    public void ConfigurarItem(ItemType item)
    {
        itemInside = item;
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