using UnityEngine;

// GDD §8.2 Boss 1 "El Espejo Fragmentado" (Zona 1) — implementa Fase 1 ("Primeros
// Reflejos": 3 paneles, 2 ecos + el jugador) como el primer boss real y jugable del
// juego. Fases 2 ("Multiplicación", contrapeso E4/E2) y 3 ("La Convergencia", todos los
// slots activos) quedan fuera de este pase — cada una añade una regla nueva encima de
// esta base, no son arquitectura distinta.
//
// Simplificación de VS respecto al GDD: el GDD asume que el jugador llega al boss con
// los ecos que ya generó en las salas 1-2 de la run — pero EchoManager.ClearAllEchos()
// se llama en cada transición de sala (por diseño, cada sala es su propio intento
// autocontenido), así que los ecos NO persisten entre salas en esta implementación.
// El boss genera sus propios ecos dentro de la sala como cualquier otra — el ADN del
// puzzle (leer el timing del oscilador + coordinar 3 cuerpos) es el mismo.
public class BossController : MonoBehaviour
{
    [SerializeField] private MirrorPanel[] _panels;
    [SerializeField] private BossCenterTrigger _centerTrigger;
    [SerializeField] private float _requiredHoldTime = 1f;

    private float _holdTimer;
    private bool _defeated;

    private void Awake() => Services.Register(this);

    private void Update()
    {
        if (_defeated) return;

        if (AllPanelsActive && _centerTrigger != null && _centerTrigger.PlayerInCenter)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= _requiredHoldTime)
            {
                _defeated = true;
                if (Services.TryGet<RoomAssembler>(out var assembler))
                    assembler.OnBossDefeated();
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    public bool AllPanelsActive
    {
        get
        {
            foreach (var p in _panels)
                if (p != null && !p.IsActive) return false;
            return true;
        }
    }

    public float HoldProgress => Mathf.Clamp01(_holdTimer / _requiredHoldTime);

    public string DebugPanelStates()
    {
        if (_panels == null) return "panels=NULL";
        var parts = new string[_panels.Length];
        for (int i = 0; i < _panels.Length; i++)
            parts[i] = _panels[i] == null ? $"[{i}]=NULL" : $"[{i}] {_panels[i].DebugState}";
        return string.Join(" | ", parts);
    }
}
