using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{

    private InputSystem_Actions inputActions;
    private Attack attackScript;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        attackScript = GetComponent<Attack>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Attack.performed += OnAttack;
    }

    private void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttack;
        inputActions.Disable();
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        Attack();
    }

    private void Attack() 
    {
        attackScript.OnAttack();
        Debug.Log("ATACOU");
    }
}
