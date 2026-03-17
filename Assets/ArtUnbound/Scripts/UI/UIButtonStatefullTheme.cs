using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Theme for buttons with selected/unselected state (filter tabs, difficulty with progress).
    /// Normal = unselected or selected color; Hover = same as selected (so selected buttons don't change on hover).
    /// </summary>
    public static class UIButtonStatefullTheme
    {
        public static readonly Color UnselectedColor = new Color(0.537f, 0.424f, 0.29f, 1f);   // #896C4A
        public static readonly Color SelectedColor = new Color(0.831f, 0.753f, 0.537f, 1f);   // #d4c089 (same as hover)

        /// <summary>
        /// Applies the stateful theme. Selected: normal = SelectedColor, hover = same (no change).
        /// Unselected: normal = UnselectedColor, hover = SelectedColor.
        /// </summary>
        public static void ApplyTo(Button button, bool isSelected)
        {
            if (button == null) return;

            var semantic = button.GetComponent<SemanticSelectedButton>();
            if (semantic == null) semantic = button.gameObject.AddComponent<SemanticSelectedButton>();
            semantic.IsSelected = isSelected;

            Color normalColor = isSelected ? SelectedColor : UnselectedColor;
            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = SelectedColor;
            colors.pressedColor = SelectedColor;
            colors.selectedColor = SelectedColor;
            colors.disabledColor = new Color(UnselectedColor.r * 0.8f, UnselectedColor.g * 0.8f, UnselectedColor.b * 0.8f, 0.9f);
            button.colors = colors;

            var image = button.targetGraphic as Graphic;
            if (image != null)
                image.color = normalColor;

            if (button.GetComponent<ButtonHoverColor>() == null)
                button.gameObject.AddComponent<ButtonHoverColor>();
        }
    }
}
