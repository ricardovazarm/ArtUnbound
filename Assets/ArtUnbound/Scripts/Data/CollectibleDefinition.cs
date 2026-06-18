using UnityEngine;

namespace ArtUnbound.Data
{
    /// <summary>
    /// Definicion de un coleccionable (placa o mejora de presentacion). Un asset por coleccionable
    /// (mismo patron que ArtworkDefinition). El conteo del catalogo de obras se fija como `threshold`
    /// al generar; luego es editable a mano en el Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "ArtUnbound/Collectible Definition", fileName = "Collectible")]
    public class CollectibleDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Id estable y unico (se guarda en el save cuando se obtiene). No cambiar tras lanzar.")]
        public string id;
        public CollectibleKind kind = CollectibleKind.AuthorMaster;

        [Header("Text (English)")]
        [Tooltip("Linea hero, p.ej. 'Van Gogh — Master'.")]
        public string title;
        [TextArea(1, 2)]
        [Tooltip("Condicion, p.ej. 'Complete all 34 works'.")]
        public string conditionText;

        [Header("Unlock")]
        [Tooltip("Autor o movimiento (para Master/GrandMaster). Vacio en comportamiento/estatus/mejoras.")]
        public string themeKey;
        [Tooltip("Obras/cuadros requeridos. Umbral fijo (editable).")]
        public int threshold;

        [Header("Behaviour")]
        [Tooltip("Si es colgable (placas). Las mejoras Cedula/Frame/Lamp van en false: se auto-aplican.")]
        public bool hangable = true;

        [Header("Visual")]
        [Tooltip("Icono mostrado en la tarjeta de Collection.")]
        public Sprite icon;
        [Tooltip("Prefab 3D colgable de este coleccionable. Si esta vacio se usa el molde procedural.")]
        public GameObject visualPrefab;
    }
}
