using UnityEngine;

// Fase 10 M2.4 (GDD §6.2 "Regla de Escalado por Zona"): variedad visual real por zona.
// RoomAssembler la lee al activar cada sala y tiñe el fondo de la cámara acorde —
// sin esto, cada zona jugaría idéntica pese a tener temas distintos en el GDD
// (Z1 Umbral, Z3 Abismo espacio-tiempo distorsionado negro y púrpura, etc).
public class RoomVisualTheme : MonoBehaviour
{
    public Color backgroundColor = new Color(0.04f, 0.05f, 0.08f);
}
