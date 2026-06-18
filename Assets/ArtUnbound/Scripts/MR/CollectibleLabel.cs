using ArtUnbound.Data;
using TMPro;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Etiqueta de un coleccionable colgable (placa). Vive en el prefab visual asignado a un
    /// CollectibleDefinition. CollectibleFactory llama Apply() al instanciar el prefab para volcar
    /// titulo/condicion/fecha en los TMP del modelo, de modo que un mismo prefab de placa sirva
    /// para todos los coleccionables (el texto sale del asset, no del prefab).
    /// </summary>
    public class CollectibleLabel : MonoBehaviour
    {
        [Tooltip("Texto principal: nombre de la placa (ej. 'Rembrandt - Master').")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("Texto secundario opcional: condicion / descripcion.")]
        [SerializeField] private TMP_Text subtitleText;
        [Tooltip("Texto opcional de fecha de obtencion.")]
        [SerializeField] private TMP_Text dateText;

        [Header("Tier finish (tier de estatus)")]
        [Tooltip("Renderers cuyo material cambia con el tier. Si se deja vacio, se usan todos los del prefab.")]
        [SerializeField] private Renderer[] tieredRenderers;
        [Tooltip("Material por tier de estatus.")]
        [SerializeField] private Material cobreMaterial;
        [SerializeField] private Material plataMaterial;
        [SerializeField] private Material oroMaterial;
        [SerializeField] private Material platinoMaterial;

        /// <summary>Vuelca los textos del coleccionable en los TMP cableados (los nulos se ignoran).</summary>
        public void Apply(string title, string condition, string date = null)
        {
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (subtitleText != null) subtitleText.text = condition ?? string.Empty;
            if (dateText != null) dateText.text = string.IsNullOrEmpty(date) ? string.Empty : date;
        }

        /// <summary>
        /// Asigna el acabado del tier de estatus (Cobre/Plata/Oro/Platino) a los renderers cableados
        /// (o a todos los del prefab si el array esta vacio). Si el material del tier no esta asignado,
        /// conserva el actual sin romper nada. Los TMP usan CanvasRenderer, no Renderer: no se tocan.
        /// </summary>
        public void ApplyTier(PlayerTier tier)
        {
            Material mat = tier switch
            {
                PlayerTier.Platino => platinoMaterial,
                PlayerTier.Oro     => oroMaterial,
                PlayerTier.Plata   => plataMaterial,
                _                  => cobreMaterial,
            };
            if (mat == null) return;
            var targets = (tieredRenderers != null && tieredRenderers.Length > 0)
                ? tieredRenderers
                : GetComponentsInChildren<Renderer>(true);
            foreach (var r in targets)
                if (r != null) r.sharedMaterial = mat;
        }
    }
}
