using UnityEngine;

// Script linkado com o objeto da escada para fazer o jogador ir para a proxima dungeon
public class LadderNextDungeon : MonoBehaviour
{
    AbstractDungeonGenerator dungeonGenerator;

    // Para saber se a escada ja foi usada, evitando duplicar a chamada de geracao
    private bool used = false;
    public void SetDungeonGenerator(AbstractDungeonGenerator generator)
    {
        this.dungeonGenerator = generator;
    }

    // Ao entrar na colisao da escada
    private void OnTriggerEnter2D(Collider2D collision)
    {

        // Se a escada ja foi usada, nao faz nada para evitar criar multiplas dungeons em uma escada
        if (used) return;

        // Se quem entrou na colisao foi o jogador
        if (collision.CompareTag("Player"))
        {
            if (GameManager.getBossMorreu() == false) { return; } // So desce se o boss morreu
            used = true;

            if (dungeonGenerator != null) 
            {
                GameManager.setDeuTeleport(false);
                GameManager.setBossMorreu(false);
                if (DowngradeMenuManager.Instance != null)
                {
                    DowngradeMenuManager.Instance.OpenMenu(dungeonGenerator); // Abre o menu de downgrade antes de gerar a proxima dungeon
                }
                else
                {
                    dungeonGenerator.GenerateDungeon(); // Gera uma nova dungeon
                }
            }
        }
    }
}
