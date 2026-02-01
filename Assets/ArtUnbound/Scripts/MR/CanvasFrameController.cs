using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.MR
{
    public class CanvasFrameController : MonoBehaviour
    {
        [SerializeField] private BoxCollider boundsCollider;
        [SerializeField] private Transform frameRoot;

        public Vector2 CurrentSize { get; private set; }
        public Orientation CurrentOrientation { get; private set; } = Orientation.Portrait;

        private void Awake()
        {
            if (boundsCollider == null)
            {
                boundsCollider = GetComponentInChildren<BoxCollider>();
            }
        }

        public void SetBaseSize(ArtworkDefinition definition, Orientation orientation)
        {
            if (definition == null)
            {
                return;
            }

            CurrentOrientation = orientation;
            CurrentSize = definition.GetBaseSize(orientation);

            if (frameRoot != null)
            {
                frameRoot.localScale = new Vector3(CurrentSize.x, CurrentSize.y, frameRoot.localScale.z);
            }

            if (boundsCollider != null)
            {
                boundsCollider.size = new Vector3(CurrentSize.x, CurrentSize.y, boundsCollider.size.z);
            }
        }

        public bool IsInsideFrame(Vector3 worldPosition)
        {
            if (boundsCollider == null)
            {
                return false;
            }

            return boundsCollider.bounds.Contains(worldPosition);
        }
    }
}
