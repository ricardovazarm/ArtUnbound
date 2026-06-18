using System;
using System.Collections.Generic;
using ArtUnbound.Data;

namespace ArtUnbound.Services
{
    /// <summary>
    /// Calculo de progreso por autor y por movimiento (GDD 8.6), derivado de las obras
    /// completadas en SaveData cruzadas con el catalogo. No duplica datos: el conteo se deriva
    /// on-the-fly. Lo consume el Frente 4 (placas) y la seccion Collection (Frente 5).
    /// </summary>
    public static class ProgressionStats
    {
        /// <summary>Total de obras distintas completadas (motor de la escalera global).</summary>
        public static int CompletedCount(SaveData data)
            => data?.GetCompletedArtworkIds()?.Count ?? 0;

        /// <summary>Tier/material global del jugador.</summary>
        public static PlayerTier GetPlayerTier(SaveData data)
            => ProgressionRules.GetTier(CompletedCount(data));

        /// <summary>Obras completadas de un autor dado.</summary>
        public static int CompletedInAuthor(SaveData data, LocalCatalogService catalog, string author)
        {
            if (data == null || catalog == null || string.IsNullOrEmpty(author)) return 0;
            var completed = ToSet(data.GetCompletedArtworkIds());
            int count = 0;
            foreach (var art in catalog.GetByAuthor(author))
                if (art != null && completed.Contains(art.artworkId)) count++;
            return count;
        }

        /// <summary>Obras completadas de un movimiento dado.</summary>
        public static int CompletedInMovement(SaveData data, LocalCatalogService catalog, string movement)
        {
            if (data == null || catalog == null || string.IsNullOrEmpty(movement)) return 0;
            var completed = ToSet(data.GetCompletedArtworkIds());
            int count = 0;
            foreach (var art in catalog.GetByArtMovement(movement))
                if (art != null && completed.Contains(art.artworkId)) count++;
            return count;
        }

        /// <summary>Obras totales del catalogo para un autor (denominador del progreso).</summary>
        public static int TotalInAuthor(LocalCatalogService catalog, string author)
            => catalog != null && !string.IsNullOrEmpty(author) ? catalog.GetByAuthor(author).Count : 0;

        /// <summary>Obras totales del catalogo para un movimiento (denominador del progreso).</summary>
        public static int TotalInMovement(LocalCatalogService catalog, string movement)
            => catalog != null && !string.IsNullOrEmpty(movement) ? catalog.GetByArtMovement(movement).Count : 0;

        /// <summary>Conteo de completadas agrupado por autor (solo autores con al menos una completada).</summary>
        public static Dictionary<string, int> CompletedByAuthor(SaveData data, LocalCatalogService catalog)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (data == null || catalog == null) return result;
            foreach (var id in data.GetCompletedArtworkIds())
            {
                var art = catalog.GetById(id);
                if (art == null || string.IsNullOrEmpty(art.author)) continue;
                result.TryGetValue(art.author, out int c);
                result[art.author] = c + 1;
            }
            return result;
        }

        /// <summary>Conteo de completadas agrupado por movimiento (solo movimientos con al menos una).</summary>
        public static Dictionary<string, int> CompletedByMovement(SaveData data, LocalCatalogService catalog)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (data == null || catalog == null) return result;
            foreach (var id in data.GetCompletedArtworkIds())
            {
                var art = catalog.GetById(id);
                if (art == null || string.IsNullOrEmpty(art.artMovement)) continue;
                result.TryGetValue(art.artMovement, out int c);
                result[art.artMovement] = c + 1;
            }
            return result;
        }

        private static HashSet<string> ToSet(List<string> ids)
            => ids != null ? new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase)
                           : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}
