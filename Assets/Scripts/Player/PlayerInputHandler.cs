using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // Inputs definidos pelo InputSystem do unity


    
    private Attack attackScript;
    private InputAction moveAction;
    private InputAction attackAction;

    private void Awake()
    {
        
        attackScript = GetComponent<Attack>();
        var actions = GetComponent<PlayerInput>().actions;
        moveAction = actions.FindAction("Player/Move", true);
        attackAction = actions.FindAction("Player/Attack", true);
    }

    private void OnEnable()
    {
        GameFlow.Changed += AtualizarAcoes;
        AtualizarAcoes();
    }

    private void OnDisable()
    {
        GameFlow.Changed -= AtualizarAcoes;
        moveAction?.Disable();
        attackAction?.Disable();
    }

    private void AtualizarAcoes()
    {
        if (GameFlow.CanPlay) { moveAction.Enable(); attackAction.Enable(); }
        else { moveAction.Disable(); attackAction.Disable(); }
    }

    private void Attack(InputAction.CallbackContext context) 
    {
        if (context.performed && enabled && GameFlow.CanPlay)
        {
            attackScript.OnAttack();
            Debug.Log("ATACOU");
        }
    }

}
