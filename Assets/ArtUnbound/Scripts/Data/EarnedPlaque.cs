using System;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Estado guardado de una placa obtenida por el jugador (GDD 8.3). Se persiste en
    /// SaveData. Guarda solo el id de la placa y la fecha de obtencion (para "Earned June 2026"
    /// y el orden cronologico del cuerpo de la placa de estatus).
    /// </summary>
    [Serializable]
    public class EarnedPlaque
    {
        public string plaqueId;
        public string earnedAtIso;

        public EarnedPlaque() { }

        public EarnedPlaque(string plaqueId)
        {
            this.plaqueId = plaqueId;
            earnedAtIso = DateTime.UtcNow.ToString("o");
        }
    }
}
