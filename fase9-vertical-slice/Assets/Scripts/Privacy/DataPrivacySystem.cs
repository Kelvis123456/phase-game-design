using System;
using System.IO;
using UnityEngine;

// GDD §15.4 — los 2 derechos GDPR que no dependen de un backend de nube que todavía no
// existe (M5.5 diferido): exportar datos y borrar cuenta. Sin nube real, "borrar cuenta"
// acá borra el save LOCAL — lo único que existe —, mismo alcance que ya cubría
// SaveSystem.DeleteAll(), esto solo le pone un flujo real con confirmación encima. La
// pantalla de consentimiento (GDD §15.4, "antes de cualquier sync de nube") se difiere
// junto con esa nube — no tiene sentido pedir consentimiento para algo que no pasa todavía.
public class DataPrivacySystem : MonoBehaviour
{
    private SaveSystem _save;

    private void Awake() => Services.Register(this);
    private void Start() => _save = Services.Get<SaveSystem>();

    // GDD §15.4: "genera un JSON descargable". En desktop, el lugar obvio y accesible
    // para el jugador es el Escritorio — con timestamp para no pisar exportaciones previas.
    public string ExportData()
    {
        string json = JsonUtility.ToJson(_save.Current, true);
        string fileName = $"phase_datos_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), fileName);
        File.WriteAllText(path, json);
        return path;
    }

    public void DeleteAccount() => _save.DeleteAll();
}
