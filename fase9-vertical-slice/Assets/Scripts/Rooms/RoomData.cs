using UnityEngine;

// Fase 8 §17.4.1 + Fase 10 M2.1: metadatos de una sala del pool. Cada sala real
// (construida por RoomBuilder) referencia una de estas para que el algoritmo de
// ensamblaje (RoomAssembler) pueda filtrarla y ordenarla dentro de una run.
public enum PrimaryMechanic { SYNC, TIMING, DEPENDENCY, FRUSTRATION, SOLO }
public enum DoorPosition { North, South, East, West }

[CreateAssetMenu(menuName = "PHASE/Room Data")]
public class RoomData : ScriptableObject
{
    public string roomId;
    public int zoneId;
    [Range(1, 10)] public int difficultyTier;
    public PrimaryMechanic mechanic;
    [Range(1, 5)] public int ecoCountRequired;
    public float estimatedDurationS;
    public bool hasAltSolution;
    public int introRunMin;
    public float weightBase = 1f;
    public DoorPosition entrada = DoorPosition.West;
    public DoorPosition salida = DoorPosition.East;

    // Nombre de la escena real que contiene la geometría de esta sala (Assets/Scenes/Rooms/).
    public string sceneName;
}
