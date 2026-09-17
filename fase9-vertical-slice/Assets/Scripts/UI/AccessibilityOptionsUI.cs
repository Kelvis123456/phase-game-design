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
    // GDD §14.3: 2 ajustes más además del modo daltónico — velocidad de ecos en
    // bullet-time (1.0x-0.5x) y tiempo de carga de bullet-time (0.1s-0.5s). ↑/↓ elige
    // cuál de los 3 ajustes está seleccionado, ←/→ (o los botones < >  de cada fila) lo
    // cambia — el mismo patrón de un selector de consola, útil para una pantalla que es
    // justamente la de accesibilidad y no debería depender solo del mouse.
    private const int RowCount = 3;
    private int _selectedRow = 0;

    private Canvas _canvas;
    private SaveSystem _save;
    private Text _modeLabel;
    private Text _echoSpeedLabel;
    private Text _chargeTimeLabel;
    private Text _modeTitleLabel;
    private Text _echoSpeedTitleLabel;
    private Text _chargeTimeTitleLabel;

    private static readonly Color SelectedColor = new Color(1f, 0.95f, 0.6f, 1f);
    private static readonly Color UnselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

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

        if (Input.GetKeyDown(KeyCode.UpArrow)) _selectedRow = (_selectedRow - 1 + RowCount) % RowCount;
        if (Input.GetKeyDown(KeyCode.DownArrow)) _selectedRow = (_selectedRow + 1) % RowCount;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) AdjustRow(_selectedRow, -1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) AdjustRow(_selectedRow, 1);

        // El estado también se puede cambiar desde fuera de esta pantalla (ej. los
        // hotkeys de debug) — refrescar cada frame que está visible es barato y evita
        // que quede mostrando un valor viejo si eso pasa mientras está abierta.
        Refresh();
    }

    private void AdjustRow(int row, int direction)
    {
        switch (row)
        {
            case 0: Cycle(direction); break;
            case 1: AdjustEchoSpeed(direction); break;
            case 2: AdjustChargeTime(direction); break;
        }
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

        _modeTitleLabel = AddSectionLabel(canvasGO.transform, "SectionLabel", "Modo daltónico (GDD §14.2)", -110f);

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

        // GDD §14.3 — el resto de accesibilidad de este pase: velocidad de ecos en
        // bullet-time y tiempo de carga de bullet-time. Steppers (< >), no sliders reales
        // — misma simplificación de VS que el resto de la UI (Button/Text de uGUI, no un
        // Slider nuevo, para un ajuste de solo 2 pasos discretos con GDD §-rango fijo).
        _echoSpeedTitleLabel = AddSectionLabel(canvasGO.transform, "EchoSpeedSection", "Velocidad de ecos en bullet-time (GDD §14.3)", -260f);
        var echoSpeedRowGO = new GameObject("EchoSpeedRow");
        echoSpeedRowGO.transform.SetParent(canvasGO.transform, false);
        var echoSpeedRowRt = echoSpeedRowGO.AddComponent<RectTransform>();
        echoSpeedRowRt.anchorMin = new Vector2(0.5f, 1f);
        echoSpeedRowRt.anchorMax = new Vector2(0.5f, 1f);
        echoSpeedRowRt.pivot = new Vector2(0.5f, 1f);
        echoSpeedRowRt.sizeDelta = new Vector2(480f, 40f);
        echoSpeedRowRt.anchoredPosition = new Vector2(0f, -288f);
        AddButton(echoSpeedRowGO.transform, "PrevButton", "<", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => AdjustEchoSpeed(-1));
        var echoSpeedLabelGO = new GameObject("Label");
        echoSpeedLabelGO.transform.SetParent(echoSpeedRowGO.transform, false);
        _echoSpeedLabel = echoSpeedLabelGO.AddComponent<Text>();
        _echoSpeedLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _echoSpeedLabel.fontSize = 20;
        _echoSpeedLabel.alignment = TextAnchor.MiddleCenter;
        _echoSpeedLabel.color = Color.white;
        var echoSpeedLabelRt = echoSpeedLabelGO.GetComponent<RectTransform>();
        echoSpeedLabelRt.anchorMin = new Vector2(0f, 0f);
        echoSpeedLabelRt.anchorMax = new Vector2(1f, 1f);
        echoSpeedLabelRt.offsetMin = new Vector2(52f, 0f);
        echoSpeedLabelRt.offsetMax = new Vector2(-52f, 0f);
        AddButton(echoSpeedRowGO.transform, "NextButton", ">", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => AdjustEchoSpeed(1));

        _chargeTimeTitleLabel = AddSectionLabel(canvasGO.transform, "ChargeTimeSection", "Tiempo de carga de bullet-time (GDD §14.3)", -340f);
        var chargeRowGO = new GameObject("ChargeTimeRow");
        chargeRowGO.transform.SetParent(canvasGO.transform, false);
        var chargeRowRt = chargeRowGO.AddComponent<RectTransform>();
        chargeRowRt.anchorMin = new Vector2(0.5f, 1f);
        chargeRowRt.anchorMax = new Vector2(0.5f, 1f);
        chargeRowRt.pivot = new Vector2(0.5f, 1f);
        chargeRowRt.sizeDelta = new Vector2(480f, 40f);
        chargeRowRt.anchoredPosition = new Vector2(0f, -368f);
        AddButton(chargeRowGO.transform, "PrevButton", "<", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => AdjustChargeTime(-1));
        var chargeLabelGO = new GameObject("Label");
        chargeLabelGO.transform.SetParent(chargeRowGO.transform, false);
        _chargeTimeLabel = chargeLabelGO.AddComponent<Text>();
        _chargeTimeLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _chargeTimeLabel.fontSize = 20;
        _chargeTimeLabel.alignment = TextAnchor.MiddleCenter;
        _chargeTimeLabel.color = Color.white;
        var chargeLabelRt = chargeLabelGO.GetComponent<RectTransform>();
        chargeLabelRt.anchorMin = new Vector2(0f, 0f);
        chargeLabelRt.anchorMax = new Vector2(1f, 1f);
        chargeLabelRt.offsetMin = new Vector2(52f, 0f);
        chargeLabelRt.offsetMax = new Vector2(-52f, 0f);
        AddButton(chargeRowGO.transform, "NextButton", ">", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(44f, 44f), () => AdjustChargeTime(1));

        AddLabel(canvasGO.transform, "Hint", "O para cerrar — ↑↓ elige ajuste, ←→ o < > lo cambia", 14, new Vector2(0f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(460f, 24f));
    }

    private Text AddSectionLabel(Transform parent, string name, string text, float y)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = UnselectedColor;
        label.text = text;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 26f);
        rt.anchoredPosition = new Vector2(0f, y);
        return label;
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

    private const float EchoSpeedStep = 0.1f;
    private const float ChargeTimeStep = 0.05f;

    private void AdjustEchoSpeed(int direction)
    {
        var prefs = _save.Current.accessibilityPrefs;
        prefs.btEchoSpeed = Mathf.Clamp(Mathf.Round((prefs.btEchoSpeed + direction * EchoSpeedStep) * 100f) / 100f, 0.5f, 1f);
        _save.Save();
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiNavigate();
        Refresh();
    }

    private void AdjustChargeTime(int direction)
    {
        var prefs = _save.Current.accessibilityPrefs;
        prefs.btChargeTime = Mathf.Clamp(Mathf.Round((prefs.btChargeTime + direction * ChargeTimeStep) * 100f) / 100f, 0.1f, 0.5f);
        _save.Save();
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiNavigate();
        Refresh();
    }

    private void Refresh()
    {
        var prefs = _save.Current.accessibilityPrefs;

        _modeLabel.text = ColorblindPalette.DisplayName(prefs.colorblindMode);
        var palette = ColorblindPalette.Apply(EchoManager.EchoColors, prefs.colorblindMode);
        for (int i = 0; i < 5; i++) _swatches[i].color = palette[i];

        _echoSpeedLabel.text = prefs.btEchoSpeed >= 1f ? "1.0x (normal)" : $"{prefs.btEchoSpeed:F1}x";
        _chargeTimeLabel.text = $"{prefs.btChargeTime:F2}s";

        _modeTitleLabel.color = _selectedRow == 0 ? SelectedColor : UnselectedColor;
        _echoSpeedTitleLabel.color = _selectedRow == 1 ? SelectedColor : UnselectedColor;
        _chargeTimeTitleLabel.color = _selectedRow == 2 ? SelectedColor : UnselectedColor;
    }
}
