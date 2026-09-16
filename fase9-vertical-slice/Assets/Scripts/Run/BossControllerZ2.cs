using UnityEngine;

// GDD §8.3 Boss Z2 "La Fractura" (Zona 2) — plataformas que colapsan, el jugador
// reconstruye el camino con ecos. 3 fases, mismo patrón universal de boss que
// BossController (Z1): cada fase revela la siguiente al resolverse, nada es
// permanente (sin fail-state, GDD §8.1).
//
// Fase 1: 1 eco sostiene L1 (CollapsingPlatform 1) para que el jugador cruce.
// Fase 2: cadena de dependencia REAL, no solo narrativa — L2 está físicamente del
// otro lado de CP1, así que un eco debe cruzar CP1 (con otro eco sosteniendo L1)
// antes de poder sostener L2 para el jugador.
// Fase 3: con las 3 plataformas estables a la vez (mínimo 3 ecos, GDD §8.3), el
// jugador llega a la meta final.
public class BossControllerZ2 : MonoBehaviour
{
    private enum Phase { Fractura1, Fractura2, Fractura3, Defeated }

    [SerializeField] private CollapsingPlatform _platform1, _platform2, _platform3;
    [SerializeField] private GameObject _phase2Reveal; // L2 + CP2 + Goal2, ocultos hasta fase 2
    [SerializeField] private GameObject _phase3Reveal; // L3 + CP3 + GoalFinal, ocultos hasta fase 3
    [SerializeField] private BossCenterTrigger _goal1;
    [SerializeField] private BossCenterTrigger _goal2;
    [SerializeField] private BossCenterTrigger _goalFinal;

    private Phase _phase = Phase.Fractura1;

    private void Awake()
    {
        Services.Register(this);
        if (_phase2Reveal != null) _phase2Reveal.SetActive(false);
        if (_phase3Reveal != null) _phase3Reveal.SetActive(false);
    }

    private void Update()
    {
        switch (_phase)
        {
            case Phase.Fractura1:
                if (_platform1.IsStable && _goal1.PlayerInCenter)
                {
                    if (_phase2Reveal != null) _phase2Reveal.SetActive(true);
                    _phase = Phase.Fractura2;
                }
                break;

            case Phase.Fractura2:
                if (_platform1.IsStable && _platform2.IsStable && _goal2.PlayerInCenter)
                {
                    if (_phase3Reveal != null) _phase3Reveal.SetActive(true);
                    _phase = Phase.Fractura3;
                }
                break;

            case Phase.Fractura3:
                if (_platform1.IsStable && _platform2.IsStable && _platform3.IsStable && _goalFinal.PlayerInCenter)
                {
                    _phase = Phase.Defeated;
                    if (Services.TryGet<RoomAssembler>(out var assembler))
                        assembler.OnBossDefeated();
                }
                break;
        }
    }
}
