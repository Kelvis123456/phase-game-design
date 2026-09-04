using System;

// Upgrades de run (GDD §7.3) — modificadores TEMPORALES que duran una sola run, distintos
// de los nodos permanentes del árbol de progresión. Set reducido de la tabla completa (R01-R12)
// para probar el patrón real: presentar 2 opciones entre salas, aplicar el efecto elegido.
[Serializable]
public class RunUpgrade
{
    public string id;
    public string displayName;
    public string description;
    public Action<RunUpgradeEffects> apply;

    public RunUpgrade(string id, string displayName, string description, Action<RunUpgradeEffects> apply)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.apply = apply;
    }
}

// Efectos activos de la run actual — se resetean al iniciar una run nueva (RunManager.StartRun).
public class RunUpgradeEffects
{
    public float bulletTimeDeactivateBonus = 0f; // R03 Bullet Extendido
    public int pcBonusOnComplete = 0;             // R12 PC Bonus
    public float worldSlowMultiplier = 1f;        // R11 Mundo Lento
    public float echoSpeedMultiplier = 1f;        // R01 Eco Veloz / R02 Eco Lento
    public float loopDurationMultiplier = 1f;     // R04 Doble Loop
}
