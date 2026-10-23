using UnityEngine;

public class Goblin : MonoBehaviour
{

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float cooldown = 0.8f;
    [SerializeField] private int maxAmmo = 12;
    [SerializeField] private float reloadTime = 3.0f;

    private int ammo = 0;
    private float proximoTiro = 0f;
    private Transform player;
    private bool recarregando;
    public int ProjeteisRestantes => Mathf.Max(0, ammo);
    public string EstadoDisparos => ammo <= 0 || (recarregando && Time.time < proximoTiro) ? "RECARREGANDO" : "ATIRANDO";

    private void Start()
    {
        if (GetComponent<BossAmmoIndicator>() == null) gameObject.AddComponent<BossAmmoIndicator>();
        // ammo = maxAmmo; Comeca sem municao para dar tempo do jogador reagir ao teleporte

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
                recarregando = false;
                Shoot();
                proximoTiro = Time.time + cooldown;
            }
            else // Precisa recarregar
            {
                ammo = maxAmmo;
                recarregando = true;
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

// Compartilhado pelos dois bosses. Le os contadores reais sem alterar a cadencia dos ataques.
public class BossAmmoIndicator : MonoBehaviour
{
    private Goblin goblin;
    private Coco coco;
    private TMPro.TextMeshPro texto;
    private UnityEngine.MeshRenderer renderTexto;
    private SpriteRenderer[] partes;
    private GameObject objeto;
    private int ultimoNumero = -1;
    private string ultimoEstado;

    private void Start()
    {
        goblin = GetComponent<Goblin>();
        coco = GetComponent<Coco>();
        partes = GetComponentsInChildren<SpriteRenderer>(true);
        objeto = new GameObject("Projeteis restantes do boss");
        objeto.transform.SetParent(transform, false);
        texto = objeto.AddComponent<TMPro.TextMeshPro>();
        texto.fontSize = 3.2f;
        texto.alignment = TMPro.TextAlignmentOptions.Center;
        texto.fontStyle = TMPro.FontStyles.Bold;
        texto.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        texto.rectTransform.sizeDelta = new Vector2(6f, 1.3f);
        texto.outlineWidth = .2f;
        texto.outlineColor = Color.black;
        renderTexto = objeto.GetComponent<UnityEngine.MeshRenderer>();
        renderTexto.sortingLayerName = "UI";
        renderTexto.sortingOrder = 500;
        objeto.SetActive(false);
    }

    private void LateUpdate()
    {
        if (objeto == null) return;
        bool ativo = GameManager.getDeuTeleport() && GameFlow.CanPlay &&
            ((goblin != null && goblin.isActiveAndEnabled) || (coco != null && coco.isActiveAndEnabled));
        objeto.SetActive(ativo);
        if (!ativo) return;
        int numero = goblin != null ? goblin.ProjeteisRestantes : coco.ProjeteisRestantes;
        string estado = goblin != null ? goblin.EstadoDisparos : coco.EstadoDisparos;
        if (numero != ultimoNumero || estado != ultimoEstado)
        {
            texto.text = $"<size=140%>{numero}</size> TIROS\n{estado}";
            texto.color = estado == "ATIRANDO" ? new Color(1f, .7f, .3f) : new Color(.85f, .9f, 1f);
            ultimoNumero = numero;
            ultimoEstado = estado;
        }
        float topo = transform.position.y;
        foreach (var parte in partes)
            if (parte != null && parte.enabled) topo = Mathf.Max(topo, parte.bounds.max.y);
        Vector3 escala = transform.lossyScale;
        objeto.transform.localScale = new Vector3(1f / Mathf.Max(.001f, Mathf.Abs(escala.x)),
            1f / Mathf.Max(.001f, Mathf.Abs(escala.y)), 1);
        objeto.transform.rotation = Quaternion.identity;
        objeto.transform.position = new Vector3(transform.position.x, topo + .8f, transform.position.z);
    }

    private void OnDisable()
    {
        if (objeto != null) objeto.SetActive(false);
    }
}
