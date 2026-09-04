using UnityEngine;

// Fase 10 M2.4 (GDD §6.2 Zona 3 — FRUSTRATION, "Eco Frustrado intencional"): un pincho
// que alterna activo/retraído en un timer fijo, independiente del jugador. El motor
// actual reproduce ecos como posiciones puras (EchoPlayer.ApplySnapshot) sin colisión
// contra hazards, así que un eco nunca "muere" — la sala usa esto a propósito: el
// jugador debe aprender el timing seguro en carne propia (loop 1, con riesgo real de
// morir), y su eco grabado YA sabe cuándo cruzar. En el loop 2 el jugador confía en
// que su eco de hace un momento va a activar la palanca en la ventana correcta —
// entender y anticipar el comportamiento de tu propio eco, en vez de solo compartir
// espacio con él (SYNC) o encadenar una acción permanente (DEPENDENCY).
[RequireComponent(typeof(Collider2D))]
public class TimedHazard : MonoBehaviour
{
    [SerializeField] private float _onDuration = 1.4f;
    [SerializeField] private float _offDuration = 1.6f;
    [SerializeField] private SpriteRenderer _sprite;

    private Collider2D _collider;
    private float _timer;
    private bool _active = true;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        _timer = _onDuration;
        SetActive(true);
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        SetActive(!_active);
        _timer = _active ? _onDuration : _offDuration;
    }

    private void SetActive(bool active)
    {
        _active = active;
        _collider.enabled = active;
        if (_sprite != null) _sprite.color = new Color(1f, 1f, 1f, active ? 1f : 0.25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Services.TryGet<PlayerStats>(out var stats))
            stats.TakeDamage();
    }
}
