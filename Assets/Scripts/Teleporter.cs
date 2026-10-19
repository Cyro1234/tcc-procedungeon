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
            yield return TeleportarParaBoss(collision.gameObject);
        }
    }

    // Tambem usado pelo F5, mesmo com o portal oculto ou ja utilizado.
    public static IEnumerator TeleportarParaBoss(GameObject player)
    {
        if (player == null) yield break;
        var camera = FindAnyObjectByType<CinemachineCamera>();
        var follow = camera != null ? camera.GetComponent<CinemachineFollow>() : null;
        Vector3 dampingAnterior = follow != null ? follow.TrackerSettings.PositionDamping : Vector3.zero;
        if (follow != null) follow.TrackerSettings.PositionDamping = Vector3.zero;
        GameManager.setDeuTeleport(true);
        player.transform.position = new Vector2(1000, 0);
        GameEffects.Instance.OnEnterBossRoom();
        yield return new WaitForEndOfFrame();
        if (follow != null) follow.TrackerSettings.PositionDamping = dampingAnterior;
    }
}
