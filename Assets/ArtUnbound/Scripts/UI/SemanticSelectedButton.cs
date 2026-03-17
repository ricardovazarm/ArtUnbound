using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Marks a button as "semantically selected" (e.g. filter tab, difficulty with progress).
    /// When true, ButtonHoverColor skips the hover effect since the button already has the correct color.
    /// </summary>
    public class SemanticSelectedButton : MonoBehaviour
    {
        public bool IsSelected { get; set; }
    }
}
