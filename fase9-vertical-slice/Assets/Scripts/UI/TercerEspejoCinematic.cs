using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// GDD §6.3 "EL TERCER SLOT — Tercer Espejo": el desbloqueo más importante del juego —
// el primer momento en que el jugador ve tres versiones de sí mismo moviéndose a la vez.
// Se dispara al comprar el nodo A2 (ver ProgressionTreeUI.OnNodeClicked). Secuencia real
// de 8s (saltable después de 3s): Eco 1 entra desde la izquierda, Eco 2 desde la derecha,
// un tercer eco cae desde arriba y se une a los otros dos, los tres miran al jugador,
// flash de luz, y el número "3" aparece en el mismo cian de los ecos.
public class TercerEspejoCinematic : MonoBehaviour
{
    [SerializeField] private Sprite _echoSprite;

    private const float SkipUnlockTime = 3f;
    private const float TotalDuration = 8f;

    private static readonly Color Eco1Color = new Color(0.227f, 1.000f, 0.831f, 1f); // Cyan
    private static readonly Color Eco2Color = new Color(0.659f, 0.333f, 0.969f, 1f); // Violet
    private static readonly Color Eco3Color = new Color(0.976f, 0.451f, 0.086f, 1f); // Ember

    private Canvas _canvas;
    private RectTransform _echo1, _echo2, _echo3;
    private Image _flash;
    private Text _threeText;
    private Coroutine _running;

    private void Awake()
    {
        Services.Register(this);
        BuildUI();
        _canvas.gameObject.SetActive(false);
    }

    public void Play()
    {
        if (_running != null) StopCoroutine(_running);
        _canvas.gameObject.SetActive(true);
        _running = StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        float t = 0f;
        bool skipped = false;

        void ResetVisuals()
        {
            _echo1.anchoredPosition = new Vector2(-700f, 0f);
            _echo2.anchoredPosition = new Vector2(700f, 0f);
            _echo3.anchoredPosition = new Vector2(0f, 500f);
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _threeText.color = new Color(0.227f, 1f, 0.831f, 0f);
        }
        ResetVisuals();

        while (t < TotalDuration && !skipped)
        {
            t += Time.unscaledDeltaTime;

            if (t >= SkipUnlockTime && (Input.anyKeyDown))
                skipped = true;

            // Eco 1: entra desde la izquierda entre 0.3s-1.8s
            float e1 = Mathf.InverseLerp(0.3f, 1.8f, t);
            _echo1.anchoredPosition = Vector2.Lerp(new Vector2(-700f, 0f), new Vector2(-220f, 0f), Ease(e1));

            // Eco 2: entra desde la derecha entre 1.0s-2.5s
            float e2 = Mathf.InverseLerp(1.0f, 2.5f, t);
            _echo2.anchoredPosition = Vector2.Lerp(new Vector2(700f, 0f), new Vector2(220f, 0f), Ease(e2));

            // Eco 3: cae desde arriba entre 2.8s-4.2s
            float e3 = Mathf.InverseLerp(2.8f, 4.2f, t);
            _echo3.anchoredPosition = Vector2.Lerp(new Vector2(0f, 500f), Vector2.zero, Ease(e3));

            // Flash entre 4.6s-5.0s
            float flashA = t < 4.6f ? 0f : Mathf.Clamp01(1f - Mathf.Abs(t - 4.8f) / 0.3f);
            _flash.color = new Color(1f, 1f, 1f, flashA);

            // "3" aparece 4.9s-7.6s
            float threeA = Mathf.Clamp01(Mathf.InverseLerp(4.9f, 5.4f, t)) * (1f - Mathf.Clamp01(Mathf.InverseLerp(7.2f, 7.6f, t)));
            _threeText.color = new Color(0.227f, 1f, 0.831f, threeA);

            yield return null;
        }

        _canvas.gameObject.SetActive(false);
        _running = null;
    }

    private static float Ease(float t) => t * t * (3f - 2f * t); // smoothstep

    private void BuildUI()
    {
        var canvasGO = new GameObject("TercerEspejoCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 300; // por encima de todo, incluyendo el menú principal
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.02f, 0.03f, 1f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        _echo1 = BuildEcho(canvasGO.transform, "Echo1", Eco1Color);
        _echo2 = BuildEcho(canvasGO.transform, "Echo2", Eco2Color);
        _echo3 = BuildEcho(canvasGO.transform, "Echo3", Eco3Color);

        var flashGO = new GameObject("Flash");
        flashGO.transform.SetParent(canvasGO.transform, false);
        _flash = flashGO.AddComponent<Image>();
        _flash.color = new Color(1f, 1f, 1f, 0f);
        var flashRt = flashGO.GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;

        var threeGO = new GameObject("ThreeLabel");
        threeGO.transform.SetParent(canvasGO.transform, false);
        _threeText = threeGO.AddComponent<Text>();
        _threeText.text = "3";
        _threeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _threeText.fontSize = 140;
        _threeText.fontStyle = FontStyle.Bold;
        _threeText.alignment = TextAnchor.MiddleCenter;
        var threeRt = threeGO.GetComponent<RectTransform>();
        threeRt.anchorMin = new Vector2(0.5f, 0.5f);
        threeRt.anchorMax = new Vector2(0.5f, 0.5f);
        threeRt.sizeDelta = new Vector2(300f, 200f);
        threeRt.anchoredPosition = Vector2.zero;

        var hintGO = new GameObject("SkipHint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hint = hintGO.AddComponent<Text>();
        hint.text = "click o cualquier tecla para saltar";
        hint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hint.fontSize = 14;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.5f, 0.5f, 0.55f, 0.8f);
        var hintRt = hintGO.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.sizeDelta = new Vector2(400f, 30f);
        hintRt.anchoredPosition = new Vector2(0f, 30f);
    }

    private RectTransform BuildEcho(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _echoSprite;
        img.color = color;
        img.preserveAspect = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.45f);
        rt.anchorMax = new Vector2(0.5f, 0.45f);
        rt.sizeDelta = new Vector2(64f, 128f);
        return rt;
    }
}
