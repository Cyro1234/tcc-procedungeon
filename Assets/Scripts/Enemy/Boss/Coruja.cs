using Unity.VisualScripting;
using UnityEngine;

public class Coruja : MonoBehaviour
{
    [SerializeField] private GameObject redemoinhoPrefab;
    [SerializeField] private float cooldown = 0.5f;
    [SerializeField] private int maxAmmo = 1;
    [SerializeField] private float reloadTime = 4.0f;

    [SerializeField] private float offset_x = 4.0f;
    [SerializeField] private float offset_y = 4.0f;

    private int ammo = 0;
    private float proximoTiro = 0f;

    private Transform player;
    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        if (GameManager.getDeuTeleport() == false) return; // Nao atira se o jogador ainda nao teleportou

        if (Time.time >= proximoTiro)
        {
            if (ammo > 0)
            {
                ammo--;
                Shoot();
                proximoTiro = Time.time + cooldown;
            }
            else // Esta vulneravel
            {
                ammo = maxAmmo;
                proximoTiro = Time.time + reloadTime;
            }
        }
    }
    private Vector3 posicaoRedemoinho()
    {
        Vector3 pos = new Vector3(1000, 0, 0); // Pos padrao sala boss
        bool rngX = Random.value < 0.5f;
        bool rngY = Random.value < 0.5f;

        float newPosX = pos.x;
        float newPosY = pos.y;

        if (rngX)
        {
            newPosX += offset_x;
        }
        else
        {
            newPosX -= offset_x;
        }

        if (rngY)
        {
            newPosY += offset_y;
        }
        else
        {
            newPosY -= offset_y;
        }
        Vector3 newPos = new Vector3(newPosX, newPosY, pos.z);

        return newPos;
    }

    private void Shoot()
    {
        GameObject redemoinhoObj = Instantiate(redemoinhoPrefab, posicaoRedemoinho(), Quaternion.identity);
        Redemoinho redemoinho = redemoinhoObj.GetComponent<Redemoinho>();

        if (redemoinho != null)
        {
            redemoinho.Launch(player);
        }
    }
}
