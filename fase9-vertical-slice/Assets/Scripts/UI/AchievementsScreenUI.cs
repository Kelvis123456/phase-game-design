using UnityEngine;
using UnityEngine.UI;

// GDD §11.5.1: "Logros" es uno de los 3 botones secundarios de la pantalla de inicio
// (Opciones | Logros | Tienda Permanente). Solo este existe todavía — Opciones y Tienda
// Permanente son su propio trabajo (fuera de este pase). Pantalla real construida en
// runtime, mismo patrón que ProgressionTreeUI.
public class AchievementsScreenUI : MonoBehaviour
{
    private Canvas _canvas;
    private AchievementSystem _achievements;
    private Text _titleText;

    private static readonly Color ColorLocked = new Color(0.3f, 0.3f, 0.35f, 1f);
    private static readonly Color ColorUnlocked = new Color(0.23f, 1f, 0.6f, 1f);

    private void Start()
    {
        _achievements = Services.Get<AchievementSystem>();
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            bool willShow = !_canvas.gameObject.activeSelf;
            SetVisible(willShow);
            if (willShow) Refresh();
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
        var canvasGO = new GameObject("AchievementsCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Por encima de MainMenuUI (200) — el botón "Logros" del menú principal abre
        // esto, así que tiene que renderizar ARRIBA del fondo del menú, no detrás.
        _canvas.sortingOrder = 220;
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
        _titleText = titleGO.AddComponent<Text>();
        _titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _titleText.fontSize = 32;
        _titleText.alignment = TextAnchor.MiddleCenter;
        _titleText.color = Color.white;
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 50f);
        titleRt.anchoredPosition = new Vector2(0f, -30f);

        int row = 0;
        foreach (var achievement in AchievementSystem.Table)
        {
            var rowGO = new GameObject($"Row_{achievement.id}");
            rowGO.transform.SetParent(canvasGO.transform, false);
            var rowImg = rowGO.AddComponent<Image>();
            var rowRt = rowGO.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(700f, 56f);
            rowRt.anchoredPosition = new Vector2(0f, -100f - row * 64f);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text = $"{achievement.displayName} — {achievement.description}";
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 4f);
            labelRt.offsetMax = new Vector2(-16f, -4f);

            _rowImages[achievement.id] = rowImg;
            row++;
        }

        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hint = hintGO.AddComponent<Text>();
        hint.text = "L para cerrar";
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 16;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        var hintRt = hintGO.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.sizeDelta = new Vector2(300f, 30f);
        hintRt.anchoredPosition = new Vector2(0f, 20f);
    }

    private readonly System.Collections.Generic.Dictionary<string, Image> _rowImages = new System.Collections.Generic.Dictionary<string, Image>();

    private void Refresh()
    {
        int unlockedCount = 0;
        foreach (var achievement in AchievementSystem.Table)
        {
            bool unlocked = _achievements.IsUnlocked(achievement.id);
            if (unlocked) unlockedCount++;
            if (_rowImages.TryGetValue(achievement.id, out var img))
                img.color = unlocked ? ColorUnlocked : ColorLocked;
        }
        _titleText.text = $"LOGROS ({unlockedCount}/{AchievementSystem.Table.Count})";
    }
}
