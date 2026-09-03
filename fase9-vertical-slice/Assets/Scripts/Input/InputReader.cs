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

    // Desktop keyboard/mouse equivalent (no touchscreen on this build target).
    // Preserves the real mechanic: no active movement input for _btHoldDuration => bullet-time,
    // same as lifting the finger off a stationary touch. NOT a dedicated toggle key.
    private float _kbStationaryTimer;

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
        if (Touch.activeTouches.Count > 0)
            HandleTouch();
        else
            HandleKeyboardDesktop();
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

    // Desktop control scheme (this machine has no touchscreen):
    //   A/D or Left/Right = move   |   Space or Up = jump
    //   Bullet-time = the same mechanic as touch, translated to keyboard:
    //   releasing/never pressing a movement key for _btHoldDuration seconds triggers it
    //   (mirrors "lift your finger off the stationary touch"), pressing a movement key
    //   again cancels it. There is no dedicated bullet-time key — that would test a
    //   different mechanic than the one PHASE actually ships.
    private void HandleKeyboardDesktop()
    {
        float h = Input.GetAxisRaw("Horizontal");
        bool hasMoveInput = Mathf.Abs(h) > 0.1f;

        OnMove?.Invoke(hasMoveInput ? h : 0f);

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
            OnJumpStarted?.Invoke();

        if (hasMoveInput)
        {
            _kbStationaryTimer = 0f;
            if (_bulletTimeActive) SetBulletTime(false);
        }
        else
        {
            _kbStationaryTimer += Time.unscaledDeltaTime;
            if (_kbStationaryTimer >= _btHoldDuration && !_bulletTimeActive)
                SetBulletTime(true);
        }
    }

    private void SetBulletTime(bool active)
    {
        _bulletTimeActive = active;
        OnBulletTimeChanged?.Invoke(active);

        if (Services.TryGet<TimeManager>(out var tm))
            tm.SetBulletTime(active);
    }
}
