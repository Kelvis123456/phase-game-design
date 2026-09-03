using System;
using System.Collections.Generic;

// Estructura de datos exacta de la Fase 4 GDD §15.2 (guardado local, sin sync de nube todavía —
// esa decisión queda diferida a Fase 10 M5 según el propio plan de producción).
[Serializable]
public class SaveData
{
    public string version = "1.1";
    public string playerId;
    public string createdAt;
    public string lastSyncedAt;

    public MetaProgression metaProgression = new MetaProgression();
    public Achievements achievements = new Achievements();
    public AccessibilityPrefs accessibilityPrefs = new AccessibilityPrefs();
    public List<RunHistoryEntry> runHistory = new List<RunHistoryEntry>();

    [Serializable]
    public class MetaProgression
    {
        public int fragmentsTotal; // Phase Crystals ganados históricamente
        public int fragmentsSpent;
        public List<string> skillTreeNodes = new List<string>();
        public int highestRunScore;
        public int totalRunsCompleted;
        public int totalRunsAttempted;
        public int phaseCrystalBalance;
    }

    [Serializable]
    public class Achievements
    {
        public List<string> unlocked = new List<string>();
    }

    [Serializable]
    public class AccessibilityPrefs
    {
        public string colorblindMode = "normal";
        public float btEchoSpeed = 1.0f;
        public float btChargeTime = 0.15f;
        public bool soundEnabled = true;
    }

    [Serializable]
    public class RunHistoryEntry
    {
        public string runId;
        public int seed;
        public string date;
        public float durationSeconds;
        public int roomsCleared;
        public bool bossDefeated;
        public int score;
        public int fragmentsEarned;
    }
}
