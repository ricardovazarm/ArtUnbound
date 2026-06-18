namespace ArtUnbound.Data
{
    /// <summary>
    /// Material/acabado del marco y de las placas. A partir del GDD v6 NO se deriva de la
    /// dificultad, sino del TIER GLOBAL del jugador (ver PlayerTier / ProgressionRules):
    /// el material es una propiedad global por total de obras completadas, y todas las placas
    /// + el marco "ascienden de material" a la vez.
    ///
    /// Mapeo PlayerTier -> FrameTier: Cobre->Bronce, Plata->Plata, Oro->Oro, Platino->Platino.
    /// (Madera = sin marco, base de madera; valor por defecto.)
    /// </summary>
    public enum FrameTier
    {
        Madera = 0,   // Sin marco (base de madera lisa)
        Bronce = 1,   // == Cobre (tier global base)
        Plata = 2,
        Oro = 3,
        Platino = 4
    }
}
