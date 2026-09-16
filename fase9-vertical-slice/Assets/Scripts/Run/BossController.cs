using UnityEngine;

// GDD §8.2 Boss 1 "El Espejo Fragmentado" (Zona 1) — las 3 fases completas.
//
// Fase 1 "Primeros Reflejos": 3 paneles (E1/E2/E3), 2 ecos + el jugador.
// Fase 2 "Multiplicación": se revelan E4/E5 (antes ocultos); E4 tiene un contrapeso
// sobre E2 (ver MirrorCounterweightLink) — hace falta un tercer cuerpo (el eco de la
// Sala 3) para volver a alinear E2 después del reset.
// Fase 3 "La Convergencia": el Trigger Final se revela en el centro. El jugador debe
// pararse en el tile central mientras los 5 paneles siguen activos a la vez,
// sostenido _requiredHoldTime segundos seguidos — el "movimiento inverso" del tagline:
// el jugador llega primero y espera que sus ecos hagan el resto.
//
// Simplificación de VS respecto al GDD: el GDD asume que el jugador llega al boss con
// los ecos que ya generó en las salas 1-2 de la run — pero EchoManager.ClearAllEchos()
// se llama en cada transición de sala (por diseño, cada sala es su propio intento
// autocontenido), así que los ecos NO persisten entre salas en esta implementación.
// El boss genera sus propios ecos dentro de la sala como cualquier otra — el ADN del
// puzzle (leer el timing del oscilador + coordinar varios cuerpos) es el mismo.
//
// Regla explícita del GDD (§8.1): ningún boss tiene fail-state — el jugador siempre
// puede alejarse a medio intento sin quedar en un estado inconsistente. Nada aquí
// bloquea al jugador de forma permanente: los paneles se resetean solos vía el
// contrapeso o el oscilador, nunca por una acción irreversible.
public class BossController : MonoBehaviour
{
    private enum Phase { Reflejos, Multiplicacion, Convergencia, Defeated }

    [SerializeField] private MirrorPanel[] _phase1Panels; // E1, E2, E3
    [SerializeField] private MirrorPanel[] _phase2Panels; // E4, E5 — empiezan ocultos
    [SerializeField] private BossCenterTrigger _centerTrigger; // "Trigger Final": oculto hasta Fase 3
    [SerializeField] private float _phase1TransitionDelay = 2f; // Fase 1 -> 2
    [SerializeField] private float _phase2TransitionDelay = 1f; // Fase 2 -> 3
    [SerializeField] private float _requiredHoldTime = 1f;
    [SerializeField] private AudioClip _defeatSfx;

    private Phase _phase = Phase.Reflejos;
    private float _transitionTimer;
    private float _holdTimer;

    private void Awake()
    {
        Services.Register(this);

        foreach (var p in _phase2Panels)
            if (p != null) p.gameObject.SetActive(false);
        if (_centerTrigger != null) _centerTrigger.gameObject.SetActive(false);
    }

    private void Update()
    {
        switch (_phase)
        {
            case Phase.Reflejos: UpdateReflejos(); break;
            case Phase.Multiplicacion: UpdateMultiplicacion(); break;
            case Phase.Convergencia: UpdateConvergencia(); break;
        }
    }

    private static bool AllActive(MirrorPanel[] panels)
    {
        foreach (var p in panels)
            if (p != null && !p.IsActive) return false;
        return true;
    }

    private void UpdateReflejos()
    {
        if (!AllActive(_phase1Panels)) { _transitionTimer = 0f; return; }

        _transitionTimer += Time.deltaTime;
        if (_transitionTimer < _phase1TransitionDelay) return;

        foreach (var p in _phase2Panels)
            if (p != null) p.gameObject.SetActive(true);
        _phase = Phase.Multiplicacion;
        _transitionTimer = 0f;
    }

    private void UpdateMultiplicacion()
    {
        if (!AllPanelsActive) { _transitionTimer = 0f; return; }

        _transitionTimer += Time.deltaTime;
        if (_transitionTimer < _phase2TransitionDelay) return;

        if (_centerTrigger != null) _centerTrigger.gameObject.SetActive(true);
        _phase = Phase.Convergencia;
        _transitionTimer = 0f;
    }

    private void UpdateConvergencia()
    {
        if (AllPanelsActive && _centerTrigger != null && _centerTrigger.PlayerInCenter)
        {
            _holdTimer += Time.deltaTime;
            if (_holdTimer >= _requiredHoldTime)
            {
                _phase = Phase.Defeated;
                if (Services.TryGet<AudioManager>(out var audio)) audio.PlaySfx(_defeatSfx);
                if (Services.TryGet<RoomAssembler>(out var assembler))
                    assembler.OnBossDefeated();
            }
        }
        else
        {
            _holdTimer = 0f;
        }
    }

    public bool AllPanelsActive => AllActive(_phase1Panels) && AllActive(_phase2Panels);

    public float HoldProgress => _phase == Phase.Convergencia ? Mathf.Clamp01(_holdTimer / _requiredHoldTime) : 0f;

    public string DebugPanelStates()
    {
        var all = new System.Collections.Generic.List<MirrorPanel>(_phase1Panels);
        all.AddRange(_phase2Panels);
        var parts = new string[all.Count + 1];
        parts[0] = $"phase={_phase}";
        for (int i = 0; i < all.Count; i++)
            parts[i + 1] = all[i] == null ? $"[{i}]=NULL" : $"[{i}] {all[i].DebugState}";
        return string.Join(" | ", parts);
    }
}
