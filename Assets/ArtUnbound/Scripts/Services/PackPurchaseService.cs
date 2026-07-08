using System;
using ArtUnbound.Data;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;
// Desambiguar tipos que chocan entre UnityEngine/ArtUnbound.Core y Oculus.Platform:
using Application = UnityEngine.Application;
using PlatformCore = Oculus.Platform.Core;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Maneja el desbloqueo UNICO del catalogo completo (modelo freemium del GDD 12):
    /// 12 obras gratis + una sola compra ($9.99) que desbloquea las 240+ restantes. No hay
    /// packs, bundles ni seccion de tienda; la compra se ofrece de forma CONTEXTUAL al abrir
    /// el detalle de una obra bloqueada.
    ///
    /// Integra Meta IAP (Oculus.Platform.IAP) con el SKU durable "catalog_complete":
    ///   - InitializePlatform: inicializa el Platform SDK, verifica entitlement, carga precio
    ///     real (GetProductsBySKU) y restaura la compra desde Meta (GetViewerPurchases).
    ///   - PurchaseCatalog: lanza el checkout nativo (LaunchCheckoutFlow). En el Editor concede
    ///     de inmediato (no hay sesion de plataforma) para poder iterar la UI.
    ///
    /// Fuente de verdad del entitlement = Meta, no el save local. El save (purchasedPackIds) es
    /// solo una cache: al arrancar se re-sincroniza desde GetViewerPurchases, por lo que la compra
    /// sobrevive reinstalaciones.
    ///
    /// Nota: se conservan el nombre `PackPurchaseService` y `SaveData.purchasedPackIds` por
    /// compatibilidad con la escena y los saves existentes; internamente solo gestionan el SKU
    /// del catalogo completo.
    /// </summary>
    public class PackPurchaseService : MonoBehaviour
    {
        /// <summary>SKU unico (durable) que desbloquea todo el catalogo. Debe coincidir letra por
        /// letra con el add-on creado en el Developer Dashboard de Meta.</summary>
        public const string CatalogSku = "catalog_complete";

        [Header("Complete Catalog Purchase")]
        [Tooltip("Precio de respaldo mostrado si Meta aun no devolvio el precio localizado real. " +
                 "DIAGNOSTICO: dejalo en un valor obvio (ej. 'FALLBACK') para distinguir a simple " +
                 "vista si el precio viene de Meta o no.")]
        [SerializeField] private string catalogPrice = "FALLBACK";

        private SaveDataService saveDataService;

        // Estado del Platform SDK (solo en device; en Editor se usa el fallback).
        private bool _initStarted;
        private bool _platformReady;

        // Precio localizado devuelto por Meta (politica: no hardcodear precios). Vacio = usa el de respaldo.
        private string _metaFormattedPrice;

        /// <summary>Se dispara cuando cambia el estado de compra/entitlement (compra exitosa o
        /// restauracion asincrona desde Meta). La UI lo usa para refrescar candados y precio.</summary>
        public event Action OnPurchaseStateChanged;

        /// <summary>Precio a mostrar: el localizado de Meta si esta disponible, si no el de respaldo.</summary>
        public string CatalogPrice => string.IsNullOrEmpty(_metaFormattedPrice) ? catalogPrice : _metaFormattedPrice;

        public void Initialize(SaveDataService sds)
        {
            saveDataService = sds;
        }

        /// <summary>
        /// Arranca el Platform SDK de Meta. Llamar una vez al inicio (GameBootstrap). En el Editor
        /// no hace nada: no hay sesion de plataforma, asi que la compra usa el fallback de desarrollo.
        /// </summary>
        public void InitializePlatform()
        {
            if (Application.isEditor)
                return;

            try
            {
                // El App ID se toma de Oculus/Platform Settings (Meta > Platform > Edit Settings).
                Debug.Log($"[PackPurchaseService] InitializePlatform: SKU='{CatalogSku}', platform={Application.platform}. Llamando Core.AsyncInitialize...");
                PlatformCore.AsyncInitialize();
                _initStarted = true;
                Debug.Log("[PackPurchaseService] Platform SDK init solicitado. Esperando IsInitialized...");
            }
            catch (Exception e)
            {
                Debug.LogError($"[PackPurchaseService] No se pudo iniciar el Platform SDK: {e.Message}\n{e}");
            }
        }

        private void Update()
        {
            // El Platform SDK requiere bombear los callbacks cada frame para que .OnComplete dispare.
            if (!_initStarted)
                return;

            Request.RunCallbacks();

            if (!_platformReady && PlatformCore.IsInitialized())
            {
                _platformReady = true;
                OnPlatformReady();
            }
        }

        private void OnPlatformReady()
        {
            Debug.Log("[PackPurchaseService] Platform SDK INICIALIZADO OK (Core.IsInitialized=true).");

            // Verificar que el usuario tiene derecho a la app (entitlement). En desarrollo solo se
            // registra; no forzamos salida para no estorbar las pruebas con test users.
            Entitlements.IsUserEntitledToApplication().OnComplete(msg =>
            {
                if (msg.IsError)
                    Debug.LogError($"[PackPurchaseService] ENTITLEMENT FALLO: {msg.GetError().Message} (code={msg.GetError().Code}). " +
                                   "La cuenta del Quest no tiene acceso a la app (no es test user / firma del APK no coincide).");
                else
                    Debug.Log("[PackPurchaseService] Entitlement OK: la cuenta tiene acceso a la app.");
            });

            RefreshFromMeta();
        }

        /// <summary>
        /// Sincroniza desde Meta: precio localizado real y estado de compra del catalogo.
        /// </summary>
        public void RefreshFromMeta()
        {
            if (!_platformReady)
            {
                Debug.LogWarning("[PackPurchaseService] RefreshFromMeta llamado pero el SDK no esta listo.");
                return;
            }

            // Precio localizado real del add-on.
            Debug.Log($"[PackPurchaseService] GetProductsBySKU: pidiendo info del SKU '{CatalogSku}'...");
            IAP.GetProductsBySKU(new[] { CatalogSku }).OnComplete((Message<ProductList> msg) =>
            {
                if (msg.IsError)
                {
                    Debug.LogError($"[PackPurchaseService] GetProductsBySKU FALLO: {msg.GetError().Message} (code={msg.GetError().Code}).");
                    return;
                }

                var list = msg.GetProductList();
                Debug.Log($"[PackPurchaseService] GetProductsBySKU OK: Meta devolvio {list.Count} producto(s).");
                if (list.Count == 0)
                    Debug.LogWarning("[PackPurchaseService] La lista vino VACIA: el SKU no esta disponible para esta cuenta " +
                                     "(add-on no propagado, sin descripcion, o cuenta sin acceso).");

                foreach (Product product in list)
                {
                    Debug.Log($"[PackPurchaseService]   -> SKU='{product.Sku}' name='{product.Name}' price='{product.FormattedPrice}'");
                    if (product.Sku == CatalogSku && !string.IsNullOrEmpty(product.FormattedPrice))
                    {
                        _metaFormattedPrice = product.FormattedPrice;
                        Debug.Log($"[PackPurchaseService] Precio de Meta aplicado: '{_metaFormattedPrice}'");
                        OnPurchaseStateChanged?.Invoke();
                    }
                }
            });

            // Restaurar la compra: Meta es la fuente de verdad (sobrevive reinstalaciones).
            Debug.Log("[PackPurchaseService] GetViewerPurchases: consultando compras previas...");
            IAP.GetViewerPurchases().OnComplete((Message<PurchaseList> msg) =>
            {
                if (msg.IsError)
                {
                    Debug.LogError($"[PackPurchaseService] GetViewerPurchases FALLO: {msg.GetError().Message} (code={msg.GetError().Code}).");
                    return;
                }

                var list = msg.GetPurchaseList();
                Debug.Log($"[PackPurchaseService] GetViewerPurchases OK: {list.Count} compra(s) previa(s).");
                foreach (Purchase purchase in list)
                {
                    Debug.Log($"[PackPurchaseService]   -> compra SKU='{purchase.Sku}'");
                    if (purchase.Sku == CatalogSku && !IsCatalogPurchased())
                    {
                        saveDataService?.MarkAsPurchased(CatalogSku);
                        Debug.Log("[PackPurchaseService] Compra del catalogo restaurada desde Meta.");
                        OnPurchaseStateChanged?.Invoke();
                    }
                }
            });
        }

        /// <summary>True una vez que el usuario compro el catalogo completo.</summary>
        public bool IsCatalogPurchased() => IsPurchased(CatalogSku);

        /// <summary>
        /// Una obra esta bloqueada si no es gratis y el catalogo completo no se ha comprado.
        /// </summary>
        public bool IsArtworkLocked(ArtworkDefinition artwork)
            => artwork != null && !artwork.isFree && !IsCatalogPurchased();

        /// <summary>
        /// Inicia la compra del catalogo completo via Meta IAP. En el Editor concede de inmediato
        /// (no hay checkout disponible) para poder probar el flujo de UI.
        /// </summary>
        public void PurchaseCatalog(Action onSuccess, Action onFailure = null)
        {
            if (Application.isEditor)
            {
                // Fallback de desarrollo: no existe checkout en el Editor.
                saveDataService?.MarkAsPurchased(CatalogSku);
                Debug.Log("[PackPurchaseService] Catalogo concedido (fallback de Editor).");
                onSuccess?.Invoke();
                OnPurchaseStateChanged?.Invoke();
                return;
            }

            if (!_platformReady)
            {
                Debug.LogError("[PackPurchaseService] PurchaseCatalog: Platform SDK NO listo; no se puede comprar. " +
                               "(Por eso el boton 'no hace nada'.)");
                onFailure?.Invoke();
                return;
            }

            Debug.Log($"[PackPurchaseService] PurchaseCatalog: lanzando checkout para SKU '{CatalogSku}'...");
            IAP.LaunchCheckoutFlow(CatalogSku).OnComplete((Message<Purchase> msg) =>
            {
                if (msg.IsError)
                {
                    Debug.LogError($"[PackPurchaseService] Checkout FALLO/cancelado: {msg.GetError().Message} (code={msg.GetError().Code})");
                    onFailure?.Invoke();
                    return;
                }

                saveDataService?.MarkAsPurchased(CatalogSku);
                Debug.Log($"[PackPurchaseService] Compra completada: {msg.GetPurchase().Sku}");
                onSuccess?.Invoke();
                OnPurchaseStateChanged?.Invoke();
            });
        }

        private bool IsPurchased(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            return saveDataService != null && saveDataService.IsPurchased(id);
        }
    }
}
