using UnityEngine;
using UnityEngine.UI;

// GDD §11.5.1 "Opciones" (el 1er botón secundario del menú, junto a Logros y Tienda
// Permanente — ya no queda fuera) + GDD §14.1/§14.2: selector de modo daltónico, el único
// ajuste de accesibilidad de este pase (velocidad de bullet-time, tamaño de texto y
// soporte de una sola mano — GDD §14.3-14.5 — quedan para un pase aparte). Cambiar el modo
// acá persiste en SaveSystem y EchoManager lo lee en cada eco nuevo (ColorblindPalette).
// Mismo patrón runtime que AchievementsScreenUI/EchoShopUI, con < > por mouse Y flechas de
// teclado — esta es justo la pantalla de accesibilidad, tiene sentido que no dependa
// exclusivamente del mouse.
public class AccessibilityOptionsUI : MonoBehaviour
{
    private Canvas _canvas;
    private SaveSystem _save;
    private Text _modeLabel;

    private void Start()
    {
        _save = Services.Get<SaveSystem>();
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            bool willShow = !_canvas.gameObject.activeSelf;
            SetVisible(willShow);
            if (willShow) Refresh();
        }

        if (!_canvas.gameObject.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Cycle(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) Cycle(1);

        // El modo también se puede cambiar desde fuera de esta pantalla (ej. el hotkey de
        // debug F-P) — refrescar cada frame que está visible es barato y evita que quede
        // mostrando un valor viejo si eso pasa mientras el jugador la tiene abierta.
        Refresh();
    }

    public void Show()
    {
        SetVisible(true);
        Refresh();
    }

    private void SetVisible(bool visible) => _canvas.gameObject.SetActive(visible);

    private void BuildUI()
    {
        var canvasGO = new GameObject("AccessibilityOptionsCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 220; // igual que AchievementsScreenUI/EchoShopUI
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.04f, 0.97f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 28;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.85f, 0.87f, 0.92f, 1f);
        title.text = "OPCIONES";
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 44f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);

        var sectionGO = new GameObject("SectionLabel");
        sectionGO.transform.SetParent(canvasGO.transform, false);
        var section = sectionGO.AddComponent<Text>();
        section.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        section.fontSize = 16;
        section.alignment = TextAnchor.MiddleCenter;
        section.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        section.text = "Modo daltónico (GDD §14.2)";
        var sectionRt = sectionGO.GetComponent<RectTransform>();
        sectionRt.anchorMin = new Vector2(0f, 1f);
        sectionRt.anchorMax = new Vector2(1f, 1f);
        sectionRt.pivot = new Vector2(0.5f, 1f);
        sectionRt.sizeDelta = new Vector2(0f, 26f);
        sectionRt.anchoredPosition = new Vector2(0f, -110f);

        var rowGO = new GameObject("ModeRow");
        rowGO.transform.SetParent(canvasGO.transform, false);
        var rowRt = rowGO.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 1f);
        rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(480f, 44f);
        rowRt.anchoredPosition = new Vector2(0f, -150f);

        AddButton(rowGO.transform, "PrevButton", "<", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => Cycle(-1));

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowGO.transform, false);
        _modeLabel = labelGO.AddComponent<Text>();
        _modeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _modeLabel.fontSize = 20;
        _modeLabel.alignment = TextAnchor.MiddleCenter;
        _modeLabel.color = Color.white;
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(52f, 0f);
        labelRt.offsetMax = new Vector2(-52f, 0f);

        AddButton(rowGO.transform, "NextButton", ">", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => Cycle(1));

        var previewGO = new GameObject("PalettePreview");
        previewGO.transform.SetParent(canvasGO.transform, false);
        var previewRt = previewGO.AddComponent<RectTransform>();
        previewRt.anchorMin = new Vector2(0.5f, 1f);
        previewRt.anchorMax = new Vector2(0.5f, 1f);
        previewRt.pivot = new Vector2(0.5f, 1f);
        previewRt.sizeDelta = new Vector2(360f, 40f);
        previewRt.anchoredPosition = new Vector2(0f, -210f);

        _swatches = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var swGO = new GameObject($"Swatch_{i}");
            swGO.transform.SetParent(previewGO.transform, false);
            var sw = swGO.AddComponent<Image>();
            var swRt = swGO.GetComponent<RectTransform>();
            swRt.anchorMin = new Vector2(0f, 0.5f);
            swRt.anchorMax = new Vector2(0f, 0.5f);
            swRt.pivot = new Vector2(0.5f, 0.5f);
            swRt.sizeDelta = new Vector2(32f, 32f);
            swRt.anchoredPosition = new Vector2(i * 44f, 0f);
            _swatches[i] = sw;
        }

        AddLabel(canvasGO.transform, "Hint", "O para cerrar — ← → o < > para cambiar de modo", 14, new Vector2(0f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420f, 24f));
    }

    private Image[] _swatches;

    private void AddLabel(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        label.text = text;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
    }

    private void AddButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.3f, 0.32f, 0.4f, 0.9f);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = Vector2.zero;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var text = labelGO.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRt = labelGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    private void Cycle(int direction)
    {
        string current = _save.Current.accessibilityPrefs.colorblindMode;
        int index = System.Array.IndexOf(ColorblindPalette.AllModes, current);
        if (index < 0) index = 0;
        index = (index + direction + ColorblindPalette.AllModes.Length) % ColorblindPalette.AllModes.Length;
        _save.Current.accessibilityPrefs.colorblindMode = ColorblindPalette.AllModes[index];
        _save.Save();
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiNavigate();
        Refresh();
    }

    private void Refresh()
    {
        string mode = _save.Current.accessibilityPrefs.colorblindMode;
        _modeLabel.text = ColorblindPalette.DisplayName(mode);
        var palette = ColorblindPalette.Apply(EchoManager.EchoColors, mode);
        for (int i = 0; i < 5; i++) _swatches[i].color = palette[i];
    }
}
