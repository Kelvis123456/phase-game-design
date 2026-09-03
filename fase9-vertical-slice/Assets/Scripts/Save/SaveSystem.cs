using System;
using System.IO;
using UnityEngine;

// Fase 10 M1.3: guardado JSON local. La sincronización de nube (ICloudSync) queda
// deliberadamente diferida a M5 — no bloquear el núcleo de producción con una decisión
// de backend que todavía no se necesita.
[DefaultExecutionOrder(-95)]
public class SaveSystem : MonoBehaviour
{
    private const string FileName = "phase_save.json";
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public SaveData Current { get; private set; }

    private void Awake()
    {
        Services.Register(this);
        Load();
    }

    public void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                Current = JsonUtility.FromJson<SaveData>(json);
                if (Current == null) Current = NewSave();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Save file corrupt, starting fresh: {e.Message}");
                Current = NewSave();
            }
        }
        else
        {
            Current = NewSave();
        }
    }

    public void Save()
    {
        Current.lastSyncedAt = DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(Current, true);
        File.WriteAllText(FilePath, json);
    }

    private SaveData NewSave()
    {
        var data = new SaveData
        {
            playerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow.ToString("o"),
        };
        return data;
    }

    public void DeleteAll()
    {
        if (File.Exists(FilePath)) File.Delete(FilePath);
        Current = NewSave();
    }
}
