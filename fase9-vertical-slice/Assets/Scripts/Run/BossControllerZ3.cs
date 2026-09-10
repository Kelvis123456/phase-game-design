using UnityEngine;

// GDD §8.3 Boss Z3 "El Abismo" (Zona 3) — el suelo se desintegra, los ecos deben
// activar palancas en el orden correcto para reconstruirlo antes de que el jugador
// pise (DisintegratingFloor, ventana de 2s [VS] por activación). Mismo patrón
// universal de boss que Z1/Z2: cada fase revela la siguiente al resolverse.
//
// A diferencia de BossControllerZ2 (que sí revisa CollapsingPlatform.IsStable en el
// momento de la meta), acá no hace falta: si el piso no se reconstruyó a tiempo, el
// jugador cae al pozo de abajo (HazardSpike, mismo reset de siempre) y nunca llega al
// trigger de meta — llegar a la meta YA ES la prueba de que cruzó a tiempo.
public class BossControllerZ3 : MonoBehaviour
{
    private enum Phase { Abismo1, Abismo2, Abismo3, Defeated }

    [SerializeField] private GameObject _phase2Reveal; // Lv2 + DF2 + Goal2
    [SerializeField] private GameObject _phase3Reveal; // Lv3 + DF3 + GoalFinal
    [SerializeField] private BossCenterTrigger _goal1;
    [SerializeField] private BossCenterTrigger _goal2;
    [SerializeField] private BossCenterTrigger _goalFinal;

    private Phase _phase = Phase.Abismo1;

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
            case Phase.Abismo1:
                if (_goal1.PlayerInCenter)
                {
                    if (_phase2Reveal != null) _phase2Reveal.SetActive(true);
                    _phase = Phase.Abismo2;
                }
                break;

            case Phase.Abismo2:
                if (_goal2.PlayerInCenter)
                {
                    if (_phase3Reveal != null) _phase3Reveal.SetActive(true);
                    _phase = Phase.Abismo3;
                }
                break;

            case Phase.Abismo3:
                if (_goalFinal.PlayerInCenter)
                {
                    _phase = Phase.Defeated;
                    if (Services.TryGet<RoomAssembler>(out var assembler))
                        assembler.OnBossDefeated();
                }
                break;
        }
    }
}
