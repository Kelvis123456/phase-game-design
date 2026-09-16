using UnityEngine;

// GDD §14.2 — 3 modos daltónicos + normal, seleccionables en Options (GDD §14.1: feature
// obligatorio para el rating Bronze de accesibilidad). El GDD especifica remapeos por HEX
// exacto sobre una paleta de eco (§11.3) que nunca se implementó tal cual en el VS —
// EchoManager usa su propia paleta de 5 colores (Cyan/Violet/Ember/Verdant/Magenta).
// Remapeamos por INTENCIÓN (qué par de colores es indistinguible para cada tipo de
// daltonismo), no por igualdad literal de hex contra un palette que no existe acá.
public static class ColorblindPalette
{
    public const string Normal = "normal";
    public const string DeuteranopiaProtanopia = "deuteranopia";
    public const string Tritanopia = "tritanopia";
    public const string HighContrast = "high_contrast";

    public static readonly string[] AllModes = { Normal, DeuteranopiaProtanopia, Tritanopia, HighContrast };

    public static string DisplayName(string mode) => mode switch
    {
        DeuteranopiaProtanopia => "Deuteranopia/Protanopia",
        Tritanopia => "Tritanopia",
        HighContrast => "Alto contraste",
        _ => "Normal",
    };

    // baseColors = EchoManager.EchoColors (slots 0-4: Cyan, Violet, Ember, Verdant, Magenta).
    public static Color[] Apply(Color[] baseColors, string mode)
    {
        var result = (Color[])baseColors.Clone();
        switch (mode)
        {
            case DeuteranopiaProtanopia:
                // Rojo-verde: el par problemático es Ember (naranja, slot 2) vs Verdant
                // (verde, slot 3) — casi indistinguibles para este tipo de daltonismo.
                result[2] = new Color(0f, 0.75f, 1f, 1f);    // azul brillante
                result[3] = new Color(1f, 0.55f, 0.26f, 1f); // naranja, ya distinto de Ember
                break;
            case Tritanopia:
                // Azul-amarillo: Cyan (slot 0) y Violet (slot 1, con componente azul) son
                // el par problemático acá.
                result[0] = new Color(1f, 0.9f, 0.4f, 1f);   // amarillo
                result[1] = new Color(1f, 0.53f, 0.78f, 1f); // rosa
                break;
            case HighContrast:
                // GDD §14.2: todos los ecos en blanco, diferenciación solo por forma de
                // ícono — el ícono en sí (Eco Strip, GDD §11.2.3) todavía no existe en el
                // VS, así que esto por ahora solo garantiza que el color deja de confundir.
                for (int i = 0; i < result.Length; i++) result[i] = Color.white;
                break;
        }
        return result;
    }
}
