using System;

// GDD §4.2 + §15 nota de producción: la lista completa de 30+ logros depende de datos
// reales de comportamiento de jugadores ("Fin del VS"), así que esto no pretende ser esa
// lista — es la arquitectura real (definición + condición + desbloqueo + persistencia)
// más un set inicial de logros genuinamente verificables contra datos que el juego ya
// trackea (SaveData.metaProgression / runHistory), incluidos los 2 de la tabla de skins
// del GDD §4.2 que sí son chequeables con el estado actual (Eco Dorado; los otros 2 de
// esa tabla necesitan sistemas que todavía no existen — ver AchievementSystem.cs).
[Serializable]
public class Achievement
{
    public string id;
    public string displayName;
    public string description;
    public Func<SaveData, bool> condition;

    public Achievement(string id, string displayName, string description, Func<SaveData, bool> condition)
    {
        this.id = id;
        this.displayName = displayName;
        this.description = description;
        this.condition = condition;
    }
}
