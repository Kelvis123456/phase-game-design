using System;
using System.Collections;
using UnityEngine;

// Reproduce un recording en bucle infinito.
// Corre en Layer.Echo — NUNCA se ve afectado por el bullet-time del jugador.
public class EchoPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Animator _animator;

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

    public void Initialize(InputRecorder.Snapshot[] recording, Color color, int slotIndex, float speedMultiplier = 1f)
    {
        _recording = recording;
        _slotIndex = slotIndex;
        _speedMultiplier = speedMultiplier;
        _frameIndex = 0;
        _frameTimer = 0f;
        _timeManager = Services.Get<TimeManager>();

        // Material instanciado para no afectar el original
        _mat = new Material(_sprite.sharedMaterial);
        _mat.SetColor(ShaderColor, color);
        _mat.SetFloat(ShaderOpacity, GetOpacityForSlot(slotIndex));
        _sprite.material = _mat;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (_recording == null || _recording.Length == 0) return;

        // Echo siempre ignora bullet-time; R01/R02 (echoSpeedMultiplier) sí lo escalan.
        _frameTimer += _timeManager.Delta(TimeManager.Layer.Echo) * _speedMultiplier;

        if (_frameTimer < FRAME_DURATION) return;
        _frameTimer -= FRAME_DURATION;

        _frameIndex = (_frameIndex + 1) % _recording.Length;
        ApplySnapshot(_recording[_frameIndex]);
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
        _mat?.SetFloat(ShaderOpacity, GetOpacityForSlot(newSlot));
    }

    // Ecos más viejos = más transparentes
    private float GetOpacityForSlot(int slot) => Mathf.Lerp(0.75f, 0.40f, slot / 4f);

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }
}
