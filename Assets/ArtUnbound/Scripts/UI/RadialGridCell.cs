using System;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Una celda del Radial Grid Gallery.
    /// Muestra el thumbnail de una obra usando SpriteRenderer en world-space.
    /// La opacidad y escala se calculan según la distancia radial al centro del grid.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class RadialGridCell : MonoBehaviour
    {
        // ── Umbrales de zona (distancia normalizada desde el centro: 0=centro, 1=borde)
        public const float ZONE_A_MAX = 0.28f;   // Nítido + seleccionable
        public const float ZONE_B_MAX = 0.50f;   // Leve fade + seleccionable
        public const float ZONE_C_MAX = 0.72f;   // Fade medio — solo visual
        public const float ZONE_D_MAX = 0.88f;   // Fade fuerte — solo visual
        // > ZONE_D_MAX → fade hasta transparente

        // ── Colores
        private static readonly Color ColorNormal    = Color.white;
        private static readonly Color ColorHighlight = new Color(1.15f, 1.15f, 0.85f, 1f); // cálido al hacer hover

        private SpriteRenderer _sr;

        // ── Coordenadas virtuales en el grid (columna, fila)
        public int  GridX     { get; private set; }
        public int  GridY     { get; private set; }

        // ── Artwork asignada a esta celda
        public ArtworkDefinition Artwork    { get; private set; }
        public bool              HasArtwork => Artwork != null;

        // ── ¿Puede el usuario seleccionarla con pinch?
        public bool IsSelectable { get; private set; }

        /// <summary>Se dispara cuando el usuario hace pinch sobre esta celda.</summary>
        public event Action<RadialGridCell> OnCellSelected;

        // Escala base calculada a partir del tamaño del sprite y cellSize deseado
        private float _baseScale = 1f;

        // ─────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.sortingLayerName = "Default";
            _sr.sortingOrder     = 10;
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Asigna una obra a la celda y calcula la escala base para que quepa
        /// en un cuadro de <paramref name="cellWorldSize"/> metros de lado.
        /// </summary>
        public void SetArtwork(ArtworkDefinition artwork, int gx, int gy, float cellWorldSize)
        {
            GridX   = gx;
            GridY   = gy;
            Artwork = artwork;

            Sprite sprite = artwork?.thumbnail ?? artwork?.fullImage;
            _sr.sprite = sprite;

            if (sprite != null)
            {
                // bounds.size está en world-units (píxeles / pixelsPerUnit)
                float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                _baseScale   = maxDim > 0f ? cellWorldSize / maxDim : cellWorldSize;
            }
            else
            {
                _baseScale = cellWorldSize;
            }

            _sr.color = ColorNormal;
            gameObject.SetActive(artwork != null);
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Actualiza opacidad y escala según la distancia normalizada al centro
        /// del grid (0 = justo en el centro, 1 = en el borde del círculo visible).
        /// </summary>
        public void UpdateRadialVisuals(float normalizedDist)
        {
            float alpha, radialScale;

            if (normalizedDist <= ZONE_A_MAX)
            {
                alpha = 1.0f;  radialScale = 1.0f;  IsSelectable = true;
            }
            else if (normalizedDist <= ZONE_B_MAX)
            {
                float t  = Mathf.InverseLerp(ZONE_A_MAX, ZONE_B_MAX, normalizedDist);
                alpha    = Mathf.Lerp(1.00f, 0.72f, t);
                radialScale = Mathf.Lerp(1.00f, 0.93f, t);
                IsSelectable = true;
            }
            else if (normalizedDist <= ZONE_C_MAX)
            {
                float t  = Mathf.InverseLerp(ZONE_B_MAX, ZONE_C_MAX, normalizedDist);
                alpha    = Mathf.Lerp(0.72f, 0.38f, t);
                radialScale = Mathf.Lerp(0.93f, 0.84f, t);
                IsSelectable = false;
            }
            else if (normalizedDist <= ZONE_D_MAX)
            {
                float t  = Mathf.InverseLerp(ZONE_C_MAX, ZONE_D_MAX, normalizedDist);
                alpha    = Mathf.Lerp(0.38f, 0.10f, t);
                radialScale = Mathf.Lerp(0.84f, 0.75f, t);
                IsSelectable = false;
            }
            else
            {
                float t  = Mathf.InverseLerp(ZONE_D_MAX, 1.0f, normalizedDist);
                alpha    = Mathf.Lerp(0.10f, 0.00f, Mathf.Clamp01(t));
                radialScale = 0.74f;
                IsSelectable = false;
            }

            // Aplica opacidad manteniendo el color base
            Color c = _sr.color;
            c.a     = alpha;
            _sr.color = c;

            // Escala = escala_base × factor_radial
            float finalScale = _baseScale * radialScale;
            transform.localScale = new Vector3(finalScale, finalScale, 1f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>Resalta la celda visualmente (hover / pre-selección).</summary>
        public void SetHighlight(bool on)
        {
            Color c   = on ? ColorHighlight : ColorNormal;
            c.a       = _sr.color.a;   // mantener alfa radial
            _sr.color = c;
        }

        // ─────────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Llamado por el controlador cuando el usuario hace pinch sobre esta celda.
        /// Solo dispara el evento si la celda es seleccionable y tiene artwork.
        /// </summary>
        public void TriggerSelect()
        {
            if (IsSelectable && HasArtwork)
                OnCellSelected?.Invoke(this);
        }
    }
}
