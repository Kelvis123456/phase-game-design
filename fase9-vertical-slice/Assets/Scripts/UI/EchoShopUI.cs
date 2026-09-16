using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// GDD §11.5.1 "Tienda Permanente" (el 3er botón secundario del menú, junto a Logros —
// Opciones queda fuera) + GDD §4.2 "MIS ECOS": asignar una skin (Rama C, ya desbloqueada)
// a cada uno de los 5 slots de eco, cambiable fuera de una run, más la compra directa de
// las 4 skins Premium y el modo sin anuncios (GDD §9.2 vías 1 y 2) para quien no quiere
// esperar a juntar Phase Crystals. Mismo patrón runtime que AchievementsScreenUI.
public class EchoShopUI : MonoBehaviour
{
    private static readonly string[] SkinOrder = { "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10" };
    private static readonly string[] PremiumProducts = { "skin_c4", "skin_c7", "skin_c8", "skin_c10" };

    private Canvas _canvas;
    private ProgressionSystem _progression;
    private MonetizationSystem _monetization;

    private readonly Text[] _slotLabels = new Text[5];
    private Text _adRemovalLabel;
    private Button _adRemovalButton;
    private readonly Dictionary<string, Text> _premiumLabels = new Dictionary<string, Text>();
    private readonly Dictionary<string, Button> _premiumButtons = new Dictionary<string, Button>();

    private void Start()
    {
        _progression = Services.Get<ProgressionSystem>();
        _monetization = Services.Get<MonetizationSystem>();
        BuildUI();
        SetVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
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
        var canvasGO = new GameObject("EchoShopCanvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 220; // igual que AchievementsScreenUI — encima del menú principal (200)
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

        AddLabel(canvasGO.transform, "Title", "MIS ECOS", 28, new Vector2(0f, -30f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 44f));

        // Fila por slot: nombre de la skin equipada + botones < > para ciclar entre las
        // skins YA desbloqueadas (GDD §4.2 — el cambio es libre entre lo que ya se tiene).
        for (int i = 0; i < 5; i++)
        {
            int slot = i;
            var rowGO = new GameObject($"SlotRow_{i}");
            rowGO.transform.SetParent(canvasGO.transform, false);
            var rowRt = rowGO.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 1f);
            rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(480f, 40f);
            rowRt.anchoredPosition = new Vector2(0f, -90f - i * 46f);

            AddButton(rowGO.transform, "PrevButton", "<", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, 40f), () => CycleSkin(slot, -1));

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(48f, 0f);
            labelRt.offsetMax = new Vector2(-48f, 0f);
            _slotLabels[i] = label;

            AddButton(rowGO.transform, "NextButton", ">", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(40f, 40f), () => CycleSkin(slot, 1));
        }

        AddLabel(canvasGO.transform, "PremiumTitle", "COMPRA DIRECTA", 18, new Vector2(0f, -340f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 26f));

        int row2 = 0;
        foreach (var productId in PremiumProducts)
        {
            BuildPurchaseRow(canvasGO.transform, productId, ProgressionSystem.NodeTable.Find(n => n.id == MonetizationSystem.SkinProductToNodeId[productId]).displayName, row2, () => _monetization.PurchaseSkin(productId, _ => Refresh()));
            row2++;
        }
        BuildPurchaseRow(canvasGO.transform, MonetizationSystem.AdRemovalProductId, "Modo Sin Anuncios", row2, () => _monetization.PurchaseAdRemoval(_ => Refresh()));

        AddLabel(canvasGO.transform, "Hint", "M para cerrar — < > para cambiar skin de un slot", 14, new Vector2(0f, 20f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(420f, 24f));
    }

