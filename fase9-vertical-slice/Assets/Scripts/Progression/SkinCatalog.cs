using System.Collections.Generic;
using UnityEngine;

// GDD §4.1 Rama C (cosméticos de eco) — traduce la descripción visual de cada skin a lo
// que EchoPlayer/EchoShader pueden expresar hoy (tinte, opacidad, luz, ciclo de color por
// loop). Simplificación de VS: varias skins prometen tratamiento único (partículas de
// cristal fragmentándose, pixel art 8-bit, reflejo especular metálico, distorsión de
// calor) que necesitarían shaders/arte nuevos — acá se aproximan con lo que ya existe, la
// misma prioridad de "la regla sobre el arte final" del resto del proyecto.
public static class SkinCatalog
{
    public class SkinVisual
    {
        public Color tint;
        public float opacityOverride = -1f; // -1 = usa el degradado normal por antigüedad de slot
        public bool emitsLight;             // C8 Espectro de Luz — Light2D real
        public bool cyclesColorPerLoop;     // C10 Arco Iris Cuántico — cambia de color cada loop
    }

    // "C1" no tiene entrada — Eco Base usa el color por slot de EchoManager.EchoColors sin
    // override, es el estado por defecto gratuito (GDD: "Silueta del jugador en azul
    // semitransparente", que en VS es el arcoíris por slot ya existente).
    public static readonly Dictionary<string, SkinVisual> Table = new Dictionary<string, SkinVisual>
    {
        { "C2", new SkinVisual { tint = new Color(1f, 0.15f, 0.85f, 1f) } },                    // Neón Pulso
        { "C3", new SkinVisual { tint = new Color(0.05f, 0.05f, 0.06f, 1f) } },                 // Sombra Distorsionada
        { "C4", new SkinVisual { tint = new Color(0.72f, 0.90f, 1f, 1f) } },                    // Partículas de Cristal
        { "C5", new SkinVisual { tint = new Color(0.86f, 0.87f, 0.92f, 1f) } },                 // Espejo Puro
        { "C6", new SkinVisual { tint = new Color(0.55f, 0.95f, 0.55f, 1f), opacityOverride = 0.5f } }, // Fantasma Retro
        { "C7", new SkinVisual { tint = new Color(0.45f, 0.35f, 1f, 1f) } },                    // Plasma Temporal
        { "C8", new SkinVisual { tint = Color.white, emitsLight = true } },                     // Espectro de Luz
        { "C9", new SkinVisual { tint = Color.black, opacityOverride = 1f } },                  // Vacío
        { "C10", new SkinVisual { tint = Color.white, cyclesColorPerLoop = true } },            // Arco Iris Cuántico
    };

    // Colores por los que rota C10 al completar cada loop — mismo set que EchoManager usa
    // por slot, reordenado para no coincidir con el color base del slot en el primer ciclo.
    public static readonly Color[] RainbowCycleColors =
    {
        new Color(0.976f, 0.451f, 0.086f, 1f),
        new Color(0.227f, 1.000f, 0.831f, 1f),
        new Color(0.925f, 0.286f, 0.600f, 1f),
        new Color(0.659f, 0.333f, 0.969f, 1f),
        new Color(0.133f, 0.773f, 0.369f, 1f),
    };

    public static SkinVisual Get(string skinNodeId) => Table.TryGetValue(skinNodeId, out var v) ? v : null;
}
