using System;
using System.Collections.Generic;
using UnityEngine;

// Fase 10 M2: versión simplificada del algoritmo de ensamblaje de la Fase 8 §17.4.2.
// Las salas viven como contenedores pre-construidos en la MISMA escena (no additive
// scene loading todavía — esa es la arquitectura final de la Fase 8 §1, pero requiere
// separar Persistent/Gameplay scenes, que es su propio trabajo; esta versión demuestra
// el sistema real de selección + transición de salas sin bloquear en esa refactorización).
[Serializable]
public class RoomInstance
{
    public RoomData data;
    public GameObject container;
    public Transform spawnPoint;
    public Transform cameraAnchor;
    public bool isBoss;
}

public class RoomAssembler : MonoBehaviour
{
    [SerializeField] private List<RoomInstance> _pool = new List<RoomInstance>();
    [SerializeField] private RoomInstance _bossRoom;
    [SerializeField] private List<RoomInstance> _tutorialRooms = new List<RoomInstance>();
    [SerializeField] private Camera _camera;
    [SerializeField] private PlayerController _player;
    [SerializeField] private EchoManager _echoManager;
    [SerializeField] private LoopTimer _loopTimer;

    private List<RoomInstance> _runSequence = new List<RoomInstance>();
    private int _currentIndex = -1;
    // GDD §5 Tutorial: las 4 salas del tutorial NO limpian ecos entre sí — el eco de la
    // Sala 0 tiene que seguir vivo cuando el jugador llega a la Sala 1. Toda sala normal
    // deja esto en false (comportamiento sin cambios).
    private bool _carryEchoesAcrossRooms;

    private void Awake() => Services.Register(this);

    public void RegisterRoom(RoomInstance instance)
    {
        _pool.Add(instance);
        instance.container.SetActive(false);
    }

    // GDD §7.1: "4 salas estándar + 1 sala de boss determinada por zona activa" — el
    // boss no sale del sorteo aleatorio del pool, es un slot fijo al final de la run.
    public void RegisterBossRoom(RoomInstance instance)
    {
        instance.isBoss = true;
        _bossRoom = instance;
        instance.container.SetActive(false);
    }

    // GDD §5: las salas del tutorial no salen del pool aleatorio — son una secuencia
    // fija, en orden, solo para la primera run del jugador. Se registran aparte para
    // que AssembleRun (el sorteo normal) nunca las toque.
    public void RegisterTutorialRoom(RoomInstance instance)
    {
        _tutorialRooms.Add(instance);
        instance.container.SetActive(false);
    }

    public void AssembleTutorialRun()
    {
        _runSequence.Clear();
        _runSequence.AddRange(_tutorialRooms);
        _carryEchoesAcrossRooms = true;

        if (Services.TryGet<EchoManager>(out var echoManager))
            echoManager.SetTemporaryMaxEchos(3);
        // Loop más corto en todo el tutorial: sin esto, un jugador rápido puede cruzar
        // la Sala 0 (una sola palanca+puerta latching, sin exigir coordinación) antes de
        // que el loop de 8s siquiera termine una vez — y sin loop, no hay eco grabado
        // que llevar a la Sala 1, rompiendo la lección central del tutorial.
        if (_loopTimer != null) _loopTimer.SetDurationMultiplier(0.4f);

        _currentIndex = -1;
        LoadNext();
    }

    // Selección con las restricciones reales de la Fase 8 §17.4.2: sala 1 siempre SOLO
    // (calienta sin exigir coordinación), el resto mezclado sin repetir mecánica
    // consecutiva cuando el pool lo permite.
    public void AssembleRun(int roomCount, int seed)
    {
        var rng = new System.Random(seed);
        _runSequence.Clear();
        _carryEchoesAcrossRooms = false;

        var soloRooms = _pool.FindAll(r => r.data.mechanic == PrimaryMechanic.SOLO);
        var nonSolo = _pool.FindAll(r => r.data.mechanic != PrimaryMechanic.SOLO);

        RoomInstance firstRoom = soloRooms.Count > 0 ? soloRooms[rng.Next(soloRooms.Count)] : null;
        if (firstRoom != null) _runSequence.Add(firstRoom);

        PrimaryMechanic lastMechanic = _runSequence.Count > 0 ? _runSequence[0].data.mechanic : PrimaryMechanic.SOLO;
        // Defensivo: excluir tanto la sala 1 (nunca repetir la MISMA sala en un run) como
        // cualquier otra sala SOLO — solo la sala de apertura usa ese pool.
        var remaining = new List<RoomInstance>(nonSolo);
        remaining.RemoveAll(r => r == firstRoom);
        while (_runSequence.Count < roomCount && remaining.Count > 0)
        {
            var candidates = remaining.FindAll(r => r.data.mechanic != lastMechanic);
            if (candidates.Count == 0) candidates = remaining;

            var pick = candidates[rng.Next(candidates.Count)];
            _runSequence.Add(pick);
            lastMechanic = pick.data.mechanic;
            remaining.Remove(pick);
        }

        if (_bossRoom != null) _runSequence.Add(_bossRoom);

        _currentIndex = -1;
        LoadNext();
    }

