using System;

// GDD §4.1 Rama B "Modificadores de Run" — a diferencia de los upgrades R01-R12 (elegidos
// al azar entre salas DENTRO de una run), estos son permanentes en el árbol de PC y el
// jugador elige UNO activo para la próxima run completa (GDD: "la sala se refleja...",
// "los ecos corren al...", etc. — todos describen la run entera, no una sala puntual).
public class BranchBEffects
{
    public bool mirrorRoom;                      // B2 Modo Espejo
    public float echoSpeedMultiplier = 1f;       // B3 Eco Acelerado
    public bool fogOfWar;                        // B4 Niebla de Sala
    public bool doubleBulletTime;                // B5 Doble Bullet
    public bool bulletTimeDisabled;              // B6 Sin Bullet
    public float pcBonusMultiplier = 1f;         // B6 Sin Bullet (+50% PC)
    public bool echoesInvisibleOutsideBulletTime; // B7 Eco Fantasma
    public int roomCountOverride = -1;           // B8 Sala Única (-1 = default de RunManager)
}

public static class BranchBTable
{
    public static readonly System.Collections.Generic.Dictionary<string, Action<BranchBEffects>> Apply =
        new System.Collections.Generic.Dictionary<string, Action<BranchBEffects>>
    {
        { "B1", e => { } }, // Run Limpia — sin modificador, default gratuito
        { "B2", e => e.mirrorRoom = true },
        { "B3", e => e.echoSpeedMultiplier = 1.3f },
        { "B4", e => e.fogOfWar = true },
        { "B5", e => e.doubleBulletTime = true },
        { "B6", e => { e.bulletTimeDisabled = true; e.pcBonusMultiplier = 1.5f; } },
        { "B7", e => e.echoesInvisibleOutsideBulletTime = true },
        { "B8", e => e.roomCountOverride = 2 },
    };
}
