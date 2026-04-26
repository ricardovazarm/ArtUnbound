using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Centralized button color theme.
    ///
    /// Brand colors (visibles, opacos) — usados por elementos 3D (TrayScrollButtons)
    /// y como referencia visual:
    ///   NormalColor    = #896C4A
    ///   HighlightColor = #d4c089
    ///
    /// Estados de botones de panel (Unity UI Button.ColorBlock):
    ///   Normal/Selected/Disabled = transparente
    ///   Hover/Pressed            = #896C4A con alpha 0.5 (cafe semi-transparente)
    /// </summary>
    public static class UIButtonTheme
    {
        public static readonly Color NormalColor = new Color(0.537f, 0.424f, 0.29f, 1f);   // #896C4A
        public static readonly Color HighlightColor = new Color(0.831f, 0.753f, 0.537f, 1f); // #d4c089

        // Panel button states
        public static readonly Color RestColor = new Color(0f, 0f, 0f, 0f);                       // transparente
        public static readonly Color HoverFillColor = new Color(0.537f, 0.424f, 0.29f, 0.5f);     // #896C4A @ 50%

        /// <summary>
        /// Applies the theme to a Unity UI Button (normal, hover, pressed, selected).
        /// Skips buttons with StatefulButton; those use UIButtonStatefullTheme.
        /// </summary>
        public static void ApplyTo(Button button)
        {
            if (button == null) return;
            if (button.GetComponent<StatefulButton>() != null)
                return;

            var colors = button.colors;
            colors.normalColor = RestColor;
            colors.highlightedColor = HoverFillColor;
            colors.pressedColor = HoverFillColor;
            colors.selectedColor = RestColor;
            colors.disabledColor = RestColor;
            button.colors = colors;

            // Initialize the target graphic to the rest (transparent) state so the
            // visual matches the ColorBlock at startup, regardless of what the prefab had.
            var graphic = button.targetGraphic as Graphic;
            if (graphic != null)
                graphic.color = RestColor;

            if (button.GetComponent<ButtonHoverColor>() == null)
                button.gameObject.AddComponent<ButtonHoverColor>();
        }

        /// <summary>
        /// Applies the theme to all Buttons under the given transform (recursive).
        /// </summary>
        public static void ApplyToAllIn(Transform root)
        {
            if (root == null) return;

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                ApplyTo(button);
            }
        }
    }
}
