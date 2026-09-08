using UnityEngine;
using UnityEngine.InputSystem;

public class Attack : MonoBehaviour
{
    public GameObject Melee;
    public GameObject Pivot;
    public float rotationSpeed = 360f;
    bool isAttacking = false;
    bool isCooldown = false;
    float atkDuration = 0.3f; // Podemos deixar isso no PlayerStatsHandler.cs depois, pra gente ter a possibilidade de modificar o tempo de duração do ataque com buffs e debuffs (Xicote de Alex)
    float atkTimer = 0f;

    private PlayerStatsHandler stats;


    // Guardamos os valores originais para usar como base
    private float originalAtkDuration;
    private Vector3 originalMeleeScale;

    [SerializeField] private AudioClip swordSound;

    private void Awake()
    {
        stats = GetComponent<PlayerStatsHandler>();
    }

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
        if (Time.timeScale == 0f) return;

        if (isCooldown == false && isAttacking == false)
        {
            // O jogador "pergunta" ao inventário qual item está na mão
            Chest.ItemType itemAtual = InventoryManager.Instance.ObterItemSelecionado();
            PrepararArma(itemAtual);

            AudioManager.Instance.PlaySFX("Ataque");
            Melee.SetActive(true);
            Pivot.SetActive(true);
            isAttacking = true;
        }
    }

    //public void OnAttack()
    //{
    //    if (Time.timeScale == 0f) return;

    //    if (isCooldown == false)
    //    {
    //        if (isAttacking == false)
    //        {
    //            AudioManager.Instance.PlaySFX("Ataque");
    //            Melee.SetActive(true);
    //            Pivot.SetActive(true);
    //            isAttacking = true;
    //        }
    //    }
    //}

    // Tempo que a hitbox do ataque fica ativada
    void checkMeleeTimer()
    {
        if (isAttacking)
        {


            atkTimer += Time.deltaTime;

            float progress = atkTimer / atkDuration;
            float currentAngle = Mathf.Lerp(-30f, 30f, progress);

            Pivot.transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

            if (atkTimer > atkDuration)
            {
                atkTimer = 0f;
                isAttacking = false;
                Melee.SetActive(false);
                Pivot.SetActive(false);
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

    // Antiga EquipWeapon agora se chama PrepararArma e é acionada a cada clique
    private void PrepararArma(Chest.ItemType weaponType)
    {
        if (weaponType == Chest.ItemType.LongSword)
        {
            Melee.transform.localScale = originalMeleeScale * 1.5f;
            atkDuration = originalAtkDuration * 2.0f;
        }
        else if (weaponType == Chest.ItemType.Dagger)
        {
            Melee.transform.localScale = originalMeleeScale * 0.7f;
            atkDuration = originalAtkDuration * 0.5f;
        }
        else // Se for Chest.ItemType.None (mãos vazias) ou poção
        {
            Melee.transform.localScale = originalMeleeScale;
            atkDuration = originalAtkDuration;
        }
    }

    // NOVA FUNÇÃO: Chamada pelo baú para trocar a arma
    //public void EquipWeapon(string weaponType)
    //{
    //    if (weaponType == "LongSword")
    //    {
    //        // Espada Longa: 50% maior, mas demora o dobro do tempo na tela (ataque mais lento)
    //        Melee.transform.localScale = originalMeleeScale * 1.5f;
    //        atkDuration = originalAtkDuration * 2.0f;
    //        Debug.Log("Equipou Espada Longa! Área MAIOR, ataque mais LENTO.");
    //    }
    //    else if (weaponType == "Dagger")
    //    {
    //        // Adaga: 30% menor, mas some da tela bem mais rápido (ataque mais rápido)
    //        Melee.transform.localScale = originalMeleeScale * 0.7f;
    //        atkDuration = originalAtkDuration * 0.5f;
    //        Debug.Log("Equipou Adaga! Área MENOR, ataque mais RÁPIDO.");
    //    }
    //}
}