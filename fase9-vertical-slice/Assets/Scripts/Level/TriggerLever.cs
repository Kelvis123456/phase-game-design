using System.Collections;
using UnityEngine;

// Fase 8 §17.2.3: la interfaz entre ecos cinemáticos y la física del mundo. Un eco no
// empuja objetos con física — al pasar sobre la palanca (jugador O eco), el objeto
// vinculado cambia de estado directamente. 100% determinista, sin física real.
[RequireComponent(typeof(Collider2D))]
public class TriggerLever : MonoBehaviour
{
    [SerializeField] private DoorGate _linkedDoor;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Sprite _spriteOff;
    [SerializeField] private Sprite _spriteOn;

    // GDD §7.3 R05 "Trigger Anticipado": zona de detección extra, solo para ecos, que
    // activa la palanca ~0.3s antes de que el eco la toque de verdad — un margen de
    // seguridad para puzzles de timing ajustado. _realOverlap sigue siendo la fuente de
    // verdad normal (jugador o eco tocando el collider real); _earlyOverlap es una
    // segunda fuente independiente que solo existe cuando R05 está activo en la run.
    // Combinadas con OR: cualquiera de las dos mantiene la palanca activa.
    private static readonly Vector2 EarlyDetectSize = new Vector2(2.2f, 2.2f);

    // GDD §7.3 R10 "Revelación": color de pulso al entrar a la sala — pista visual para
    // un jugador atorado, no cambia ninguna regla de simulación.
    private static readonly Color RevealColor = new Color(1f, 0.95f, 0.5f, 1f);

    public bool IsActive { get; private set; }

    private bool _realOverlap;
    private bool _earlyOverlap;
    private Coroutine _revealRoutine;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        bool wantEarly = Services.TryGet<RunManager>(out var run) && run.ActiveUpgrades.triggerAnticipationEnabled;
        bool newEarly = false;
        if (wantEarly)
        {
            int echoLayer = LayerMask.NameToLayer("Echo");
            newEarly = Physics2D.OverlapBox(transform.position, EarlyDetectSize, 0f, 1 << echoLayer) != null;
        }
        if (newEarly == _earlyOverlap) return;
        _earlyOverlap = newEarly;
        Recompute();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerOrEcho(other)) return;
        _realOverlap = true;
        Recompute();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayerOrEcho(other)) return;
        _realOverlap = false;
        Recompute();
    }

    private static bool IsPlayerOrEcho(Collider2D other)
    {
        int layer = other.gameObject.layer;
        return layer == LayerMask.NameToLayer("Player") || layer == LayerMask.NameToLayer("Echo");
    }

    private void Recompute()
    {
        bool active = _realOverlap || _earlyOverlap;
        if (active == IsActive) return;

        IsActive = active;
        if (_sprite != null)
        {
            var next = active ? _spriteOn : _spriteOff;
            if (next != null) _sprite.sprite = next;
        }
        if (_linkedDoor) _linkedDoor.SetHeld(this, active);
    }

    // R10 Revelación: RoomAssembler.LoadNext llama esto en cada palanca de la sala
    // recién activada, si el upgrade está en la run.
    public void PlayRevealPulse(float seconds)
    {
        if (_sprite == null) return;
        if (_revealRoutine != null) StopCoroutine(_revealRoutine);
        _revealRoutine = StartCoroutine(RevealPulseRoutine(seconds));
    }

    private IEnumerator RevealPulseRoutine(float seconds)
    {
        var original = _sprite.color;
        _sprite.color = RevealColor;
        yield return new WaitForSeconds(seconds);
        _sprite.color = original;
        _revealRoutine = null;
    }
}
