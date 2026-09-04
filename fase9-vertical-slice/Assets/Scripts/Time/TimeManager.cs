using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Time.timeScale NUNCA se toca. Cada sistema pide su propio delta via Delta(layer).
// Bullet-time = Layer.Player va a 0.1x. Layer.Echo siempre 1.0x.
[DefaultExecutionOrder(-90)]
public class TimeManager : MonoBehaviour
{
    public enum Layer { World, Player, Echo, UI }

    [Header("Bullet-Time")]
    [SerializeField] private float _bulletTimeScale = 0.1f;
    [SerializeField] private float _smoothSpeed = 10f;

    [Header("Post-Processing")]
    [SerializeField] private Volume _globalVolume;
    [SerializeField] private float _vignetteNormal = 0.25f;
    [SerializeField] private float _vignetteBulletTime = 0.55f;
    [SerializeField] private float _chromaticNormal = 0f;
    [SerializeField] private float _chromaticBulletTime = 0.35f;

    private float[] _scales = { 1f, 1f, 1f, 1f }; // World, Player, Echo, UI
    private float _playerTarget = 1f;

    // Run upgrades (RunUpgradeEffects) — reseteados por RunManager en cada StartRun.
    private float _deactivateSmoothBonus = 0f; // R03 Bullet Extendido: más lento = más suave

    private Vignette _vignette;
    private ChromaticAberration _chromatic;

    private void Awake()
    {
        Services.Register(this);
        if (_globalVolume != null)
        {
            _globalVolume.profile.TryGet(out _vignette);
            _globalVolume.profile.TryGet(out _chromatic);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        // Al activar bullet-time usa _smoothSpeed normal; al DESACTIVAR (volver a 1x),
        // R03 Bullet Extendido reduce la velocidad de la interpolación — más lento acá
        // significa una transición más suave, no más rápida.
        bool deactivating = _playerTarget >= 1f;
        float speed = deactivating ? Mathf.Max(1f, _smoothSpeed - _deactivateSmoothBonus) : _smoothSpeed;
        _scales[(int)Layer.Player] = Mathf.Lerp(_scales[(int)Layer.Player], _playerTarget, speed * dt);

        UpdatePostProcessing();

        // FMOD: actualizar parámetro si está integrado
        // FMODUnity.RuntimeManager.StudioSystem.setParameterByName("BulletTimeAmount", 1f - _scales[(int)Layer.Player]);
    }

    private void UpdatePostProcessing()
    {
        float t = 1f - _scales[(int)Layer.Player]; // 0 = normal, 1 = bullet-time completo

        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(_vignetteNormal, _vignetteBulletTime, t);

        if (_chromatic != null)
            _chromatic.intensity.value = Mathf.Lerp(_chromaticNormal, _chromaticBulletTime, t);
    }

    public void SetBulletTime(bool active)
    {
        _playerTarget = active ? _bulletTimeScale : 1f;
    }

    // R03 Bullet Extendido.
    public void SetDeactivateSmoothBonus(float bonus) => _deactivateSmoothBonus = bonus;

    // R11 Mundo Lento — Layer.World es lo que corre el LoopTimer, así que esto sí
    // "facilita el timing sin afectar ecos" (los ecos corren siempre en Layer.Echo).
    public void SetWorldScale(float scale) => _scales[(int)Layer.World] = scale;

    public float Delta(Layer layer) => _scales[(int)layer] * Time.deltaTime;
    public float Scale(Layer layer) => _scales[(int)layer];
    public bool IsBulletTimeActive => _playerTarget < 0.5f;
}
