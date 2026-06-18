namespace ArtUnbound.Data
{
    /// <summary>
    /// Tipo de coleccionable (GDD 8.x). Placas con nombre + las 3 mejoras de presentacion.
    /// Las mejoras (Cedula/Frame/Lamp) NO se cuelgan: se auto-aplican a las obras; aparecen en
    /// Collection como desbloqueables.
    /// </summary>
    public enum CollectibleKind
    {
        AuthorMaster,
        AuthorGrandMaster,
        MovementMaster,
        MovementGrandMaster,
        Behavior,
        Status,
        Cedula,   // Museum Label
        Frame,    // Frame
        Lamp      // Picture Light
    }
}
