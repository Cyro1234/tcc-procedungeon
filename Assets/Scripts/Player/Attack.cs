using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public GameObject Melee;
    bool isAttacking = false;
<<<<<<< HEAD

    // Deixei public para podermos ver no Inspector
    public float atkDuration = 0.3f;
    float atkTimer = 0f;

    // Guardamos os valores originais para usar como base
    private float originalAtkDuration;
    private Vector3 originalMeleeScale;

    [SerializeField] private AudioClip swordSound;

    void Start()
    {
        // Ao iniciar, o jogo memoriza o tamanho e velocidade padrão
        originalAtkDuration = atkDuration;
        originalMeleeScale = Melee.transform.localScale;
=======
    bool isCooldown = false;
    float atkDuration = 0.3f; // Podemos deixar isso no PlayerStatsHandler.cs depois, pra gente ter a possibilidade de modificar o tempo de duração do ataque com buffs e debuffs (Xicote de Alex)
    float atkTimer = 0f;

    private PlayerStatsHandler stats;


    [SerializeField] private AudioClip swordSound;

    private void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
>>>>>>> teste
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

    // NOVA FUNÇÃO: Chamada pelo baú para trocar a arma
    public void EquipWeapon(string weaponType)
    {
        if (weaponType == "LongSword")
        {
            // Espada Longa: 50% maior, mas demora o dobro do tempo na tela (ataque mais lento)
            Melee.transform.localScale = originalMeleeScale * 1.5f;
            atkDuration = originalAtkDuration * 2.0f;
            Debug.Log("Equipou Espada Longa! Área MAIOR, ataque mais LENTO.");
        }
        else if (weaponType == "Dagger")
        {
            // Adaga: 30% menor, mas some da tela bem mais rápido (ataque mais rápido)
            Melee.transform.localScale = originalMeleeScale * 0.7f;
            atkDuration = originalAtkDuration * 0.5f;
            Debug.Log("Equipou Adaga! Área MENOR, ataque mais RÁPIDO.");
        }
    }
}
