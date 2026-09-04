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
    }

    private void OnPlay()
    {
        _canvas.gameObject.SetActive(false);
        SetGameplayEnabled(true);
        if (Services.TryGet<RunManager>(out var runManager))
            runManager.StartRun();
    }

    private void OnQuit()
    {
        Application.Quit();
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
        quitRt.anchoredPosition = new Vector2(0f, 80f);

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
