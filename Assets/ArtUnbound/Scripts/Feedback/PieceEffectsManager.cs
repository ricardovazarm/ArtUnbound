using UnityEngine;

namespace ArtUnbound.Feedback
{
    /// <summary>
    /// Manages visual effects for piece placement and puzzle completion.
    /// Creates particle effects when pieces are placed correctly.
    /// </summary>
    public class PieceEffectsManager : MonoBehaviour
    {
        public static PieceEffectsManager Instance { get; private set; }

        [Header("Particle Settings")]
        [SerializeField] private GameObject particlePrefab;
        [SerializeField] private Color correctPlacementColor = new Color(0.2f, 1f, 0.3f, 1f); // Green
        [SerializeField] private Color milestoneColor = new Color(1f, 0.85f, 0.2f, 1f); // Gold/amber
        [SerializeField] private Color wrongPlacementColor = new Color(1f, 0.15f, 0.1f, 1f); // Red
        // Unused particle settings - kept for future use if needed
        // [SerializeField] private int particleCount = 15;
        // [SerializeField] private float particleLifetime = 0.5f;
        // [SerializeField] private float particleSpeed = 0.5f;
        // [SerializeField] private float particleSize = 0.02f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Spawns particles at the specified position when a piece is correctly placed.
        /// </summary>
        public void PlayCorrectPlacementEffect(Vector3 position)
        {
            // Use simple burst effect which is more reliable and performant
            PlaySimpleBurstEffect(position);
        }

        /// <summary>
        /// Plays milestone effect: larger particle burst.
        /// Text is shown on the HUD via PuzzleHUDController.ShowMilestoneText.
        /// </summary>
        public void PlayMilestoneEffect(Vector3 position, string message)
        {
            PlayMilestoneBurstEffect(position);
        }

        /// <summary>
        /// Larger burst effect for milestones (gold/amber color).
        /// </summary>
        private void PlayMilestoneBurstEffect(Vector3 position)
        {
            for (int i = 0; i < 16; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
                sphere.transform.localScale = Vector3.one * 0.015f;

                Destroy(sphere.GetComponent<Collider>());

                var renderer = sphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = milestoneColor;

                var animator = sphere.AddComponent<SimpleBurstAnimator>();
                animator.Initialize(Random.onUnitSphere * 0.15f, 0.7f);
            }
        }

        /// <summary>
        /// Red burst effect to highlight a wrongly placed piece.
        /// </summary>
        public void PlayWrongPlacementHighlight(Vector3 position)
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
                sphere.transform.localScale = Vector3.one * 0.012f;
                Destroy(sphere.GetComponent<Collider>());
                var renderer = sphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = wrongPlacementColor;
                var animator = sphere.AddComponent<SimpleBurstAnimator>();
                animator.Initialize(Random.onUnitSphere * 0.08f, 0.5f);
            }
        }

        /// <summary>
        /// Creates a simple burst effect for correct placement.
        /// This is a fallback if particle system doesn't work well.
        /// </summary>
        public void PlaySimpleBurstEffect(Vector3 position)
        {
            // Create small spheres that expand and fade
            for (int i = 0; i < 8; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = position;
                sphere.transform.localScale = Vector3.one * 0.01f;

                // Remove collider
                Destroy(sphere.GetComponent<Collider>());

                // Set color
                var renderer = sphere.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = correctPlacementColor;

                // Add animation component
                var animator = sphere.AddComponent<SimpleBurstAnimator>();
                animator.Initialize(Random.onUnitSphere * 0.1f, 0.5f);
            }
        }

        /// <summary>
        /// Simple component to animate burst spheres.
        /// </summary>
        private class SimpleBurstAnimator : MonoBehaviour
        {
            private Vector3 direction;
            private float lifetime;
            private float elapsed;
            private Material material;

            public void Initialize(Vector3 dir, float life)
            {
                direction = dir;
                lifetime = life;
                elapsed = 0f;
                material = GetComponent<Renderer>().material;
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / lifetime;

                if (progress >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }

                // Move outward
                transform.position += direction * Time.deltaTime;

                // Fade out
                Color color = material.color;
                color.a = 1f - progress;
                material.color = color;

                // Scale down
                transform.localScale = Vector3.one * 0.01f * (1f - progress);
            }
        }
    }
}
