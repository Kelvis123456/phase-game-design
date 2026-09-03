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
}

public class RoomAssembler : MonoBehaviour
{
    [SerializeField] private List<RoomInstance> _pool = new List<RoomInstance>();
    [SerializeField] private Camera _camera;
    [SerializeField] private PlayerController _player;
    [SerializeField] private EchoManager _echoManager;
    [SerializeField] private LoopTimer _loopTimer;

    private List<RoomInstance> _runSequence = new List<RoomInstance>();
    private int _currentIndex = -1;

    private void Awake() => Services.Register(this);

    public void RegisterRoom(RoomInstance instance)
    {
        _pool.Add(instance);
        instance.container.SetActive(false);
    }

    // Selección con las restricciones reales de la Fase 8 §17.4.2: sala 1 siempre SOLO
    // (calienta sin exigir coordinación), el resto mezclado sin repetir mecánica
    // consecutiva cuando el pool lo permite.
    public void AssembleRun(int roomCount, int seed)
    {
        var rng = new System.Random(seed);
        _runSequence.Clear();

        var soloRooms = _pool.FindAll(r => r.data.mechanic == PrimaryMechanic.SOLO);
        var nonSolo = _pool.FindAll(r => r.data.mechanic != PrimaryMechanic.SOLO);

        if (soloRooms.Count > 0)
            _runSequence.Add(soloRooms[rng.Next(soloRooms.Count)]);

        PrimaryMechanic lastMechanic = _runSequence.Count > 0 ? _runSequence[0].data.mechanic : PrimaryMechanic.SOLO;
        var remaining = new List<RoomInstance>(nonSolo);
        while (_runSequence.Count < roomCount && remaining.Count > 0)
        {
            var candidates = remaining.FindAll(r => r.data.mechanic != lastMechanic);
            if (candidates.Count == 0) candidates = remaining;

            var pick = candidates[rng.Next(candidates.Count)];
            _runSequence.Add(pick);
            lastMechanic = pick.data.mechanic;
            remaining.Remove(pick);
        }

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

        _echoManager.ClearAllEchos();
        _loopTimer.StartLoop();
    }

    public void OnRoomCleared()
    {
        if (Services.TryGet<RunManager>(out var run))
            run.RoomCleared();
        LoadNext();
    }

    public RoomData CurrentRoom => (_currentIndex >= 0 && _currentIndex < _runSequence.Count) ? _runSequence[_currentIndex].data : null;
}
