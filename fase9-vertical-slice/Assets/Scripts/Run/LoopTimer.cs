using System;
using UnityEngine;
using UnityEngine.UI;

// Timer del loop. Al llegar a 0: dispara OnLoopEnd y se reinicia.
// Corre en Layer.World — NO se ve afectado por bullet-time.
public class LoopTimer : MonoBehaviour
{
    [Header("Config — tunear en Sprint 3")]
    [SerializeField] private float _loopDuration = 8f;

    [Header("HUD (opcional)")]
    [SerializeField] private Image _timerRing;   // Image de tipo Filled para el ring circular
    [SerializeField] private UnityEngine.UI.Text _timerText;

    public event Action<float> OnLoopEnd; // parámetro = duración del loop que terminó

    private float _remaining;
    private bool _running;
    private TimeManager _timeManager;

    public float Progress => 1f - (_remaining / _loopDuration); // 0=inicio, 1=fin
    public float Remaining => _remaining;
    public float Duration => _loopDuration;

    private void Awake() => Services.Register(this);

    private void Start()
    {
        _timeManager = Services.Get<TimeManager>();
        StartLoop();
    }

    public void StartLoop()
    {
        _remaining = _loopDuration;
        _running = true;
    }

    private void Update()
    {
        if (!_running) return;

        // Timer corre en tiempo WORLD (no afectado por bullet-time)
        _remaining -= _timeManager.Delta(TimeManager.Layer.World);

        UpdateHUD();

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _running = false;
            float duration = _loopDuration;
            OnLoopEnd?.Invoke(duration);
            StartLoop(); // reinicia automáticamente
        }
    }

    private void UpdateHUD()
    {
        if (_timerRing) _timerRing.fillAmount = 1f - Progress;
        if (_timerText) _timerText.text = Mathf.CeilToInt(_remaining).ToString();
    }

    public void ResetAndStop()
    {
        _remaining = _loopDuration;
        _running = false;
        UpdateHUD();
    }
}