    private void BuildPurchaseRow(Transform parent, string productId, string displayName, int row, UnityEngine.Events.UnityAction onBuy)
    {
        var rowGO = new GameObject($"PurchaseRow_{productId}");
        rowGO.transform.SetParent(parent, false);
        var rowRt = rowGO.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 1f);
        rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.sizeDelta = new Vector2(480f, 36f);
        rowRt.anchoredPosition = new Vector2(0f, -374f - row * 40f);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(rowGO.transform, false);
        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = Color.white;
        label.text = $"{displayName} — {MonetizationSystem.DisplayPrices[productId]}";
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(0.7f, 1f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        if (productId == MonetizationSystem.AdRemovalProductId) _adRemovalLabel = label;
        else _premiumLabels[productId] = label;

        var buyGO = new GameObject("BuyButton");
        buyGO.transform.SetParent(rowGO.transform, false);
        var buyImg = buyGO.AddComponent<Image>();
        buyImg.color = new Color(0.86f, 0.91f, 0.96f, 0.95f);
        var buyBtn = buyGO.AddComponent<Button>();
        buyBtn.onClick.AddListener(onBuy);
        var buyRt = buyGO.GetComponent<RectTransform>();
        buyRt.anchorMin = new Vector2(0.72f, 0.1f);
        buyRt.anchorMax = new Vector2(1f, 0.9f);
        buyRt.offsetMin = Vector2.zero;
        buyRt.offsetMax = Vector2.zero;

        var buyLabelGO = new GameObject("Label");
        buyLabelGO.transform.SetParent(buyGO.transform, false);
        var buyLabel = buyLabelGO.AddComponent<Text>();
        buyLabel.text = "Comprar";
        buyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buyLabel.fontSize = 13;
        buyLabel.alignment = TextAnchor.MiddleCenter;
        buyLabel.color = new Color(0.05f, 0.06f, 0.1f, 1f);
        var buyLabelRt = buyLabelGO.GetComponent<RectTransform>();
        buyLabelRt.anchorMin = Vector2.zero;
        buyLabelRt.anchorMax = Vector2.one;
        buyLabelRt.offsetMin = Vector2.zero;
        buyLabelRt.offsetMax = Vector2.zero;
        if (productId == MonetizationSystem.AdRemovalProductId) _adRemovalButton = buyBtn;
        else _premiumButtons[productId] = buyBtn;
    }

    private void AddLabel(Transform parent, string name, string text, int fontSize, Vector2 anchoredPos, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
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

    private void CycleSkin(int slot, int direction)
    {
        var unlocked = new List<string>();
        foreach (var id in SkinOrder)
            if (id == "C1" || _progression.IsNodeUnlocked(id)) unlocked.Add(id);
        if (unlocked.Count == 0) return;

        string current = _progression.GetEquippedSkin(slot);
        int index = unlocked.IndexOf(current);
        if (index < 0) index = 0;
        index = (index + direction + unlocked.Count) % unlocked.Count;

        if (_progression.TryEquipSkin(slot, unlocked[index]))
        {
            if (Services.TryGet<AudioManager>(out var audio)) audio.PlayUiNavigate();
            Refresh();
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < 5; i++)
        {
            string skinId = _progression.GetEquippedSkin(i);
            var node = ProgressionSystem.NodeTable.Find(n => n.id == skinId);
            _slotLabels[i].text = $"Eco {i + 1}: {(node != null ? node.displayName : skinId)}";
        }

        bool adsRemoved = _progression.IsNodeUnlocked("D2");
        _adRemovalLabel.text = adsRemoved ? "Modo Sin Anuncios — ya comprado" : $"Modo Sin Anuncios — {MonetizationSystem.DisplayPrices[MonetizationSystem.AdRemovalProductId]}";
        _adRemovalButton.interactable = !adsRemoved;

        foreach (var productId in PremiumProducts)
        {
            string nodeId = MonetizationSystem.SkinProductToNodeId[productId];
            bool owned = _progression.IsNodeUnlocked(nodeId);
            var node = ProgressionSystem.NodeTable.Find(n => n.id == nodeId);
            _premiumLabels[productId].text = owned
                ? $"{node.displayName} — ya comprado"
                : $"{node.displayName} — {MonetizationSystem.DisplayPrices[productId]}";
            _premiumButtons[productId].interactable = !owned;
        }
    }
}
