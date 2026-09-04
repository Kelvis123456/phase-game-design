using System;
using UnityEngine;

// Fase 10 M1.2: FSM de estado de run. El VS solo tiene 1 sala hardcodeada (no hay pool de
// 50 salas todavía — eso es Milestone 2, contenido real de diseño de nivel, no arquitectura).
// Esta FSM es el esqueleto real sobre el que Milestone 2 conecta el algoritmo de ensamblaje
// de la Fase 8 §17.4 cuando exista un RoomData pool.
[DefaultExecutionOrder(-93)]
public class RunManager : MonoBehaviour
{
    public enum RunState { Idle, RoomTransition, RoomActive, UpgradeChoice, BossFight, RunComplete, RunFailed }

    public RunState CurrentState { get; private set; } = RunState.Idle;
    public event Action<RunState, RunState> OnStateChanged; // (from, to)

    private float _runStartTime;
    private int _roomsCleared;
    private int _echosCreated;
    private SaveSystem _save;
    private ProgressionSystem _progression;

    // Fase 10 M3.3 + M3.4 + M3.5: upgrades de run (GDD §7.3, tabla R01-R12). 9 de 12 con
    // efecto real conectado. R05 (lookahead de trigger) y R06/R10 (trails de eco / UI de
    // revelación) quedan fuera — cada uno es un sistema visual nuevo, no una regla de
    // simulación, así que no entran en este pase para no vender un upgrade seleccionable
    // que en realidad no hace nada.
    public RunUpgradeEffects ActiveUpgrades { get; private set; } = new RunUpgradeEffects();
    public static readonly System.Collections.Generic.List<RunUpgrade> UpgradeTable = new System.Collections.Generic.List<RunUpgrade>
    {
        new RunUpgrade("R01", "Eco Veloz", "Los ecos de esta run corren al 1.2x — hace puzzles de timing más difíciles", e => e.echoSpeedMultiplier = 1.2f),
        new RunUpgrade("R02", "Eco Lento", "Los ecos de esta run corren al 0.8x — amplía ventanas de sincronización", e => e.echoSpeedMultiplier = 0.8f),
        new RunUpgrade("R03", "Bullet Extendido", "Transición de bullet-time más suave (QoL, no poder)", e => e.bulletTimeDeactivateBonus += 3f),
        new RunUpgrade("R04", "Doble Loop", "Los ecos completan su loop dos veces más rápido (frecuencia, no velocidad)", e => e.loopDurationMultiplier = 0.5f),
        new RunUpgrade("R07", "Sala Bonus", "Se añade una 5ta sala de dificultad baja, +50% Phase Crystals al completar", e => { e.bonusRoomRequested = true; e.pcBonusOnComplete += 15; }),
        new RunUpgrade("R08", "Reinicio de Sala", "Si mueres una vez, esa sala te perdona (conservas ecos y progreso) — un solo uso", e => e.roomRestartAvailable = true),
        new RunUpgrade("R09", "Eco Duplicado", "El primer eco de la run se duplica — 2 ecos con la misma ruta desde el principio", e => e.duplicateFirstEcho = true),
        new RunUpgrade("R11", "Mundo Lento", "El mundo corre a 0.85x — facilita el timing sin afectar ecos", e => e.worldSlowMultiplier = 0.85f),
        new RunUpgrade("R12", "PC Bonus", "+100 Phase Crystals al completar la run", e => e.pcBonusOnComplete += 100),
    };

    private void PushUpgradesToSystems()
    {
        if (Services.TryGet<TimeManager>(out var time))
        {
            time.SetDeactivateSmoothBonus(ActiveUpgrades.bulletTimeDeactivateBonus);
            time.SetWorldScale(ActiveUpgrades.worldSlowMultiplier);
        }
        if (Services.TryGet<LoopTimer>(out var loop))
            loop.SetDurationMultiplier(ActiveUpgrades.loopDurationMultiplier);

        // R07 Sala Bonus: se consume una sola vez — si el jugador ya la tiene y la
        // vuelve a ver en otra selección (no debería, pero por si acaso), no duplicar salas.
        if (ActiveUpgrades.bonusRoomRequested)
        {
            ActiveUpgrades.bonusRoomRequested = false;
            if (Services.TryGet<RoomAssembler>(out var assembler))
                assembler.InjectBonusRoom();
        }
    }

    // R08 Reinicio de Sala: VSRoomController llama esto al morir. Si hay un reinicio
    // disponible, se consume (una sola vez por run) y el llamador hace un reset suave
    // (conserva ecos/grabación) en vez del reset completo.
    public bool ConsumeRoomRestart()
    {
        if (!ActiveUpgrades.roomRestartAvailable) return false;
        ActiveUpgrades.roomRestartAvailable = false;
        return true;
    }

    private void Awake() => Services.Register(this);

    private void Start()
    {
        _save = Services.Get<SaveSystem>();
        Services.TryGet<ProgressionSystem>(out _progression);
    }

    public void StartRun()
    {
        if (CurrentState != RunState.Idle) return;
        _runStartTime = Time.time;
        _roomsCleared = 0;
        _echosCreated = 0;
        _save.Current.metaProgression.totalRunsAttempted++;
        _save.Save();
        ActiveUpgrades = new RunUpgradeEffects();
        PushUpgradesToSystems();
        if (Services.TryGet<EchoManager>(out var echoManager))
            echoManager.ResetForNewRun();
        TransitionTo(RunState.RoomTransition);
        TransitionTo(RunState.RoomActive);

        if (Services.TryGet<RoomAssembler>(out var assembler))
            assembler.AssembleRun(roomCount: 4, seed: UnityEngine.Random.Range(100000, 999999));
    }

    public void RoomCleared()
    {
        if (CurrentState != RunState.RoomActive) return;
        _roomsCleared++;
        TransitionTo(RunState.RoomTransition);
        TransitionTo(RunState.RoomActive);
    }

    public void EnterBossFight()
    {
        if (CurrentState != RunState.RoomActive) return;
        TransitionTo(RunState.BossFight);
    }

    public void CompleteRun(bool bossDefeated)
    {
        TransitionTo(RunState.RunComplete);

        float duration = Time.time - _runStartTime;
        _save.Current.metaProgression.totalRunsCompleted++;

        var entry = new SaveData.RunHistoryEntry
        {
            runId = Guid.NewGuid().ToString(),
            seed = UnityEngine.Random.Range(100000, 999999),
            date = DateTime.UtcNow.ToString("o"),
            durationSeconds = duration,
            roomsCleared = _roomsCleared,
            bossDefeated = bossDefeated,
        };
        _save.Current.runHistory.Add(entry);
        _save.Save();

        if (_progression != null)
        {
            _progression.EarnCrystals(ProgressionSystem.EarnSource.RunZone1);
            if (ActiveUpgrades.pcBonusOnComplete > 0)
                _progression.EarnFlat(ActiveUpgrades.pcBonusOnComplete);
        }

        TransitionTo(RunState.Idle);
    }

    public void ApplyUpgrade(RunUpgrade upgrade)
    {
        upgrade?.apply?.Invoke(ActiveUpgrades);
        PushUpgradesToSystems();
    }

    public void FailRun()
    {
        TransitionTo(RunState.RunFailed);
        TransitionTo(RunState.Idle);
    }

    private void TransitionTo(RunState next)
    {
        var prev = CurrentState;
        CurrentState = next;
        OnStateChanged?.Invoke(prev, next);
    }
}
