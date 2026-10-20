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
        // F12: F9-F11 ja sao usados pelo StatusEffectTester
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            playerObj.GetComponent<StatusEffectManager>()?.ApplyEffect(new SnailEffect());
            return;
        }
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
