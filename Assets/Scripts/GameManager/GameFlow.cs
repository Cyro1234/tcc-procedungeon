using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Cada sistema libera apenas a sua propria pausa. Game Over tem prioridade sobre os menus.
public static class GameFlow
{
    [Flags]
    public enum State { Playing = 0, Inventory = 1, PauseMenu = 2, Downgrade = 4, GameOver = 8, Cutscene = 16, Victory = 32, DebugMenu = 64 }

    private static State pauses;
    private static bool hitStopped;
    public static event Action Changed;
    public static bool CanPlay => pauses == State.Playing;
    public static bool IsHitStopped => hitStopped;
    public static State Current
    {
        get
        {
            if ((pauses & State.GameOver) != 0) return State.GameOver;
            if ((pauses & State.Victory) != 0) return State.Victory;
            if ((pauses & State.Cutscene) != 0) return State.Cutscene;
            if ((pauses & State.DebugMenu) != 0) return State.DebugMenu;
            if ((pauses & State.Downgrade) != 0) return State.Downgrade;
            if ((pauses & State.PauseMenu) != 0) return State.PauseMenu;
            if ((pauses & State.Inventory) != 0) return State.Inventory;
            return State.Playing;
        }
    }

    // Pausa e inventario usam as mesmas acoes e remapeamentos do personagem.
    public static InputActionAsset PlayerActions => UnityEngine.Object.FindAnyObjectByType<PlayerInput>()?.actions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pauses = State.Playing;
        hitStopped = false;
        Changed = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Reset();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) Reset();
    }

    public static void Pause(State reason) => SetPaused(reason, true);
    public static void Resume(State reason) => SetPaused(reason, false);

    private static void SetPaused(State reason, bool paused)
    {
        State previous = Current;
        if (paused) { pauses |= reason; hitStopped = false; }
        else pauses &= ~reason;
        ApplyTime();
        if (Current != previous) Changed?.Invoke();
    }

    public static void SetHitStop(bool active)
    {
        hitStopped = active && CanPlay;
        ApplyTime();
    }

    private static void ApplyTime() => Time.timeScale = CanPlay ? (hitStopped ? 0.02f : 1f) : 0f;

    public static void Reset()
    {
        pauses = State.Playing;
        hitStopped = false;
        ApplyTime();
        Changed?.Invoke();
    }
}
