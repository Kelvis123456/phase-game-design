using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Reproduce un recording en bucle infinito.
// Corre en Layer.Echo — NUNCA se ve afectado por el bullet-time del jugador.
public class EchoPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Animator _animator;
    [SerializeField] private TrailRenderer _trail; // R06 Persistencia Ampliada (GDD §7.3)

    // Nombres de los parámetros del Echo Shader (deben coincidir con EchoShader.shader)
    private static readonly int ShaderColor = Shader.PropertyToID("_EchoColor");
    private static readonly int ShaderOpacity = Shader.PropertyToID("_Opacity");

    // Nombres de parámetros del Animator (mismos que el jugador)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimGrounded = Animator.StringToHash("Grounded");
    private static readonly int AnimVelY = Animator.StringToHash("VelocityY");

    private InputRecorder.Snapshot[] _recording;
    private int _frameIndex;
    private float _frameTimer;
    private const float FRAME_DURATION = 1f / 24f;

    private TimeManager _timeManager;
    private Material _mat;
    private int _slotIndex;
    private float _speedMultiplier = 1f;

    // GDD §4.2/§9.2 Rama C — skin equipada en este slot (null = C1 Eco Base, sin override).
    // Ver SkinCatalog para qué expresa cada una con el material/luz/ciclo disponibles hoy.
    private SkinCatalog.SkinVisual _skinVisual;
    private Light2D _skinLight; // C8 Espectro de Luz
    private int _rainbowCycleIndex;

    // R06 Persistencia Ampliada: rastro visible solo en bullet-time (ayuda a leer la
    // ruta del eco mientras el tiempo está lento, no en juego normal a velocidad real
    // donde solo sería ruido visual). La duración base se dobla si el upgrade está activo.
    private const float BaseTrailTime = 0.5f;

    public void Initialize(InputRecorder.Snapshot[] recording, Color color, int slotIndex, float speedMultiplier = 1f, SkinCatalog.SkinVisual skinVisual = null)
    {
        _recording = recording;
        _slotIndex = slotIndex;
        _speedMultiplier = speedMultiplier;
        _skinVisual = skinVisual;
        _rainbowCycleIndex = 0;
        _frameIndex = 0;
        _frameTimer = 0f;
        _timeManager = Services.Get<TimeManager>();

        Color appliedColor = _skinVisual != null ? _skinVisual.tint : color;
        float appliedOpacity = _skinVisual != null && _skinVisual.opacityOverride >= 0f
            ? _skinVisual.opacityOverride
            : GetOpacityForSlot(slotIndex);

        // Material instanciado para no afectar el original
        _mat = new Material(_sprite.sharedMaterial);
        _mat.SetColor(ShaderColor, appliedColor);
        _mat.SetFloat(ShaderOpacity, appliedOpacity);
        _sprite.material = _mat;

        if (_trail != null)
        {
            _trail.Clear();
            _trail.emitting = false;
            _trail.startColor = new Color(appliedColor.r, appliedColor.g, appliedColor.b, 0.6f);
            _trail.endColor = new Color(appliedColor.r, appliedColor.g, appliedColor.b, 0f);
        }

        if (_skinVisual != null && _skinVisual.emitsLight)
        {
            if (_skinLight == null)
            {
                _skinLight = gameObject.AddComponent<Light2D>();
                _skinLight.lightType = Light2D.LightType.Point;
                _skinLight.pointLightOuterRadius = 2.5f;
                _skinLight.intensity = 1.2f;
            }
            _skinLight.color = Color.white;
            _skinLight.enabled = true;
        }
        else if (_skinLight != null)
        {
            _skinLight.enabled = false;
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        UpdateTrail();
        UpdateBranchBVisibility();

        if (_recording == null || _recording.Length == 0) return;

        // Echo siempre ignora bullet-time; R01/R02 (echoSpeedMultiplier) sí lo escalan.
        _frameTimer += _timeManager.Delta(TimeManager.Layer.Echo) * _speedMultiplier;

        if (_frameTimer < FRAME_DURATION) return;
        _frameTimer -= FRAME_DURATION;

        _frameIndex = (_frameIndex + 1) % _recording.Length;
        if (_frameIndex == 0) OnLoopWrapped();
        ApplySnapshot(_recording[_frameIndex]);
    }

    // C10 "Arco Iris Cuántico" (GDD §4.1): "cada loop del eco cambia de color" — se
    // detecta acá porque es el único punto donde EchoPlayer ya sabe que un loop terminó.
    private void OnLoopWrapped()
    {
        if (_skinVisual == null || !_skinVisual.cyclesColorPerLoop) return;
        _rainbowCycleIndex = (_rainbowCycleIndex + 1) % SkinCatalog.RainbowCycleColors.Length;
        _mat.SetColor(ShaderColor, SkinCatalog.RainbowCycleColors[_rainbowCycleIndex]);
    }

    // B7 Eco Fantasma (Rama B, GDD §4.1): "los ecos son invisibles excepto en bullet-time".
    // Solo el renderer se apaga — la simulación (posición, triggers) sigue corriendo igual,
    // el eco real sigue activando palancas aunque no se vea, tal como pide el GDD.
    private void UpdateBranchBVisibility()
    {
        if (_sprite == null) return;
        bool ghostMode = Services.TryGet<RunManager>(out var run) && run.ActiveBranchB.echoesInvisibleOutsideBulletTime;
        _sprite.enabled = !ghostMode || (_timeManager != null && _timeManager.IsBulletTimeActive);
    }

    private void UpdateTrail()
    {
        if (_trail == null || _timeManager == null) return;

        bool bulletTime = _timeManager.IsBulletTimeActive;
        _trail.emitting = bulletTime;
        if (!bulletTime) return;

        float mult = Services.TryGet<RunManager>(out var run) ? run.ActiveUpgrades.echoTrailDurationMultiplier : 1f;
        _trail.time = BaseTrailTime * mult;
    }

    private void ApplySnapshot(InputRecorder.Snapshot snap)
    {
        // snap.position es relativa al origen X de la sala donde se grabó — sumar el
        // origen de la sala ACTUAL reconstruye la posición absoluta correcta incluso si
        // el eco se está reproduciendo en una sala distinta a la que se grabó.
        float roomOriginX = Services.TryGet<RoomAssembler>(out var asm) ? asm.CurrentRoomOriginX : 0f;
        transform.position = new Vector3(snap.position.x + roomOriginX, snap.position.y, 0f);
        if (_sprite) _sprite.flipX = !snap.facingRight;

        if (_animator)
        {
            bool isWalking = snap.state == PlayerController.PlayerState.Walk;
            bool isGrounded = snap.state == PlayerController.PlayerState.Idle
                           || snap.state == PlayerController.PlayerState.Walk;
            float velY = snap.state == PlayerController.PlayerState.Jump ? 5f :
                         snap.state == PlayerController.PlayerState.Fall ? -5f : 0f;

            _animator.SetFloat(AnimSpeed, isWalking ? 1f : 0f);
            _animator.SetBool(AnimGrounded, isGrounded);
            _animator.SetFloat(AnimVelY, velY);
        }
    }

    // Fase 10 M1.1: pool de 10 EchoPlayer pre-instanciados (5 activos + 5 en reserva) en vez
    // de Instantiate/Destroy por eco — evita spikes de GC en cada shift de slot. Die() ya no
    // destruye el GameObject: se desvanece y vuelve al pool inactivo, listo para reusarse.
    public event Action<EchoPlayer> OnRecycled;

    public void Die()
    {
        if (Services.TryGet<VFXPool>(out var vfx))
            vfx.Play("EchoDissolve", transform.position, _mat.GetColor(ShaderColor));

        _recording = null;
        if (_trail != null) _trail.emitting = false;
        if (_skinLight != null) _skinLight.enabled = false;
        StartCoroutine(RecycleAfterFade(0.4f));
    }

    private IEnumerator RecycleAfterFade(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
        OnRecycled?.Invoke(this);
    }

    public void UpdateSlot(int newSlot)
    {
        _slotIndex = newSlot;
        float opacity = _skinVisual != null && _skinVisual.opacityOverride >= 0f
            ? _skinVisual.opacityOverride
            : GetOpacityForSlot(newSlot);
        _mat?.SetFloat(ShaderOpacity, opacity);
    }

    // Ecos más viejos = más transparentes
    private float GetOpacityForSlot(int slot) => Mathf.Lerp(0.75f, 0.40f, slot / 4f);

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
