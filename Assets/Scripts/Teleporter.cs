using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    private bool used = false;

    public CinemachineFollow cinemachineFollow;

    private void Start()
    {   
        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam != null)
        {
            cinemachineFollow = cam.GetComponent<CinemachineFollow>();
        }
    }
    private IEnumerator OnTriggerEnter2D(Collider2D collision)
    {

        // Se a escada ja foi usada, nao faz nada para evitar criar multiplas dungeons em uma escada
        if (used) yield break;

        // Se quem entrou na colisao foi o jogador
        if (collision.CompareTag("Player"))
        {
            used = true;

            if (cinemachineFollow != null)
            {
                cinemachineFollow.TrackerSettings.PositionDamping = Vector3.zero;
            }

            GameManager.setDeuTeleport(true);
            collision.transform.position = new Vector2(1000, 0);

            yield return new WaitForEndOfFrame();

            if (cinemachineFollow != null)
            {
                cinemachineFollow.TrackerSettings.PositionDamping = new Vector3(1f, 1f, 1f);
            }
        }
    }
}
