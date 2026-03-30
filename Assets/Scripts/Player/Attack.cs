using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public GameObject Melee;
    bool isAttacking = false;
    float atkDuration = 0.3f;
    float atkTimer = 0f;

    // Update is called once per frame
    void Update()
    {
        checkMeleeTimer();
    }

    public void OnAttack()
    {
        if (isAttacking == false)
        {
            Melee.SetActive(true);
            isAttacking = true;
            // Realizar animacao
        }
    }

    void checkMeleeTimer() 
    {
        if (isAttacking) 
        {
            atkTimer += Time.deltaTime;
            if (atkTimer > atkDuration) 
            {
                atkTimer = 0f;
                isAttacking= false;
                Melee.SetActive(false);
            }
        }
    }
}
