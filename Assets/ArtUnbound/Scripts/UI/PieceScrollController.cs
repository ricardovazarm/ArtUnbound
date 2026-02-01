using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.UI
{
    public class PieceScrollController : MonoBehaviour
    {
        [SerializeField] private float scrollStep = 0.1f;
        private readonly List<Transform> pieceItems = new List<Transform>();

        public void Initialize(List<Transform> pieces)
        {
            pieceItems.Clear();
            if (pieces != null)
            {
                pieceItems.AddRange(pieces);
            }
        }

        public void OnSwipe(float delta)
        {
            float offset = delta * scrollStep;
            for (int i = 0; i < pieceItems.Count; i++)
            {
                pieceItems[i].localPosition += new Vector3(offset, 0f, 0f);
            }
        }
    }
}
