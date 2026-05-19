using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // Inputs definidos pelo InputSystem do unity


    
    private Attack attackScript;

    private void Awake()
    {
        
        attackScript = GetComponent<Attack>();
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void Attack(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            attackScript.OnAttack();
            Debug.Log("ATACOU");
        }
    }
}
