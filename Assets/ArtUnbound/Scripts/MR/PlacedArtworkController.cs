using System;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Controls a placed/hung artwork in the user's space.
    /// </summary>
    public class PlacedArtworkController : MonoBehaviour
    {
        public event Action<PlacedArtworkController> OnInteracted;
        public event Action<PlacedArtworkController> OnRelocateRequested;
        public event Action<PlacedArtworkController> OnRemoveRequested;

        [Header("Visual Components")]
        [SerializeField] private MeshRenderer artworkRenderer;
        [SerializeField] private MeshRenderer frameRenderer;
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private GameObject interactionPrompt;

        [Header("Frame Meshes")]
        [SerializeField] private Mesh frameMaderaMesh;
        [SerializeField] private Mesh frameBronceMesh;
        [SerializeField] private Mesh framePlataMesh;
        [SerializeField] private Mesh frameOroMesh;
        [SerializeField] private Mesh frameEbanoMesh;

        [Header("Frame Materials")]
        [SerializeField] private Material frameMaderaMaterial;
        [SerializeField] private Material frameBronceMaterial;
        [SerializeField] private Material framePlataMaterial;
        [SerializeField] private Material frameOroMaterial;
        [SerializeField] private Material frameEbanoMaterial;

        [Header("Interaction")]
        [SerializeField] private float interactionDistance = 1.5f;
        [SerializeField] private float gazeDuration = 1.0f;

        public PlacedArtwork Data => artworkData;
        public string ArtworkId => artworkData?.artworkId;
        public bool IsHighlighted => isHighlighted;

        private PlacedArtwork artworkData;
        private bool isHighlighted = false;
        private bool isBeingGazedAt = false;
        private float gazeTimer = 0f;
        private Transform userHead;

        private void Awake()
        {
            if (highlightEffect != null)
                highlightEffect.SetActive(false);

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            // Find the main camera as user head reference
            var mainCam = Camera.main;
            if (mainCam != null)
                userHead = mainCam.transform;
        }

        private void Update()
        {
            UpdateProximityInteraction();
            UpdateGazeInteraction();
        }

        /// <summary>
        /// Initializes the placed artwork with data.
        /// </summary>
        public void Initialize(PlacedArtwork data)
        {
            artworkData = data;

            // Set position and rotation
            transform.position = data.GetPosition();
            transform.rotation = data.GetRotation();
            transform.localScale = new Vector3(data.scale, data.scale, data.scale);

            // Set frame
            SetFrameTier(data.frameTier);
        }

        /// <summary>
        /// Sets the artwork texture.
        /// </summary>
        public void SetArtworkTexture(Texture2D texture)
        {
            if (artworkRenderer != null && texture != null)
            {
                Material mat = artworkRenderer.material;
                mat.mainTexture = texture;
            }
        }

        /// <summary>
        /// Sets the frame tier visual.
        /// </summary>
        public void SetFrameTier(FrameTier tier)
        {
            if (frameRenderer == null) return;

            MeshFilter meshFilter = frameRenderer.GetComponent<MeshFilter>();

            // Set mesh
            if (meshFilter != null)
            {
                meshFilter.mesh = GetFrameMesh(tier);
            }

            // Set material
            frameRenderer.material = GetFrameMaterial(tier);
        }

        private Mesh GetFrameMesh(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => frameMaderaMesh,
                FrameTier.Bronce => frameBronceMesh,
                FrameTier.Plata => framePlataMesh,
                FrameTier.Oro => frameOroMesh,
                FrameTier.Ebano => frameEbanoMesh,
                _ => frameMaderaMesh
            };
        }

        private Material GetFrameMaterial(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Madera => frameMaderaMaterial,
                FrameTier.Bronce => frameBronceMaterial,
                FrameTier.Plata => framePlataMaterial,
                FrameTier.Oro => frameOroMaterial,
                FrameTier.Ebano => frameEbanoMaterial,
                _ => frameMaderaMaterial
            };
        }

        private void UpdateProximityInteraction()
        {
            if (userHead == null) return;

            float distance = Vector3.Distance(transform.position, userHead.position);
            bool isClose = distance <= interactionDistance;

            if (isClose && !isHighlighted)
            {
                SetHighlighted(true);
            }
            else if (!isClose && isHighlighted && !isBeingGazedAt)
            {
                SetHighlighted(false);
            }
        }

        private void UpdateGazeInteraction()
        {
            if (userHead == null || !isHighlighted) return;

            // Check if user is looking at the artwork
            Vector3 directionToArtwork = (transform.position - userHead.position).normalized;
            float dot = Vector3.Dot(userHead.forward, directionToArtwork);

            bool isLookingAt = dot > 0.9f; // Within ~25 degrees

            if (isLookingAt)
            {
                if (!isBeingGazedAt)
                {
                    isBeingGazedAt = true;
                    gazeTimer = 0f;
                }

                gazeTimer += Time.deltaTime;

                if (gazeTimer >= gazeDuration)
                {
                    // Gaze completed - show interaction prompt
                    if (interactionPrompt != null)
                        interactionPrompt.SetActive(true);
                }
            }
            else
            {
                isBeingGazedAt = false;
                gazeTimer = 0f;

                if (interactionPrompt != null)
                    interactionPrompt.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the highlighted state.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;

            if (highlightEffect != null)
                highlightEffect.SetActive(highlighted);

            if (!highlighted && interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }

        /// <summary>
        /// Called when the user interacts with this artwork.
        /// </summary>
        public void OnInteract()
        {
            OnInteracted?.Invoke(this);
        }

        /// <summary>
        /// Requests relocation of this artwork.
        /// </summary>
        public void RequestRelocate()
        {
            OnRelocateRequested?.Invoke(this);
        }

        /// <summary>
        /// Requests removal of this artwork.
        /// </summary>
        public void RequestRemove()
        {
            OnRemoveRequested?.Invoke(this);
        }

        /// <summary>
        /// Updates the position and rotation data.
        /// </summary>
        public void UpdatePlacement(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;

            if (artworkData != null)
            {
                artworkData.SetPosition(position);
                artworkData.SetRotation(rotation);
            }
        }

        /// <summary>
        /// Updates the scale.
        /// </summary>
        public void UpdateScale(float scale)
        {
            transform.localScale = new Vector3(scale, scale, scale);

            if (artworkData != null)
            {
                artworkData.scale = scale;
            }
        }

        /// <summary>
        /// Gets the current placement data.
        /// </summary>
        public PlacedArtwork GetPlacementData()
        {
            return artworkData;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
