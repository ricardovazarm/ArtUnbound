using UnityEngine;


namespace ArtUnbound.UI
{
    /// <summary>
    /// Creates 3D scroll buttons (up/down arrows) positioned above and below the vertical tray.
    /// These are world-space interactable objects, not UI Canvas elements.
    /// </summary>
    public class TrayScrollButtons : MonoBehaviour
    {
        [SerializeField] private PieceScrollController scrollController;
        [SerializeField] private float buttonSize = 0.06f; // 6cm button size
        [SerializeField] private float buttonOffsetX = 0.04f; // Horizontal distance between buttons
        [SerializeField] private float buttonOffsetY = -0.35f; // Position below the tray (10cm higher than before)
        [SerializeField] private float buttonOffsetZ = 0.03f; // Bring buttons closer to user

        private GameObject upButton;
        private GameObject downButton;

        private void Start()
        {
            if (scrollController == null)
            {
                scrollController = GetComponent<PieceScrollController>();
            }

            if (scrollController == null)
            {
                Debug.LogError("[TrayScrollButtons] No PieceScrollController found!");
                return;
            }

            // Don't create buttons yet - wait until puzzle starts
            // Buttons will be created when Initialize() is called
        }

        private void OnDestroy()
        {
            if (upButton != null) Destroy(upButton);
            if (downButton != null) Destroy(downButton);
        }

        /// <summary>
        /// Call this to create the scroll buttons (should be called when puzzle starts)
        /// </summary>
        public void Initialize()
        {
            if (upButton != null || downButton != null)
            {
                // Already created
                return;
            }

            CreateScrollButtons();
        }

        /// <summary>
        /// Hide the scroll buttons
        /// </summary>
        public void Hide()
        {
            if (upButton != null) upButton.SetActive(false);
            if (downButton != null) downButton.SetActive(false);
        }

        /// <summary>
        /// Show the scroll buttons
        /// </summary>
        public void Show()
        {
            if (upButton != null) upButton.SetActive(true);
            if (downButton != null) downButton.SetActive(true);
        }

        /// <summary>
        /// Enable or disable scroll buttons based on scroll limits
        /// </summary>
        public void SetButtonStates(bool canScrollUp, bool canScrollDown)
        {
            if (upButton != null)
            {
                var renderer = upButton.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var dimmed = new Color(UIButtonTheme.NormalColor.r * 0.5f, UIButtonTheme.NormalColor.g * 0.5f, UIButtonTheme.NormalColor.b * 0.5f, 0.5f);
                    renderer.material.color = canScrollUp ? UIButtonTheme.NormalColor : dimmed;
                }
                var interactable = upButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                if (interactable != null)
                    interactable.enabled = canScrollUp;
            }
            if (downButton != null)
            {
                var renderer = downButton.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var dimmed = new Color(UIButtonTheme.NormalColor.r * 0.5f, UIButtonTheme.NormalColor.g * 0.5f, UIButtonTheme.NormalColor.b * 0.5f, 0.5f);
                    renderer.material.color = canScrollDown ? UIButtonTheme.NormalColor : dimmed;
                }
                var interactable = downButton.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                if (interactable != null)
                    interactable.enabled = canScrollDown;
            }
        }

        private void CreateScrollButtons()
        {
            // UP button (left side) - positioned to the left of center
            upButton = CreateButton("ScrollUpButton", new Vector3(-buttonOffsetX - buttonSize/2, buttonOffsetY, buttonOffsetZ), "↑");
            AddInteraction(upButton, () => scrollController.ScrollUp());

            // DOWN button (right side) - positioned to the right of center
            downButton = CreateButton("ScrollDownButton", new Vector3(buttonOffsetX + buttonSize/2, buttonOffsetY, buttonOffsetZ), "↓");
            AddInteraction(downButton, () => scrollController.ScrollDown());
        }

        private GameObject CreateButton(string name, Vector3 localPosition, string arrowText)
        {
            // Create a cube as the button base
            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
            button.name = name;
            button.transform.SetParent(transform, false);
            button.transform.localPosition = localPosition;
            button.transform.localScale = new Vector3(buttonSize, buttonSize, 0.01f); // Thin button

            Renderer renderer = button.GetComponent<Renderer>();
            Material buttonMat = new Material(Shader.Find("Unlit/Color"));
            buttonMat.color = UIButtonTheme.NormalColor;
            renderer.material = buttonMat;

            // Add collider for interaction (already has one from CreatePrimitive)
            button.layer = LayerMask.NameToLayer("Interactable"); // Use Interactable layer if it exists

            // Create text label (3D TextMesh) - Balanced size to fit in button
            GameObject textObj = new GameObject($"{name}_Text");
            textObj.transform.SetParent(button.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.006f); // Slightly in front
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = Vector3.one * 0.05f; // 5% of parent - balanced size

            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = arrowText;
            textMesh.fontSize = 48; // Medium font size
            textMesh.color = new Color(0.92f, 0.92f, 0.92f, 1f); // Soft gray (Meta: avoid pure white)
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 1.0f; // Normal character size

            return button;
        }

        private void AddInteraction(GameObject button, System.Action onPress)
        {
            var interactable = button.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var renderer = button.GetComponent<Renderer>();

            interactable.hoverEntered.AddListener(_ => { if (renderer != null) renderer.material.color = UIButtonTheme.HighlightColor; });
            interactable.hoverExited.AddListener(_ => { if (renderer != null) renderer.material.color = UIButtonTheme.NormalColor; });

            interactable.selectEntered.AddListener((args) =>
            {
                if (ArtUnbound.Feedback.AudioManager.Instance != null)
                    ArtUnbound.Feedback.AudioManager.Instance.PlayButtonClick();
                onPress?.Invoke();
                StartCoroutine(FlashButton(button));
            });
        }

        private System.Collections.IEnumerator FlashButton(GameObject button)
        {
            Renderer renderer = button.GetComponent<Renderer>();
            if (renderer == null) yield break;

            Color originalColor = renderer.material.color;
            renderer.material.color = Color.white; // Flash white

            yield return new UnityEngine.WaitForSeconds(0.1f);

            renderer.material.color = originalColor;
        }
    }
}
