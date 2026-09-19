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
    private bool _isTutorialRun;
    private int _currentZoneId = 1;
    private SaveSystem _save;
    private ProgressionSystem _progression;

    // Fase 10 M3.3 + M3.4 + M3.5 + cierre: upgrades de run (GDD §7.3, tabla R01-R12) —
    // los 12 con efecto real. R05/R06/R10 estuvieron fuera porque cada uno necesitaba
    // un sistema nuevo, no solo una regla de simulación (TriggerLever.EarlyDetectSize
    // para R05, EchoPlayer.TrailRenderer para R06, RevealTriggers de RoomAssembler para
    // R10) — ya existen, así que entran a la tabla.
    public RunUpgradeEffects ActiveUpgrades { get; private set; } = new RunUpgradeEffects();

    // GDD §4.1 Rama B — modificador permanente elegido para la run completa (ver
    // BranchBModifier.cs). Antes de este pase, Rama B no tenía NINGÚN efecto de gameplay
    // conectado (solo costaba PC y se guardaba como "desbloqueado" sin hacer nada).
    public BranchBEffects ActiveBranchB { get; private set; } = new BranchBEffects();
    public static readonly System.Collections.Generic.List<RunUpgrade> UpgradeTable = new System.Collections.Generic.List<RunUpgrade>
    {
        new RunUpgrade("R01", "Eco Veloz", "Los ecos de esta run corren al 1.2x — hace puzzles de timing más difíciles", e => e.echoSpeedMultiplier = 1.2f),
        new RunUpgrade("R02", "Eco Lento", "Los ecos de esta run corren al 0.8x — amplía ventanas de sincronización", e => e.echoSpeedMultiplier = 0.8f),
        new RunUpgrade("R03", "Bullet Extendido", "Transición de bullet-time más suave (QoL, no poder)", e => e.bulletTimeDeactivateBonus += 3f),
        new RunUpgrade("R04", "Doble Loop", "Los ecos completan su loop dos veces más rápido (frecuencia, no velocidad)", e => e.loopDurationMultiplier = 0.5f),
        new RunUpgrade("R05", "Trigger Anticipado", "Las palancas detectan a tus ecos un poco antes de tocarlas — red de seguridad para timing ajustado", e => e.triggerAnticipationEnabled = true),
        new RunUpgrade("R06", "Persistencia Ampliada", "Los rastros de tus ecos duran el doble en bullet-time — más fácil leer sus rutas", e => e.echoTrailDurationMultiplier = 2f),
        new RunUpgrade("R07", "Sala Bonus", "Se añade una 5ta sala de dificultad baja, +50% Phase Crystals al completar", e => { e.bonusRoomRequested = true; e.pcBonusOnComplete += 15; }),
        new RunUpgrade("R08", "Reinicio de Sala", "Si mueres una vez, esa sala te perdona (conservas ecos y progreso) — un solo uso", e => e.roomRestartAvailable = true),
        new RunUpgrade("R09", "Eco Duplicado", "El primer eco de la run se duplica — 2 ecos con la misma ruta desde el principio", e => e.duplicateFirstEcho = true),
        new RunUpgrade("R10", "Revelación", "Al entrar a una sala, sus palancas brillan 2s — una pista si no sabés por dónde empezar", e => e.revealFutureTriggersEnabled = true),
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

    private void PushBranchBToSystems()
    {
        if (Services.TryGet<TimeManager>(out var time))
        {
            time.SetBranchBBulletTime(ActiveBranchB.doubleBulletTime, ActiveBranchB.bulletTimeDisabled);
            time.SetBranchBFogOfWar(ActiveBranchB.fogOfWar);
        }
        if (Services.TryGet<RoomAssembler>(out var assembler))
            assembler.SetMirrored(ActiveBranchB.mirrorRoom);
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

        ActiveBranchB = new BranchBEffects();
        string branchBId = _save.Current.metaProgression.selectedBranchBModifier;
        if (BranchBTable.Apply.TryGetValue(branchBId, out var applyBranchB)) applyBranchB(ActiveBranchB);
        PushBranchBToSystems();

        if (Services.TryGet<EchoManager>(out var echoManager))
            echoManager.ResetForNewRun();
        TransitionTo(RunState.RoomTransition);
        TransitionTo(RunState.RoomActive);

        // GDD §5: la PRIMERA run del jugador es siempre el tutorial — 4 salas fijas en
        // vez del pool aleatorio, sin texto, la lección viene del diseño de nivel.
        _isTutorialRun = !_save.Current.metaProgression.tutorialCompleted;

        _currentZoneId = _isTutorialRun ? 1 : CurrentZoneId();

        if (Services.TryGet<RoomAssembler>(out var assembler))
        {
            if (_isTutorialRun) assembler.AssembleTutorialRun();
            else
            {
                // B8 Sala Única: 2 salas en vez de 4 (el boss se agrega aparte siempre,
                // ver RoomAssembler.AssembleRun — "más corta, recompensa normal").
                int roomCount = ActiveBranchB.roomCountOverride > 0 ? ActiveBranchB.roomCountOverride : 4;
                assembler.AssembleRun(roomCount: roomCount, seed: UnityEngine.Random.Range(100000, 999999), zoneId: _currentZoneId);
            }
        }
    }

    // GDD §6.2 "Regla de Escalado por Zona": la zona activa depende de runs completadas
    // en total, no de una elección del jugador — Z1 runs 1-10, Z2 runs 11-20, Z3 runs
    // 21-30. Z4/Z5 existen en el GDD pero quedan fuera del alcance v1.0 (desarrollo-
    // completo.md §4.3), así que cualquier progreso más allá de la run 20 se queda en Z3.
    private int CurrentZoneId()
    {
        int completed = _save.Current.metaProgression.totalRunsCompleted;
        if (completed < 10) return 1;
        if (completed < 20) return 2;
        return 3;
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

        if (_isTutorialRun)
            _save.Current.metaProgression.tutorialCompleted = true;

        _save.Save();

        if (_progression != null)
        {
            var earnSource = _currentZoneId switch
            {
                1 => ProgressionSystem.EarnSource.RunZone1,
                2 => ProgressionSystem.EarnSource.RunZone2,
                _ => ProgressionSystem.EarnSource.RunZone3,
            };
            // B6 Sin Bullet (GDD: "+50% PC al completar SALA") — el VS no tiene un premio
            // de PC por sala individual, solo por run completa, así que el bonus se aplica
            // acá, sobre el total de la run — simplificación honesta, no un premio fantasma.
            int baseAmount = ProgressionSystem.AmountFor(earnSource);
            _progression.EarnFlat(Mathf.RoundToInt(baseAmount * ActiveBranchB.pcBonusMultiplier));
            if (ActiveUpgrades.pcBonusOnComplete > 0)
                _progression.EarnFlat(ActiveUpgrades.pcBonusOnComplete);
        }

        if (Services.TryGet<AchievementSystem>(out var achievements))
            achievements.CheckAndUnlock();

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
