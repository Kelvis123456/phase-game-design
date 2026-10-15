using UnityEngine;
using UnityEngine.UI;

// GDD §15.4: "en primera instalación, antes de cualquier sync de nube, se presenta el
// resumen de datos recopilados con enlace a la Privacy Policy completa". No hay sync de
// nube real todavía (M5.5 diferido) ni una Privacy Policy publicada para enlazar — esto
// muestra igual la tabla real de datos del GDD en el primer arranque, que es la pantalla
// que el GDD describe, sin inventar una URL que no existe. Bloquea el menú principal
// hasta aceptar, igual que "antes de cualquier..." implica en el GDD.
public class ConsentScreenUI : MonoBehaviour
{
    private static readonly (string dato, string razon, string retencion)[] DataTable =
    {
        ("UUID de jugador (no vinculado a PII)", "Identificar el save", "Mientras la cuenta existe"),
        ("Estadísticas de runs (score, duración)", "Balanceo de contenido", "36 meses"),
        ("Timestamp de última sesión", "Notificaciones de reenganche", "12 meses"),
        ("Preferencias de accesibilidad", "Sincronización entre dispositivos", "Mientras la cuenta existe"),
        ("Crash reports (anonimizados)", "QA y estabilidad", "6 meses"),
    };

    private Canvas _canvas;
    private SaveSystem _save;

    private void Start()
    {
        _save = Services.Get<SaveSystem>();
        BuildUI();
        SetVisible(!_save.Current.dataConsentAccepted);
    }

    private void Update()
    {
        if (IsBlocking && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) OnAccept();
    }

    public bool IsBlocking => _canvas != null && _canvas.gameObject.activeSelf;

    private void SetVisible(bool visible) => _canvas.gameObject.SetActive(visible);

    private void BuildUI()
    {
        var canvasGO = new GameObject("ConsentScreenCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300; // por encima de todo — es lo primero que se ve, si aplica
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.04f, 0.99f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        AddLabel(canvasGO.transform, "Title", "TUS DATOS (GDD §15.4)", 26, -30f, 32f);
        AddLabel(canvasGO.transform, "Intro",
            "PHASE recopila solo lo necesario para que el juego funcione. No hay perfil publicitario ni venta de datos.",
            14, -76f, 40f);

        int row = 0;
        foreach (var entry in DataTable)
        {
            AddLabel(canvasGO.transform, $"Row_{row}", $"{entry.dato} — {entry.razon} — retención: {entry.retencion}", 13, -130f - row * 26f, 22f);
            row++;
        }

        AddLabel(canvasGO.transform, "NotCollected",
            "No se recopila: nombre real, email, ubicación, ni un ID de dispositivo vinculado a tu identidad.",
            13, -130f - row * 26f - 16f, 22f);

        var acceptGO = new GameObject("AcceptButton");
        acceptGO.transform.SetParent(canvasGO.transform, false);
        var acceptImg = acceptGO.AddComponent<Image>();
        acceptImg.color = new Color(0.86f, 0.91f, 0.96f, 0.95f);
        var acceptBtn = acceptGO.AddComponent<Button>();
        acceptBtn.onClick.AddListener(OnAccept);
        var acceptRt = acceptGO.GetComponent<RectTransform>();
        acceptRt.anchorMin = new Vector2(0.5f, 0f);
        acceptRt.anchorMax = new Vector2(0.5f, 0f);
        acceptRt.pivot = new Vector2(0.5f, 0f);
        acceptRt.sizeDelta = new Vector2(200f, 48f);
        acceptRt.anchoredPosition = new Vector2(0f, 40f);

        var acceptLabelGO = new GameObject("Label");
        acceptLabelGO.transform.SetParent(acceptGO.transform, false);
        var acceptLabel = acceptLabelGO.AddComponent<Text>();
        acceptLabel.text = "Acepto (Enter)";
        acceptLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        acceptLabel.fontSize = 20;
        acceptLabel.fontStyle = FontStyle.Bold;
        acceptLabel.alignment = TextAnchor.MiddleCenter;
        acceptLabel.color = new Color(0.05f, 0.06f, 0.1f, 1f);
        var acceptLabelRt = acceptLabelGO.GetComponent<RectTransform>();
        acceptLabelRt.anchorMin = Vector2.zero;
        acceptLabelRt.anchorMax = Vector2.one;
        acceptLabelRt.offsetMin = Vector2.zero;
        acceptLabelRt.offsetMax = Vector2.zero;
    }

    private void AddLabel(Transform parent, string name, string text, int fontSize, float y, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.85f, 0.87f, 0.92f, 1f);
        label.text = text;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-80f, height);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private void OnAccept()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiConfirm();
        _save.Current.dataConsentAccepted = true;
        _save.Save();
        SetVisible(false);
    }
}
