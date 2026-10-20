using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.InputSystem;

public class Xiter : MonoBehaviour
{

    private GameObject playerObj;
    private PlayerStatsHandler stats;


    void Start()
    {
        playerObj = GameObject.FindWithTag("Player");
        stats = playerObj.GetComponent<PlayerStatsHandler>();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            tpTeleport();
            return;
        }
        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            maxStr();
            return;
        }
        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            noDamage();
            return;
        }
        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            godMode();
            return;
        }
        // F12: caracol
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            playerObj.GetComponent<StatusEffectManager>()?.ApplyEffect(new SnailEffect());
            return;
        }
        // Insert: bau de raridade aleatoria na frente do jogador
        if (Keyboard.current.insertKey.wasPressedThisFrame)
        {
            spawnarBauNaFrente();
            return;
        }
    }

    private void spawnarBauNaFrente()
    {
        var spawner = FindAnyObjectByType<ChestSpawner>();
        if (spawner == null || playerObj == null) return;
        var movimento = playerObj.GetComponent<PlayerMovement>();
        Vector2 frente = movimento != null ? movimento.FacingDirection : Vector2.down;
        // 2 tiles de distancia: perto o bastante pra ver, longe o bastante pra nao abrir sozinho em cima do jogador
        spawner.SpawnarBauDeTeste(playerObj.transform.position + (Vector3)(frente.normalized * 2f));
    }

    private void tpTeleport()
    {
        if (playerObj != null) StartCoroutine(Teleporter.TeleportarParaBoss(playerObj));
    }
    private void maxStr()
    {
        stats.AddTemporaryModifier(new StatModifier("AumentarDano", StatType.Damage, ModifierSource.Buff, ModifierType.Flat, float.MaxValue, 0f));
    }
    private void noDamage()
    {
        playerObj.GetComponent<HeartSystem>()?.EquipShield(int.MaxValue);
    }
    private void godMode()
    {
        noDamage();
        maxStr();
    }

}
