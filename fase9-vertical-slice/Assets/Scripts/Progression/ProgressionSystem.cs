using System;
using System.Collections.Generic;
using UnityEngine;

// Fase 10 M1.4 + M3.1: economía de Phase Crystals y árbol de meta-progresión (GDD §4.1, §9.1).
// Rama A (Capacidad de Ecos) está completa porque EchoManager depende de ella directamente.
// Ramas B/C/D llevan nodos representativos de muestra — añadir el resto de los 24 nodos es
// entrada de datos siguiendo este mismo patrón, no un problema de arquitectura nuevo.
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

        // Rama B — Modificadores de Run (muestra representativa)
        new ProgressionNode("B1", "B", "Run Limpia", 0),
        new ProgressionNode("B3", "B", "Eco Acelerado", 80),
        new ProgressionNode("B6", "B", "Sin Bullet", 150),

        // Rama C — Cosméticos de Eco (muestra representativa)
        new ProgressionNode("C1", "C", "Eco Base", 0),
        new ProgressionNode("C2", "C", "Neón Pulso", 80),

        // Rama D — Calidad de Vida (muestra representativa)
        new ProgressionNode("D1", "D", "Historial de Runs", 50),
        new ProgressionNode("D3", "D", "Salto de Tutorial", 30),
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
        int amount = AmountFor(source);
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
