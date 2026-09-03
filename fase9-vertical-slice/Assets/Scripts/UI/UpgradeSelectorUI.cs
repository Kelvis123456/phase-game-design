using System;
using UnityEngine;
using UnityEngine.UI;

// Fase 10 M3.3: selector de upgrades de run real (GDD §7.3) — 2 opciones entre salas,
// el jugador elige 1 o pasa. Pausa la transición de sala hasta que se resuelve.
public class UpgradeSelectorUI : MonoBehaviour
{
    private Canvas _canvas;
    private Text _optionAText, _optionBText;
    private Button _optionAButton, _optionBButton, _skipButton;
    private Action<RunUpgrade> _onResolved;
    private RunUpgrade _optionA, _optionB;

    private void Awake()
    {
        Services.Register(this);
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public void Show(RunUpgrade a, RunUpgrade b, Action<RunUpgrade> onResolved)
    {
        _optionA = a;
        _optionB = b;
        _onResolved = onResolved;
        _optionAText.text = $"{a.displayName}\n\n{a.description}";
        _optionBText.text = $"{b.displayName}\n\n{b.description}";
        _canvas.gameObject.SetActive(true);
    }

    private void Resolve(RunUpgrade picked)
    {
        _canvas.gameObject.SetActive(false);
        var callback = _onResolved;
        _onResolved = null;
        callback?.Invoke(picked);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("UpgradeSelectorCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 90;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.9f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var title = titleGO.AddComponent<Text>();
        title.text = "ELIGE UN UPGRADE DE RUN";
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 26;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 60f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);

        (_optionAButton, _optionAText) = BuildOptionButton(canvasGO.transform, -220f, () => Resolve(_optionA));
        (_optionBButton, _optionBText) = BuildOptionButton(canvasGO.transform, 220f, () => Resolve(_optionB));

        var skipGO = new GameObject("SkipButton");
        skipGO.transform.SetParent(canvasGO.transform, false);
        var skipImg = skipGO.AddComponent<Image>();
        skipImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        _skipButton = skipGO.AddComponent<Button>();
        _skipButton.onClick.AddListener(() => Resolve(null));
        var skipRt = skipGO.GetComponent<RectTransform>();
        skipRt.anchorMin = new Vector2(0.5f, 0f);
        skipRt.anchorMax = new Vector2(0.5f, 0f);
        skipRt.pivot = new Vector2(0.5f, 0f);
        skipRt.sizeDelta = new Vector2(160f, 45f);
        skipRt.anchoredPosition = new Vector2(0f, 60f);

        var skipLabelGO = new GameObject("Label");
        skipLabelGO.transform.SetParent(skipGO.transform, false);
        var skipLabel = skipLabelGO.AddComponent<Text>();
        skipLabel.text = "Pasar";
        skipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipLabel.fontSize = 18;
        skipLabel.alignment = TextAnchor.MiddleCenter;
        skipLabel.color = Color.white;
        var skipLabelRt = skipLabelGO.GetComponent<RectTransform>();
        skipLabelRt.anchorMin = Vector2.zero;
        skipLabelRt.anchorMax = Vector2.one;
        skipLabelRt.offsetMin = Vector2.zero;
        skipLabelRt.offsetMax = Vector2.zero;
    }

    private (Button, Text) BuildOptionButton(Transform parent, float xOffset, Action onClick)
    {
        var btnGO = new GameObject($"Option_{xOffset}");
        btnGO.transform.SetParent(parent, false);
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.18f, 0.32f, 0.55f, 1f);
        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(() => onClick());
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(340f, 220f);
        rt.anchoredPosition = new Vector2(xOffset, 0f);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(btnGO.transform, false);
        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 18;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(10f, 10f);
        labelRt.offsetMax = new Vector2(-10f, -10f);

        return (btn, label);
    }
}
