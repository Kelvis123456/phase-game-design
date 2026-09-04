using UnityEngine;

// GDD §8.2 Fase 3: el tile central (1 tile, a propósito pequeño) donde el jugador debe
// pararse mientras todos los paneles están activos. Solo reacciona al PLAYER real —
// un eco parado ahí (posicional, sin intención) no cuenta, tiene que ser el jugador.
[RequireComponent(typeof(Collider2D))]
public class BossCenterTrigger : MonoBehaviour
{
    public bool PlayerInCenter { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) PlayerInCenter = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player")) PlayerInCenter = false;
    }
}
