using UnityEngine;

// GDD Sala Tutorial 2 "El Momento No Planeado" (y Boss 1 Fase 2 "Multiplicación"):
// activar la palanca A fuerza el cierre de la puerta que B sostiene, sin importar si
// B sigue pisando su propia palanca. El jugador resuelve A primero (su plan "obvio"),
// eso rompe lo que ya había logrado con B, y necesita otro cuerpo (su eco) cubriendo B
// en el momento correcto para que ambas queden resueltas a la vez.
public class CounterweightLink : MonoBehaviour
{
    [SerializeField] private TriggerLever _triggerLever;
    [SerializeField] private DoorGate _affectedDoor;

    private bool _wasActive;

    private void Update()
    {
        if (_triggerLever == null || _affectedDoor == null) return;

        bool isActive = _triggerLever.IsActive;
        if (isActive && !_wasActive)
            _affectedDoor.ForceClose();
        _wasActive = isActive;
    }
}
