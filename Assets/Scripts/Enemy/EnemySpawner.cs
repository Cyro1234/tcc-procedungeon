using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    [SerializeField] private int maxEnemiesPerRoom = 3;
    [SerializeField, Min(0.1f)] private float atrasoAparecimento = 1.2f;
    private readonly HashSet<GameObject> aguardandoEntrada = new HashSet<GameObject>();
    private readonly List<GameObject> fumacas = new List<GameObject>();
    private readonly List<GameObject> recompensas = new List<GameObject>();
    private readonly Dictionary<GameObject, BoundsInt> salasDosInimigos = new Dictionary<GameObject, BoundsInt>();
    private List<BoundsInt> salas;

    private static bool Dentro(BoundsInt sala, Vector3 pos) =>
        pos.x >= sala.xMin && pos.x < sala.xMax && pos.y >= sala.yMin && pos.y < sala.yMax;

    public bool TemInimigos(BoundsInt sala)
    {
        foreach (var enemy in enemies)
            if (enemy != null && (salasDosInimigos.TryGetValue(enemy, out var origem)
                ? origem == sala : Dentro(sala, enemy.transform.position))) return true;
        return false;
    }

    public void EntrarNaSala(BoundsInt sala)
    {
        foreach (var enemy in enemies)
            if (enemy != null && Dentro(sala, enemy.transform.position) && aguardandoEntrada.Remove(enemy))
                StartCoroutine(Aparecer(enemy));
    }

    private IEnumerator Aparecer(GameObject enemy)
    {
        var fumaca = new GameObject("Fumaca de invocacao");
        fumaca.transform.position = enemy.transform.position;
        fumacas.Add(fumaca);
        // Sprites quadrados na camada visivel: a camada Default ficava atras do piso.
        EmitirFumaca(fumaca.transform, 12);
        float proximaEmissao = .08f;
        for (float tempo = 0; tempo < atrasoAparecimento; tempo += Time.deltaTime)
        {
            if (tempo >= proximaEmissao)
            {
                EmitirFumaca(fumaca.transform, 4);
                proximaEmissao += .08f;
            }
            yield return null;
        }
        if (enemy != null) enemy.SetActive(true);
        yield return new WaitForSeconds(.8f);
        fumacas.Remove(fumaca);
        Destroy(fumaca);
    }

    private void EmitirFumaca(Transform origem, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var quadrado = new GameObject("Fumaca cinza");
            quadrado.transform.SetParent(origem, false);
            Vector2 deslocamento = Rng.EffectsInsideUnitCircle() * .55f;
            quadrado.transform.localPosition = new Vector3(deslocamento.x, deslocamento.y, 0);
            var sprite = quadrado.AddComponent<SpriteRenderer>();
            sprite.sprite = GameEffects.WhiteSprite;
            float cinza = Rng.EffectsRange(.5f, .8f);
            sprite.color = new Color(cinza, cinza, cinza, .95f);
            sprite.sortingLayerName = "UI";
            sprite.sortingOrder = 400;
            var particula = quadrado.AddComponent<FxParticle>();
            particula.Init(new Vector2(deslocamento.x * .7f, Rng.EffectsRange(.6f, 1.3f)),
                Rng.EffectsRange(.5f, .8f), Rng.EffectsRange(1.8f, 3.4f));
            quadrado.transform.rotation = Quaternion.identity;
        }
    }

    public void PrepararRecompensas(List<BoundsInt> rooms)
    {
        salas = new List<BoundsInt>(rooms);
        foreach (var boss in FindObjectsByType<EnemyMovement>())
            if (boss.CompareTag("Boss") && !enemies.Contains(boss.gameObject))
            {
                enemies.Add(boss.gameObject);
                // A cutscene controla a aparicao do chefe final, sem fumaca de invocacao.
                if (boss.GetComponentInParent<FinalBossSequence>() == null)
                {
                    aguardandoEntrada.Add(boss.gameObject);
                    boss.gameObject.SetActive(false);
                }
            }
        foreach (var enemy in enemies)
            if (enemy != null)
                foreach (var sala in salas)
                    if (Dentro(sala, enemy.transform.position)) { salasDosInimigos[enemy] = sala; break; }
        foreach (var chest in FindObjectsByType<Chest>()) OcultarRecompensa(chest.gameObject);
        foreach (var teleport in FindObjectsByType<Teleporter>()) OcultarRecompensa(teleport.gameObject);
    }

    private void OcultarRecompensa(GameObject objeto)
    {
        foreach (var sala in salas)
            if (Dentro(sala, objeto.transform.position) && TemInimigos(sala))
            {
                objeto.SetActive(false);
                recompensas.Add(objeto);
                return;
            }
    }

    private void Update()
    {
        for (int i = recompensas.Count - 1; i >= 0; i--)
        {
            var recompensa = recompensas[i];
            if (recompensa == null) { recompensas.RemoveAt(i); continue; }
            bool ocupada = false;
            foreach (var sala in salas)
                if (Dentro(sala, recompensa.transform.position) && TemInimigos(sala)) { ocupada = true; break; }
            if (ocupada) continue;
            recompensa.SetActive(true);
            GameEffects.Instance.Burst(recompensa.transform.position, new Color(.9f, .8f, .5f), 12, 2f, null);
            recompensas.RemoveAt(i);
        }
    }
    private List<GameObject> enemies = new List<GameObject>();
    [SerializeField] private WeightedTable<GameObject> enemyTable;
    private WeightedTable<GameObject> tabelaModular;
    private void OnValidate()
    {
        WeightedTable<GameObject>.ValidateInEditor(enemyTable, this, nameof(enemyTable));
    }
    public void SpawnEnemies(SpawnPlacement placement)
    {
        // Filtra apenas o spawn comum. A inicializacao compartilhada com os bosses permanece livre.
        tabelaModular = new WeightedTable<GameObject>();
        if (enemyTable != null && enemyTable.items != null)
            foreach (var entrada in enemyTable.items)
            {
                if (entrada == null || entrada.item == null || entrada.weight <= 0) continue;
                var prefab = entrada.item;
                if (prefab.GetComponentInChildren<EnemyModularBuilder>(true) == null &&
                    prefab.GetComponentInChildren<FastEnemyModularBuilder>(true) == null)
                {
                    Debug.LogWarning("Prefab nao modular ignorado no spawn comum: " + prefab.name, this);
                    continue;
                }
                tabelaModular.items.Add(entrada);
            }
        if (!tabelaModular.TryValidate(out string erro))
            throw new System.InvalidOperationException("Spawn requer inimigos modulares configurados: " + erro);
        // A primeira sala fica livre, exceto se tambem for a sala do teleporte.
        for (int i = 0; i < placement.RoomCount; i++)
        {
            bool salaTeleport = i == placement.TeleportRoomIndex;
            if (i == 0 && !salaTeleport) continue;
            int limite = Mathf.Max(salaTeleport ? 1 : 0, maxEnemiesPerRoom);
            int enemyCount = Rng.EnemyRange(salaTeleport ? 1 : 0, limite + 1);
            for (int j = 0; j < enemyCount; j++)
            {
                Vector2Int position;
                if (salaTeleport && j == 0)
                {
                    if (!placement.TryTakeTeleportEnemyPosition(out position))
                        throw new System.InvalidOperationException("Inimigo obrigatorio do teleporte sem posicao reservada.");
                }
                else if (!placement.TryTakeRandomPosition(i, Rng.enemyRng, out position)) break;
                GameObject enemy = Instantiate(getRandomEnemy(), new Vector3(position.x, position.y, 0), Quaternion.identity);
                InitializeEnemy(enemy);
                enemy.SetActive(false);
                aguardandoEntrada.Add(enemy);
                enemies.Add(enemy);
            }
        }
        Debug.Log("SPAWNOU " + enemies.Count);
    }

    // Inicializa conteudo e drops antes dos callbacks Start dos prefabs.
    public static void InitializeEnemy(GameObject enemy)
    {
        var movements = enemy.GetComponentsInChildren<EnemyMovement>(true);
        for (int i = 0; i < movements.Length; i++) movements[i].InitializeRandom();
        var regular = enemy.GetComponentsInChildren<EnemyModularBuilder>(true);
        for (int i = 0; i < regular.Length; i++)
            regular[i].Initialize(Rng.NewEnemyRandom());
        var fast = enemy.GetComponentsInChildren<FastEnemyModularBuilder>(true);
        for (int i = 0; i < fast.Length; i++)
            fast[i].Initialize(Rng.NewEnemyRandom());
        foreach (var owl in enemy.GetComponentsInChildren<Coruja>(true)) owl.InitializeRandom();
        foreach (var witch in enemy.GetComponentsInChildren<Bruxinha>(true)) witch.InitializeRandom();
    }

    private GameObject getRandomEnemy()
    {
        return tabelaModular.getRandom(Rng.enemyRng);
    }

    public void ClearEnemies()
    {
        StopAllCoroutines();
        foreach (var fumaca in fumacas) if (fumaca != null) Destroy(fumaca);
        fumacas.Clear();
        aguardandoEntrada.Clear();
        salasDosInimigos.Clear();
        recompensas.Clear();
        // Limpa inimigos antes de tudo
        foreach (var enemy in enemies)
        {
            Destroy(enemy);
        }
        enemies.Clear();
    }
}
