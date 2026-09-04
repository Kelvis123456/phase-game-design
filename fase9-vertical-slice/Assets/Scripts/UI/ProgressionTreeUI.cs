using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Fase 10 M3.1: UI real del árbol de meta-progresión (GDD §4.1, §11.5.5) — no el HUD de
// debug con texto plano, una pantalla real con botones que el jugador puede tocar/clickear.
// Se construye en runtime (no depende de un prefab pre-armado en el editor) para no
// duplicar la infraestructura de Canvas/EventSystem que ya existe en la escena.
public class ProgressionTreeUI : MonoBehaviour
{
    private Canvas _canvas;
    private ProgressionSystem _progression;
    private readonly Dictionary<string, Button> _buttons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Text> _buttonLabels = new Dictionary<string, Text>();
    private Text _balanceText;
    private bool _visible;

    private static readonly Color ColorLocked = new Color(0.3f, 0.3f, 0.35f, 1f);
    private static readonly Color ColorAvailable = new Color(0.23f, 0.5f, 0.9f, 1f);
    private static readonly Color ColorUnlocked = new Color(0.23f, 1f, 0.6f, 1f);

    private void Start()
    {
        _progression = Services.Get<ProgressionSystem>();
        BuildUI();
        Refresh();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            SetVisible(!_visible);
            if (_visible) Refresh();
        }
    }

    private void SetVisible(bool v)
    {
        _visible = v;
        _canvas.gameObject.SetActive(v);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("ProgressionTreeCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100; // por encima del DeathFlash y de todo lo demás
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleText = titleGO.AddComponent<Text>();
        titleText.text = "ÁRBOL DE PROGRESIÓN";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 28;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;
        var titleRt = titleGO.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.sizeDelta = new Vector2(0f, 60f);
        titleRt.anchoredPosition = new Vector2(0f, -10f);

        var balanceGO = new GameObject("Balance");
        balanceGO.transform.SetParent(canvasGO.transform, false);
        _balanceText = balanceGO.AddComponent<Text>();
        _balanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _balanceText.fontSize = 22;
        _balanceText.alignment = TextAnchor.MiddleCenter;
        _balanceText.color = new Color(1f, 0.85f, 0.3f, 1f);
        var balanceRt = balanceGO.GetComponent<RectTransform>();
        balanceRt.anchorMin = new Vector2(0f, 1f);
        balanceRt.anchorMax = new Vector2(1f, 1f);
        balanceRt.pivot = new Vector2(0.5f, 1f);
        balanceRt.sizeDelta = new Vector2(0f, 30f);
        balanceRt.anchoredPosition = new Vector2(0f, -60f);

        // Grid simple: una columna por rama (A/B/C/D), filas por nodo dentro de la rama.
        // La altura de fila se calcula según la rama MÁS LARGA (hoy Rama C, 10 nodos) para
        // que ninguna columna se salga de la ventana sin importar cuántos nodos tenga cada
        // rama — antes esto estaba hardcodeado a 60px asumiendo ramas de 2-3 nodos de muestra,
        // y con las 10 filas reales de Rama C el último nodo quedaba cortado fuera de pantalla.
        string[] branches = { "A", "B", "C", "D" };
        float colWidth = 300f;
        float startX = -((branches.Length - 1) * colWidth) / 2f;

        int maxRows = 1;
        foreach (var b in branches)
            maxRows = Mathf.Max(maxRows, ProgressionSystem.NodeTable.FindAll(n => n.branch == b).Count);

        const float gridTop = -150f;
        const float gridBottom = -670f; // deja espacio para el hint "TAB para cerrar" debajo
        float rowHeight = Mathf.Clamp((gridBottom - gridTop) / -maxRows, 40f, 60f);
        float btnHeight = rowHeight - 10f;

        for (int col = 0; col < branches.Length; col++)
        {
            string branch = branches[col];
            var nodesInBranch = ProgressionSystem.NodeTable.FindAll(n => n.branch == branch);

            var headerGO = new GameObject($"Header_{branch}");
            headerGO.transform.SetParent(canvasGO.transform, false);
            var headerText = headerGO.AddComponent<Text>();
            headerText.text = $"Rama {branch}";
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 18;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            var headerRt = headerGO.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.sizeDelta = new Vector2(280f, 26f);
            headerRt.anchoredPosition = new Vector2(startX + col * colWidth, -110f);

            for (int row = 0; row < nodesInBranch.Count; row++)
            {
                var node = nodesInBranch[row];
                var btnGO = new GameObject($"Node_{node.id}");
                btnGO.transform.SetParent(canvasGO.transform, false);
                var btnImg = btnGO.AddComponent<Image>();
                var btn = btnGO.AddComponent<Button>();
                var btnRt = btnGO.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 1f);
                btnRt.anchorMax = new Vector2(0.5f, 1f);
                btnRt.pivot = new Vector2(0.5f, 1f);
                btnRt.sizeDelta = new Vector2(260f, btnHeight);
                btnRt.anchoredPosition = new Vector2(startX + col * colWidth, gridTop - row * rowHeight);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(btnGO.transform, false);
                var label = labelGO.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 14;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                var labelRt = labelGO.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;

                string nodeId = node.id; // captura local para el closure del listener
                btn.onClick.AddListener(() => OnNodeClicked(nodeId));

                _buttons[node.id] = btn;
                _buttonLabels[node.id] = label;
            }
        }

        var hintGO = new GameObject("Hint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hintText = hintGO.AddComponent<Text>();
        hintText.text = "TAB para cerrar";
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 16;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(0.6f, 0.6f, 0.6f, 1f);
        var hintRt = hintGO.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.sizeDelta = new Vector2(300f, 30f);
        hintRt.anchoredPosition = new Vector2(0f, 20f);
    }

    private void OnNodeClicked(string nodeId)
    {
        _progression.TryUnlock(nodeId);
        Refresh();
    }

    private void Refresh()
    {
        _balanceText.text = $"Phase Crystals: {_progression.PhaseCrystalBalance}";

        foreach (var node in ProgressionSystem.NodeTable)
        {
            if (!_buttons.TryGetValue(node.id, out var btn)) continue;
            bool unlocked = _progression.IsNodeUnlocked(node.id);
            bool canUnlock = _progression.CanUnlock(node.id);

            var img = btn.GetComponent<Image>();
            img.color = unlocked ? ColorUnlocked : (canUnlock ? ColorAvailable : ColorLocked);
            btn.interactable = canUnlock;

            _buttonLabels[node.id].text = unlocked
                ? $"{node.displayName}\n[desbloqueado]"
                : $"{node.displayName}\n{node.cost} PC";
        }
    }
}
