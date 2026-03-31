using UnityEngine;
using System.Collections;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] int damage = 1; // quantidade de dano que o inimigo da no jogador

    private Rigidbody2D rb;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Ao entrar na colisao do inimigo
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HeartSystem heartSystem = collision.gameObject.GetComponent<HeartSystem>(); // Tenta pegar o sistema de vidas do jogador
        if (heartSystem != null) // se quem entrou na colisao nao foi um jogador
        {
            heartSystem.takeDamage(damage);
        }
    }
}
