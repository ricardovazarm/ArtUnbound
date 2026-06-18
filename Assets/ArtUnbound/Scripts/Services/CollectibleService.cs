using System;
using System.Collections.Generic;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Evalua y otorga coleccionables con nombre (placas) a partir del CollectibleCatalog (asset).
    /// El estado obtenido (id + fecha) vive en SaveData (earnedPlaques). Se evalua al COMPLETAR una
    /// obra (autor/movimiento/estatus + mejoras Cedula/Frame/Lamp por umbral de obras completadas) y
    /// al COLGAR (comportamiento). Las mejoras ADEMAS se auto-aplican a las obras (desbloqueo); aqui
    /// se otorga su PLACA coleccionable paralela, que el jugador decide si colgar o no.
    /// Devuelve las recien otorgadas para la linea de hito del post-juego (Frente 7).
    /// </summary>
    public class CollectibleService
    {
        private readonly SaveDataService _save;
        private readonly LocalCatalogService _catalog;
        private readonly CollectibleCatalog _collectibles;

        public CollectibleCatalog Catalog => _collectibles;

        public CollectibleService(SaveDataService save, LocalCatalogService catalog, CollectibleCatalog collectibles)
        {
            _save = save;
            _catalog = catalog;
            _collectibles = collectibles;
        }

        public List<CollectibleDefinition> EvaluateOnCompletion()
        {
            var newly = new List<CollectibleDefinition>();
            var data = _save?.GetCachedData();
            if (data == null || _collectibles?.collectibles == null) return newly;

            foreach (var c in _collectibles.collectibles)
            {
                if (c == null || !IsThresholdPlaque(c.kind)) continue;
                if (data.HasPlaque(c.id)) continue;
                if (IsThresholdMet(c, data)) { data.GrantPlaque(c.id); newly.Add(c); }
            }
            if (newly.Count > 0) _save.MarkDirty();
            return newly;
        }

        public List<CollectibleDefinition> EvaluateOnHang()
        {
            var newly = new List<CollectibleDefinition>();
            var data = _save?.GetCachedData();
            if (data == null || _collectibles?.collectibles == null) return newly;

            int hung = HungCount(data);
            foreach (var c in _collectibles.collectibles)
            {
                if (c == null || c.kind != CollectibleKind.Behavior) continue;
                if (data.HasPlaque(c.id)) continue;
                if (hung >= c.threshold) { data.GrantPlaque(c.id); newly.Add(c); }
            }
            if (newly.Count > 0) _save.MarkDirty();
            return newly;
        }

        public bool IsEarned(string id) => _save?.GetCachedData()?.HasPlaque(id) ?? false;

        // ── Placa de estatus (GDD 8.4): solo estado global = rango (hero) + agregados ──

        /// <summary>Rango con nombre vigente (Visitor -> Patron) para el hero de la placa de estatus.</summary>
        public string GetStatusRank()
        {
            var data = _save?.GetCachedData();
            return ProgressionRules.GetRankName(data?.GetCompletedCount() ?? 0);
        }

        /// <summary>
        /// Linea de agregados de la placa de estatus: "completadas / total  .  N plaques".
        /// Numeros resumen (no la lista de logros). Las placas se cuentan excluyendo la de estatus.
        /// </summary>
        public string GetStatusAggregates()
        {
            var data = _save?.GetCachedData();
            int completed = data?.GetCompletedCount() ?? 0;
            int total = _catalog?.GetTotalCount() ?? 0;
            int plaques = 0;
            if (data?.earnedPlaques != null)
                foreach (var p in data.earnedPlaques)
                    if (p != null && p.plaqueId != "status") plaques++;
            string left = total > 0 ? $"{completed} / {total}" : $"{completed} completed";
            return $"{left}  ·  {plaques} plaques";
        }

        // Placas evaluadas al completar (autor/movimiento/estatus + mejoras Cedula/Frame/Lamp).
        // Behavior se evalua al colgar. Las mejoras ademas se auto-aplican (desbloqueo) por su cuenta;
        // aqui solo se otorga la PLACA coleccionable paralela, que el jugador decide si colgar.
        private static bool IsThresholdPlaque(CollectibleKind k) =>
            k == CollectibleKind.AuthorMaster || k == CollectibleKind.AuthorGrandMaster ||
            k == CollectibleKind.MovementMaster || k == CollectibleKind.MovementGrandMaster ||
            k == CollectibleKind.Status ||
            k == CollectibleKind.Cedula || k == CollectibleKind.Frame || k == CollectibleKind.Lamp;

        private bool IsThresholdMet(CollectibleDefinition c, SaveData data)
        {
            switch (c.kind)
            {
                case CollectibleKind.AuthorMaster:
                case CollectibleKind.AuthorGrandMaster:
                    return ProgressionStats.CompletedInAuthor(data, _catalog, c.themeKey) >= c.threshold;
                case CollectibleKind.MovementMaster:
                case CollectibleKind.MovementGrandMaster:
                    return ProgressionStats.CompletedInMovement(data, _catalog, c.themeKey) >= c.threshold;
                case CollectibleKind.Status:
                case CollectibleKind.Cedula:
                case CollectibleKind.Frame:
                case CollectibleKind.Lamp:
                    return data.GetCompletedCount() >= c.threshold;
                default:
                    return false;
            }
        }

        private static int HungCount(SaveData data)
        {
            // "On Display" mide obras exhibidas SIMULTANEAMENTE (GDD 8.6): cuenta solo lo que esta
            // colgado ahora, no lo retirado (placedArtworks con isActive=false son obras guardadas).
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (data.placedArtworks != null)
                foreach (var pa in data.placedArtworks)
                    if (pa != null && pa.isActive && !string.IsNullOrEmpty(pa.artworkId)) set.Add(pa.artworkId);
            if (data.anchoredArtworks != null)
                foreach (var aa in data.anchoredArtworks)
                    if (aa != null && !string.IsNullOrEmpty(aa.artworkId)) set.Add(aa.artworkId);
            if (data.galleryPaintings != null)
                foreach (var kv in data.galleryPaintings)
                    if (kv.Value != null)
                        foreach (var gp in kv.Value)
                            if (gp != null && !string.IsNullOrEmpty(gp.artworkId)) set.Add(gp.artworkId);
            return set.Count;
        }
    }
}
