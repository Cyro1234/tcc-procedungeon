using UnityEngine;

public class Chest : MonoBehaviour
{
    private bool isOpen = false; // Controla se o baú já foi aberto
    private Animator animator;

    void Start()
    {
        // Pega o componente de animação que colocaremos no baú
        animator = GetComponent<Animator>();
    }

    // Essa função é chamada automaticamente pelo Unity quando algo entra na área de colisão (Trigger)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se o baú está fechado e se quem encostou tem a Tag "Player"
        if (!isOpen && collision.CompareTag("Player"))
        {
            OpenChest(collision.gameObject);
        }
    }

    private void OpenChest(GameObject player)
    {
        isOpen = true; // Marca como aberto para não abrir de novo

        // Toca a animação "Open". (Criaremos esse gatilho no Animator depois)
        if (animator != null)
        {
            animator.SetTrigger("Open");
        }

        // Procura o HeartSystem no jogador e dá o escudo
        HeartSystem heartSystem = player.GetComponent<HeartSystem>();
        if (heartSystem != null)
        {
            heartSystem.EquipShield();
            Debug.Log("Baú aberto! Entregou o escudo ao jogador.");
        }
    }
}
