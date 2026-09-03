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
        _scales[(int)Layer.Player] = Mathf.Lerp(_scales[(int)Layer.Player], _playerTarget, _smoothSpeed * dt);

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

    public float Delta(Layer layer) => _scales[(int)layer] * Time.deltaTime;
    public float Scale(Layer layer) => _scales[(int)layer];
    public bool IsBulletTimeActive => _playerTarget < 0.5f;
}
