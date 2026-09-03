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

    // Fase 10 M3.3: upgrades de run (GDD §7.3) — muestra reducida de la tabla R01-R12,
    // suficiente para probar el patrón real de "elegir 1 de 2 entre salas".
    public RunUpgradeEffects ActiveUpgrades { get; private set; } = new RunUpgradeEffects();
    public static readonly System.Collections.Generic.List<RunUpgrade> UpgradeTable = new System.Collections.Generic.List<RunUpgrade>
    {
        new RunUpgrade("R12", "PC Bonus", "+100 Phase Crystals al completar la run", e => e.pcBonusOnComplete += 100),
        new RunUpgrade("R03", "Bullet Extendido", "Transición de bullet-time más suave (QoL, no poder)", e => e.bulletTimeDeactivateBonus += 0.1f),
        new RunUpgrade("R11", "Mundo Lento", "El mundo corre a 0.85x — facilita el timing sin afectar ecos", e => e.worldSlowMultiplier = 0.85f),
    };

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

    public void ApplyUpgrade(RunUpgrade upgrade) => upgrade?.apply?.Invoke(ActiveUpgrades);

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
