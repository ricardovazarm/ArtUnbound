using System;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Maneja el desbloqueo UNICO del catalogo completo (modelo freemium del GDD 12):
    /// 12 obras gratis + una sola compra ($9.99) que desbloquea las 240+ restantes. No hay
    /// packs, bundles ni seccion de tienda; la compra se ofrece de forma CONTEXTUAL al abrir
    /// el detalle de una obra bloqueada. Sustituir el stub de PurchaseCatalog por Meta IAP.
    ///
    /// Nota: se conservan el nombre `PackPurchaseService` y `SaveData.purchasedPackIds` por
    /// compatibilidad con la escena y los saves existentes; internamente solo gestionan el SKU
    /// del catalogo completo.
    /// </summary>
    public class PackPurchaseService : MonoBehaviour
    {
        /// <summary>SKU unico que desbloquea todo el catalogo.</summary>
        public const string CatalogSku = "catalog_complete";

        [Header("Complete Catalog Purchase")]
        [Tooltip("Precio mostrado en el boton de comprar el catalogo completo.")]
        [SerializeField] private string catalogPrice = "$9.99";

        private SaveDataService saveDataService;

        public string CatalogPrice => catalogPrice;

        public void Initialize(SaveDataService sds)
        {
            saveDataService = sds;
        }

        /// <summary>True una vez que el usuario compro el catalogo completo.</summary>
        public bool IsCatalogPurchased() => IsPurchased(CatalogSku);

        /// <summary>
        /// Una obra esta bloqueada si no es gratis y el catalogo completo no se ha comprado.
        /// </summary>
        public bool IsArtworkLocked(ArtworkDefinition artwork)
            => artwork != null && !artwork.isFree && !IsCatalogPurchased();

        /// <summary>
        /// Inicia la compra del catalogo completo. Stub: marca el SKU como comprado y guarda.
        /// Reemplazar el cuerpo por Meta IAP para produccion.
        /// </summary>
        public void PurchaseCatalog(Action onSuccess, Action onFailure = null)
        {
            // --- Stub: conceder de inmediato ---
            saveDataService?.MarkAsPurchased(CatalogSku);
            Debug.Log("[PackPurchaseService] Catalogo completo comprado (stub).");
            onSuccess?.Invoke();
            // --- Fin stub ---

            // TODO: reemplazar por IAP real, p.ej. MetaIAP.Purchase(CatalogSku, onSuccess, onFailure);
        }

        private bool IsPurchased(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return saveDataService != null && saveDataService.IsPurchased(id);
        }
    }
}