    private void LoadNext()
    {
        if (_currentIndex >= 0 && _currentIndex < _runSequence.Count)
            _runSequence[_currentIndex].container.SetActive(false);

        _currentIndex++;

        if (_currentIndex >= _runSequence.Count)
        {
            // Fin de secuencia (normal o tutorial) — restaurar todo lo que el tutorial
            // pudo haber alterado temporalmente, para que la SIGUIENTE run (ya normal)
            // no herede nada de esto.
            _carryEchoesAcrossRooms = false;
            if (_loopTimer != null) _loopTimer.SetDurationMultiplier(1f);
            if (Services.TryGet<EchoManager>(out var echoManagerReset)) echoManagerReset.RestoreMaxEchos();

            if (Services.TryGet<RunManager>(out var run))
                run.CompleteRun(bossDefeated: false); // sin bosses todavía (Fase 10 M4)
            return;
        }

        var next = _runSequence[_currentIndex];
        next.container.SetActive(true);
        foreach (var exit in next.container.GetComponentsInChildren<RoomExit>(true))
            exit.ResetTrigger();

        _player.ResetToPosition(next.spawnPoint.position);
        if (_camera != null && next.cameraAnchor != null)
            _camera.transform.position = new Vector3(next.cameraAnchor.position.x, next.cameraAnchor.position.y, _camera.transform.position.z);

        if (_camera != null)
        {
            var theme = next.container.GetComponent<RoomVisualTheme>();
            if (theme != null) _camera.backgroundColor = theme.backgroundColor;
        }

        if (!_carryEchoesAcrossRooms) _echoManager.ClearAllEchos();
        _loopTimer.StartLoop();

        if (next.isBoss && Services.TryGet<RunManager>(out var runBoss))
            runBoss.EnterBossFight();
    }

    // R07 Sala Bonus (GDD §7.3): agrega una 5ta sala de dificultad baja a la run YA EN
    // CURSO. Se inserta justo antes del boss (o al final si no hay boss) — nunca antes
    // de _currentIndex, para no alterar la sala que el jugador ya está jugando.
    public void InjectBonusRoom()
    {
        var usedIds = new HashSet<string>();
        foreach (var r in _runSequence) usedIds.Add(r.data.roomId);

        var candidates = _pool.FindAll(r => r.data.mechanic == PrimaryMechanic.SOLO
            && r.data.difficultyTier <= 1 && !usedIds.Contains(r.data.roomId));
        if (candidates.Count == 0) return;

        var bonusRoom = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        int insertAt = _bossRoom != null ? _runSequence.Count - 1 : _runSequence.Count;
        insertAt = Mathf.Max(insertAt, _currentIndex + 1);
        _runSequence.Insert(insertAt, bonusRoom);
    }

    // Llamado por BossController cuando se cumple la condición de victoria del boss
    // (todos los paneles activos + jugador en el centro por _requiredHoldTime). A
    // diferencia de OnRoomCleared, esto NO pasa por RunManager.RoomCleared() (que
    // requiere CurrentState==RoomActive; el boss ya transicionó a BossFight) ni ofrece
    // upgrade de run — es la última sala, la run termina aquí.
    public void OnBossDefeated()
    {
        if (Services.TryGet<RunManager>(out var run))
            run.CompleteRun(bossDefeated: true);
    }

    public void OnRoomCleared()
    {
        if (Services.TryGet<RunManager>(out var run))
            run.RoomCleared();

        // GDD §7.3: 60% de probabilidad de ofrecer un upgrade de run entre salas.
        bool offerUpgrade = _currentIndex + 1 < _runSequence.Count // no ofrecer después de la última sala
            && UnityEngine.Random.value < 0.6f
            && Services.TryGet<UpgradeSelectorUI>(out var selectorUI)
            && RunManager.UpgradeTable.Count >= 2;

        if (offerUpgrade)
        {
            int idxA = UnityEngine.Random.Range(0, RunManager.UpgradeTable.Count);
            int idxB;
            do { idxB = UnityEngine.Random.Range(0, RunManager.UpgradeTable.Count); } while (idxB == idxA);

            Services.Get<UpgradeSelectorUI>().Show(RunManager.UpgradeTable[idxA], RunManager.UpgradeTable[idxB], picked =>
            {
                if (picked != null && Services.TryGet<RunManager>(out var runRef))
                    runRef.ApplyUpgrade(picked);
                LoadNext();
            });
        }
        else
        {
            LoadNext();
        }
    }

    // Ayuda de QA: saltar directo a una sala del pool por id, sin pasar por el sorteo
    // aleatorio de AssembleRun (útil para probar salas raras/tardías del pool, como las
    // de Zona 3, sin jugar decenas de runs esperando que salgan por azar).
    public bool DebugJumpToRoom(string roomId)
    {
        var target = _pool.Find(r => r.data.roomId == roomId);
        if (target == null && _bossRoom != null && _bossRoom.data.roomId == roomId) target = _bossRoom;
        if (target == null) target = _tutorialRooms.Find(r => r.data.roomId == roomId);
        if (target == null) return false;

        _runSequence.Clear();
        _runSequence.Add(target);
        _currentIndex = -1;
        LoadNext();
        return true;
    }

    public RoomData CurrentRoom => (_currentIndex >= 0 && _currentIndex < _runSequence.Count) ? _runSequence[_currentIndex].data : null;
    public int RunSequenceCount => _runSequence.Count;

    // El controlador de muerte/reset (VSRoomController) necesita saber DÓNDE resetear al
    // jugador — no siempre es la Room 0 original. Sin esto, morir en cualquier otra sala
    // te devolvía al spawn de la Room 0 (posiblemente desactivada), rompiendo el flujo.
    public Transform CurrentSpawnPoint => (_currentIndex >= 0 && _currentIndex < _runSequence.Count) ? _runSequence[_currentIndex].spawnPoint : null;

    // InputRecorder/EchoPlayer usan esto para grabar/reproducir posiciones relativas a
    // la sala en vez de absolutas — ver comentario en _carryEchoesAcrossRooms.
    public float CurrentRoomOriginX => (_currentIndex >= 0 && _currentIndex < _runSequence.Count)
        ? _runSequence[_currentIndex].container.transform.position.x
        : 0f;
}
