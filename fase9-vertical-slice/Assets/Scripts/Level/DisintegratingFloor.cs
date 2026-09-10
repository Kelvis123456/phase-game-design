using UnityEngine;

// GDD §8.3 Boss Z3 "El Abismo" (Zona 3): un segmento de piso que empieza desintegrado
// (no sólido) y solo se reconstruye por _windowSeconds tras un pulso de palanca — a
// diferencia de CollapsingPlatform (Boss Z2, sostenida CONTINUAMENTE), acá es un
// disparo puntual: el eco activa la palanca UNA VEZ en el momento correcto y el piso
// queda sólido esa ventana de tiempo nada más (GDD: "ventana de 2s [VS] por
// activación"). Si el jugador llega tarde, el piso ya volvió a desaparecer y cae al
// pozo de abajo — mismo HazardSpike/reset de siempre, no un fail-state del boss.
public class DisintegratingFloor : MonoBehaviour
{
    [SerializeField] private float _windowSeconds = 2f;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Color _colorSolid = new Color(0.6f, 0.85f, 0.9f, 1f);
    [SerializeField] private Color _colorGone = new Color(0.15f, 0.1f, 0.2f, 0.35f);

    private Collider2D _collider;
    private float _remaining;

    public bool IsSolid => _remaining > 0f;

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        ApplyState(false);
    }

    private void Update()
    {
        if (_remaining <= 0f) return;

        _remaining -= Time.deltaTime;
        if (_remaining <= 0f) ApplyState(false);
    }

    // Firma compartida con CollapsingPlatform.SetHeld para que TriggerLever pueda
    // reenviar a cualquiera de los dos sin distinguir tipo — acá "held" solo importa en
    // el flanco de subida (pulso), no se sostiene continuamente.
    public void SetHeld(TriggerLever lever, bool held)
    {
        if (!held) return;
        _remaining = _windowSeconds;
        ApplyState(true);
    }

    private void ApplyState(bool solid)
    {
        if (_collider != null) _collider.enabled = solid;
        if (_sprite != null) _sprite.color = solid ? _colorSolid : _colorGone;
    }
}
