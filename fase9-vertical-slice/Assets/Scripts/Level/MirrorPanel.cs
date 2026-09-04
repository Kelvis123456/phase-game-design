using UnityEngine;

// GDD §8.2 Boss 1 "El Espejo Fragmentado" (Zona 1), Fase 1 — un panel de espejo que
// oscila entre "alineado" (posición A, la palanca sirve) y "lejos" (posición B, la
// palanca no hace nada) en un período fijo (8s [VS], igual que el GDD). Simplificación
// de VS: en vez de mover el panel físicamente por el mundo, el oscilador solo decide
// si la palanca puede activarlo AHORA — el efecto narrativo (leer el timing del
// oscilador) es el mismo sin necesitar animar geometría real. Una vez activado, se
// queda así (igual que el latch de DoorGate) — sin esto, 2 ecos sosteniendo 2 palancas
// simultáneamente sería el único momento válido posible, imposible de alinear con un
// tercer cuerpo (el jugador) en E3 al mismo tiempo.
[RequireComponent(typeof(SpriteRenderer))]
public class MirrorPanel : MonoBehaviour
{
    [SerializeField] private TriggerLever _lever;
    [SerializeField] private float _period = 8f;
    [SerializeField] private Color _colorDim = new Color(0.25f, 0.28f, 0.35f, 1f);   // fuera de ventana
    [SerializeField] private Color _colorAlignable = new Color(0.6f, 0.75f, 0.95f, 1f); // ventana abierta
    [SerializeField] private Color _colorActive = new Color(0.4f, 1f, 0.85f, 1f);     // resuelto

    private SpriteRenderer _sprite;
    private float _clock;

    public bool IsActive { get; private set; }
    public bool IsAlignable => (_clock % _period) < (_period * 0.5f);
    public string DebugState => $"clock={_clock:F2} period={_period:F2} alignable={IsAlignable} leverNull={_lever == null} leverActive={(_lever != null && _lever.IsActive)} active={IsActive}";

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _clock += Time.deltaTime;

        if (!IsActive && _lever != null && _lever.IsActive && IsAlignable)
            IsActive = true;

        _sprite.color = IsActive ? _colorActive : (IsAlignable ? _colorAlignable : _colorDim);
    }

    public void ResetPanel()
    {
        IsActive = false;
        _clock = 0f;
    }
}
