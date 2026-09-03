using UnityEngine;

// Fase 8 §17.2.3: la interfaz entre ecos cinemáticos y la física del mundo. Un eco no
// empuja objetos con física — al pasar sobre la palanca (jugador O eco), el objeto
// vinculado cambia de estado directamente. 100% determinista, sin física real.
[RequireComponent(typeof(Collider2D))]
public class TriggerLever : MonoBehaviour
{
    [SerializeField] private DoorGate _linkedDoor;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Color _colorOff = new Color(0.23f, 0.29f, 0.42f, 1f);
    [SerializeField] private Color _colorOn = new Color(0.31f, 1f, 0.81f, 1f);

    public bool IsActive { get; private set; }

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = other.gameObject.layer == LayerMask.NameToLayer("Player");
        bool isEcho = other.gameObject.layer == LayerMask.NameToLayer("Echo");
        if (!isPlayer && !isEcho) return;

        SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        bool isPlayer = other.gameObject.layer == LayerMask.NameToLayer("Player");
        bool isEcho = other.gameObject.layer == LayerMask.NameToLayer("Echo");
        if (!isPlayer && !isEcho) return;

        SetActive(false);
    }

    private void SetActive(bool active)
    {
        IsActive = active;
        if (_sprite) _sprite.color = active ? _colorOn : _colorOff;
        if (_linkedDoor) _linkedDoor.SetHeld(this, active);
    }
}
