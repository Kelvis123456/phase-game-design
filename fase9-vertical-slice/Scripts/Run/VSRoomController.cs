using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Gestiona el ciclo muerte → reset de la sala de prueba del VS.
// No es el RunManager completo — solo lo necesario para testear.
public class VSRoomController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform _playerSpawnPoint;
    [SerializeField] private EchoManager _echoManager;
    [SerializeField] private InputRecorder _recorder;
    [SerializeField] private LoopTimer _loopTimer;

    [Header("Death FX")]
    [SerializeField] private Image _deathFlash;      // Image negro/rojo que cubre pantalla
    [SerializeField] private float _flashDuration = 0.3f;
    [SerializeField] private float _resetDelay = 0.8f;

    [Header("Estadísticas VS (solo debug)")]
    [SerializeField] private UnityEngine.UI.Text _deathCountText;

    private PlayerStats _playerStats;
    private PlayerController _player;
    private int _deathCount;
    private bool _resetting;

    private void Start()
    {
        _playerStats = Services.Get<PlayerStats>();
        _player = Services.Get<PlayerController>();
        _playerStats.OnDeath += OnPlayerDeath;

        if (_deathFlash) _deathFlash.color = new Color(1f, 0.2f, 0.2f, 0f);
    }

    private void OnDestroy()
    {
        if (_playerStats != null) _playerStats.OnDeath -= OnPlayerDeath;
    }

    private void OnPlayerDeath()
    {
        if (_resetting) return;
        _resetting = true;
        _deathCount++;
        if (_deathCountText) _deathCountText.text = $"Muertes: {_deathCount}";
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Flash rojo de muerte
        if (_deathFlash)
        {
            float t = 0f;
            while (t < _flashDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Sin(t / _flashDuration * Mathf.PI);
                _deathFlash.color = new Color(1f, 0.2f, 0.2f, alpha * 0.7f);
                yield return null;
            }
            _deathFlash.color = new Color(1f, 0.2f, 0.2f, 0f);
        }
        else
        {
            yield return new WaitForSeconds(_flashDuration);
        }

        yield return new WaitForSeconds(_resetDelay - _flashDuration);

        ResetRoom();
    }

    private void ResetRoom()
    {
        // Reset jugador
        _player.ResetToPosition(_playerSpawnPoint.position);
        _playerStats.ResetStats();

        // Borrar ecos
        _echoManager.ClearAllEchos();

        // Reiniciar grabación
        _recorder.ResetBuffer();

        // Reiniciar timer
        _loopTimer.StartLoop();

        _resetting = false;
    }
}
