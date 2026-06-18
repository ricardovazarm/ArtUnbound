namespace ArtUnbound.Data
{
    /// <summary>
    /// Tier global del jugador (GDD 8.4). Es una propiedad GLOBAL derivada del total de
    /// obras distintas completadas — NO se asigna por obra ni por dificultad. Define el
    /// material de TODAS las placas y del marco a la vez.
    ///
    /// Nota de naming: "Cobre" es el tier base (el GDD usa Cobre; en los materiales de marco
    /// existentes el equivalente es "Bronce"). El Frente 2 mapea PlayerTier -> material.
    /// </summary>
    public enum PlayerTier
    {
        Cobre = 0,    // 1+ obras (tier base, primera obra)
        Plata = 1,    // 10 obras
        Oro = 2,      // 25 obras
        Platino = 3   // 50 obras (tope)
    }
}
