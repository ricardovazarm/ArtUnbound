using ArtUnbound.Data;
using TMPro;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Etiqueta de cedula de museo (GDD 8.2). Vive en el prefab de cedula asignado en GameBootstrap.
    /// PresentationDecorator llama Apply() para volcar titulo / autor / (ano - movimiento - museo) de
    /// la obra, y ApplyTier() para asignar el acabado segun el tier de estatus del jugador
    /// (Cobre/Plata/Oro/Platino), igual que las placas y el marco. Asi un mismo molde sirve para todas
    /// las obras: la informacion y el material se inyectan al instanciar.
    /// </summary>
    public class CedulaLabel : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text authorText;
        [Tooltip("Linea compuesta: Ano - Movimiento - Museo.")]
        [SerializeField] private TMP_Text infoText;

        [Header("Tier finish (tier de estatus)")]
        [Tooltip("Renderers cuyo material cambia con el tier (placa + remaches).")]
        [SerializeField] private Renderer[] tieredRenderers;
        [Tooltip("Material por tier de estatus.")]
        [SerializeField] private Material cobreMaterial;
        [SerializeField] private Material plataMaterial;
        [SerializeField] private Material oroMaterial;
        [SerializeField] private Material platinoMaterial;

        /// <summary>Vuelca los 3 renglones de la obra (los TMP nulos se ignoran).</summary>
        public void Apply(string title, string author, string info)
        {
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (authorText != null) authorText.text = author ?? string.Empty;
            if (infoText != null) infoText.text = info ?? string.Empty;
        }

        /// <summary>
        /// Asigna el acabado del tier de estatus a todos los renderers cableados. Si el material del
        /// tier no esta asignado, conserva el material actual (el del editor) sin romper nada.
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
            if (mat == null || tieredRenderers == null) return;
            foreach (var r in tieredRenderers)
                if (r != null) r.sharedMaterial = mat;
        }
    }
}
