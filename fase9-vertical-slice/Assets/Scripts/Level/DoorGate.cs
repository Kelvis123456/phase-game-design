using System.Collections.Generic;
using UnityEngine;

// Puerta que requiere N palancas sostenidas SIMULTÁNEAMENTE para abrirse (GDD Sala
// Tutorial 1: "la puerta requiere que AMBAS palancas estén activadas al mismo tiempo").
// Esto es lo que hace que un eco sea necesario en vez de solo útil: el jugador no puede
// estar en dos palancas a la vez, así que necesita que su eco sostenga una mientras
// él sostiene la otra.
public class DoorGate : MonoBehaviour
{
    [SerializeField] private int _requiredCount = 1;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Sprite _spriteClosed;
    [SerializeField] private Sprite _spriteOpen;

    private readonly HashSet<TriggerLever> _holding = new HashSet<TriggerLever>();
    private Collider2D _blocker;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        _blocker = GetComponent<Collider2D>();
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
    }

    public void SetHeld(TriggerLever lever, bool held)
    {
        if (held) _holding.Add(lever);
        else _holding.Remove(lever);

        bool shouldBeOpen = _holding.Count >= _requiredCount;
        if (shouldBeOpen == IsOpen) return;

        IsOpen = shouldBeOpen;
        if (_blocker) _blocker.enabled = !IsOpen;
        if (_sprite != null)
        {
            var next = IsOpen ? _spriteOpen : _spriteClosed;
            if (next != null) _sprite.sprite = next;
        }
    }
}
