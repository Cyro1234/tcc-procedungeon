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
            used = true;

            if (dungeonGenerator != null) 
            {
                dungeonGenerator.GenerateDungeon(); // Gera uma nova dungeon
            }
        }
    }
}
