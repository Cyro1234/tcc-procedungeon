using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Xiter : MonoBehaviour
{

    private GameObject playerObj;
    private PlayerStatsHandler stats;
    private bool menuDebugAberto;
    private bool carregandoNivel;
    private string mensagemDebug = string.Empty;
    private enum Aba { Niveis, Debuffs, Geracao }
    private Aba abaAtual = Aba.Niveis;
    private StatusEffectKind debuffSelecionado = StatusEffectKind.Drunkenness;
    private string campoSeed = string.Empty;


    void Start()
    {
        playerObj = GameObject.FindWithTag("Player");
        stats = playerObj.GetComponent<PlayerStatsHandler>();
    }

    private void Update()
    {
        if (Keyboard.current == null || carregandoNivel) return;
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            if (menuDebugAberto) FecharDebug();
            else if (GameFlow.CanPlay)
            {
                menuDebugAberto = true;
                mensagemDebug = string.Empty;
                GameFlow.Pause(GameFlow.State.DebugMenu);
            }
            return;
        }
        if (menuDebugAberto)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) FecharDebug();
            return;
        }
        if (!GameFlow.CanPlay) return;
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            tpTeleport();
            return;
        }
        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            maxStr();
            return;
        }
        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            noDamage();
            return;
        }
        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            godMode();
            return;
        }
        // F12: caracol
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            playerObj.GetComponent<StatusEffectManager>()?.ApplyEffect(new SnailEffect());
            return;
        }
        // Insert: bau de raridade aleatoria na frente do jogador
        if (Keyboard.current.insertKey.wasPressedThisFrame)
        {
            spawnarBauNaFrente();
            return;
        }
    }

    private void OnGUI()
    {
        if (!menuDebugAberto) return;
        float escala = Mathf.Min(Screen.width / 640f, Screen.height / 660f, 1.5f);
        Matrix4x4 anterior = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * escala);
        float x = (Screen.width / escala - 440f) * .5f;
        float y = (Screen.height / escala - 610f) * .5f;
        GUI.Box(new Rect(x, y, 440, 610), "DEBUG — F9");
        if (GUI.Button(new Rect(x + 24, y + 35, 124, 34), "Níveis / Armas" + (abaAtual == Aba.Niveis ? " ✓" : "")))
            abaAtual = Aba.Niveis;
        if (GUI.Button(new Rect(x + 158, y + 35, 124, 34), "Debuffs" + (abaAtual == Aba.Debuffs ? " ✓" : "")))
            abaAtual = Aba.Debuffs;
        if (GUI.Button(new Rect(x + 292, y + 35, 124, 34), "Geração" + (abaAtual == Aba.Geracao ? " ✓" : "")))
        {
            if (abaAtual != Aba.Geracao) campoSeed = Rng.baseSeed.ToString();
            abaAtual = Aba.Geracao;
        }
        if (abaAtual == Aba.Debuffs)
        {
            DesenharDebuffs(x, y);
            GUI.matrix = anterior;
            return;
        }
        if (abaAtual == Aba.Geracao)
        {
            DesenharGeracao(x, y);
            GUI.matrix = anterior;
            return;
        }
        if (GUI.Button(new Rect(x + 24, y + 82, 392, 44), "Ir ao nível final — entrada da caverna"))
            StartCoroutine(PularParaFinal(false));
        if (GUI.Button(new Rect(x + 24, y + 136, 392, 44), "Ir direto ao Cocô — iniciar cutscene"))
            StartCoroutine(PularParaFinal(true));
        if (GUI.Button(new Rect(x + 24, y + 190, 190, 38), "Escudo infinito (F7)"))
        {
            noDamage();
            mensagemDebug = "Escudo infinito ativado.";
        }
        if (GUI.Button(new Rect(x + 226, y + 190, 190, 38), "Dano máximo (F6)"))
        {
            maxStr();
            mensagemDebug = "Dano máximo ativado.";
        }
        var vida = playerObj != null ? playerObj.GetComponent<HeartSystem>() : null;
        int penas = vida != null ? vida.Penas : 0;
        if (GUI.Button(new Rect(x + 24, y + 238, 392, 38), "+1 " + PenaFenix.Nome + " (tem " + penas + ")") && vida != null)
        {
            vida.GanharPena();
            mensagemDebug = PenaFenix.Nome + " adicionada. Total: " + vida.Penas + ".";
        }
        GUI.Label(new Rect(x + 24, y + 288, 392, 28), "Equipar arma:");
        for (int i = 0; i < Chest.ArmasDoBau.Length; i++)
        {
            Chest.ItemType arma = Chest.ArmasDoBau[i];
            string nome = arma switch
            {
                Chest.ItemType.BasicSword => "Espada inicial",
                Chest.ItemType.LongSword => "Espada longa",
                Chest.ItemType.Dagger => "Adaga",
                Chest.ItemType.Axe => "Machado",
                Chest.ItemType.Cannon => "Canhão",
                _ => arma.ToString()
            };
            bool equipada = InventoryManager.Instance != null &&
                InventoryManager.Instance.ObterItemSelecionado() == arma;
            if (GUI.Button(new Rect(x + 24 + (i % 2) * 202, y + 322 + (i / 2) * 48, 190, 40),
                nome + (equipada ? " ✓" : "")))
            {
                mensagemDebug = InventoryManager.Instance != null && InventoryManager.Instance.EquiparArmaDebug(arma)
                    ? nome + " equipada." : "Não foi possível equipar a arma.";
            }
        }
        DesenharPocaoDebug(x + 24, y + 470, Chest.ItemType.HealthPotion, "Vida");
        DesenharPocaoDebug(x + 226, y + 470, Chest.ItemType.PowerPotion, "Força");
        GUI.Label(new Rect(x + 24, y + 510, 392, 35), mensagemDebug);
        if (GUI.Button(new Rect(x + 24, y + 550, 392, 42), "Voltar ao jogo — F9 / Esc")) FecharDebug();
        GUI.matrix = anterior;
    }

    private void DesenharPocaoDebug(float x, float y, Chest.ItemType tipo, string nome)
    {
        var inventario = InventoryManager.Instance;
        bool habilitado = GUI.enabled;
        GUI.enabled = habilitado && inventario != null;
        int quantidade = inventario != null ? inventario.ObterQuantidade(tipo) : 0;
        if (GUI.Button(new Rect(x, y, 190, 32), "+1 Poção de " + nome + " (" + quantidade + ")"))
        {
            mensagemDebug = inventario.AdicionarItem(tipo)
                ? "Poção de " + nome + " adicionada. Total: " + inventario.ObterQuantidade(tipo) + "."
                : "Não foi possível adicionar a poção.";
        }
        GUI.enabled = habilitado;
    }

    private static string NomeDebuff(StatusEffectKind kind) => kind switch
    {
        StatusEffectKind.Drunkenness => "Embriaguez",
        StatusEffectKind.HeavyLimp => "Perneta",
        StatusEffectKind.Labyrinthitis => "Labirintite",
        StatusEffectKind.SlipperyToes => "Pés escorregadios",
        StatusEffectKind.Lag => "Lag",
        StatusEffectKind.Sleepwalker => "Sonâmbulo",
        StatusEffectKind.Recoil => "Recuo",
        StatusEffectKind.Snail => "Caracol imortal",
        _ => kind.ToString()
    };

    private void DesenharDebuffs(float x, float y)
    {
        var efeitos = playerObj != null ? playerObj.GetComponent<StatusEffectManager>() : null;
        GUI.Label(new Rect(x + 24, y + 82, 392, 28), "Selecione um debuff e ajuste seus stacks:");
        int stacks = efeitos != null ? efeitos.CountStacks(debuffSelecionado) : 0;
        GUI.Label(new Rect(x + 24, y + 116, 392, 28), NomeDebuff(debuffSelecionado) + " — " + stacks + " stack(s)");
        bool habilitado = GUI.enabled;
        GUI.enabled = efeitos != null;
        if (GUI.Button(new Rect(x + 24, y + 152, 190, 40), "+ Acrescentar 1 stack"))
        {
            efeitos.AddDebugStack(debuffSelecionado);
            mensagemDebug = "Adicionado: " + NomeDebuff(debuffSelecionado);
        }
        GUI.enabled = efeitos != null && stacks > 0;
        if (GUI.Button(new Rect(x + 226, y + 152, 190, 40), "− Retirar 1 stack"))
        {
            efeitos.RemoveDebugStack(debuffSelecionado);
            mensagemDebug = "Removido: " + NomeDebuff(debuffSelecionado);
        }
        GUI.enabled = habilitado;
        var tipos = (StatusEffectKind[])System.Enum.GetValues(typeof(StatusEffectKind));
        int i = 0;
        foreach (var tipo in tipos)
        {
            if (tipo == StatusEffectKind.None) continue;
            int quantidade = efeitos != null ? efeitos.CountStacks(tipo) : 0;
            string titulo = NomeDebuff(tipo) + " ×" + quantidade + (tipo == debuffSelecionado ? " ✓" : "");
            if (GUI.Button(new Rect(x + 24 + (i % 2) * 202, y + 212 + (i / 2) * 48, 190, 40), titulo))
                debuffSelecionado = tipo;
            i++;
        }
        GUI.Label(new Rect(x + 24, y + 410, 392, 45), StatusEffectFactory.Describe(debuffSelecionado));
        GUI.Label(new Rect(x + 24, y + 454, 392, 28), "Stacks adicionados aqui duram até serem retirados.");
        GUI.Label(new Rect(x + 24, y + 488, 392, 48), mensagemDebug);
        if (GUI.Button(new Rect(x + 24, y + 550, 392, 42), "Voltar ao jogo — F9 / Esc")) FecharDebug();
    }

    // Aba Geração: reiniciar a partida e trocar a seed. Reiniciar recarrega a cena,
    // entao vida, itens, debuffs e andar voltam ao comeco
    private void DesenharGeracao(float x, float y)
    {
        GUI.Label(new Rect(x + 24, y + 82, 392, 28), "Seed atual: " + Rng.baseSeed + "   •   Andar " + Rng.CurrentFloor);
        if (GUI.Button(new Rect(x + 24, y + 116, 392, 38), "Copiar seed atual"))
        {
            GUIUtility.systemCopyBuffer = Rng.baseSeed.ToString();
            mensagemDebug = "Seed " + Rng.baseSeed + " copiada.";
        }

        if (GUI.Button(new Rect(x + 24, y + 172, 392, 44), "Reiniciar o jogo — mesma seed"))
            GameManager.ReiniciarComSeed(Rng.baseSeed);
        if (GUI.Button(new Rect(x + 24, y + 226, 392, 44), "Reiniciar o jogo — seed aleatória"))
            GameManager.ReiniciarComSeed(System.DateTime.Now.GetHashCode());

        GUI.Label(new Rect(x + 24, y + 290, 392, 28), "Trocar a seed (número ou palavra):");
        GUI.SetNextControlName("CampoSeed");
        campoSeed = GUI.TextField(new Rect(x + 24, y + 322, 392, 36), campoSeed, 40);
        bool enterNoCampo = Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) &&
            GUI.GetNameOfFocusedControl() == "CampoSeed";
        bool habilitado = GUI.enabled;
        GUI.enabled = !string.IsNullOrWhiteSpace(campoSeed);
        if ((GUI.Button(new Rect(x + 24, y + 368, 392, 44), "Gerar com esta seed") || enterNoCampo) && GUI.enabled)
            GameManager.ReiniciarComSeed(SeedDoTexto(campoSeed));
        GUI.enabled = habilitado;

        GUI.Label(new Rect(x + 24, y + 424, 392, 48),
            "A mesma seed gera sempre o mesmo mapa, inimigos, baús e maldições.");
        GUI.Label(new Rect(x + 24, y + 488, 392, 48), mensagemDebug);
        if (GUI.Button(new Rect(x + 24, y + 550, 392, 42), "Voltar ao jogo — F9 / Esc")) FecharDebug();
    }

    // Numero vira a propria seed; palavra vira um numero fixo (o mesmo texto sempre da a mesma seed)
    private static int SeedDoTexto(string texto)
    {
        texto = texto.Trim();
        if (int.TryParse(texto, out int numero)) return numero;
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in texto.ToLowerInvariant()) hash = (hash ^ c) * 16777619;
            return hash;
        }
    }

    private IEnumerator PularParaFinal(bool diretoAoBoss)
    {
        var gerador = FindAnyObjectByType<RoomFirstDungeonGenerator>();
        if (carregandoNivel || gerador == null || playerObj == null) yield break;
        carregandoNivel = true;
        menuDebugAberto = false;
        try
        {
            playerObj.GetComponent<PlayerMovement>()?.ForcarParada();
            gerador.GerarNivelFinalDebug();
            // Espera a destruicao da sala anterior antes de localizar a nova cutscene.
            yield return null;
            GameFlow.Resume(GameFlow.State.DebugMenu);
            if (diretoAoBoss)
            {
                var final = gerador.FinalSequence;
                if (final == null) throw new System.InvalidOperationException("Sala final nao foi criada.");
                yield return StartCoroutine(Teleporter.TeleportarParaBoss(playerObj, final));
            }
        }
        finally
        {
            carregandoNivel = false;
            GameFlow.Resume(GameFlow.State.DebugMenu);
        }
    }

    private void FecharDebug()
    {
        menuDebugAberto = false;
        GameFlow.Resume(GameFlow.State.DebugMenu);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        carregandoNivel = false;
        FecharDebug();
    }

    private void spawnarBauNaFrente()
    {
        var spawner = FindAnyObjectByType<ChestSpawner>();
        if (spawner == null || playerObj == null) return;
        var movimento = playerObj.GetComponent<PlayerMovement>();
        Vector2 frente = movimento != null ? movimento.FacingDirection : Vector2.down;
        // 2 tiles de distancia: perto o bastante pra ver, longe o bastante pra nao abrir sozinho em cima do jogador
        spawner.SpawnarBauDeTeste(playerObj.transform.position + (Vector3)(frente.normalized * 2f));
    }

    private void tpTeleport()
    {
        if (playerObj != null) StartCoroutine(Teleporter.TeleportarParaBoss(playerObj));
    }
    private void maxStr()
    {
        stats.AddTemporaryModifier(new StatModifier("AumentarDano", StatType.Damage, ModifierSource.Buff, ModifierType.Flat, float.MaxValue, 0f));
    }
    private void noDamage()
    {
        playerObj.GetComponent<HeartSystem>()?.EquipShield(int.MaxValue);
    }
    private void godMode()
    {
        noDamage();
        maxStr();
    }

}
