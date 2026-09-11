using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Bruxinha : MonoBehaviour
{

    [SerializeField] private GameObject raioPrefab;
    [SerializeField] private GameObject raioSeguroPrefab;
    [SerializeField] private float warningTime = 3.0f; // Tempo de aviso do shock
    [SerializeField] private float shockTime = 5.0f;

    [SerializeField] private float cooldown = 3.0f;

    [SerializeField] private float charmCooldown = 10.0f;
    private float proximoCharm = 0f;

    private float proximoTiro = 0f;
    private bool avisado = false;

    private Transform player;

    private GameObject raioObj;

    private GameObject raioSeguroObj;

    private bool randomDirecao = true;

    private PlayerStatsHandler stats;

    void Start()
    {
        stats = GetComponent<PlayerStatsHandler>(); // Para aplicar o charm

        randomDirecao = Random.value > 0.5f;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            stats = playerObj.GetComponent<PlayerStatsHandler>();
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        //if (GameManager.getDeuTeleport() == false) return; // Nao atira se o jogador ainda nao teleportou

        if (Time.time >= proximoCharm)
        {
            Debug.Log("CHARM");
            charm();
            proximoCharm = Time.time + charmCooldown;
        }

        if (Time.time >= proximoTiro)
        {
            // proximoTiro = Time.time + cooldown;
            if (avisado == false)
            {
                shockAviso();
                proximoTiro = Time.time + warningTime;
                avisado = true;
            }
            else
            {
                shock();
                randomDirecao = Random.value > 0.5f;
                avisado = false;
                proximoTiro = Time.time + shockTime + cooldown;
            }
           
            
        }
    }

    private void charm()
    {
        stats.AddTemporaryModifier(new StatModifier("MudarDirecao", StatType.Speed, ModifierSource.Debuff, ModifierType.Percent, -2f, 5f));
    }

    private void shock()
    {
        raioObj = Instantiate(raioPrefab, new Vector3(1000, 0, 0), Quaternion.identity);
        raioObj.GetComponent<Raio>().DefinirDirecao(randomDirecao);
        Destroy(raioObj, shockTime);
    }

    private void shockAviso()
    {
        raioSeguroObj = Instantiate(raioSeguroPrefab, new Vector3(1000, 0, 0), Quaternion.identity);
        Destroy(raioSeguroObj, warningTime);
    }
}
