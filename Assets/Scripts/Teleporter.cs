using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class Teleporter : MonoBehaviour
{
    private bool used = false;

    public CinemachineFollow cinemachineFollow;

    [SerializeField, Min(0f)] private float atrasoAtivacao = 1.5f;
    private float ativoEm;
    private bool precisaSair;
    private SpriteRenderer[] sprites;
    private Color[] coresOriginais;

    private void Start()
    {
        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam != null)
        {
            cinemachineFollow = cam.GetComponent<CinemachineFollow>();
        }
    }

    private void OnEnable()
    {
        ativoEm = Time.time + atrasoAtivacao;
        precisaSair = false;
        if (sprites == null)
        {
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            coresOriginais = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) coresOriginais[i] = sprites[i].color;
        }
        StartCoroutine(Carregar());
    }

    private IEnumerator Carregar()
    {
        while (Time.time < ativoEm)
        {
            AplicarAlfa(Mathf.Lerp(.25f, .6f, Mathf.PingPong(Time.time * 4f, 1f)));
            yield return null;
        }
        AplicarAlfa(1f);
    }

    private void AplicarAlfa(float alfa)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == null) continue;
            Color cor = coresOriginais[i];
            cor.a *= alfa;
            sprites[i].color = cor;
        }
    }

    private IEnumerator OnTriggerEnter2D(Collider2D collision)
    {

        // Se a escada ja foi usada, nao faz nada para evitar criar multiplas dungeons em uma escada
        if (used) yield break;

        // Se quem entrou na colisao foi o jogador
        if (collision.CompareTag("Player"))
        {
            if (Time.time < ativoEm || precisaSair)
            {
                precisaSair = true;
                yield break;
            }
            used = true;
            yield return TeleportarParaBoss(collision.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) precisaSair = false;
    }

    // Tambem usado pelo F5, mesmo com o portal oculto ou ja utilizado.
    public static IEnumerator TeleportarParaBoss(GameObject player, FinalBossSequence finalSequence = null)
    {
        if (player == null) yield break;
        var camera = FindAnyObjectByType<CinemachineCamera>();
        var follow = camera != null ? camera.GetComponent<CinemachineFollow>() : null;
        Vector3 dampingAnterior = follow != null ? follow.TrackerSettings.PositionDamping : Vector3.zero;
        if (follow != null) follow.TrackerSettings.PositionDamping = Vector3.zero;
        GameManager.setDeuTeleport(true);
        Vector3 destino = finalSequence != null ? finalSequence.transform.position : new Vector3(1000, 0, 0);
        player.GetComponent<PlayerMovement>()?.ForcarParada();
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = destino;
        }
        player.transform.position = destino;
        Physics2D.SyncTransforms();
        FindAnyObjectByType<RoomDetector>()?.FocarSalaBossAposTeleporte();
        var final = finalSequence != null ? finalSequence : FindAnyObjectByType<FinalBossSequence>();
        if (final != null) final.Begin(player);
        else GameEffects.Instance.OnEnterBossRoom();
        yield return new WaitForEndOfFrame();
        if (follow != null) follow.TrackerSettings.PositionDamping = dampingAnterior;
    }
}
