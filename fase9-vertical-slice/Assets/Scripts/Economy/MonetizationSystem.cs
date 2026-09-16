using System;
using System.Collections.Generic;
using UnityEngine;

// GDD §9.2 — las 2 vías de monetización que no dependen de contenido estacional nuevo:
// vía 1 (Modo Sin Anuncios, $3.99) y vía 2 (skins Premium C4/C7/C8/C10, $0.99-$2.49). La
// vía 3 (Season Pass "Frecuencia") queda fuera de este pase — necesita su propio sistema
// de rotación de contenido trimestral (fase10-desarrollo/desarrollo-completo.md M5.3).
//
// Usa IPurchaseProvider (stub por defecto, ver ese archivo) — lo real acá es el flujo
// completo alrededor del pago: catálogo de producto, aplicar el efecto vía
// ProgressionSystem.UnlockViaPurchase, persistir el estado. El SDK de cobro real
// (Unity IAP + consola de Google Play / Apple) es trabajo de plataforma pendiente.
[DefaultExecutionOrder(-93)]
public class MonetizationSystem : MonoBehaviour
{
    public const string AdRemovalProductId = "ads_removal";

    // GDD §9.2 vía 2 — precio de venta directa; el árbol de Phase Crystals (ProgressionSystem)
    // sigue siendo la vía gratuita para el mismo nodo, nunca se retira esa opción.
    public static readonly Dictionary<string, string> SkinProductToNodeId = new Dictionary<string, string>
    {
        { "skin_c4", "C4" },
        { "skin_c7", "C7" },
        { "skin_c8", "C8" },
        { "skin_c10", "C10" },
    };

    public static readonly Dictionary<string, string> DisplayPrices = new Dictionary<string, string>
    {
        { AdRemovalProductId, "$3.99" },
        { "skin_c4", "$0.99" },
        { "skin_c7", "$1.49" },
        { "skin_c8", "$1.99" },
        { "skin_c10", "$2.49" },
    };

    private IPurchaseProvider _provider = new StubPurchaseProvider();
    private ProgressionSystem _progression;

    private void Awake()
    {
        Services.Register(this);
    }

    private void Start()
    {
        _progression = Services.Get<ProgressionSystem>();
    }

    // Para QA/tests: reemplazar el stub por un provider real o uno de prueba controlado.
    public void SetProvider(IPurchaseProvider provider) => _provider = provider;

    public void PurchaseAdRemoval(Action<bool> onComplete = null)
    {
        _provider.Purchase(AdRemovalProductId, success =>
        {
            // D2 "Modo Sin Anuncios" ya existe en el árbol como nodo comprable con PC
            // (GDD §4.1) — esta es la vía 2 alternativa con dinero real hacia el MISMO
            // efecto, no un flag separado, para que solo haya una fuente de verdad.
            if (success) _progression.UnlockViaPurchase("D2");
            onComplete?.Invoke(success);
        });
    }

    public void PurchaseSkin(string productId, Action<bool> onComplete = null)
    {
        if (!SkinProductToNodeId.TryGetValue(productId, out var nodeId))
        {
            onComplete?.Invoke(false);
            return;
        }
        _provider.Purchase(productId, success =>
        {
            if (success) _progression.UnlockViaPurchase(nodeId);
            onComplete?.Invoke(success);
        });
    }
}
