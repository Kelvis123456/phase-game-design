using System;
using UnityEngine;

// VS simplificado: 1 hit = muerte. HP completo en Fase 10.
public class PlayerStats : MonoBehaviour
{
    public event Action OnDeath;

    private bool _dead;

    private void Awake() => Services.Register(this);

    public void TakeDamage()
    {
        if (_dead) return;
        _dead = true;
        OnDeath?.Invoke();
    }

    public void ResetStats() => _dead = false;
}
