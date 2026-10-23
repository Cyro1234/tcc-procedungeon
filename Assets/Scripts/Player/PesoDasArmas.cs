using UnityEngine;

// Armas pesadas deixam o jogador mais lento enquanto estao na mao.
// Usa um modificador de velocidade so, trocado quando a arma na mao muda.
// O Attack.cs adiciona este componente no Player, entao nao precisa mexer no prefab.
public class PesoDasArmas : MonoBehaviour
{
    [Tooltip("Fracao tirada da velocidade com o canhao na mao (0.3 = 30% mais lento).")]
    [SerializeField, Range(0f, 0.9f)] private float lentidaoCanhao = 0.3f;
    [Tooltip("Fracao tirada da velocidade com a espada grande na mao.")]
    [SerializeField, Range(0f, 0.9f)] private float lentidaoEspadaGrande = 0.1f;

    private PlayerStatsHandler stats;
    private StatModifier modificador;
    private float lentidaoAtual;

    private void Awake() => stats = GetComponent<PlayerStatsHandler>();

    private void OnDisable() => Aplicar(0f);

    private void Update()
    {
        var inventario = InventoryManager.Instance;
        Aplicar(inventario != null ? Lentidao(inventario.ObterItemSelecionado()) : 0f);
    }

    private float Lentidao(Chest.ItemType arma) => arma switch
    {
        Chest.ItemType.Cannon => lentidaoCanhao,
        Chest.ItemType.LongSword => lentidaoEspadaGrande,
        _ => 0f
    };

    private void Aplicar(float lentidao)
    {
        if (stats == null || Mathf.Approximately(lentidao, lentidaoAtual)) return;
        if (modificador != null) stats.RemoveSimpleModifier(modificador);
        modificador = null;
        if (lentidao > 0f)
        {
            modificador = new StatModifier("Peso da arma", StatType.Speed,
                ModifierSource.Item, ModifierType.Percent, -lentidao);
            stats.AddSimpleModifier(modificador);
        }
        lentidaoAtual = lentidao;
    }
}
