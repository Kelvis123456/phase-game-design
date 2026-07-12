# PHASE — Fase 8: Arquitectura de Software
> Diseño de sistemas implementable directamente en Unity 2022 LTS. Cada sección incluye la decisión arquitectónica, el código de referencia y la justificación. Este documento es la guía de implementación de la Fase 9 (Vertical Slice) y Fase 10 (Desarrollo completo).

---

## 0. Vista General del Sistema

```
┌──────────────────────────────────────────────────────────────────┐
│                        PHASE — SISTEMAS                          │
├──────────────────┬───────────────────────────────────────────────┤
│  CORE            │  GameManager · SceneLoader · ServiceLocator   │
│  TIME            │  TimeManager (per-layer scale, sin timeScale) │
│  INPUT           │  InputReader (New Input System, touch)        │
│  ECHO            │  InputRecorder · EchoPlayer · EchoManager     │
│  PLAYER          │  PlayerController · PlayerStats · PlayerFX    │
│  NIVEL           │  RoomManager · TilemapBuilder · CameraFollow  │
│  RUN             │  RunManager (FSM) · CrystalBank · LoopTimer   │
│  META            │  ProgressionManager · UpgradeRegistry         │
│  AUDIO           │  FMODManager · MusicController                │
│  SAVE            │  SaveSystem (JSON) · SaveData                 │
│  VFX             │  VFXPool · VFXRequest                         │
│  UI              │  UIManager · HUDController · MenuController   │
└──────────────────┴───────────────────────────────────────────────┘
```

**Patrón arquitectónico central: ServiceLocator + ScriptableObject Events**

No usamos Singletons clásicos (causan dependencias implícitas difíciles de testear). En su lugar:
- `ServiceLocator` estático para servicios globales (GameManager, TimeManager, SaveSystem)
- `ScriptableObject` con `UnityEvent` para comunicación entre sistemas sin referencias directas
- Cada sistema es independiente: puede compilar y testear sin los demás

---

## 1. Estructura de Escenas (Additive Loading)

```
Scenes/
  Persistent.unity     — NUNCA se descarga. Contiene: GameManager, ServiceLocator,
                         AudioManager (FMOD), InputReader, SaveSystem
  MainMenu.unity       — Cargada sobre Persistent al inicio
  Gameplay.unity       — Cargada al iniciar un run (tilemap, player, enemies)
  HUD.unity            — Cargada siempre que Gameplay esté activa (overlay UI)
  RunEnd.unity         — Modal sobre HUD, no descarga Gameplay
  MetaProgression.unity
  Tutorial.unity       — Reemplaza Gameplay para el tutorial
```

```csharp
// SceneLoader.cs — en Persistent
public class SceneLoader : MonoBehaviour
{
    private string _activeGameScene;

    public async Awaitable LoadGameplay()
    {
        await SceneManager.LoadSceneAsync("Gameplay", LoadSceneMode.Additive);
        await SceneManager.LoadSceneAsync("HUD", LoadSceneMode.Additive);
        _activeGameScene = "Gameplay";
    }

    public async Awaitable UnloadGameplay()
    {
        await SceneManager.UnloadSceneAsync("HUD");
        await SceneManager.UnloadSceneAsync(_activeGameScene);
    }
}
```

---

## 2. ServiceLocator

Punto de registro y acceso para todos los servicios globales. Evita el problema del Singleton: los sistemas no se referencian entre sí directamente.

```csharp
// ServiceLocator.cs
public static class Services
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) => _services[typeof(T)] = service;

    public static T Get<T>()
    {
        if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
        throw new Exception($"Service {typeof(T).Name} not registered.");
    }

    public static bool TryGet<T>(out T service)
    {
        if (_services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
        service = default; return false;
    }
}

// Cada Manager se registra en su Awake():
public class TimeManager : MonoBehaviour
{
    private void Awake() => Services.Register<TimeManager>(this);
}

// Otros sistemas lo consumen:
var time = Services.Get<TimeManager>();
```

---

## 3. TimeManager — La Decisión Arquitectónica Más Crítica

### El problema

La mecánica de bullet-time requiere que el **jugador** vaya a 0.1× velocidad mientras los **ecos** continúan a 1.0×. Usar `Time.timeScale = 0.1` ralentizaría todo, incluyendo los ecos y FMOD.

### Solución: Time Layers (sin tocar Time.timeScale)

`Time.timeScale` permanece en **1.0 siempre**. Cada sistema consulta su propio multiplicador.

