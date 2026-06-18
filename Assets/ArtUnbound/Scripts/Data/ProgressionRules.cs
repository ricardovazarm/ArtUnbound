namespace ArtUnbound.Data
{
    /// <summary>
    /// Reglas centrales de la escalera de progreso global (GDD 8.2 / 8.4).
    /// Una sola escalera, basada en el total de obras DISTINTAS completadas, que en cada
    /// peldano (a) sube el material/tier global y (b) en algunos peldanos desbloquea una
    /// mejora de presentacion (cedula/marco/lampara).
    ///
    /// | Total obras | Tier   | Desbloqueo            |
    /// |-------------|--------|-----------------------|
    /// | 1           | Cobre  | Placa de estatus      |
    /// | 10          | Plata  | Cedula                |
    /// | 25          | Oro    | Marco                 |
    /// | 50          | Platino| Lampara               |
    /// | 100         | Platino| (hito, sin asset)     |
    /// </summary>
    public static class ProgressionRules
    {
        // --- Escalera de tier/material por total de obras completadas ---
        public const int PlataThreshold   = 10;
        public const int OroThreshold     = 25;
        public const int PlatinoThreshold = 50;
        // Cobre es el tier base desde la primera obra (>=1). Antes de la primera no hay placa.

        // --- Umbrales de desbloqueo de mejoras de presentacion (GDD 8.2) ---
        public const int CedulaThreshold  = 10;
        public const int MarcoThreshold   = 25;
        public const int LamparaThreshold = 50;

        // --- Umbral de la placa de estatus (se desbloquea al completar la primera obra) ---
        public const int StatusPlaqueThreshold = 1;

        // --- Rangos con nombre de la placa de estatus (GDD 8.4): Visitor -> Patron ---
        public const int RankCollectorThreshold   = 10;
        public const int RankConnoisseurThreshold = 25;
        public const int RankCuratorThreshold     = 50;
        public const int RankPatronThreshold      = 100;

        /// <summary>
        /// Rango con nombre vigente (hero de la placa de estatus) para un total de obras completadas
        /// (GDD 8.4): Visitor (>=1) -> Collector (10) -> Connoisseur (25) -> Curator (50) -> Patron (100).
        /// </summary>
        public static string GetRankName(int completedCount)
        {
            if (completedCount >= RankPatronThreshold)      return "Patron";
            if (completedCount >= RankCuratorThreshold)     return "Curator";
            if (completedCount >= RankConnoisseurThreshold) return "Connoisseur";
            if (completedCount >= RankCollectorThreshold)   return "Collector";
            return "Visitor"; // de la primera obra en adelante
        }

        /// <summary>Tier/material global para un total de obras completadas.</summary>
        public static PlayerTier GetTier(int completedCount)
        {
            if (completedCount >= PlatinoThreshold) return PlayerTier.Platino;
            if (completedCount >= OroThreshold)     return PlayerTier.Oro;
            if (completedCount >= PlataThreshold)   return PlayerTier.Plata;
            return PlayerTier.Cobre; // base (incluye 0; el material base es Cobre)
        }

        public static bool StatusPlaqueUnlocked(int completedCount) => completedCount >= StatusPlaqueThreshold;
        public static bool CedulaUnlocked(int completedCount)       => completedCount >= CedulaThreshold;
        public static bool MarcoUnlocked(int completedCount)        => completedCount >= MarcoThreshold;
        public static bool LamparaUnlocked(int completedCount)      => completedCount >= LamparaThreshold;
    }
}
