using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A sala e dona da sequencia: a morte do boss nao interrompe o final.
public class FinalBossSequence : MonoBehaviour
{
    private Coco boss;
    private EnemyMovement enemy;
    private SpriteRenderer[] bossSprites;
    private Collider2D[] bossColliders;
    private GameObject player;
    private Animator playerAnimator;
    private Sprite playerPortrait, bossPortrait;
    private Canvas canvas;
    private GameObject dialogue;
    private TextMeshProUGUI line;
    private Image portrait, fade;
    private CanvasGroup victory;
    private bool started, fighting, ending;
    private Vector3 arenaPosition;
    // O assento fica na fileira y=6; a fileira y=5 e o tapete em frente.
    private Vector3 Toilet => transform.TransformPoint(new Vector3(0f, 6.3f, 0f));

    public void Initialize()
    {
        boss = GetComponentInChildren<Coco>();
        if (boss == null) { enabled = false; return; }
        enemy = boss.GetComponent<EnemyMovement>();
        arenaPosition = boss.transform.position;
        bossSprites = boss.GetComponentsInChildren<SpriteRenderer>();
        bossColliders = boss.GetComponentsInChildren<Collider2D>();
        if (bossSprites.Length > 0) bossPortrait = bossSprites[0].sprite;
        boss.enabled = false;
        enemy.enabled = false;
        SetBossVisible(false);
    }

    private void SetBossVisible(bool visible)
    {
        foreach (var sprite in bossSprites) sprite.enabled = visible;
        foreach (var collider in bossColliders) collider.enabled = visible && fighting;
    }

    public void Begin(GameObject alex)
    {
        if (started || boss == null) return;
        started = true;
        player = alex;
        playerAnimator = player.GetComponent<Animator>();
        playerPortrait = player.GetComponent<SpriteRenderer>()?.sprite;
        GameFlow.Pause(GameFlow.State.Cutscene);
        BuildUI();
        StartCoroutine(Introduction());
    }

