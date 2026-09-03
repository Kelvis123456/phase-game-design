using UnityEngine;

// VS: gestiona hasta 5 slots. Empieza con 1 slot activo.
// Se integra con LoopTimer via evento OnLoopEnd.
public class EchoManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private EchoPlayer _echoPrefab;
    [SerializeField] private InputRecorder _recorder;
    [SerializeField] private LoopTimer _loopTimer;

    [Header("VS — slots activos al inicio")]
    [SerializeField, Range(1, 5)] private int _maxEchos = 1;

    private EchoPlayer[] _slots = new EchoPlayer[5];
    private int _activeCount;

    private static readonly Color[] EchoColors =
    {
        new Color(0.227f, 1.000f, 0.831f, 1f), // Cyan    #3AFFD4
        new Color(0.659f, 0.333f, 0.969f, 1f), // Violet  #A855F7
        new Color(0.976f, 0.451f, 0.086f, 1f), // Ember   #F97316
        new Color(0.133f, 0.773f, 0.369f, 1f), // Verdant #22C55E
        new Color(0.925f, 0.286f, 0.600f, 1f), // Magenta #EC4899
    };

    private void Awake() => Services.Register(this);

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

        var echo = Instantiate(_echoPrefab);
        echo.Initialize(recording, EchoColors[_activeCount], _activeCount);
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
