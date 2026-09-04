using UnityEngine;

public class Goblin : MonoBehaviour
{

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float cooldown = 0.8f;
    [SerializeField] private int maxAmmo = 12;
    [SerializeField] private float reloadTime = 3.0f;

    private int ammo;
    private float proximoTiro = 0f;
    private Transform player;

    private void Start()
    {
        ammo = maxAmmo;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        if (Time.time >= proximoTiro)
        {
            if (ammo > 0)
            {
                ammo--;
                Shoot();
                proximoTiro = Time.time + cooldown;
            }
            else // Precisa recarregar
            {
                ammo = maxAmmo;
                proximoTiro = Time.time + reloadTime;
            }
        }
    }

    private void Shoot()
    {
        Vector2 direction = (player.position - this.transform.position).normalized;

        GameObject bulletObj = Instantiate(bulletPrefab, this.transform.position, Quaternion.identity);
        Bullet bullet = bulletObj.GetComponent<Bullet>();

        if (bullet != null)
        {
            bullet.Launch(direction);
        }
    }
}
