using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// GDD §4.2 + §11.5.1 ("Logros" en la fila de botones del inicio). Chequea condiciones
// contra SaveData después de cada run y persiste lo que se desbloquee. Sin comportamiento
// de jugadores real todavía (esto es la Fase 10 vertical slice, no post-lanzamiento), así
// que el set es deliberadamente chico y 100% verificable — no una lista de 30+ inventada.
[DefaultExecutionOrder(-92)]
public class AchievementSystem : MonoBehaviour
{
    public static readonly List<Achievement> Table = new List<Achievement>
    {
        new Achievement("first_steps", "Primer Paso", "Completa el tutorial",
            s => s.metaProgression.tutorialCompleted),
        new Achievement("first_run", "Primera Run", "Completa tu primera run",
            s => s.metaProgression.totalRunsCompleted >= 1),
        new Achievement("mirror_hunter", "Cazador de Reflejos", "Derrota a un boss",
            s => s.runHistory.Any(r => r.bossDefeated)),
        new Achievement("golden_echo", "Eco Dorado", "Completa 50 runs (GDD §4.2 — desbloquea la skin Eco Dorado)",
            s => s.metaProgression.totalRunsCompleted >= 50),
        new Achievement("tree_builder", "Constructor", "Desbloquea 5 nodos del árbol de progresión",
            s => s.metaProgression.skillTreeNodes.Count >= 5),
        new Achievement("tree_master", "Maestro del Árbol", "Desbloquea 15 nodos del árbol de progresión",
            s => s.metaProgression.skillTreeNodes.Count >= 15),
        new Achievement("crystal_collector", "Coleccionista de Cristales", "Acumula 1000 Phase Crystals ganados en total",
            s => s.metaProgression.fragmentsTotal >= 1000),
    };

    public event System.Action<Achievement> OnAchievementUnlocked;

    private SaveSystem _save;

    private void Awake() => Services.Register(this);

    private void Start() => _save = Services.Get<SaveSystem>();

    public bool IsUnlocked(string id) => _save.Current.achievements.unlocked.Contains(id);

    // Llamado por RunManager al final de cada run (completada o no) — algunas
    // condiciones (ej. tutorial) pueden cumplirse sin terminar la run en sí.
    public void CheckAndUnlock()
    {
        foreach (var achievement in Table)
        {
            if (IsUnlocked(achievement.id)) continue;
            if (!achievement.condition(_save.Current)) continue;

            _save.Current.achievements.unlocked.Add(achievement.id);
            OnAchievementUnlocked?.Invoke(achievement);
            if (Services.TryGet<AudioManager>(out var audio)) audio.PlayAchievementUnlock();
        }
        _save.Save();
    }
}
