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
        TransitionTo(RunState.RoomTransition);
        TransitionTo(RunState.RoomActive);
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
            _progression.EarnCrystals(ProgressionSystem.EarnSource.RunZone1);

        TransitionTo(RunState.Idle);
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
