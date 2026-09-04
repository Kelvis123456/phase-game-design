using System.Collections.Generic;
using UnityEngine;

// Gestiona hasta 5 slots de eco. Fase 10 M1.1: pool de 10 EchoPlayer pre-instanciados
// (5 activos + 5 en reserva, Fase 8 §17.2.4) en vez de Instantiate/Destroy por eco.
// Se integra con LoopTimer via evento OnLoopEnd.
public class EchoManager : MonoBehaviour
{
    private const int PoolSize = 10;

    [Header("Setup")]
    [SerializeField] private EchoPlayer _echoPrefab;
    [SerializeField] private InputRecorder _recorder;
    [SerializeField] private LoopTimer _loopTimer;

    [Header("VS — slots activos al inicio")]
    [SerializeField, Range(1, 5)] private int _maxEchos = 1;

    private EchoPlayer[] _slots = new EchoPlayer[5];
    private int _activeCount;
    private readonly Queue<EchoPlayer> _pool = new Queue<EchoPlayer>();

    private static readonly Color[] EchoColors =
    {
        new Color(0.227f, 1.000f, 0.831f, 1f), // Cyan    #3AFFD4
        new Color(0.659f, 0.333f, 0.969f, 1f), // Violet  #A855F7
        new Color(0.976f, 0.451f, 0.086f, 1f), // Ember   #F97316
        new Color(0.133f, 0.773f, 0.369f, 1f), // Verdant #22C55E
        new Color(0.925f, 0.286f, 0.600f, 1f), // Magenta #EC4899
    };

    private void Awake()
    {
        Services.Register(this);
        for (int i = 0; i < PoolSize; i++)
        {
            var echo = Instantiate(_echoPrefab, transform);
            echo.gameObject.SetActive(false);
            echo.OnRecycled += HandleRecycled;
            _pool.Enqueue(echo);
        }
    }

    private EchoPlayer RentFromPool()
    {
        if (_pool.Count == 0)
        {
            // No debería pasar con PoolSize=10 y máximo 5 activos, pero no dejar el juego
            // sin eco si algún día el balance cambia — instanciar uno extra es más seguro
            // que un NullReferenceException en medio de una run.
            var extra = Instantiate(_echoPrefab, transform);
            extra.OnRecycled += HandleRecycled;
            return extra;
        }
        return _pool.Dequeue();
    }

    private void HandleRecycled(EchoPlayer echo) => _pool.Enqueue(echo);

    private void OnEnable()
    {
        if (_loopTimer) _loopTimer.OnLoopEnd += HandleLoopEnd;
    }

    private void OnDisable()
    {
        if (_loopTimer) _loopTimer.OnLoopEnd -= HandleLoopEnd;
    }

    private void HandleLoopEnd(float duration)
    {
        if (_activeCount >= _maxEchos)
            EvictOldest();

        var recording = _recorder.GetRecording(duration);
        if (recording.Length == 0) return;

        float speedMultiplier = Services.TryGet<RunManager>(out var run) ? run.ActiveUpgrades.echoSpeedMultiplier : 1f;
        var echo = RentFromPool();
        echo.Initialize(recording, EchoColors[_activeCount], _activeCount, speedMultiplier);
        _slots[_activeCount] = echo;
        _activeCount++;
    }

    private void EvictOldest()
    {
        _slots[0]?.Die();
        for (int i = 0; i < _maxEchos - 1; i++)
        {
            _slots[i] = _slots[i + 1];
            _slots[i]?.UpdateSlot(i);
        }
        _slots[_maxEchos - 1] = null;
        _activeCount--;
    }

    public void ClearAllEchos()
    {
        for (int i = 0; i < _activeCount; i++)
        {
            _slots[i]?.Die();
            _slots[i] = null;
        }
        _activeCount = 0;
    }

    public void UnlockSlot() => _maxEchos = Mathf.Min(_maxEchos + 1, 5);
    public int ActiveCount => _activeCount;
}
