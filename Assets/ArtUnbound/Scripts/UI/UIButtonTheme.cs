using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Centralized button color theme. All buttons use:
    /// Normal: #896C4A | Hover/Selected: #d4c089
    /// </summary>
    public static class UIButtonTheme
    {
        public static readonly Color NormalColor = new Color(0.537f, 0.424f, 0.29f, 1f);   // #896C4A
        public static readonly Color HighlightColor = new Color(0.831f, 0.753f, 0.537f, 1f); // #d4c089

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
            colors.normalColor = NormalColor;
            colors.highlightedColor = HighlightColor;
            colors.pressedColor = HighlightColor;
            colors.selectedColor = HighlightColor;
            colors.disabledColor = new Color(NormalColor.r * 0.8f, NormalColor.g * 0.8f, NormalColor.b * 0.8f, 0.9f); // Keep normal color visible when disabled
            button.colors = colors;

            // Fix Image with transparent/invisible color (e.g. CatalogPageLeftButton after ratio was removed)
            var graphic = button.targetGraphic as Graphic;
            if (graphic != null && graphic.color.a < 0.01f)
                graphic.color = NormalColor;

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