```csharp
// TimeManager.cs
public class TimeManager : MonoBehaviour
{
    public enum Layer { World, Player, Echo, UI, Physics }

    private float[] _scales = { 1f, 1f, 1f, 1f, 1f };
    private float _bulletTimeTarget = 1f;
    private float _bulletTimeSmooth = 8f; // lerp speed

    public float Delta(Layer layer) => _scales[(int)layer] * Time.deltaTime;
    public float Scale(Layer layer) => _scales[(int)layer];

    // Se llama cuando el jugador activa bullet-time
    public void SetBulletTime(bool active)
    {
        _bulletTimeTarget = active ? 0.1f : 1f;
        // Echoes (Layer.Echo) no cambian — siempre 1.0f
        // FMOD BulletTimeAmount se actualiza en Update()
    }

    private void Update()
    {
        // Smooth transition en/fuera de bullet-time
        _scales[(int)Layer.Player] = Mathf.Lerp(
            _scales[(int)Layer.Player], _bulletTimeTarget, _bulletTimeSmooth * Time.deltaTime);

        // Informar a FMOD del estado actual
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName(
            "BulletTimeAmount", 1f - _scales[(int)Layer.Player]);
    }
}
```

```csharp
// Cómo lo usa cada sistema:
// PlayerController — usa Layer.Player
velocity += gravity * _timeManager.Delta(TimeManager.Layer.Player);
rb.MovePosition(rb.position + velocity * _timeManager.Delta(TimeManager.Layer.Player));

// EchoPlayer — usa Layer.Echo (siempre 1.0x)
transform.position = Vector2.MoveTowards(
    transform.position, nextWaypoint, speed * _timeManager.Delta(TimeManager.Layer.Echo));

// UI — usa Layer.UI (nunca se ve afectada)
cooldownBar.fillAmount -= _timeManager.Delta(TimeManager.Layer.UI);
```

### Por qué no usar Time.timeScale

- FMOD: el audio engine escucha `Time.timeScale`. Si baja a 0.1, el pitch baja globalmente — lo que queremos es controlar eso por parámetro, no por timeScale
- Physics 2D: el motor de física de Unity respeta `Time.timeScale`. Los ecos son cinemáticos (Rigidbody2D kinematic), así que podrían ignorarlo, pero la colisión del jugador con física real se complica
- FixedUpdate: corre en función de `Time.fixedDeltaTime * Time.timeScale`. Mejor no tocarlo

---

## 4. Input System — Control Táctil

### Esquema de controles (mobile)

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│  ZONA IZQUIERDA          ZONA DERECHA               │
│  (50% pantalla)          (50% pantalla)             │
│                                                     │
│  Tap       = Salto       Tap       = Salto          │
│  Hold+drag = Mover       Hold+drag = Mover          │
│                                                     │
│  ─────────────────────────────────────────────────  │
│                                                     │
│  HOLD SIN MOVER (cualquier zona) = BULLET-TIME      │
│  (touch velocity < 5px/s por 0.15s → activa BT)    │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### InputReader.cs — Unity New Input System

```csharp
// InputReader.cs — en Persistent scene
// Usa Unity's new Input System (com.unity.inputsystem)
[CreateAssetMenu(menuName = "PHASE/InputReader")]
public class InputReader : ScriptableObject, IInputActionCollection
{
    private PhaseInputActions _actions; // auto-generado por Input System

    // Eventos que otros sistemas escuchan
    public event Action<float> OnMove;          // -1.0 a 1.0
    public event Action OnJumpStarted;
    public event Action OnJumpCanceled;
    public event Action<bool> OnBulletTimeChanged; // true = activar

    // Estado interno de touch
    private Vector2 _lastTouchPos;
    private float _stationaryTime;
    private bool _bulletTimeActive;
    private const float BT_VELOCITY_THRESHOLD = 5f;  // px/s
    private const float BT_HOLD_DURATION = 0.15f;

    private void OnEnable()
    {
        _actions = new PhaseInputActions();
        _actions.Gameplay.Enable();
        _actions.Gameplay.Touch.performed += OnTouchPerformed;
    }

    private void OnTouchPerformed(InputAction.CallbackContext ctx)
    {
        var touch = ctx.ReadValue<UnityEngine.InputSystem.EnhancedTouch.Touch>();
        Vector2 currentPos = touch.screenPosition;
        float velocity = (currentPos - _lastTouchPos).magnitude / Time.deltaTime;
        _lastTouchPos = currentPos;

        // Movimiento: posición horizontal relativa al centro de pantalla
        float screenHalf = Screen.width * 0.5f;
        float moveInput = (currentPos.x - screenHalf) / screenHalf; // -1 a 1
        OnMove?.Invoke(Mathf.Clamp(moveInput, -1f, 1f));

        // Bullet-time: detectar quietud
        if (velocity < BT_VELOCITY_THRESHOLD)
        {
            _stationaryTime += Time.deltaTime;
            if (_stationaryTime >= BT_HOLD_DURATION && !_bulletTimeActive)
            {
                _bulletTimeActive = true;
                OnBulletTimeChanged?.Invoke(true);
            }
        }
        else
        {
            _stationaryTime = 0f;
            if (_bulletTimeActive)
            {
                _bulletTimeActive = false;
                OnBulletTimeChanged?.Invoke(false);
            }
        }
    }
}
```

