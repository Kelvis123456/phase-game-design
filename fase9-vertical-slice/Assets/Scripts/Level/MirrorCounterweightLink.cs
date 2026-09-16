using UnityEngine;

// GDD §8.2 Boss 1 "El Espejo Fragmentado" (Zona 1), Fase 2 "Multiplicación": el panel
// E4 tiene un mecanismo de contrapeso con E2 — si el jugador activa E4, E2 se
// desactiva (el jugador no puede tener ambos activos por su cuenta). Resolver la sala
// exige que un tercer cuerpo (el eco de la Sala 3) vuelva a activar E2 en la ventana
// correcta del oscilador después del reset. Misma idea que CounterweightLink
// (palanca→puerta), aplicada panel→panel.
public class MirrorCounterweightLink : MonoBehaviour
{
    [SerializeField] private MirrorPanel _triggerPanel; // E4
    [SerializeField] private MirrorPanel _affectedPanel; // E2

    private bool _wasActive;

    private void Update()
    {
        if (_triggerPanel == null || _affectedPanel == null) return;

        bool isActive = _triggerPanel.IsActive;
        if (isActive && !_wasActive)
            _affectedPanel.ForceDeactivate();
        _wasActive = isActive;
    }
}
