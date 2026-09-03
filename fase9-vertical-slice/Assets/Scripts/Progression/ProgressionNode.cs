using System;

// Un nodo del árbol de meta-progresión (GDD §4.1). Dato puro, no MonoBehaviour —
// la tabla completa vive en ProgressionSystem.NodeTable.
[Serializable]
public class ProgressionNode
{
    public string id;
    public string branch; // "A" Capacidad de Ecos, "B" Modificadores, "C" Cosméticos, "D" Calidad de Vida
    public string displayName;
    public int cost;
    public string requiresNodeId; // null/"" si no requiere nodo previo

    public ProgressionNode(string id, string branch, string displayName, int cost, string requiresNodeId = null)
    {
        this.id = id;
        this.branch = branch;
        this.displayName = displayName;
        this.cost = cost;
        this.requiresNodeId = requiresNodeId;
    }
}
