using System.Collections;
using UnityEngine;

// El tech stack original (fase7-tecnologia/seleccion-tecnologia.md) elegía FMOD para
// audio, pero autorear eventos/bancos FMOD requiere FMOD Studio (app de escritorio
// separada, no disponible en este entorno) — solo el plugin de Unity no alcanza sin
// un proyecto .fspro ya armado. Este AudioManager usa AudioSource/AudioLowPassFilter
// nativos de Unity para cubrir el mismo terreno (música con crossfade por zona, SFX
// pooleados, ducking de bullet-time) sin esa dependencia externa. Reemplaza el hook
// comentado en TimeManager.Update() ("FMOD: actualizar parámetro...").
public class AudioManager : MonoBehaviour
{
    private const int SfxPoolSize = 8;
    private const float MusicCrossfadeSeconds = 1.2f;
    private const float LowpassNormalHz = 22000f;
    private const float LowpassBulletTimeHz = 900f;

    [SerializeField] private AudioClip _menuTheme;
    [SerializeField] private AudioClip[] _zoneAmbient; // índice = zoneId - 1
    [SerializeField] private AudioClip _bossTheme;

    // SFX "globales" — usados por sistemas de UI/progresión que se construyen 100% por
    // código (MainMenuUI, UpgradeSelectorUI, ProgressionSystem, AchievementSystem) y no
    // pasan por el wiring de VSSceneBuilder por instancia como los objetos de sala.
    [SerializeField] private AudioClip _uiConfirmSfx;
    [SerializeField] private AudioClip _uiCancelSfx;
    [SerializeField] private AudioClip _uiNavigateSfx;
    [SerializeField] private AudioClip _nodeUnlockSfx;
    [SerializeField] private AudioClip _upgradeUnlockSfx;
    [SerializeField] private AudioClip _achievementUnlockSfx;
    [SerializeField] private AudioClip _zoneTransitionSfx;

    private AudioSource _musicA, _musicB, _activeMusic;
    private AudioLowPassFilter _lowpassA, _lowpassB;
    private AudioSource[] _sfxSources;
    private int _sfxIndex;
    private Coroutine _crossfadeRoutine;
    private bool _muted;

    private void Awake()
    {
        Services.Register(this);

        (_musicA, _lowpassA) = CreateMusicSource("MusicA");
        (_musicB, _lowpassB) = CreateMusicSource("MusicB");
        _activeMusic = _musicA;

        _sfxSources = new AudioSource[SfxPoolSize];
        for (int i = 0; i < SfxPoolSize; i++)
        {
            var go = new GameObject($"SfxSource_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            _sfxSources[i] = src;
        }
    }

    private (AudioSource, AudioLowPassFilter) CreateMusicSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.volume = 0f;
        var lowpass = go.AddComponent<AudioLowPassFilter>();
        lowpass.cutoffFrequency = LowpassNormalHz;
        return (src, lowpass);
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || _muted) return;
        var src = _sfxSources[_sfxIndex];
        _sfxIndex = (_sfxIndex + 1) % _sfxSources.Length;
        src.pitch = 1f;
        src.PlayOneShot(clip, volume);
    }

    public void PlayMenuMusic() => CrossfadeTo(_menuTheme);

    // RoomAssembler.LoadNext llama esto en cada transición de sala.
    public void PlayZoneMusic(int zoneId, bool isBoss)
    {
        CrossfadeTo(isBoss ? _bossTheme : ZoneClip(zoneId));
        PlaySfx(_zoneTransitionSfx);
    }

    public void PlayUiConfirm() => PlaySfx(_uiConfirmSfx);
    public void PlayUiCancel() => PlaySfx(_uiCancelSfx);
    public void PlayUiNavigate() => PlaySfx(_uiNavigateSfx);
    public void PlayNodeUnlock() => PlaySfx(_nodeUnlockSfx);
    public void PlayUpgradeUnlock() => PlaySfx(_upgradeUnlockSfx);
    public void PlayAchievementUnlock() => PlaySfx(_achievementUnlockSfx);

    private AudioClip ZoneClip(int zoneId)
    {
        int idx = zoneId - 1;
        return (_zoneAmbient != null && idx >= 0 && idx < _zoneAmbient.Length) ? _zoneAmbient[idx] : null;
    }

    private void CrossfadeTo(AudioClip clip)
    {
        if (clip == null || _activeMusic.clip == clip) return;

        var incoming = _activeMusic == _musicA ? _musicB : _musicA;
        var outgoing = _activeMusic;

        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
        _crossfadeRoutine = StartCoroutine(CrossfadeRoutine(outgoing, incoming));
        _activeMusic = incoming;
    }

    private IEnumerator CrossfadeRoutine(AudioSource outgoing, AudioSource incoming)
    {
        float t = 0f;
        float outStart = outgoing.volume;
        float targetVolume = _muted ? 0f : 1f;
        while (t < MusicCrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float f = Mathf.Clamp01(t / MusicCrossfadeSeconds);
            outgoing.volume = Mathf.Lerp(outStart, 0f, f);
            incoming.volume = Mathf.Lerp(0f, targetVolume, f);
            yield return null;
        }
        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = targetVolume;
    }

    // TimeManager.Update() llama esto cada frame con 0=normal, 1=bullet-time completo.
    // Baja el lowpass de la música activa — el mismo ducking que FMOD habría hecho vía
    // un parámetro de banco, sin necesitar un mixer aparte.
    public void SetBulletTimeAmount(float amount)
    {
        float hz = Mathf.Lerp(LowpassNormalHz, LowpassBulletTimeHz, amount);
        _lowpassA.cutoffFrequency = hz;
        _lowpassB.cutoffFrequency = hz;
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        _activeMusic.volume = muted ? 0f : 1f;
    }

    public bool IsMuted => _muted;
}
