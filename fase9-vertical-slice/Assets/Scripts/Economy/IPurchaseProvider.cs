using System;

// Fase 10 M5.1/M5.2 (GDD §9.2): abstracción sobre el store real (Unity IAP + Google Play
// Billing / Apple StoreKit). Conectar el SDK real requiere una cuenta de consola de tienda
// y credenciales de proyecto que no existen en este entorno — MonetizationSystem depende de
// esta interfaz, no del SDK, para que ese trabajo de plataforma no bloquee el resto del
// flujo (mismo patrón que la deuda de ICloudSync documentada en SaveSystem).
public interface IPurchaseProvider
{
    void Purchase(string productId, Action<bool> onComplete);
}

// Simula una tienda que siempre completa la compra con éxito de inmediato — permite probar
// el flujo completo (catálogo, aplicar el efecto, persistir) sin credenciales de tienda real.
// NO reemplaza la integración real antes de un build de lanzamiento (GDD Fase10 M5.1).
public class StubPurchaseProvider : IPurchaseProvider
{
    public void Purchase(string productId, Action<bool> onComplete) => onComplete?.Invoke(true);
}