---

## 5. Echo System — El Corazón de PHASE

Este es el sistema más complejo y el más importante de implementar correctamente.

### Arquitectura en 3 capas

```
InputRecorder        →   graba posiciones del jugador en un ring buffer
EchoManager          →   gestiona hasta 5 slots de eco, decide cuándo crear nuevos
EchoPlayer           →   reproduce el buffer de un slot específico en bucle
```

### 5.1 InputRecorder

```csharp
// InputRecorder.cs — componente en el Player GameObject
public class InputRecorder : MonoBehaviour
{
    // Ring buffer: guarda los últimos MAX_LOOP_DURATION segundos
    private const int SAMPLE_RATE = 24;              // muestras por segundo
    private const float MAX_LOOP_DURATION = 8f;      // máximo de un loop
    private const int BUFFER_SIZE = (int)(SAMPLE_RATE * MAX_LOOP_DURATION); // 192 frames

    private PlayerSnapshot[] _buffer = new PlayerSnapshot[BUFFER_SIZE];
    private int _writeIndex;
    private float _sampleTimer;

    [System.Serializable]
    public struct PlayerSnapshot
    {
        public Vector2 position;
        public Vector2 velocity;
        public PlayerState state;    // Idle, Walk, Jump, Fall, BulletTime
        public float timestamp;
        public bool facingRight;
    }

    public enum PlayerState { Idle, Walk, Jump, Fall, Land, BulletTime, Hurt }

    private void Update()
    {
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer < 1f / SAMPLE_RATE) return;
        _sampleTimer = 0f;

        _buffer[_writeIndex % BUFFER_SIZE] = new PlayerSnapshot
        {
            position = transform.position,
            velocity = _rb.velocity,
            state = _playerController.CurrentState,
            timestamp = Time.time,
            facingRight = _playerController.FacingRight
        };
        _writeIndex++;
    }

    // Devuelve un snapshot de los últimos 'duration' segundos
    public PlayerSnapshot[] GetRecording(float duration)
    {
        int frames = Mathf.Min((int)(duration * SAMPLE_RATE), BUFFER_SIZE);
        var result = new PlayerSnapshot[frames];
        for (int i = 0; i < frames; i++)
        {
            int idx = (_writeIndex - frames + i + BUFFER_SIZE) % BUFFER_SIZE;
            result[i] = _buffer[idx];
        }
        return result;
    }
}
```

### 5.2 EchoManager

```csharp
// EchoManager.cs — en Persistent o GameManager
public class EchoManager : MonoBehaviour
{
    [SerializeField] private EchoPlayer _echoPrefab;
    [SerializeField] private InputRecorder _recorder;

    private EchoPlayer[] _slots = new EchoPlayer[5];
    private int _activeEchos;
    private int _maxEchos = 2; // empieza en 2, sube con meta-progresión

    // Paleta de colores por slot (definitivas, del arte-direction)
    private static readonly Color[] EchoColors =
    {
        new Color(0.23f, 1.00f, 0.83f),  // Eco 1 — Cyan    #3AFFD4
        new Color(0.66f, 0.33f, 0.97f),  // Eco 2 — Violet  #A855F7
        new Color(0.98f, 0.45f, 0.09f),  // Eco 3 — Ember   #F97316
        new Color(0.13f, 0.77f, 0.37f),  // Eco 4 — Verdant #22C55E
        new Color(0.93f, 0.28f, 0.60f),  // Eco 5 — Magenta #EC4899
    };

    // Se llama cuando termina un loop (LoopTimer llega a 0)
    public void OnLoopEnd(float loopDuration)
    {
        if (_activeEchos >= _maxEchos) ShiftEchos(); // eco más viejo muere

        var recording = _recorder.GetRecording(loopDuration);
        var slot = _activeEchos;

        _slots[slot] = Instantiate(_echoPrefab);
        _slots[slot].Initialize(recording, EchoColors[slot], slot);
        _activeEchos = Mathf.Min(_activeEchos + 1, _maxEchos);
    }

    private void ShiftEchos()
    {
        // El eco más viejo (slot 0) muere, todos se desplazan
        if (_slots[0] != null) _slots[0].Die();
        for (int i = 0; i < _maxEchos - 1; i++)
        {
            _slots[i] = _slots[i + 1];
            _slots[i]?.SetSlotIndex(i);
        }
        _slots[_maxEchos - 1] = null;
        _activeEchos--;
    }

    public void UnlockSlot() => _maxEchos = Mathf.Min(_maxEchos + 1, 5);
    public int ActiveEchos => _activeEchos;
}
```

