using UnityEngine;
using UnityEngine.InputSystem;

// Oi eu sou o arquivo que testa os debuffs que fiz mapeado para as teclas F1, F2 et als.
public class StatusEffectTester : MonoBehaviour
{
    [SerializeField] private StatusEffectManager statusEffects;

    private void Update()
    {
        if (statusEffects == null || Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            statusEffects.ApplyEffect(new DrunkennessEffect(8f)); // esse faz o movimento ficar meio circulante
            Debug.Log("Aplicado: Embriaguez (8s)");
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            statusEffects.ApplyEffect(new LimpEffect(8f)); // esse faz o movimento ter algumas pausas
            Debug.Log("Aplicado: Perneta (8s)");
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            statusEffects.ApplyEffect(new LabyrinthitisEffect(8f)); // esse faz a camera ficar girando 5 graus a cada segundo 
            Debug.Log("Aplicado: Labirintite (8s)");
        }
    }
}
