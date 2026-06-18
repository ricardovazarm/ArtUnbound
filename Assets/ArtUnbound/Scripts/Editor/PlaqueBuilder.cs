using System.Collections.Generic;
using ArtUnbound.MR;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Crea una placa de museo en la escena abierta para luego arrastrarla a Project y generar el
    /// prefab. La placa es un cubo aplanado con un Real Material (laton) + un World Space Canvas con
    /// dos TextMeshProUGUI (titulo y condicion) cableados a un CollectibleLabel, 4 remaches
    /// semiesfericos en las esquinas, y un BoxCollider en la raiz para el flujo de grab.
    ///
    /// Se usa un World Space Canvas (no TMP 3D) porque renderiza de forma predecible en URP y es el
    /// patron estandar para etiquetas sobre objetos 3D.
    ///
    /// Asignar el prefab resultante al campo `visualPrefab` de un CollectibleDefinition: en runtime,
    /// CollectibleFactory le inyecta el texto del asset via CollectibleLabel.Apply().
    /// </summary>
    public static class PlaqueBuilder
    {
        // Real Material por defecto (laton, look clasico de placa de museo). Editable a mano luego.
        private const string PrimaryMaterialPath = "Assets/Real Materials/RM Brass common.mat";
        private const string FallbackMaterialPath = "Assets/Real Materials/RM Gold freckles.mat";

        // Fuentes TMP (mismas que el resto de la UI del juego).
        private const string TitleFontPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/CormorantGaramond-SemiBold SDF.asset";
        private const string SubtitleFontPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/HankenGrotesk-Medium SDF.asset";

        // Colores (hex del GDD).
        private static readonly Color TitleColor = Hex(0x24, 0x1B, 0x12);    // 241B12
        private static readonly Color SubtitleColor = Hex(0x3A, 0x2C, 0x1C); // 3A2C1C

        // Dimensiones (m), iguales al molde procedural de CollectibleFactory.
        private const float W = 0.30f, H = 0.18f, D = 0.02f;
        // Canvas en unidades UI (px) escalado a metros: 300x180 px * 0.001 = 0.30x0.18 m.
        private const float CanvasW = 300f, CanvasH = 180f, CanvasScale = 0.001f;
        // Remaches semiesfericos.
        private const float ScrewDiameter = 0.018f, ScrewInset = 0.024f;

        [MenuItem("Tools/ArtUnbound/Create Plaque In Scene")]
        public static void CreatePlaque()
        {
            var root = new GameObject("Plaque");
            TrySetTag(root, "PlacedArtwork");
            int layer = LayerMask.NameToLayer("PuzzlePiece");
            if (layer >= 0) root.layer = layer;

            var mat = LoadMaterial();
            var tiered = new List<Renderer>(5);

            // Placa fisica: cubo aplanado con profundidad real (look metalico).
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.SetParent(root.transform, false);
            plate.transform.localScale = new Vector3(W, H, D);
            if (mat != null) plate.GetComponent<Renderer>().sharedMaterial = mat;
            tiered.Add(plate.GetComponent<Renderer>());

            // World Space Canvas en la cara frontal (-Z), justo por delante de la placa, con
            // rotacion identidad para que el texto mire al frente y se lea correctamente.
            var canvasGO = new GameObject("LabelCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(root.transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRT = (RectTransform)canvasGO.transform;
            canvasRT.sizeDelta = new Vector2(CanvasW, CanvasH);
            canvasRT.localScale = new Vector3(CanvasScale, CanvasScale, CanvasScale);
            canvasRT.localPosition = new Vector3(0f, 0f, -(D * 0.5f + 0.002f));
            canvasRT.localRotation = Quaternion.identity;

            var titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
            var subtitleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SubtitleFontPath);
            if (titleFont == null) Debug.LogWarning($"[PlaqueBuilder] Fuente de titulo no encontrada en '{TitleFontPath}'.");
            if (subtitleFont == null) Debug.LogWarning($"[PlaqueBuilder] Fuente de subtitulo no encontrada en '{SubtitleFontPath}'.");

            // Titulo (mitad superior) y condicion (mitad inferior), tamano fijo.
            var title = CreateUIText("TitleText", canvasRT, new Vector2(0f, 0.5f), Vector2.one,
                "Plaque Title", TitleColor, titleFont, 32f);
            var subtitle = CreateUIText("SubtitleText", canvasRT, Vector2.zero, new Vector2(1f, 0.5f),
                "Condition", SubtitleColor, subtitleFont, 22f);

            // Remaches semiesfericos en las 4 esquinas: esfera centrada en la cara frontal (-Z) para
            // que solo sobresalga la cupula (la mitad trasera queda dentro de la placa).
            float zFront = -(D * 0.5f);
            float x = W * 0.5f - ScrewInset, y = H * 0.5f - ScrewInset;
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, y, zFront)));   // sup-izq
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, y, zFront)));    // sup-der
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, -y, zFront)));  // inf-izq
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, -y, zFront)));   // inf-der

            // Collider en la raiz para el flujo de grab/colgado (mismo que el molde procedural).
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(W, H, D);

            // CollectibleLabel cableado a los dos TMP + renderers para el acabado por tier.
            var label = root.AddComponent<CollectibleLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("subtitleText").objectReferenceValue = subtitle;
            var arr = so.FindProperty("tieredRenderers");
            arr.arraySize = tiered.Count;
            for (int i = 0; i < tiered.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = tiered[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Plaque");
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("[PlaqueBuilder] Placa creada en la escena. Ajustala y arrastrala a Project " +
                      "para crear el prefab; luego asignalo al campo visualPrefab de un CollectibleDefinition.");
        }

        private static Material LoadMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(PrimaryMaterialPath)
                      ?? AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
            if (mat == null)
                Debug.LogWarning($"[PlaqueBuilder] No encontre el material en '{PrimaryMaterialPath}'. " +
                                 "Asignale a mano uno de Real Materials.");
            return mat;
        }

        private static Renderer CreateScrew(Transform parent, Material mat, Vector3 localPos)
        {
            var screw = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            screw.name = "Screw";
            Object.DestroyImmediate(screw.GetComponent<Collider>());
            screw.transform.SetParent(parent, false);
            screw.transform.localPosition = localPos;
            screw.transform.localScale = Vector3.one * ScrewDiameter;
            var r = screw.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = mat;
            return r;
        }

        private static TextMeshProUGUI CreateUIText(string name, RectTransform parent,
            Vector2 anchorMin, Vector2 anchorMax, string text, Color color, TMP_FontAsset font, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (font != null) tmp.font = font;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            tmp.fontSize = fontSize;
            return tmp;
        }

        private static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch { Debug.LogWarning($"[PlaqueBuilder] El tag '{tag}' no existe; la placa queda Untagged."); }
        }

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