### 5.3 EchoPlayer

```csharp
// EchoPlayer.cs — componente en el Echo prefab
public class EchoPlayer : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Animator _animator;

    private InputRecorder.PlayerSnapshot[] _recording;
    private int _frameIndex;
    private float _frameTimer;
    private Color _echoColor;
    private TimeManager _timeManager;
    private Material _echoMaterial;  // instancia del echo shader

    private const float FRAME_DURATION = 1f / 24f; // mismo SAMPLE_RATE

    public void Initialize(InputRecorder.PlayerSnapshot[] recording, Color color, int slot)
    {
        _recording = recording;
        _echoColor = color;
        _echoMaterial = new Material(_spriteRenderer.sharedMaterial); // instancia
        _echoMaterial.SetColor("_EchoColor", color);
        _echoMaterial.SetFloat("_Opacity", Mathf.Lerp(0.75f, 0.45f, slot / 4f)); // más viejo = más tenue
        _spriteRenderer.material = _echoMaterial;
        _timeManager = Services.Get<TimeManager>();
        _frameIndex = 0;
    }

    private void Update()
    {
        // Ecos corren en Layer.Echo — NUNCA se ven afectados por bullet-time
        _frameTimer += _timeManager.Delta(TimeManager.Layer.Echo);

        if (_frameTimer < FRAME_DURATION) return;
        _frameTimer -= FRAME_DURATION;

        _frameIndex = (_frameIndex + 1) % _recording.Length; // BUCLE
        ApplySnapshot(_recording[_frameIndex]);
    }

    private void ApplySnapshot(InputRecorder.PlayerSnapshot snap)
    {
        transform.position = snap.position;
        _spriteRenderer.flipX = !snap.facingRight;
        _animator.Play(snap.state.ToString());
    }

    public void Die()
    {
        // VFX de muerte del eco
        Services.Get<VFXPool>().Play("EchoDissolve", transform.position, _echoColor);
        Destroy(gameObject, 0.3f); // delay para que VFX alcance a verse
    }

    public void SetSlotIndex(int slot)
    {
        _echoMaterial.SetFloat("_Opacity", Mathf.Lerp(0.75f, 0.45f, slot / 4f));
    }
}
```

---

## 6. PlayerController

