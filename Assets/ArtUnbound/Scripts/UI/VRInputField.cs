using TMPro;
using UnityEngine.EventSystems;

namespace ArtUnbound.UI
{
    /// <summary>
    /// TMP_InputField adaptado para Meta Quest con XR Interaction Toolkit.
    ///
    /// PROBLEMA: En VR, OnPointerDown se dispara múltiples veces durante un
    /// pinch sostenido. Cada disparo roba el foco y cierra el teclado del sistema
    /// antes de que termine de mostrarse.
    ///
    /// SOLUCIÓN: Suprimir OnPointerDown (no activa el campo) y mover la
    /// activación a OnPointerClick, que se dispara una sola vez al soltar.
    ///
    /// USO: En el prefab SearchInput, reemplaza el componente TMP_InputField
    /// por este VRInputField. Todos los campos serializados son idénticos.
    /// </summary>
    public class VRInputField : TMP_InputField
    {
        public override void OnPointerDown(PointerEventData eventData)
        {
            // Intencional: ignorar pointer down para evitar robo de foco
            // durante pinch sostenido en Meta Quest.
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            // Activar el campo en click (una sola vez al soltar el pinch)
            base.OnPointerDown(eventData);
            base.OnPointerClick(eventData);
        }
    }
}
