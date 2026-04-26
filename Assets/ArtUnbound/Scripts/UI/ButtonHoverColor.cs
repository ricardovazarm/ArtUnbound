using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Ensures buttons change to #d4c089 when the XR ray hovers over them.
    /// Works with XR UI Input Module; uses IPointerEnter/Exit for reliable hover feedback.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class ButtonHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Selectable _selectable;
        private Graphic _targetGraphic;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _targetGraphic = _selectable?.targetGraphic as Graphic;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var semantic = GetComponent<SemanticSelectedButton>();
            if (semantic != null && semantic.IsSelected)
                return; // Already has correct color, don't change
            if (_targetGraphic != null && _selectable != null && _selectable.interactable)
                _targetGraphic.color = UIButtonTheme.HoverFillColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var semantic = GetComponent<SemanticSelectedButton>();
            if (semantic != null && semantic.IsSelected)
                return; // Keep current color
            if (_targetGraphic != null && _selectable != null)
            {
                var colors = _selectable.colors;
                _targetGraphic.color = _selectable.interactable ? colors.normalColor : colors.disabledColor;
            }
        }
    }
}
