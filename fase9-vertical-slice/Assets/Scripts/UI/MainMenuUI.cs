using UnityEngine;
using UnityEngine.UI;

// Pantalla de inicio real: el keyart como fondo + un botón "Jugar" que arranca el
// primer run. Bloquea el input de PlayerController/InputReader hasta que se presiona
// Jugar, para que el jugador no aparezca moviéndose detrás del menú.
public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Sprite _background;

    private Canvas _canvas;

    private void Awake()
    {
        BuildUI();
    }

    private void Start()
    {
        SetGameplayEnabled(false);
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayMenuMusic();
    }

    private void OnPlay()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiConfirm();
        _canvas.gameObject.SetActive(false);
        SetGameplayEnabled(true);
        if (Services.TryGet<RunManager>(out var runManager))
            runManager.StartRun();
    }

    private void OnQuit()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiCancel();
        Application.Quit();
    }

    private void OnAchievements()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiConfirm();
        if (Services.TryGet<AchievementsScreenUI>(out var screen))
            screen.Show();
    }

    private void OnShop()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiConfirm();
        if (Services.TryGet<EchoShopUI>(out var screen))
            screen.Show();
    }

    private void OnOptions()
    {
        if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiConfirm();
        if (Services.TryGet<AccessibilityOptionsUI>(out var screen))
            screen.Show();
    }

    private void SetGameplayEnabled(bool active)
    {
        if (Services.TryGet<PlayerController>(out var player)) player.enabled = active;
        if (Services.TryGet<InputReader>(out var input)) input.enabled = active;
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("MainMenuCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.sprite = _background;
        bgImg.preserveAspect = false;
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var playGO = new GameObject("PlayButton");
        playGO.transform.SetParent(canvasGO.transform, false);
        var playImg = playGO.AddComponent<Image>();
        playImg.color = new Color(0.86f, 0.91f, 0.96f, 0.95f);
        var playBtn = playGO.AddComponent<Button>();
        playBtn.onClick.AddListener(OnPlay);
        var playRt = playGO.GetComponent<RectTransform>();
        playRt.anchorMin = new Vector2(0.5f, 0f);
        playRt.anchorMax = new Vector2(0.5f, 0f);
        playRt.pivot = new Vector2(0.5f, 0f);
        playRt.sizeDelta = new Vector2(220f, 56f);
        playRt.anchoredPosition = new Vector2(0f, 130f);

        var playLabelGO = new GameObject("Label");
        playLabelGO.transform.SetParent(playGO.transform, false);
        var playLabel = playLabelGO.AddComponent<Text>();
        playLabel.text = "JUGAR";
        playLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playLabel.fontSize = 24;
        playLabel.fontStyle = FontStyle.Bold;
        playLabel.alignment = TextAnchor.MiddleCenter;
        playLabel.color = new Color(0.05f, 0.06f, 0.1f, 1f);
        var playLabelRt = playLabelGO.GetComponent<RectTransform>();
        playLabelRt.anchorMin = Vector2.zero;
        playLabelRt.anchorMax = Vector2.one;
        playLabelRt.offsetMin = Vector2.zero;
        playLabelRt.offsetMax = Vector2.zero;

        // GDD §11.5.1: fila de botones secundarios (Opciones | Logros | Tienda
        // Permanente) — los 3 existen.
        var achievementsGO = new GameObject("AchievementsButton");
        achievementsGO.transform.SetParent(canvasGO.transform, false);
        var achievementsImg = achievementsGO.AddComponent<Image>();
        achievementsImg.color = new Color(1f, 1f, 1f, 0f);
        var achievementsBtn = achievementsGO.AddComponent<Button>();
        achievementsBtn.onClick.AddListener(OnAchievements);
        var achievementsRt = achievementsGO.GetComponent<RectTransform>();
        achievementsRt.anchorMin = new Vector2(0.5f, 0f);
        achievementsRt.anchorMax = new Vector2(0.5f, 0f);
        achievementsRt.pivot = new Vector2(0.5f, 0f);
        achievementsRt.sizeDelta = new Vector2(140f, 30f);
        achievementsRt.anchoredPosition = new Vector2(0f, 100f);

        var achievementsLabelGO = new GameObject("Label");
        achievementsLabelGO.transform.SetParent(achievementsGO.transform, false);
        var achievementsLabel = achievementsLabelGO.AddComponent<Text>();
        achievementsLabel.text = "Logros";
        achievementsLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        achievementsLabel.fontSize = 16;
        achievementsLabel.alignment = TextAnchor.MiddleCenter;
        achievementsLabel.color = new Color(0.75f, 0.78f, 0.85f, 0.85f);
        var achievementsLabelRt = achievementsLabelGO.GetComponent<RectTransform>();
        achievementsLabelRt.anchorMin = Vector2.zero;
        achievementsLabelRt.anchorMax = Vector2.one;
        achievementsLabelRt.offsetMin = Vector2.zero;
        achievementsLabelRt.offsetMax = Vector2.zero;

        var shopGO = new GameObject("ShopButton");
        shopGO.transform.SetParent(canvasGO.transform, false);
        var shopImg = shopGO.AddComponent<Image>();
        shopImg.color = new Color(1f, 1f, 1f, 0f);
        var shopBtn = shopGO.AddComponent<Button>();
        shopBtn.onClick.AddListener(OnShop);
        var shopRt = shopGO.GetComponent<RectTransform>();
        shopRt.anchorMin = new Vector2(0.5f, 0f);
        shopRt.anchorMax = new Vector2(0.5f, 0f);
        shopRt.pivot = new Vector2(0.5f, 0f);
        shopRt.sizeDelta = new Vector2(140f, 30f);
        shopRt.anchoredPosition = new Vector2(0f, 70f);

        var shopLabelGO = new GameObject("Label");
        shopLabelGO.transform.SetParent(shopGO.transform, false);
        var shopLabel = shopLabelGO.AddComponent<Text>();
        shopLabel.text = "Mis Ecos";
        shopLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shopLabel.fontSize = 16;
        shopLabel.alignment = TextAnchor.MiddleCenter;
        shopLabel.color = new Color(0.75f, 0.78f, 0.85f, 0.85f);
        var shopLabelRt = shopLabelGO.GetComponent<RectTransform>();
        shopLabelRt.anchorMin = Vector2.zero;
        shopLabelRt.anchorMax = Vector2.one;
        shopLabelRt.offsetMin = Vector2.zero;
        shopLabelRt.offsetMax = Vector2.zero;

        var optionsGO = new GameObject("OptionsButton");
        optionsGO.transform.SetParent(canvasGO.transform, false);
        var optionsImg = optionsGO.AddComponent<Image>();
        optionsImg.color = new Color(1f, 1f, 1f, 0f);
        var optionsBtn = optionsGO.AddComponent<Button>();
        optionsBtn.onClick.AddListener(OnOptions);
        var optionsRt = optionsGO.GetComponent<RectTransform>();
        optionsRt.anchorMin = new Vector2(0.5f, 0f);
        optionsRt.anchorMax = new Vector2(0.5f, 0f);
        optionsRt.pivot = new Vector2(0.5f, 0f);
        optionsRt.sizeDelta = new Vector2(140f, 30f);
        optionsRt.anchoredPosition = new Vector2(0f, 40f);

        var optionsLabelGO = new GameObject("Label");
        optionsLabelGO.transform.SetParent(optionsGO.transform, false);
        var optionsLabel = optionsLabelGO.AddComponent<Text>();
        optionsLabel.text = "Opciones";
        optionsLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        optionsLabel.fontSize = 16;
        optionsLabel.alignment = TextAnchor.MiddleCenter;
        optionsLabel.color = new Color(0.75f, 0.78f, 0.85f, 0.85f);
        var optionsLabelRt = optionsLabelGO.GetComponent<RectTransform>();
        optionsLabelRt.anchorMin = Vector2.zero;
        optionsLabelRt.anchorMax = Vector2.one;
        optionsLabelRt.offsetMin = Vector2.zero;
        optionsLabelRt.offsetMax = Vector2.zero;

        var quitGO = new GameObject("QuitButton");
        quitGO.transform.SetParent(canvasGO.transform, false);
        var quitImg = quitGO.AddComponent<Image>();
        quitImg.color = new Color(1f, 1f, 1f, 0f);
        var quitBtn = quitGO.AddComponent<Button>();
        quitBtn.onClick.AddListener(OnQuit);
        var quitRt = quitGO.GetComponent<RectTransform>();
        quitRt.anchorMin = new Vector2(0.5f, 0f);
        quitRt.anchorMax = new Vector2(0.5f, 0f);
        quitRt.pivot = new Vector2(0.5f, 0f);
        quitRt.sizeDelta = new Vector2(140f, 30f);
        quitRt.anchoredPosition = new Vector2(0f, 10f);

        var quitLabelGO = new GameObject("Label");
        quitLabelGO.transform.SetParent(quitGO.transform, false);
        var quitLabel = quitLabelGO.AddComponent<Text>();
        quitLabel.text = "Salir";
        quitLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        quitLabel.fontSize = 16;
        quitLabel.alignment = TextAnchor.MiddleCenter;
        quitLabel.color = new Color(0.75f, 0.78f, 0.85f, 0.85f);
        var quitLabelRt = quitLabelGO.GetComponent<RectTransform>();
        quitLabelRt.anchorMin = Vector2.zero;
        quitLabelRt.anchorMax = Vector2.one;
        quitLabelRt.offsetMin = Vector2.zero;
        quitLabelRt.offsetMax = Vector2.zero;
    }
}
