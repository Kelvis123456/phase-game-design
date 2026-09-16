using System;
using UnityEngine;

// GDD §9.2 vía 3 "Pase de Temporada Frecuencia" ($4.99/trimestre). El GDD promete 4 cosas:
// 5 skins de eco exclusivas del trimestre, 1 modificador de run exclusivo (Rama B,
// temporal), desafíos semanales con recompensa doble, y un badge cosmético de perfil.
//
// De esas 4, este pase solo conecta el ESTADO real (activo/vencido, compra, persistencia)
// y el badge cosmético (EchoShopUI lo muestra). Las otras 3 dependen de sistemas que
// todavía no existen de forma conectada: la Rama B de modificadores de run no tiene NINGÚN
// efecto de gameplay conectado todavía (ver el comentario en ProgressionSystem — es deuda
// previa a este pase, no algo que Season Pass debería inventar), y los desafíos semanales
// necesitarían su propio sistema de rotación de contenido en vivo, que no tiene sentido
// hardcodear como si fuera contenido fijo de este VS. Conectar esas 3 piezas es trabajo
// aparte una vez existan los sistemas de los que dependen.
//
// GDD explícito: el pase NO otorga PC extra, NO otorga slots, NO otorga ventaja de
// gameplay — por diseño no hay nada acá que conecte a poder de juego.
[DefaultExecutionOrder(-93)]
public class SeasonPassSystem : MonoBehaviour
{
    public const string ProductId = "season_pass";
    public const string DisplayPrice = "$4.99/trimestre";
    private const int QuarterDays = 90;

    private SaveSystem _save;

    private void Awake() => Services.Register(this);
    private void Start() => _save = Services.Get<SaveSystem>();

    public bool IsActive
    {
        get
        {
            string iso = _save.Current.metaProgression.seasonPassExpiresAtUtc;
            if (string.IsNullOrEmpty(iso)) return false;
            return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expires)
                && DateTime.UtcNow < expires;
        }
    }

    public DateTime? ExpiresAtUtc
    {
        get
        {
            string iso = _save.Current.metaProgression.seasonPassExpiresAtUtc;
            if (string.IsNullOrEmpty(iso)) return null;
            return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expires) ? expires : (DateTime?)null;
        }
    }

    // Se llama desde MonetizationSystem tras una compra exitosa. Extiende desde el mayor
    // entre "ahora" y la expiración actual — comprar de nuevo antes de que venza el pase
    // suma un trimestre más en vez de perder el tiempo que quedaba.
    public void Activate()
    {
        var current = ExpiresAtUtc ?? DateTime.UtcNow;
        var baseTime = current > DateTime.UtcNow ? current : DateTime.UtcNow;
        _save.Current.metaProgression.seasonPassExpiresAtUtc = baseTime.AddDays(QuarterDays).ToString("o");
        _save.Save();
    }
}
