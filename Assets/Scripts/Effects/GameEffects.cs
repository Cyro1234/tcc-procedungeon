using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

// Sumario: central dos efeitos de jogo.
// - Hit-stop (hehe quase hitslop uau)
// - Tremida de camera
// - Flash de tela, fade entre andares e banners de texto ("Andar 2 - Deserto", "CHEFE!", etc)
// - Textos flutuantes
// - Efeitos sonoros procedurais

// Ele se cria sozinho na primeira vez que alguem chama GameEffects.Instance, entao NAO precisa
// colocar nada na cena!!!! Os efeitos usam um fluxo proprio do Rng, derivado da seed,
// separado de todos os sorteios de gameplay e geracao procedural.
public class GameEffects : MonoBehaviour
{
    private static GameEffects instance;
    private static bool quitting;

    public static GameEffects Instance
    {
        get
        {
            if (instance == null && !quitting)
            {
                var go = new GameObject("[GameEffects]");
                instance = go.AddComponent<GameEffects>();
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        quitting = false;
        whiteSprite = null;
    }

    public enum Sfx { Kill, Heal }

    private const float MaxShakeOffset = 0.35f;

    private float hitStopUntil;
    private bool inHitStop;

    private float trauma;
    private CinemachineCameraOffset cameraOffset;
    private bool searchedCamera;

    private Transform player;
    private RoomDetector roomDetector;
    private bool roomLocked;

    private Canvas canvas;
    private Image flashImage;
    private Image fadeImage;
    private TextMeshProUGUI bannerTitle;
    private TextMeshProUGUI bannerSubtitle;
    private TextMeshProUGUI summaryText;
    private Coroutine bannerRoutine;
    private Coroutine flashRoutine;
    private Coroutine fadeRoutine;

    private AudioSource audioSource;
    private AudioClip[] clips;

    private static Sprite whiteSprite;
    private readonly Dictionary<Chest.ItemType, Sprite> iconesItens = new Dictionary<Chest.ItemType, Sprite>();
    private readonly Queue<(Vector3 posicao, string texto, Color cor, Chest.ItemType tipo)> coletas = new Queue<(Vector3, string, Color, Chest.ItemType)>();
    private Coroutine rotinaColeta;
    private GameObject coletaVisual;
    // Sprite branco 4x4 usado nas particulas
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.filterMode = FilterMode.Point;
                tex.Apply();
                whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 32f);
                whiteSprite.name = "GameEffects_White";
            }
            return whiteSprite;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        BuildUI();
        BuildClips();
        HookRoomDetector();
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    private void OnDestroy()
    {
        foreach (var icone in iconesItens.Values) { Destroy(icone.texture); Destroy(icone); }
        if (roomDetector != null)
        {
            roomDetector.AoEntrarNaSalaComInimigos -= OnRoomLocked;
            roomDetector.AoLimparSala -= OnRoomCleared;
        }

        if (cameraOffset != null) cameraOffset.Offset = Vector3.zero;

        // Se sair da cena no meio de um hit-stop, nao deixa o tempo travado (bizarro qnd isso acontece)
        if (inHitStop) GameFlow.SetHitStop(false);

        if (instance == this) instance = null;
    }

    private void Update()
    {
        UpdateShake();
    }

    // HITSLOP!!!!!!!

    // Congela o jogo por "duration" segundos (tempo real). So funciona se o jogo estiver rodando
    // normalmente (timeScale == 1), assim nao atrapalha pause, menu de downgrade, e coisa e tals
    public void HitStop(float duration)
    {
        if (!GameFlow.CanPlay) return;
        if (!inHitStop && !Mathf.Approximately(Time.timeScale, 1f)) return;

        hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);
        if (!inHitStop) StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        inHitStop = true;
        GameFlow.SetHitStop(true);

        while (Time.unscaledTime < hitStopUntil)
        {
            // Alguem (pause, game over...) mexeu no tempo no meio do hit-stop: respeita e sai.
            if (!GameFlow.CanPlay || !GameFlow.IsHitStopped)
            {
                inHitStop = false;
                GameFlow.SetHitStop(false);
                yield break;
            }
            yield return null;
        }

