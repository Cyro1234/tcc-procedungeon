using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public GameObject Melee;
    bool isAttacking = false;
    bool isCooldown = false;
    float atkDuration = 0.3f; // Podemos deixar isso no PlayerStatsHandler.cs depois, pra gente ter a possibilidade de modificar o tempo de duração do ataque com buffs e debuffs (Xicote de Alex)
    float atkTimer = 0f;

    private PlayerStatsHandler stats;


    [SerializeField] private AudioClip swordSound;

    private void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        checkMeleeTimer();
    }

    public void OnAttack()
    {
        if (isCooldown == false) // Adicionei essa verificação pra que a gente tenha como controlar o tempo de recarga do ataque
        {
            if (isAttacking == false)
            {
                AudioSource.PlayClipAtPoint(swordSound, transform.position, 1f); // SFX do ataque

                Melee.SetActive(true);
                isAttacking = true;
                // TODO: Realizar animacao, quando tiver
            }
        }
    }

    // Tempo que a hitbox do ataque fica ativada
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
                isCooldown = true; // Começa o cooldown do ataque depois que a hitbox é desativada
            }
        }

        // Literalmente a mesma logica do de cima, obrigada augusto mitou
        if (isCooldown) 
        {
            atkTimer += Time.deltaTime;
            if (atkTimer >= stats.GetPlayerAttackCooldown()) 
            {
                atkTimer = 0f;
                isCooldown = false;
            }
        }
    }
}
