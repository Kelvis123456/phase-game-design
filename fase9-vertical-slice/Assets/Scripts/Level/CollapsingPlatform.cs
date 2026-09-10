using System.Collections.Generic;
using UnityEngine;

// GDD §8.3 Boss 2 "La Fractura" (Zona 2): un segmento de piso sobre un pozo que solo es
// sólido mientras su(s) palanca(s) vinculada(s) estén sostenidas — igual que la cuenta
// de DoorGate, aplicada a un piso en vez de a un bloqueador. Sin lever sostenido, el
// collider se apaga y el jugador cae al pozo de abajo (mismo HazardSpike de siempre —
// no hay una animación de "colapso" real, es la misma simplificación determinista que
// ya usa TriggerLever/DoorGate: sin física real, solo estado).
public class CollapsingPlatform : MonoBehaviour
{
    [SerializeField] private int _requiredCount = 1;
    [SerializeField] private SpriteRenderer _sprite;
    [SerializeField] private Color _colorStable = new Color(0.6f, 0.75f, 0.95f, 1f);
    [SerializeField] private Color _colorCollapsed = new Color(0.3f, 0.15f, 0.15f, 0.4f);

    private readonly HashSet<TriggerLever> _holding = new HashSet<TriggerLever>();
    private Collider2D _collider;

    public bool IsStable { get; private set; }

    private void Awake()
    {
        _collider = GetComponent<Collider2D>();
        if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
        SetStable(false);
    }

    public void SetHeld(TriggerLever lever, bool held)
    {
        if (held) _holding.Add(lever);
        else _holding.Remove(lever);

        bool shouldBeStable = _holding.Count >= _requiredCount;
        if (shouldBeStable != IsStable) SetStable(shouldBeStable);
    }

    private void SetStable(bool stable)
    {
        IsStable = stable;
        if (_collider != null) _collider.enabled = stable;
        if (_sprite != null) _sprite.color = stable ? _colorStable : _colorCollapsed;
    }
}
