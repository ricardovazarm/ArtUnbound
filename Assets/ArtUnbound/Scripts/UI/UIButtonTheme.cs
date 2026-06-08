using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Centralized brand color constants.
    ///
    /// El sistema que repintaba botones en runtime (ApplyTo/ApplyToAllIn +
    /// ButtonHoverColor) fue retirado: ahora cada boton conserva los colores que
    /// tiene en el editor. Solo se conservan las constantes de marca para los
    /// componentes que todavia las referencian (ej. StoreTabsController).
    /// </summary>
    public static class UIButtonTheme
    {
        public static readonly Color NormalColor = new Color(0.537f, 0.424f, 0.29f, 1f);   // #896C4A
        public static readonly Color HighlightColor = new Color(0.831f, 0.753f, 0.537f, 1f); // #d4c089

        // Panel button states
        public static readonly Color RestColor = new Color(0f, 0f, 0f, 0f);                       // transparente
        public static readonly Color HoverFillColor = new Color(0.537f, 0.424f, 0.29f, 0.5f);     // #896C4A @ 50%
    }
}