    private IEnumerator Introduction()
    {
        yield return new WaitForSecondsRealtime(.6f);
        yield return WalkPlayer(transform.TransformPoint(Vector3.zero), 1.4f);
        FacePlayer(Vector2.up, false);
        yield return Say("Finalmente... em casa...", playerPortrait);
        yield return Say("...", playerPortrait);
        yield return Say("Hora de soltar um barro...", playerPortrait);
        dialogue.SetActive(false);
        boss.transform.position = Toilet;
        SetBossVisible(true);
        // Salto em arco da privada ate o ponto original do boss.
        Vector3 start = Toilet;
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / .9f)
        {
            boss.transform.position = Vector3.Lerp(start, arenaPosition, t) +
                Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.8f);
            yield return null;
        }
        boss.transform.position = arenaPosition;
        FacePlayer((arenaPosition - player.transform.position).normalized, false);
        yield return Say("!!!", playerPortrait);
        yield return Say("Olá papai... você veio brincar comigo???", bossPortrait);
        dialogue.SetActive(false);
        fighting = true;
        SetBossVisible(true);
        enemy.enabled = true;
        boss.enabled = true;
        GameFlow.Resume(GameFlow.State.Cutscene);
        GameEffects.Instance.OnEnterBossRoom(true);
    }

    public void BossDefeated()
    {
        if (!fighting || ending) return;
        ending = true;
        fighting = false;
        GameFlow.Pause(GameFlow.State.Cutscene);
        StartCoroutine(Ending());
    }

    private IEnumerator Ending()
    {
        yield return new WaitForSecondsRealtime(.8f);
        yield return Say("Dessa vez não vou esquecer de dar a descarga...", playerPortrait);
        dialogue.SetActive(false);
        yield return WalkPlayer(Toilet + transform.up * 0.75f, 2f);
        // Mantem a pose e a escala normais ate existir uma animacao de sentar.
        FacePlayer(Vector2.down, false);
        yield return new WaitForSecondsRealtime(2.5f);
        fade.gameObject.SetActive(true);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 3f)
        {
            fade.color = new Color(0, 0, 0, t);
            yield return null;
        }
        fade.color = Color.black;
        GameFlow.Pause(GameFlow.State.Victory);
        GameFlow.Resume(GameFlow.State.Cutscene);
        victory.gameObject.SetActive(true);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime)
        {
            victory.alpha = t;
            yield return null;
        }
        victory.alpha = 1f;
        // Aguarda soltar o botao antes de permitir voltar ao menu.
        while (ConfirmHeld()) yield return null;
        while (!ConfirmPressed()) yield return null;
        SceneManager.LoadSceneAsync(0);
    }

    private IEnumerator WalkPlayer(Vector3 destination, float duration)
    {
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        Vector3 start = player.transform.position;
        if ((destination - start).sqrMagnitude < .0001f) yield break;
        var movement = player.GetComponent<PlayerMovement>();
        // Pausar o fluxo ja bloqueia input; desliga o componente apenas durante o passeio.
        bool wasEnabled = movement != null && movement.enabled;
        if (wasEnabled) movement.enabled = false;
        AnimatorUpdateMode previousMode = playerAnimator != null ? playerAnimator.updateMode : AnimatorUpdateMode.Normal;
        if (playerAnimator != null) playerAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        FacePlayer((destination - start).normalized, true);
        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            player.transform.position = Vector3.Lerp(start, destination, t);
            yield return null;
        }
        player.transform.position = destination;
        FacePlayer(Vector2.up, false);
        if (playerAnimator != null) playerAnimator.updateMode = previousMode;
        if (wasEnabled) movement.enabled = true;
    }

    private void FacePlayer(Vector2 direction, bool walking)
    {
        if (playerAnimator == null) return;
        playerAnimator.SetBool("isWalking", walking);
        playerAnimator.SetFloat("InputX", direction.x);
        playerAnimator.SetFloat("InputY", direction.y);
        playerAnimator.SetFloat("LastInputX", direction.x);
        playerAnimator.SetFloat("LastInputY", direction.y);
        // Avalia a pose mesmo com o tempo do jogo pausado.
        playerAnimator.Update(0f);
    }

    private IEnumerator Say(string text, Sprite sprite)
    {
        dialogue.SetActive(true);
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        line.text = text;
        line.maxVisibleCharacters = 0;
        line.ForceMeshUpdate();
        int count = line.textInfo.characterCount;
        yield return null;
        while (ConfirmHeld()) yield return null;
        float characters = 0;
        while (line.maxVisibleCharacters < count)
        {
            if (ConfirmPressed()) { line.maxVisibleCharacters = count; break; }
            characters += Time.unscaledDeltaTime / .05f;
            line.maxVisibleCharacters = Mathf.Min(count, (int)characters);
            yield return null;
        }
        yield return null;
        while (ConfirmHeld()) yield return null;
        // Completar a digitacao e avancar exigem apertos separados.
        while (!ConfirmPressed()) yield return null;
        yield return null;
    }

    private static bool ConfirmPressed() =>
        (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

    private static bool ConfirmHeld() =>
        (Keyboard.current != null && (Keyboard.current.enterKey.isPressed || Keyboard.current.spaceKey.isPressed)) ||
        (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
        (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);

    private void BuildUI()
    {
        var root = new GameObject("Cutscene final", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0f;
        // Copia literal da hierarquia e dos componentes visuais da cena de abertura.
        var template = Resources.Load<GameObject>("Dialogue/IntroDialogueBox");
        dialogue = Instantiate(template, root.transform, false);
        line = dialogue.GetComponentInChildren<TextMeshProUGUI>(true);
        portrait = dialogue.transform.Find("Image").GetComponent<Image>();
        playerPortrait = portrait.sprite;
        dialogue.SetActive(false);
        fade = Box("Escurecer", root.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        fade.color = Color.clear;
        fade.gameObject.SetActive(false);
        var win = Box("Vitoria", root.transform, Vector2.zero, Vector2.one);
        victory = win.gameObject.AddComponent<CanvasGroup>();
        victory.alpha = 0;
        var title = Label("Titulo", win, new Vector2(.1f, .4f), new Vector2(.9f, .65f), 72);
        title.text = "Você ganhou!";
        title.alignment = TextAlignmentOptions.Center;
        var back = Label("Menu", win, new Vector2(.1f, .25f), new Vector2(.9f, .38f), 24);
        back.text = "Enter / Espaço / Clique / X (A) para voltar ao menu";
        back.alignment = TextAlignmentOptions.Center;
        win.gameObject.SetActive(false);
    }

    private static RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 min, Vector2 max, float size)
    {
        var text = Box(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        if (started)
        {
            GameFlow.Resume(GameFlow.State.Cutscene);
            GameFlow.Resume(GameFlow.State.Victory);
        }
    }
}