        GameFlow.SetHitStop(false);
        inHitStop = false;
    }

    // CAMERA SHAKE

    // amount entre 0 e 1. Os valores se acumulam e decaem sozinhos.
    public void Shake(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    private void UpdateShake()
    {
        if (!searchedCamera || cameraOffset == null)
        {
            if (trauma <= 0f) return;
            FindCamera();
            if (cameraOffset == null) return;
        }

        if (trauma > 0f)
        {
            trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.6f);
            float power = trauma * trauma * MaxShakeOffset;
            float t = Time.unscaledTime * 28f;
            cameraOffset.Offset = new Vector3(
                (Mathf.PerlinNoise(t, 0.37f) * 2f - 1f) * power,
                (Mathf.PerlinNoise(0.91f, t) * 2f - 1f) * power,
                0f);
        }
        else if (cameraOffset.Offset != Vector3.zero)
        {
            cameraOffset.Offset = Vector3.zero;
        }
    }

    private void FindCamera()
    {
        searchedCamera = true;
        CinemachineCamera cam = FindAnyObjectByType<CinemachineCamera>();
        if (cam == null) return;

        cameraOffset = cam.GetComponent<CinemachineCameraOffset>();
        if (cameraOffset == null)
        {
            cameraOffset = cam.gameObject.AddComponent<CinemachineCameraOffset>();
            cameraOffset.ApplyAfter = CinemachineCore.Stage.Finalize;
            cameraOffset.PreserveComposition = false;
        }
    }

    // EVENTOS DE GAMEPLAY

    public void EnemyHit(Vector3 position, SpriteRenderer reference)
    {
        HitStop(0.04f);
        Shake(0.12f);
        Burst(position, new Color(1f, 0.9f, 0.7f), 5, 4f, reference);
    }

    public void EnemyKilled(Vector3 position, bool boss, SpriteRenderer reference)
    {
        HitStop(boss ? 0.35f : 0.07f);
        Shake(boss ? 0.9f : 0.22f);
        PlaySfx(Sfx.Kill, boss ? 1f : 0.8f, 0.15f);
        Burst(position, new Color(0.85f, 0.2f, 0.25f), boss ? 40 : 12, boss ? 8f : 5f, reference);
        Burst(position, Color.white, boss ? 20 : 6, boss ? 6f : 3f, reference);

        if (boss)
        {
            FlashScreen(new Color(1f, 1f, 1f, 0.6f), 0.4f);
            Banner("CHEFE DERROTADO!", "A escada foi liberada", new Color(1f, 0.85f, 0.3f), 2.5f);
        }
    }

    public void PlayerHurt(Vector3 position)
    {
        HitStop(0.09f);
        Shake(0.45f);
        FlashScreen(new Color(0.9f, 0.05f, 0.05f, 0.35f), 0.35f);
        Burst(position, new Color(1f, 0.25f, 0.25f), 10, 5f, null);
    }

    public void PlayerShieldHit(Vector3 position, bool broke = false)
    {
        HitStop(0.05f);
        Shake(broke ? 0.4f : 0.2f);
        Burst(position, new Color(0.5f, 0.8f, 1f), broke ? 16 : 8, 5f, null);
        Popup(position + Vector3.up * 0.8f, broke ? "Escudo quebrou!" : "Bloqueado!", new Color(0.55f, 0.85f, 1f), 0.8f);
    }

    public void PlayerHealed(Vector3 position, int amount)
    {
        Popup(position + Vector3.up * 0.8f, $"+{amount} Vida", new Color(1f, 0.4f, 0.5f), 1f);
        Burst(position, new Color(1f, 0.45f, 0.55f), 10, 3f, null);
        FlashScreen(new Color(1f, 0.4f, 0.5f, 0.15f), 0.25f);
        PlaySfx(Sfx.Heal, 1f, 0f);
    }

    public void ItemObtained(Vector3 position, string text, Color color, Chest.ItemType tipo = Chest.ItemType.None)
    {
        Burst(position, color, 18, 4.5f, null);
        coletas.Enqueue((position, text, color, tipo));
        if (rotinaColeta == null) rotinaColeta = StartCoroutine(AnimarColetas());
    }

    private IEnumerator AnimarColetas()
    {
        while (coletas.Count > 0)
        {
            var coleta = coletas.Dequeue();
            coletaVisual = new GameObject("Item obtido", typeof(RectTransform), typeof(CanvasGroup));
            coletaVisual.transform.SetParent(canvas.transform, false);
            var grupo = coletaVisual.GetComponent<CanvasGroup>();
            grupo.blocksRaycasts = false;
            grupo.interactable = false;
            var imagemGo = new GameObject("Imagem do item", typeof(RectTransform), typeof(Image));
            imagemGo.transform.SetParent(coletaVisual.transform, false);
            var imagem = imagemGo.GetComponent<Image>();
            imagem.sprite = IconeItem(coleta.tipo, coleta.cor);
            imagem.preserveAspect = true;
            imagem.raycastTarget = false;
            var rect = imagem.rectTransform;
            rect.sizeDelta = new Vector2(120, 120);
            var texto = CreateText("Nome do item", coletaVisual.transform, Vector2.one * .5f, 30);
            texto.textWrappingMode = TextWrappingModes.Normal;
            texto.rectTransform.sizeDelta = new Vector2(900, 110);
            texto.rectTransform.anchoredPosition = new Vector2(0, -48);
            texto.text = coleta.texto;
            texto.color = coleta.cor;
            texto.alpha = 0;
            Vector2 inicio = Vector2.zero;
            var camera = Camera.main;
            if (camera != null)
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)coletaVisual.transform,
                    camera.WorldToScreenPoint(coleta.posicao), null, out inicio);
            var destino = new Vector2(0, 60);
            for (float t = 0; t < .55f; t += Time.deltaTime)
            {
                float p = Mathf.Clamp01(t / .55f);
                float suave = 1f - Mathf.Pow(1f - p, 3);
                rect.anchoredPosition = Vector2.Lerp(inicio, destino, suave) + Vector2.up * Mathf.Sin(p * Mathf.PI) * 100;
                rect.localScale = Vector3.one * Mathf.Lerp(.35f, 1.15f, suave);
                yield return null;
            }
            rect.anchoredPosition = destino;
            for (float t = 0; t < .18f; t += Time.deltaTime)
            {
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, t / .18f);
                texto.alpha = Mathf.Clamp01(t / .18f);
                yield return null;
            }
            rect.localScale = Vector3.one;
            texto.alpha = 1;
            yield return new WaitForSeconds(1.5f);
            for (float t = 0; t < .4f; t += Time.deltaTime)
            {
                grupo.alpha = 1f - t / .4f;
                yield return null;
            }
            Destroy(coletaVisual);
        }
        rotinaColeta = null;
    }

    private Sprite IconeItem(Chest.ItemType tipo, Color cor)
    {
        Sprite arte = ItemSprites.Obter(tipo);
        if (arte != null) return arte;
        var inventario = InventoryManager.Instance;
        if (inventario != null)
        {
            Sprite sprite = tipo switch
            {
                Chest.ItemType.LongSword => inventario.spriteEspadaLonga,
                Chest.ItemType.Dagger => inventario.spriteAdaga,
                Chest.ItemType.Axe => inventario.spriteMachado,
                Chest.ItemType.HealthPotion => inventario.spritePocaoVida,
                Chest.ItemType.PowerPotion => inventario.spritePocaoPoder,
                Chest.ItemType.BasicSword => inventario.ObterSprite(tipo),
                _ => null
            };
            if (sprite != null) return sprite;
        }
        if (iconesItens.TryGetValue(tipo, out var existente)) return existente;
        // Arte simples para os itens que ainda nao possuem sprite configurado na cena.
        var textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        bool pocao = InventoryManager.EhConsumivel(tipo);
        bool bota = tipo == Chest.ItemType.SpeedBoots || tipo == Chest.ItemType.LegendarySpeedBoots;
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                bool preenchido;
                if (pocao) preenchido = (x >= 12 && x <= 19 && y >= 21 && y <= 28) ||
                    (Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 13)) < 10);
                else if (bota) preenchido = (x >= 10 && x <= 19 && y >= 8 && y <= 27) ||
                    (x >= 10 && x <= 26 && y >= 4 && y <= 12);
                else if (tipo == Chest.ItemType.Axe) preenchido = (x >= 14 && x <= 17 && y >= 3 && y <= 28) ||
                    (x >= 5 && x <= 24 && y >= 18 && y <= 26);
                else preenchido = y >= 4 && y <= 27 && Mathf.Abs(x - 15.5f) <= Mathf.Min(11, (y - 2) * .65f);
                Color pixel = cor;
                if (pocao && y >= 25) pixel = new Color(.65f, .43f, .22f);
                else if (x <= 12 && preenchido) pixel = Color.Lerp(cor, Color.white, .45f);
                textura.SetPixel(x, y, preenchido ? pixel : Color.clear);
            }
        textura.Apply();
        var resultado = Sprite.Create(textura, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
        iconesItens[tipo] = resultado;
        return resultado;
    }

    public void EnemyNoticed(Vector3 position)
    {
        Popup(position + Vector3.up * 0.8f, "!", new Color(1f, 0.85f, 0.2f), 1.4f, 0.45f);
    }

    public void OnNewFloor(int floor, TileMapVisualizer.Biomas bioma)
    {
        if (rotinaColeta != null) StopCoroutine(rotinaColeta);
        rotinaColeta = null;
        coletas.Clear();
        if (coletaVisual != null) Destroy(coletaVisual);
        RunStats.SetFloor(floor);
        roomLocked = false;
        HeartPickup.ClearAll();
        HookRoomDetector();

        FadeFromBlack(0.7f);
        Banner($"Andar {floor}", BiomaDisplayName(bioma), BiomaColor(bioma), 2.2f);
    }

    public void OnEnterBossRoom()
    {
        roomLocked = false;
        Shake(0.5f);
        FlashScreen(new Color(0.6f, 0f, 0.1f, 0.45f), 0.6f);
        Banner(NomeDoChefe().ToUpper(), "Sobreviva e derrote-o para descer", new Color(1f, 0.3f, 0.3f), 2f);
    }

    // Descobre qual chefe esta na sala pelo script dele.
    // Chefe sem script proprio usa o nome do proprio objeto pra nao ficar bugado e tals
    private static string NomeDoChefe()
    {
        GameObject maisPerto = null;
        float menorDistancia = float.MaxValue;
        foreach (GameObject boss in GameObject.FindGameObjectsWithTag("Boss"))
        {
            float d = Vector2.Distance(boss.transform.position, new Vector2(1000f, 0f));
            if (d < menorDistancia) { menorDistancia = d; maisPerto = boss; }
        }
        if (maisPerto == null) return "Chefe";

        if (maisPerto.GetComponentInParent<Goblin>() != null || maisPerto.GetComponentInChildren<Goblin>() != null) return "Goblin";
        if (maisPerto.GetComponentInParent<Coruja>() != null || maisPerto.GetComponentInChildren<Coruja>() != null) return "Coruja Dourada";
        if (maisPerto.GetComponentInParent<Bruxinha>() != null || maisPerto.GetComponentInChildren<Bruxinha>() != null) return "Bruxinha";
        if (maisPerto.GetComponentInParent<Coco>() != null || maisPerto.GetComponentInChildren<Coco>() != null) return "Coco";

        string nome = maisPerto.name.Replace("(Clone)", "").Trim();
        if (string.IsNullOrEmpty(nome)) return "Chefe";
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nome.ToLower());
    }

    public void OnDash(Vector3 position)
    {
        Burst(position, new Color(0.8f, 0.9f, 1f), 6, 2f, null);
    }

    // SALAS (portas trancando / sala limpa)

    private void HookRoomDetector()
    {
        if (roomDetector != null) return;
        roomDetector = FindAnyObjectByType<RoomDetector>();
        if (roomDetector != null)
        {
            roomDetector.AoEntrarNaSalaComInimigos += OnRoomLocked;
            roomDetector.AoLimparSala += OnRoomCleared;
        }
    }

    private void OnRoomLocked(BoundsInt bounds)
    {
        if (roomLocked) return;
        roomLocked = true;
        Shake(0.3f);
    }

    private void OnRoomCleared()
    {
        if (!roomLocked) return;
        roomLocked = false;
        Transform p = GetPlayer();
        if (p != null) Burst(p.position, new Color(0.5f, 1f, 0.55f), 14, 4f, null);
    }

    private Transform GetPlayer()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }
        return player;
    }

    // TEXTO FLUTUANTE E PARTICULAS

    public void Popup(Vector3 position, string text, Color color, float size = 1f, float lifetime = 0.8f)
    {
        var go = new GameObject("Popup");
        go.transform.position = position + new Vector3(Rng.EffectsRange(-0.15f, 0.15f), 0f, 0f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 4.5f * size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color = color;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.rectTransform.sizeDelta = new Vector2(6f, 1f);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingLayerName = "UI";
            mr.sortingOrder = 500;
        }

        var ft = go.AddComponent<FloatingText>();
        ft.Init(lifetime);
    }

    // Explosao de particulas quadradinhas
    // de ordenacao de um sprite existente, assim as particulas ficam com a mesma iluminacao.
    public void Burst(Vector3 position, Color color, int count, float speed, SpriteRenderer reference)
    {
        int sortingLayer = 0;
        int sortingOrder = 400;
        Material material = null;

        if (reference != null)
        {
            sortingLayer = reference.sortingLayerID;
            sortingOrder = reference.sortingOrder + 10;
            material = reference.sharedMaterial;
        }
        else
        {
            Transform p = GetPlayer();
            SpriteRenderer psr = p != null ? p.GetComponent<SpriteRenderer>() : null;
            if (psr != null)
            {
                sortingLayer = psr.sortingLayerID;
                sortingOrder = psr.sortingOrder + 10;
                material = psr.sharedMaterial;
            }
        }

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Particle");
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite;
            sr.color = color;
            sr.sortingLayerID = sortingLayer;
            sr.sortingOrder = sortingOrder;
            if (material != null) sr.sharedMaterial = material;

            Vector2 dir = Rng.EffectsInsideUnitCircle().normalized;
            var particle = go.AddComponent<FxParticle>();
            particle.Init(dir * speed * Rng.EffectsRange(0.4f, 1f), Rng.EffectsRange(0.25f, 0.55f), Rng.EffectsRange(0.8f, 1.6f));
        }
    }

    // UI (flash, fade, banner, resumo da run)

    private void BuildUI()
    {
        var canvasGo = new GameObject("[GameEffects UI]", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; 

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        flashImage = CreateFullscreenImage("Flash", canvasGo.transform);
        fadeImage = CreateFullscreenImage("Fade", canvasGo.transform);

        bannerTitle = CreateText("BannerTitle", canvasGo.transform, new Vector2(0.5f, 0.72f), 96f);
        bannerSubtitle = CreateText("BannerSubtitle", canvasGo.transform, new Vector2(0.5f, 0.64f), 44f);
        summaryText = CreateText("RunSummary", canvasGo.transform, new Vector2(0.5f, 0.12f), 34f);
        summaryText.rectTransform.sizeDelta = new Vector2(1600f, 200f);
    }

    private Image CreateFullscreenImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        img.color = new Color(0f, 0f, 0f, 0f);
        return img;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchor, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(1600f, 140f);
        rt.anchoredPosition = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.text = string.Empty;
        return tmp;
    }

    public void FlashScreen(Color color, float duration)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FadeImage(flashImage, color, duration));
    }

    public void FadeFromBlack(float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeImage(fadeImage, Color.black, duration));
    }

    private IEnumerator FadeImage(Image image, Color color, float duration)
    {
        float startAlpha = color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, t / duration);
            image.color = color;
            yield return null;
        }
        color.a = 0f;
        image.color = color;
    }

    public void Banner(string title, string subtitle, Color color, float hold)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(title, subtitle, color, hold));
    }

    private IEnumerator BannerRoutine(string title, string subtitle, Color color, float hold)
    {
        bannerTitle.text = title;
        bannerSubtitle.text = subtitle;
        Color subColor = Color.white;

        const float fadeIn = 0.25f;
        const float fadeOut = 0.6f;
        float t = 0f;

        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeIn);
            float scale = Mathf.LerpUnclamped(1.4f, 1f, EaseOutBack(k));
            SetBanner(color, subColor, k, scale);
            yield return null;
        }

        SetBanner(color, subColor, 1f, 1f);
        float holdEnd = Time.unscaledTime + hold;
        while (Time.unscaledTime < holdEnd) yield return null;

        t = 0f;
        while (t < fadeOut)
        {
            t += Time.unscaledDeltaTime;
            SetBanner(color, subColor, 1f - Mathf.Clamp01(t / fadeOut), 1f);
            yield return null;
        }

        SetBanner(color, subColor, 0f, 1f);
        bannerTitle.text = string.Empty;
        bannerSubtitle.text = string.Empty;
    }

    private void SetBanner(Color titleColor, Color subColor, float alpha, float scale)
    {
        titleColor.a = alpha;
        subColor.a = alpha;
        bannerTitle.color = titleColor;
        bannerSubtitle.color = subColor;
        bannerTitle.rectTransform.localScale = Vector3.one * scale;
    }

    public void ShowRunSummary()
    {
        string newRecord = RunStats.Floor >= RunStats.BestFloor ? "  <color=#FFD54A>NOVO RECORDE!</color>" : string.Empty;
        summaryText.color = Color.white;
        summaryText.text =
            $"Andar {RunStats.Floor}  |  {RunStats.Kills} inimigos  |  {RunStats.BossesKilled} chefes  |  {RunStats.ElapsedText()}\n" +
            $"<size=70%>Recorde: Andar {RunStats.BestFloor}{newRecord}   -   [R] tentar de novo</size>";
    }

    // AUDIO PROCEDURAL oOooOOOOoOOOo

    public void PlaySfx(Sfx sfx, float volume = 1f, float pitchVariation = 0f)
    {
        if (clips == null) return;
        AudioClip clip = clips[(int)sfx];
        if (clip == null) return;

        // Respeita o volume de efeitos configurado pelo jogador
        float master = 1f;
        if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
        {
            master = AudioManager.Instance.sfxSource.volume;
        }

        audioSource.pitch = 1f + Rng.EffectsRange(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clip, volume * master);
    }

    private void BuildClips()
    {
        clips = new AudioClip[System.Enum.GetValues(typeof(Sfx)).Length];
        clips[(int)Sfx.Kill] = Tone("kill", 0.12f, 420f, 90f, 0.16f, Wave.Square);
        clips[(int)Sfx.Heal] = Arp("heal", new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.055f, 0.12f);
    }

    private enum Wave { Sine, Square, Noise }

    private static AudioClip Tone(string name, float duration, float startFreq, float endFreq, float volume, Wave wave)
    {
        const int sampleRate = 44100;
        int samples = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        FillTone(data, 0, samples, sampleRate, startFreq, endFreq, volume, wave, 0);
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip Arp(string name, float[] notes, float noteDuration, float volume)
    {
        const int sampleRate = 44100;
        int perNote = Mathf.RoundToInt(sampleRate * noteDuration);
        int tail = perNote * 2; // a ultima nota soa um pouco mais
        int samples = perNote * (notes.Length - 1) + tail;
        float[] data = new float[samples];
        for (int i = 0; i < notes.Length; i++)
        {
            int length = i == notes.Length - 1 ? tail : perNote;
            FillTone(data, i * perNote, length, sampleRate, notes[i], notes[i], volume, Wave.Square, i);
        }
        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static void FillTone(float[] data, int offset, int length, int sampleRate, float startFreq, float endFreq, float volume, Wave wave, int seed)
    {
        var noise = new System.Random(seed);
        double phase = 0;
        float attack = sampleRate * 0.004f;

        for (int i = 0; i < length && offset + i < data.Length; i++)
        {
            float t = (float)i / length;
            float freq = Mathf.Lerp(startFreq, endFreq, t);
            phase += 2.0 * Mathf.PI * freq / sampleRate;

            float s;
            switch (wave)
            {
                case Wave.Square: s = System.Math.Sin(phase) >= 0 ? 0.6f : -0.6f; break;
                case Wave.Noise:
                    // Ruido filtrado pela frequencia (soa como "whoosh"/impacto)
                    float n = (float)(noise.NextDouble() * 2.0 - 1.0);
                    s = Mathf.Lerp(n, (float)System.Math.Sin(phase), 0.35f);
                    break;
                default: s = (float)System.Math.Sin(phase); break;
            }

            float envelope = Mathf.Min(1f, i / attack) * (1f - t) * (1f - t);
            data[offset + i] += s * envelope * volume;
        }
    }

    // UTILITARIOS

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    public static string BiomaDisplayName(TileMapVisualizer.Biomas bioma)
    {
        switch (bioma)
        {
            case TileMapVisualizer.Biomas.Floresta: return "Floresta";
            case TileMapVisualizer.Biomas.Deserto: return "Deserto";
            case TileMapVisualizer.Biomas.Caverna: return "Caverna";
            case TileMapVisualizer.Biomas.Abismo: return "Abismo";
            default: return "O Infinito";
        }
    }

    private static Color BiomaColor(TileMapVisualizer.Biomas bioma)
    {
        switch (bioma)
        {
            case TileMapVisualizer.Biomas.Floresta: return new Color(0.55f, 0.95f, 0.5f);
            case TileMapVisualizer.Biomas.Deserto: return new Color(1f, 0.85f, 0.45f);
            case TileMapVisualizer.Biomas.Caverna: return new Color(0.7f, 0.75f, 1f);
            case TileMapVisualizer.Biomas.Abismo: return new Color(0.85f, 0.5f, 1f);
            default: return new Color(1f, 0.5f, 0.5f);
        }
    }
}
