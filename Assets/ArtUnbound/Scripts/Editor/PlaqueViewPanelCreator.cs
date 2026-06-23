using System.Linq;
using ArtUnbound.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Crea y cablea un PlaqueView funcional (ventana de instrucciones + boton Close) en la escena
    /// abierta, colocandolo donde aparece el panel de PostGame para heredar el mismo canvas/ubicacion.
    /// Agrega el componente PlaqueViewController, conecta sus referencias y lo asigna al campo
    /// 'plaqueView' del NativeGalleryController (el panel es un overlay del menu, hermano del detalle).
    /// Es un PUNTO DE PARTIDA funcional (sin estilo): el
    /// usuario lo reposiciona y aplica el tema visual (fuentes/colores) despues.
    ///
    /// Idempotente: si ya existe un PlaqueView, lo selecciona en vez de duplicar.
    /// </summary>
    public static class PlaqueViewPanelCreator
    {
        [MenuItem("Tools/ArtUnbound/Create PlaqueView Panel")]
        public static void CreatePlaqueView()
        {
            var existing = FindInScene<PlaqueViewController>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing);
                Debug.Log("[PlaqueViewPanelCreator] Ya existe un PlaqueViewController en la escena; seleccionado (no se duplica).");
                return;
            }

            var postGame = FindInScene<PostGameController>();
            if (postGame == null)
            {
                Debug.LogError("[PlaqueViewPanelCreator] No se encontro PostGameController en la escena. Abre la escena Main e intenta de nuevo.");
                return;
            }

            // Panel de PostGame (referencia privada): se usa como plantilla de ubicacion/tamano.
            var pgSO = new SerializedObject(postGame);
            var pgPanel = pgSO.FindProperty("panel")?.objectReferenceValue as GameObject;
            Transform parent = pgPanel != null ? pgPanel.transform.parent : postGame.transform.parent;
            if (parent == null)
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                parent = canvas != null ? canvas.transform : null;
            }
            if (parent == null)
            {
                Debug.LogError("[PlaqueViewPanelCreator] No se encontro un Canvas/parent para el panel. Abortando.");
                return;
            }

            // Raiz del PlaqueView = el propio 'panel' del controller.
            var rootGO = new GameObject("PlaqueView", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(rootGO, "Create PlaqueView");
            rootGO.transform.SetParent(parent, false);

            var rootRT = (RectTransform)rootGO.transform;
            var pgRT = pgPanel != null ? pgPanel.GetComponent<RectTransform>() : null;
            if (pgRT != null) CopyRect(pgRT, rootRT);
            else { rootRT.anchorMin = rootRT.anchorMax = rootRT.pivot = new Vector2(0.5f, 0.5f); rootRT.sizeDelta = new Vector2(600f, 420f); rootRT.anchoredPosition = Vector2.zero; }

            var bg = rootGO.GetComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.07f, 0.92f);

            // Texto de instruccion (lo rellena el controller en runtime; aqui un placeholder).
            var textGO = new GameObject("InstructionText", typeof(RectTransform));
            textGO.transform.SetParent(rootGO.transform, false);
            var textRT = (RectTransform)textGO.transform;
            textRT.anchorMin = new Vector2(0.08f, 0.30f);
            textRT.anchorMax = new Vector2(0.92f, 0.80f);
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Pinch to grab the plaque and release it against a wall to hang your plaque.";
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            // Boton Close.
            var btnGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(rootGO.transform, false);
            var btnRT = (RectTransform)btnGO.transform;
            btnRT.anchorMin = new Vector2(0.5f, 0.08f);
            btnRT.anchorMax = new Vector2(0.5f, 0.08f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.sizeDelta = new Vector2(220f, 70f);
            btnRT.anchoredPosition = Vector2.zero;
            btnGO.GetComponent<Image>().color = new Color(0.537f, 0.424f, 0.290f, 1f); // #896C4A (UIButtonTheme)
            var button = btnGO.GetComponent<Button>();

            var btnTextGO = new GameObject("Text", typeof(RectTransform));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            var btnTextRT = (RectTransform)btnTextGO.transform;
            btnTextRT.anchorMin = Vector2.zero; btnTextRT.anchorMax = Vector2.one;
            btnTextRT.offsetMin = btnTextRT.offsetMax = Vector2.zero;
            var btnLabel = btnTextGO.AddComponent<TextMeshProUGUI>();
            btnLabel.text = "Close";
            btnLabel.fontSize = 30f;
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.color = Color.white;

            // Componente + wiring.
            var controller = rootGO.AddComponent<PlaqueViewController>();
            var input = FindInScene<ArtUnbound.Input.HandTrackingInputController>();

            var cSO = new SerializedObject(controller);
            cSO.FindProperty("panel").objectReferenceValue = rootGO;
            cSO.FindProperty("instructionText").objectReferenceValue = tmp;
            cSO.FindProperty("closeButton").objectReferenceValue = button;
            if (input != null) cSO.FindProperty("inputController").objectReferenceValue = input;
            cSO.ApplyModifiedPropertiesWithoutUndo();

            // Asignar al NativeGalleryController (campo 'plaqueView'): el panel es un overlay del menu.
            var gallery = FindInScene<NativeGalleryController>();
            if (gallery != null)
            {
                var gSO = new SerializedObject(gallery);
                var prop = gSO.FindProperty("plaqueView");
                if (prop != null)
                {
                    prop.objectReferenceValue = controller;
                    gSO.ApplyModifiedPropertiesWithoutUndo();
                }
                else Debug.LogWarning("[PlaqueViewPanelCreator] No se encontro 'plaqueView' en NativeGalleryController (recompila scripts).");
            }
            else Debug.LogWarning("[PlaqueViewPanelCreator] No se encontro NativeGalleryController; asigna el PlaqueViewController a mano.");

            EditorSceneManager.MarkSceneDirty(rootGO.scene);
            Selection.activeGameObject = rootGO;
            EditorGUIUtility.PingObject(rootGO);
            Debug.Log("[PlaqueViewPanelCreator] PlaqueView creado y cableado. Reposicionalo/estilizalo a tu gusto y guarda la escena.");
        }

        private static T FindInScene<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .FirstOrDefault(c => c != null && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded
                                     && !EditorUtility.IsPersistent(c.gameObject));
        }

        private static void CopyRect(RectTransform src, RectTransform dst)
        {
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.sizeDelta = src.sizeDelta;
            dst.anchoredPosition = src.anchoredPosition;
            dst.localScale = src.localScale;
            dst.localRotation = src.localRotation;
            dst.offsetMin = src.offsetMin;
            dst.offsetMax = src.offsetMax;
        }
    }
}
