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
    private float _echoTarget = 1f;

    // Run upgrades (RunUpgradeEffects) — reseteados por RunManager en cada StartRun.
    private float _deactivateSmoothBonus = 0f; // R03 Bullet Extendido: más lento = más suave

    // GDD §4.1 Rama B — reseteados por RunManager en cada StartRun (ver BranchBModifier.cs).
    private float _worldBaseline = 1f;         // valor de fondo del Layer.World (R11 Mundo Lento)
    private bool _branchBDoubleBulletTime;     // B5 Doble Bullet
    private bool _branchBBulletTimeDisabled;   // B6 Sin Bullet
    private bool _branchBFogOfWar;             // B4 Niebla de Sala

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

        // GDD §14.3 — accesibilidad: "Velocidad de ecos en bullet-time" (slider 1.0x-0.5x).
        // Por defecto (1.0) los ecos siguen sin verse afectados por bullet-time, igual que
        // siempre — esto es estrictamente opcional, nunca cambia el comportamiento base.
        _echoTarget = _playerTarget < 1f ? EchoBulletTimeSpeedPref : 1f;
        _scales[(int)Layer.Echo] = Mathf.Lerp(_scales[(int)Layer.Echo], _echoTarget, speed * dt);

        UpdatePostProcessing();

        if (Services.TryGet<AudioManager>(out var audio))
            audio.SetBulletTimeAmount(1f - _scales[(int)Layer.Player]);
    }

    private void UpdatePostProcessing()
    {
        float t = 1f - _scales[(int)Layer.Player]; // 0 = normal, 1 = bullet-time completo

        // B4 Niebla de Sala (GDD: "visibilidad reducida a radio de 3 tiles"): el VS no
        // tiene un sistema de visibilidad por tile, así que se aproxima subiendo el piso
        // del vignette incluso FUERA de bullet-time — más oscuro en los bordes de pantalla
        // todo el tiempo, no solo al activar bullet-time.
        float vignetteFloor = _branchBFogOfWar ? Mathf.Max(_vignetteNormal, 0.6f) : _vignetteNormal;

        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(vignetteFloor, _vignetteBulletTime, t);

        if (_chromatic != null)
            _chromatic.intensity.value = Mathf.Lerp(_chromaticNormal, _chromaticBulletTime, t);
    }

    public void SetBulletTime(bool active)
    {
        // B6 Sin Bullet: bloquea la ACTIVACIÓN nada más — desactivar siempre pasa, por las
        // dudas de que quedara encendido de una run anterior con otro modificador.
        if (active && _branchBBulletTimeDisabled) return;

        // B5 Doble Bullet (GDD: "el mundo baja a 0.1x, el jugador también a 0.05x" — más
        // extremo que el bullet-time normal, que solo frena al jugador a 0.1x y deja el
        // mundo/Layer.World como estaba).
        _playerTarget = active
            ? (_branchBDoubleBulletTime ? 0.05f : _bulletTimeScale)
            : 1f;
        _scales[(int)Layer.World] = active && _branchBDoubleBulletTime ? 0.1f : _worldBaseline;
    }

    public void SetBranchBBulletTime(bool doubleIntensity, bool disabled)
    {
        _branchBDoubleBulletTime = doubleIntensity;
        _branchBBulletTimeDisabled = disabled;
    }

    public void SetBranchBFogOfWar(bool active) => _branchBFogOfWar = active;

    private static float EchoBulletTimeSpeedPref =>
        Services.TryGet<SaveSystem>(out var save) ? Mathf.Clamp(save.Current.accessibilityPrefs.btEchoSpeed, 0.5f, 1f) : 1f;

    // R03 Bullet Extendido.
    public void SetDeactivateSmoothBonus(float bonus) => _deactivateSmoothBonus = bonus;

    // R11 Mundo Lento — Layer.World es lo que corre el LoopTimer, así que esto sí
    // "facilita el timing sin afectar ecos" (los ecos corren siempre en Layer.Echo).
    // Guarda el valor como BASELINE — B5 Doble Bullet lo pisa temporalmente mientras
    // bullet-time está activo y vuelve a este valor (no a 1f a secas) al desactivarse.
    public void SetWorldScale(float scale)
    {
        _worldBaseline = scale;
        if (_playerTarget >= 1f) _scales[(int)Layer.World] = scale;
    }

    public float Delta(Layer layer) => _scales[(int)layer] * Time.deltaTime;
    public float Scale(Layer layer) => _scales[(int)layer];
    public bool IsBulletTimeActive => _playerTarget < 0.5f;
}
