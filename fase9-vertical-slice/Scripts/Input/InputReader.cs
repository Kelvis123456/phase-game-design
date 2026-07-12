using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Traduce el input táctil a eventos de gameplay.
// Eventos: OnMove (-1 a 1), OnJump, OnBulletTimeChanged.
// En Editor: WASD/flechas para mover, Espacio para saltar, Z para bullet-time.
[DefaultExecutionOrder(-80)]
public class InputReader : MonoBehaviour
{
    [Header("Bullet-Time Touch")]
    [SerializeField] private float _btVelocityThreshold = 5f;   // px/s — por debajo = "quieto"
    [SerializeField] private float _btHoldDuration = 0.15f;      // segundos quieto para activar

    public event Action<float> OnMove;
    public event Action OnJumpStarted;
    public event Action<bool> OnBulletTimeChanged;

    private Vector2 _lastTouchPos;
    private float _stationaryTimer;
    private bool _bulletTimeActive;
    private bool _touchActive;

    private void Awake() => Services.Register(this);

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        HandleKeyboardDebug();
        HandleTouch();
    }

    private void HandleTouch()
    {
        var touches = Touch.activeTouches;

        if (touches.Count == 0)
        {
            if (_touchActive)
            {
                _touchActive = false;
                OnMove?.Invoke(0f);
                SetBulletTime(false);
            }
            return;
        }

        var touch = touches[0];
        Vector2 screenPos = touch.screenPosition;

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            _lastTouchPos = screenPos;
            _stationaryTimer = 0f;
            _touchActive = true;

            // Tap rápido = salto
            if (touch.tapCount >= 1)
                OnJumpStarted?.Invoke();
        }

        // Movimiento: posición horizontal relativa al centro de pantalla
        float halfWidth = Screen.width * 0.5f;
        float moveInput = (screenPos.x - halfWidth) / halfWidth;
        OnMove?.Invoke(Mathf.Clamp(moveInput, -1f, 1f));

        // Bullet-time: dedo quieto
        float velocity = (screenPos - _lastTouchPos).magnitude / Time.deltaTime;
        _lastTouchPos = screenPos;

        if (velocity < _btVelocityThreshold)
        {
            _stationaryTimer += Time.deltaTime;
            if (_stationaryTimer >= _btHoldDuration && !_bulletTimeActive)
                SetBulletTime(true);
        }
        else
        {
            _stationaryTimer = 0f;
            if (_bulletTimeActive)
                SetBulletTime(false);
        }
    }

    private void HandleKeyboardDebug()
    {
#if UNITY_EDITOR
        float h = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(h) > 0.1f) OnMove?.Invoke(h);
        else if (Touch.activeTouches.Count == 0) OnMove?.Invoke(0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            OnJumpStarted?.Invoke();

        if (Input.GetKeyDown(KeyCode.Z)) SetBulletTime(true);
        if (Input.GetKeyUp(KeyCode.Z)) SetBulletTime(false);
#endif
    }

    private void SetBulletTime(bool active)
    {
        _bulletTimeActive = active;
        OnBulletTimeChanged?.Invoke(active);

        if (Services.TryGet<TimeManager>(out var tm))
            tm.SetBulletTime(active);
    }
}