```csharp
// PlayerController.cs
[RequireComponent(typeof(Rigidbody2D), typeof(InputRecorder))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpForce = 16f;
    [SerializeField] private float _gravity = -35f;
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private float _jumpBuffer = 0.1f;

    private Rigidbody2D _rb;
    private TimeManager _timeManager;
    private InputReader _input;

    private Vector2 _velocity;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isGrounded;
    private bool _bulletTimeActive;

    public InputRecorder.PlayerState CurrentState { get; private set; }
    public bool FacingRight { get; private set; } = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic; // control manual total
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        _input = Services.Get<InputReader>();
        _timeManager = Services.Get<TimeManager>();
        _input.OnMove += HandleMove;
        _input.OnJumpStarted += HandleJump;
        _input.OnBulletTimeChanged += HandleBulletTime;
    }

    private void FixedUpdate()
    {
        float dt = _timeManager.Delta(TimeManager.Layer.Player);

        // Gravedad manual (respeta bullet-time del jugador)
        if (!_isGrounded)
            _velocity.y += _gravity * dt;

        // Coyote time
        if (_isGrounded) _coyoteTimer = _coyoteTime;
        else _coyoteTimer -= dt;

        // Jump buffer
        if (_jumpBufferTimer > 0)
        {
            _jumpBufferTimer -= dt;
            if (_coyoteTimer > 0) ExecuteJump();
        }

        // Clamp caída
        _velocity.y = Mathf.Max(_velocity.y, -40f);

        // Mover con kinematic (detección de colisión manual vía cast)
        Vector2 move = _velocity * dt;
        move = ResolveCollisions(move);
        _rb.MovePosition(_rb.position + move);

        UpdateState();
    }

    private void HandleMove(float horizontal)
    {
        _velocity.x = horizontal * _moveSpeed;
        if (horizontal != 0) FacingRight = horizontal > 0;
    }

    private void HandleJump()
    {
        _jumpBufferTimer = _jumpBuffer;
    }

    private void HandleBulletTime(bool active)
    {
        _bulletTimeActive = active;
        Services.Get<TimeManager>().SetBulletTime(active);
        // VFX: ring de bullet-time en el HUD
        Services.Get<UIManager>().SetBulletTimeRing(active);
    }

    private void ExecuteJump()
    {
        _velocity.y = _jumpForce;
        _coyoteTimer = 0f;
        _jumpBufferTimer = 0f;
        Services.Get<VFXPool>().Play("JumpDust", transform.position, Color.white);
    }

    private Vector2 ResolveCollisions(Vector2 move)
    {
        // Cast del collider en dirección del movimiento
        // Si hay hit, cortar el movimiento al hit.distance
        // (implementación con Physics2D.BoxCast)
        var hits = Physics2D.BoxCastAll(
            _rb.position, _colliderSize, 0f, move.normalized,
            move.magnitude, LayerMask.GetMask("Ground", "Platform"));

        foreach (var hit in hits)
        {
            if (hit.normal.y > 0.5f) // suelo
            {
                _velocity.y = 0f;
                _isGrounded = true;
                move.y = hit.distance * Mathf.Sign(move.y);
            }
            else if (hit.normal.y < -0.5f) // techo
            {
                _velocity.y = Mathf.Min(0f, _velocity.y);
                move.y = hit.distance * Mathf.Sign(move.y);
            }
            else // pared
            {
                move.x = hit.distance * Mathf.Sign(move.x);
            }
        }
        if (hits.Length == 0) _isGrounded = false;
        return move;
    }

    private void UpdateState()
    {
        if (_bulletTimeActive) { CurrentState = InputRecorder.PlayerState.BulletTime; return; }
        if (!_isGrounded) { CurrentState = _velocity.y > 0 ? InputRecorder.PlayerState.Jump : InputRecorder.PlayerState.Fall; return; }
        CurrentState = Mathf.Abs(_velocity.x) > 0.1f ? InputRecorder.PlayerState.Walk : InputRecorder.PlayerState.Idle;
    }
}
```

---

## 7. RunManager — Máquina de Estados del Run

```csharp
// RunManager.cs
public class RunManager : MonoBehaviour
{
    public enum RunState { Initializing, Playing, BetweenRooms, Paused, RunEnded }

    public RunState State { get; private set; }

    [SerializeField] private LoopTimer _loopTimer;
    [SerializeField] private EchoManager _echoManager;

    private int _roomsCleared;
    private int _crystalsThisRun;
    private RunData _runData; // ScriptableObject con configuración del run actual

    private void Awake() => Services.Register<RunManager>(this);

    public void StartRun(RunData data)
    {
        _runData = data;
        _roomsCleared = 0;
        _crystalsThisRun = 0;
        State = RunState.Playing;
        _loopTimer.StartLoop(data.loopDuration);
        LoadNextRoom();
    }

    // Llamado por LoopTimer cuando el tiempo llega a 0
    public void OnLoopEnd()
    {
        _echoManager.OnLoopEnd(_runData.loopDuration);
        _loopTimer.ResetLoop();

        // Cada N loops: boss room, evento especial
        if ((_roomsCleared + 1) % _runData.bossEveryNRooms == 0)
            LoadBossRoom();
        else
            LoadNextRoom();
    }

    public void OnPlayerDeath()
    {
        State = RunState.RunEnded;
        var result = new RunResult
        {
            roomsCleared = _roomsCleared,
            crystalsEarned = _crystalsThisRun,
            echosPeak = _echoManager.ActiveEchos,
            causeOfDeath = _lastHazard
        };
        Services.Get<SceneLoader>().LoadRunEnd(result);
    }

    public void OnRoomCleared()
    {
        _roomsCleared++;
        State = RunState.BetweenRooms;
        // Transición + cargar siguiente sala
    }
}
```

---

## 8. LoopTimer

```csharp
// LoopTimer.cs — en HUD scene
public class LoopTimer : MonoBehaviour
{
    private float _duration;
    private float _remaining;
    private bool _running;

    // Evento para que HUD y RunManager escuchen sin referencia directa
    [SerializeField] private GameEvent _onLoopEnd;  // ScriptableObject event

    public float Progress => 1f - (_remaining / _duration); // 0 a 1
    public float Remaining => _remaining;

    public void StartLoop(float duration)
    {
        _duration = duration;
        _remaining = duration;
        _running = true;
    }

    public void ResetLoop()
    {
        _remaining = _duration;
    }

    private void Update()
    {
        if (!_running) return;
        // El timer corre en tiempo WORLD — no afectado por bullet-time del jugador
        _remaining -= Services.Get<TimeManager>().Delta(TimeManager.Layer.World);
        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _running = false;
            _onLoopEnd.Raise();
        }
    }
}

// GameEvent.cs — ScriptableObject para comunicación desacoplada
[CreateAssetMenu(menuName = "PHASE/GameEvent")]
public class GameEvent : ScriptableObject
{
    private List<GameEventListener> _listeners = new();
    public void Raise() => _listeners.ForEach(l => l.OnEventRaised());
    public void Register(GameEventListener l) => _listeners.Add(l);
    public void Unregister(GameEventListener l) => _listeners.Remove(l);
}
```

