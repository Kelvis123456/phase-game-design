using UnityEngine;

// Trigger de salida de sala. Si _requiredDoor está asignado, solo deja pasar cuando
// está abierto (fuerza al jugador a resolver el puzzle de la sala, no solo cruzar).
public class RoomExit : MonoBehaviour
{
    [SerializeField] private DoorGate _requiredDoor;
    private bool _triggered;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        if (_requiredDoor != null && !_requiredDoor.IsOpen) return;

        _triggered = true;
        if (Services.TryGet<RoomAssembler>(out var assembler))
            assembler.OnRoomCleared();
    }

    // El siguiente RentFromPool de sala reactiva el trigger.
    public void ResetTrigger() => _triggered = false;
}
