using UnityEngine;

// Mata al jugador al contacto. Poner en el Trigger del pincho.
// Layer del objeto: Hazard. El jugador tiene Layer: Player.
[RequireComponent(typeof(Collider2D))]
public class HazardSpike : MonoBehaviour
{
    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (Services.TryGet<PlayerStats>(out var stats))
            stats.TakeDamage();
    }
}