---

## 9. Room / Level System

### Filosofía: Semi-procedural con templates curados

El nivel NO es totalmente aleatorio. Cada sala es un `RoomTemplate` ScriptableObject diseñado a mano, pero el sistema selecciona y conecta salas de forma procedural basándose en dificultad progresiva.

```
RoomTemplates/
  Easy/    — 10-15 templates, solo plataformas básicas
  Medium/  — 15-20 templates, peligros medios
  Hard/    — 10-12 templates, combinación de enemigos + peligros
  Boss/    — 3-5 templates, encuentros boss específicos
```

```csharp
[CreateAssetMenu(menuName = "PHASE/RoomTemplate")]
public class RoomTemplate : ScriptableObject
{
    public GameObject tilemapPrefab;     // Tilemap de la sala
    public Transform[] spawnPoints;      // Puntos de spawn de enemigos
    public Transform playerStartPoint;
    public Transform exitPoint;
    public RoomDifficulty difficulty;
    [Range(0, 1)] public float echoRequiredProbability; // qué tan necesario es usar ecos
    public EnemyConfig[] enemies;
}

// RoomManager.cs
public class RoomManager : MonoBehaviour
{
    [SerializeField] private RoomTemplate[] _easyRooms;
    [SerializeField] private RoomTemplate[] _mediumRooms;
    [SerializeField] private RoomTemplate[] _hardRooms;

    private GameObject _currentRoomInstance;
    private HashSet<RoomTemplate> _usedThisRun = new();

    public RoomTemplate SelectNextRoom(int roomsCleared)
    {
        // Curva de dificultad: easy primeras 3, luego mix creciente
        var pool = roomsCleared < 3 ? _easyRooms :
                   roomsCleared < 7 ? MixPools(_easyRooms, _mediumRooms, 0.6f) :
                                      MixPools(_mediumRooms, _hardRooms, 0.7f);

        // No repetir sala hasta que se hayan visto todas del pool
        var available = pool.Where(r => !_usedThisRun.Contains(r)).ToArray();
        if (available.Length == 0) _usedThisRun.Clear();

        return available[UnityEngine.Random.Range(0, available.Length)];
    }

    public async Awaitable LoadRoom(RoomTemplate template)
    {
        if (_currentRoomInstance) Destroy(_currentRoomInstance);
        _currentRoomInstance = Instantiate(template.tilemapPrefab);
        _usedThisRun.Add(template);
        // Posicionar jugador
        var player = Services.Get<PlayerController>();
        player.transform.position = template.playerStartPoint.position;
        await Task.Yield(); // frame para que el tilemap collider se inicialice
    }
}
```

---

## 10. Meta-Progression System

```csharp
// UpgradeRegistry.cs — ScriptableObject con todos los upgrades disponibles
[CreateAssetMenu(menuName = "PHASE/UpgradeRegistry")]
public class UpgradeRegistry : ScriptableObject
{
    public UpgradeData[] allUpgrades;
}

[System.Serializable]
public class UpgradeData
{
    public string id;                  // "echo_unlock_slot3"
    public string displayName;
    public string description;
    public Sprite icon;
    public UpgradeCategory category;  // Echo, Movement, Survival, Utility
    public int maxLevel;
    public int[] crystalCosts;        // costo por nivel (índice = nivel actual)
    public string prerequisiteId;     // "" = ninguno
}

// ProgressionManager.cs
public class ProgressionManager : MonoBehaviour
{
    private SaveData _save;
    private UpgradeRegistry _registry;

    private void Awake()
    {
        Services.Register<ProgressionManager>(this);
        _save = Services.Get<SaveSystem>().Load();
        _registry = Resources.Load<UpgradeRegistry>("UpgradeRegistry");
    }

    public bool CanAfford(UpgradeData upgrade)
    {
        int level = GetLevel(upgrade.id);
        if (level >= upgrade.maxLevel) return false;
        if (!string.IsNullOrEmpty(upgrade.prerequisiteId) && GetLevel(upgrade.prerequisiteId) == 0) return false;
        return _save.totalCrystals >= upgrade.crystalCosts[level];
    }

    public void Purchase(UpgradeData upgrade)
    {
        int level = GetLevel(upgrade.id);
        _save.totalCrystals -= upgrade.crystalCosts[level];
        _save.upgradeLevels[upgrade.id] = level + 1;

        // Aplicar efecto si es un unlock de slot
        if (upgrade.id.StartsWith("echo_unlock"))
            Services.Get<EchoManager>().UnlockSlot();

        Services.Get<SaveSystem>().Save(_save);
    }

    public int GetLevel(string id) =>
        _save.upgradeLevels.TryGetValue(id, out int l) ? l : 0;
}
```

