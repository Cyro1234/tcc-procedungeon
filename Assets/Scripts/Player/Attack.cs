using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public GameObject Melee;
    bool isAttacking = false;

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
    }

    // Update is called once per frame
    void Update()
    {
        checkMeleeTimer();
    }

    public void OnAttack()
    {
        if (isAttacking == false)
        {
            AudioSource.PlayClipAtPoint(swordSound, transform.position, 1f); // SFX do ataque

            Melee.SetActive(true);
            isAttacking = true;
            // TODO: Realizar animacao, quando tiver
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
