using UnityEngine;
using System.Collections.Generic;

// Sumario: Oi galera esse arquivo que define como cada inimigo é montado! Ele é bem rudimentar só pra testar a montagem com geração procedural.
// Por enquanto usei sprites proprios beeeeem feinhos e sem animação.


public class EnemyModularBuilder : MonoBehaviour
{
    [Header("BodyParts Renderer")]  // Onde cada sprite vai ser renderizado
    public SpriteRenderer headRenderer;
    public SpriteRenderer torsoRenderer;
    public SpriteRenderer armRenderer;
    public SpriteRenderer legRenderer;

    [Header("Sprite List")]     // Listas de sprites para cada parte do corpo (da pra melhorar como ele pega isso)
    public List<Sprite> headOptions;
    public List<Sprite> torsoOptions;
    public List<Sprite> armOptions;
    public List<Sprite> legOptions;
    // Talvez seja melhor a gente colocar um scriptable object para cada parte do corpo, onde a gente pode colocar as sprites e outras informações como peso, tipo de inimigo, etc.
    // Mas por enquanto ta assim pra testar a geração procedural do inimigo.
    

    // Metodo que vai montar o inimigo com base na seed, sprites aleatorios
    public void EnemyBuild()
    {
        if (headOptions.Count > 0)
            headRenderer.sprite = headOptions[Rng.EnemyRange(0, headOptions.Count)];

        if (torsoOptions.Count > 0)
            torsoRenderer.sprite = torsoOptions[Rng.EnemyRange(0, torsoOptions.Count)];

        if (armOptions.Count > 0)
            armRenderer.sprite = armOptions[Rng.EnemyRange(0, armOptions.Count)];

        if (legOptions.Count > 0)
            legRenderer.sprite = legOptions[Rng.EnemyRange(0, legOptions.Count)];

        Debug.Log($"{gameObject.name} montado com sucesso via Seed!");
    }

    void Start()
    {
        // Monta o inimigo.
        EnemyBuild();
    }
}