---

## 11. Save System

```csharp
// SaveSystem.cs
public class SaveSystem : MonoBehaviour
{
    private const string SAVE_FILE = "phase_save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

    private void Awake() => Services.Register<SaveSystem>(this);

    public SaveData Load()
    {
        if (!File.Exists(SavePath)) return new SaveData();
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath)); }
        catch { return new SaveData(); } // archivo corrupto → datos frescos
    }

    public void Save(SaveData data)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: false));
    }
}

[System.Serializable]
public class SaveData
{
    public int totalCrystals = 0;
    public int totalRuns = 0;
    public int bestRoomsCleared = 0;
    public bool tutorialCompleted = false;
    public SerializableDictionary<string, int> upgradeLevels = new();
    public SerializableDictionary<string, bool> echoSkins = new();  // skins desbloqueadas
    public string settings_json = "";  // sub-objeto de settings serializado
}
```

---

## 12. VFX Pool

```csharp
// VFXPool.cs — object pooling para partículas
public class VFXPool : MonoBehaviour
{
    [System.Serializable]
    public struct VFXEntry { public string key; public ParticleSystem prefab; public int preloadCount; }
    [SerializeField] private VFXEntry[] _vfxDefinitions;

    private Dictionary<string, Queue<ParticleSystem>> _pools = new();

    private void Awake()
    {
        Services.Register<VFXPool>(this);
        foreach (var def in _vfxDefinitions)
        {
            _pools[def.key] = new Queue<ParticleSystem>();
            for (int i = 0; i < def.preloadCount; i++)
            {
                var ps = Instantiate(def.prefab, transform);
                ps.gameObject.SetActive(false);
                _pools[def.key].Enqueue(ps);
            }
        }
    }

    public void Play(string key, Vector3 position, Color color)
    {
        if (!_pools.TryGetValue(key, out var pool)) return;
        var ps = pool.Count > 0 ? pool.Dequeue() : Instantiate(GetPrefab(key), transform);
        ps.transform.position = position;
        var main = ps.main;
        main.startColor = color;
        ps.gameObject.SetActive(true);
        ps.Play();
        StartCoroutine(ReturnToPool(ps, key, main.duration + main.startLifetime.constantMax));
    }

    private IEnumerator ReturnToPool(ParticleSystem ps, string key, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.Stop();
        ps.gameObject.SetActive(false);
        _pools[key].Enqueue(ps);
    }
}
```

---

## 13. Estructura de Carpetas Unity

```
Assets/
├── _PHASE/                    — TODO el código del juego aquí
│   ├── Core/                  — GameManager, ServiceLocator, SceneLoader
│   ├── Time/                  — TimeManager
│   ├── Input/                 — InputReader, PhaseInputActions.inputactions
│   ├── Echo/                  — InputRecorder, EchoManager, EchoPlayer
│   ├── Player/                — PlayerController, PlayerStats, PlayerFX
│   ├── Level/                 — RoomManager, RoomTemplate, TilemapBuilder
│   ├── Run/                   — RunManager, LoopTimer, RunData, RunResult
│   ├── Progression/           — ProgressionManager, UpgradeData, UpgradeRegistry
│   ├── Audio/                 — FMODManager, MusicController
│   ├── Save/                  — SaveSystem, SaveData
│   ├── VFX/                   — VFXPool, VFXRequest
│   ├── UI/                    — UIManager, HUDController, MenuController
│   └── Events/                — GameEvent ScriptableObjects
│
├── Art/
│   ├── Characters/            — Spritesheets del jugador y ecos
│   ├── Tilesets/              — Atlas 256×256
│   ├── Enemies/               — Sprites de enemigos
│   ├── VFX/                   — Spritesheets de VFX
│   └── UI/                    — Sprites de interfaz
│
├── Audio/                     — FMOD banks (.bank files)
│   ├── Master.bank
│   ├── Master.strings.bank
│   └── [otros banks]
│
├── Prefabs/
│   ├── Player.prefab
│   ├── Echoes/                — Echo_Slot1.prefab ... Echo_Slot5.prefab
│   ├── Enemies/
│   └── VFX/
│
├── ScriptableObjects/
│   ├── Events/                — GameEvent assets (.asset)
│   ├── Rooms/                 — RoomTemplate assets
│   ├── Upgrades/              — UpgradeData assets + UpgradeRegistry.asset
│   └── RunConfigs/            — RunData assets (Normal, Tutorial, Boss)
│
├── Scenes/
│   ├── Persistent.unity
│   ├── MainMenu.unity
│   ├── Gameplay.unity
│   ├── HUD.unity
│   ├── RunEnd.unity
│   ├── MetaProgression.unity
│   └── Tutorial.unity
│
├── Shaders/
│   └── EchoShader.shader      — HLSL del eco (reemplaza blanco por _EchoColor)
│
└── Settings/
    ├── URPAsset.asset         — URP config
    ├── GlobalPostProcess.asset
    └── InputActions.inputactions
```

