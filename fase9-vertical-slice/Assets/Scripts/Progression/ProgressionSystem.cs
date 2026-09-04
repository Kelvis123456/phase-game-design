using System;
using System.Collections.Generic;
using UnityEngine;

// Fase 10 M1.4 + M3.1 + M2.5: economía de Phase Crystals y árbol de meta-progresión
// (GDD §4.1, §9.1) — las 29 nodos reales de las 4 ramas (A-D), no una muestra.
// Efecto de compra real hoy: Rama A (slots de eco, vía EchoManager.UnlockSlot()).
// El resto (B/C/D) son desbloqueables/comprables — CanUnlock/TryUnlock y el costo en
// Phase Crystals son reales — pero su efecto de GAMEPLAY (niebla de sala, skins de eco,
// etc) todavía no está conectado a ningún sistema; eso es trabajo de nivel de sistema
// nuevo por nodo, no entrada de datos. Ver RunUpgrade.cs para el mismo patrón aplicado
// a los upgrades de run (ahí sí varios ya están conectados de verdad).
[DefaultExecutionOrder(-94)]
public class ProgressionSystem : MonoBehaviour
{
    public static readonly List<ProgressionNode> NodeTable = new List<ProgressionNode>
    {
        // Rama A — Capacidad de Ecos (completa, GDD §4.1)
        new ProgressionNode("A1", "A", "Eco Base (2 slots)", 0),
        new ProgressionNode("A2", "A", "Tercer Espejo (3 slots)", 150, "A1"),
        new ProgressionNode("A3", "A", "Resonancia Cuádruple (4 slots)", 300, "A2"),
        new ProgressionNode("A4", "A", "Quinteto Temporal (5 slots)", 500, "A3"),
        new ProgressionNode("A5", "A", "Persistencia de Eco", 200, "A2"),
        new ProgressionNode("A6", "A", "Memoria de Ruta", 100, "A1"),

        // Rama B — Modificadores de Run (GDD §4.1, completa: 8 nodos)
        new ProgressionNode("B1", "B", "Run Limpia", 0),
        new ProgressionNode("B2", "B", "Modo Espejo", 80),
        new ProgressionNode("B3", "B", "Eco Acelerado", 80),
        new ProgressionNode("B4", "B", "Niebla de Sala", 120),
        new ProgressionNode("B5", "B", "Doble Bullet", 100),
        new ProgressionNode("B6", "B", "Sin Bullet", 150),
        new ProgressionNode("B7", "B", "Eco Fantasma", 120),
        new ProgressionNode("B8", "B", "Sala Única", 60),

        // Rama C — Cosméticos de Eco (GDD §4.1, completa: 10 nodos)
        new ProgressionNode("C1", "C", "Eco Base", 0),
        new ProgressionNode("C2", "C", "Neón Pulso", 80),
        new ProgressionNode("C3", "C", "Sombra Distorsionada", 80),
        new ProgressionNode("C4", "C", "Partículas de Cristal", 120),
        new ProgressionNode("C5", "C", "Espejo Puro", 100),
        new ProgressionNode("C6", "C", "Fantasma Retro", 80),
        new ProgressionNode("C7", "C", "Plasma Temporal", 150),
        new ProgressionNode("C8", "C", "Espectro de Luz", 180),
        new ProgressionNode("C9", "C", "Vacío", 60),
        new ProgressionNode("C10", "C", "Arco Iris Cuántico", 200),

        // Rama D — Calidad de Vida (GDD §4.1, completa: 5 nodos)
        new ProgressionNode("D1", "D", "Historial de Runs", 50),
        new ProgressionNode("D2", "D", "Modo Sin Anuncios", 500),
        new ProgressionNode("D3", "D", "Salto de Tutorial", 30),
        new ProgressionNode("D4", "D", "Animaciones Rápidas", 40),
        new ProgressionNode("D5", "D", "Selector de Semilla", 150),
    };

    public event Action<int> OnBalanceChanged;
    public event Action<string> OnNodeUnlocked;

    private SaveSystem _save;

    private void Awake()
    {
        Services.Register(this);
    }

    private void Start()
    {
        _save = Services.Get<SaveSystem>();
        // A1 siempre desbloqueado — es el estado inicial gratuito (GDD §4.1: "Inicio, gratis").
        if (!IsNodeUnlocked("A1")) UnlockFree("A1");
    }

    public int PhaseCrystalBalance => _save.Current.metaProgression.phaseCrystalBalance;

    // Fuentes de la GDD §9.1 — tabla completa de cómo se ganan Phase Crystals.
    public enum EarnSource { RunZone1, RunZone2, RunZone3, PerfectRunBonus, PersonalRecordBonus, DailyBonus, AdWatch }

    public static int AmountFor(EarnSource source) => source switch
    {
        EarnSource.RunZone1 => 30,
        EarnSource.RunZone2 => 45,
        EarnSource.RunZone3 => 60,
        EarnSource.PerfectRunBonus => 20,
        EarnSource.PersonalRecordBonus => 15,
        EarnSource.DailyBonus => 25,
        EarnSource.AdWatch => 10,
        _ => 0,
    };

    public void EarnCrystals(EarnSource source)
    {
        EarnFlat(AmountFor(source));
    }

    // Para bonos que no vienen de una EarnSource tabulada (ej. el upgrade de run "PC Bonus").
    public void EarnFlat(int amount)
    {
        _save.Current.metaProgression.phaseCrystalBalance += amount;
        _save.Current.metaProgression.fragmentsTotal += amount;
        OnBalanceChanged?.Invoke(PhaseCrystalBalance);
        _save.Save();
    }

    public bool IsNodeUnlocked(string nodeId) => _save.Current.metaProgression.skillTreeNodes.Contains(nodeId);

    public bool CanUnlock(string nodeId)
    {
        var node = NodeTable.Find(n => n.id == nodeId);
        if (node == null) return false;
        if (IsNodeUnlocked(nodeId)) return false;
        if (!string.IsNullOrEmpty(node.requiresNodeId) && !IsNodeUnlocked(node.requiresNodeId)) return false;
        return PhaseCrystalBalance >= node.cost;
    }

    public bool TryUnlock(string nodeId)
    {
        if (!CanUnlock(nodeId)) return false;
        var node = NodeTable.Find(n => n.id == nodeId);
        _save.Current.metaProgression.phaseCrystalBalance -= node.cost;
        _save.Current.metaProgression.fragmentsSpent += node.cost;
        _save.Current.metaProgression.skillTreeNodes.Add(nodeId);
        OnBalanceChanged?.Invoke(PhaseCrystalBalance);
        OnNodeUnlocked?.Invoke(nodeId);
        _save.Save();

        if (Services.TryGet<EchoManager>(out var echoManager) && node.branch == "A" && node.id != "A1")
            echoManager.UnlockSlot();

        return true;
    }

    private void UnlockFree(string nodeId)
    {
        _save.Current.metaProgression.skillTreeNodes.Add(nodeId);
        _save.Save();
    }
}
