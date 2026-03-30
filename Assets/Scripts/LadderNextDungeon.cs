using UnityEngine;

public class LadderNextDungeon : MonoBehaviour
{
    AbstractDungeonGenerator dungeonGenerator;

    private bool used = false;
    public void SetDungeonGenerator(AbstractDungeonGenerator generator)
    {
        this.dungeonGenerator = generator;

        Debug.Log("[SET] OI EU FUI CHAMADO EU NAO DEVERIA ESTAR NULO");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (used) return;

        if (collision.CompareTag("Player")) 
        {
            used = true;

            if (dungeonGenerator != null) 
            {
                dungeonGenerator.GenerateDungeon();
            }
            else 
            {
                Debug.Log("AINDA NULO????");
            }
        }
    }
}