---

## 14. Echo Shader (Implementación Final)

```hlsl
// EchoShader.shader
Shader "PHASE/Echo"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _EchoColor ("Echo Color", Color) = (0.23, 1.0, 0.83, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.65
        _EmissionIntensity ("Emission", Range(0, 2)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _EchoColor;
            float _Opacity;
            float _EmissionIntensity;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                if (tex.a < 0.01) discard;

                // Reemplaza el color del sprite por _EchoColor
                // Los píxeles más blancos = más saturados del color del eco
                float luminance = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float3 echoRGB = _EchoColor.rgb * luminance;

                // Glow aditivo en áreas brillantes
                echoRGB += _EchoColor.rgb * _EmissionIntensity * luminance;

                return float4(echoRGB, tex.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
```

---

## 15. Diagrama de Flujo de Datos

```
TouchInput
    │
    ▼
InputReader (ScriptableObject)
    │
    ├──► PlayerController
    │        │
    │        ├──► TimeManager.SetBulletTime()
    │        ├──► VFXPool.Play("JumpDust")
    │        └──► InputRecorder (graba snapshots)
    │
    └──► UIManager (bullet-time ring visual)

LoopTimer
    │
    ▼
GameEvent "OnLoopEnd"
    │
    ├──► EchoManager.OnLoopEnd()   → crea EchoPlayer con recording
    ├──► RunManager.OnLoopEnd()    → carga siguiente sala
    └──► HUDController             → resetea visual del timer

EchoPlayer
    │ (Lee recording en bucle, Layer.Echo — ignora bullet-time)
    ▼
Transform.position (cinematic, no physics)
    + Animator (estados del jugador)
    + EchoShader (_EchoColor, _Opacity)

PlayerStats (HP)
    │
    ▼
GameEvent "OnPlayerDeath"
    │
    ├──► RunManager → LoadRunEnd(result)
    └──► VFXPool.Play("PlayerDeath")
```

---

## 16. Autocrítica

**¿Dónde puede fallar este diseño?**

- **InputRecorder con ring buffer**: Si el loop dura más de 8 segundos, los snapshots más viejos se pierden. Solución: configurar `MAX_LOOP_DURATION` por encima del máximo del GDD (8s) + 20% margen = 10s. El buffer crece a 240 frames, ~10KB por eco — insignificante.

- **Kinematic player + Physics2D.BoxCastAll**: Es más código que usar un CharacterController o Rigidbody dinámico, pero da control total del TimeLayer. El riesgo es bugs de colisión en esquinas y plataformas en movimiento. Mitigación: bien testeado en Vertical Slice antes de construir todo el nivel system.

- **ServiceLocator global**: Si un sistema llama a `Services.Get<X>()` antes de que X se haya registrado en su `Awake()`, explota en runtime. Mitigación: orden de ejecución explícito en `Project Settings → Script Execution Order`. Persistent scene siempre carga primero.

- **ScriptableObject GameEvents**: La lista de listeners crece si no se hace `Unregister` en `OnDisable`. Puede causar memory leaks en escenas que se cargan y descargan. Mitigación: `GameEventListener.cs` hace `Unregister` en `OnDisable` automáticamente.

- **EchoPlayer.Die() + Destroy**: El eco puede intentar acceder a `Services.Get<VFXPool>()` después de que VFXPool haya sido destruido (edge case en cierre de aplicación). Mitigación: null-check en `Die()`.

---

*Fase 8 completada — Arquitectura de software diseñada y lista para implementar.*
*Próxima fase: Fase 9 — Vertical Slice (implementar las mecánicas core y validar que PHASE es divertido)*
