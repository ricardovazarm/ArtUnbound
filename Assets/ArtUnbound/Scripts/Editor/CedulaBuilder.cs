using System.Collections.Generic;
using ArtUnbound.MR;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Crea una cedula de museo en la escena abierta para arrastrarla a Project y generar el prefab.
    /// Mismo molde que la placa (cubo aplanado + World Space Canvas + 4 remaches semiesfericos), pero
    /// con 3 renglones — Titulo (Cormorant SemiBold), Autor (Cormorant Italic) y una linea compuesta
    /// Ano - Movimiento - Museo (Hanken Regular) — cableados a un CedulaLabel.
    ///
    /// El prefab resultante se asigna al campo `cedulaPrefab` del GameBootstrap: en runtime,
    /// PresentationDecorator clona la cedula bajo cada obra colgada, le inyecta su info y aplica el
    /// material del tier de estatus (los 4 materiales se asignan en el CedulaLabel del prefab).
    /// </summary>
    public static class CedulaBuilder
    {
        // Material por defecto (placeholder visible en editor; el tier real se asigna en CedulaLabel).
        private const string PrimaryMaterialPath = "Assets/Real Materials/RM Brass common.mat";
        private const string FallbackMaterialPath = "Assets/Real Materials/RM Gold freckles.mat";

        // Fuentes TMP por renglon (segun spec de la cedula).
        private const string TitleFontPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/CormorantGaramond-SemiBold SDF.asset";
        private const string AuthorFontPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/CormorantGaramond-Italic SDF.asset";
        private const string InfoFontPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/HankenGrotesk-Regular SDF.asset";

        // Colores (hex del spec).
        private static readonly Color TitleColor  = Hex(0x24, 0x1B, 0x12); // 241B12
        private static readonly Color AuthorColor = Hex(0x2E, 0x24, 0x18); // 2E2418
        private static readonly Color InfoColor   = Hex(0x4A, 0x3A, 0x28); // 4A3A28

        // Dimensiones (m). Cedula mas chica/horizontal que la placa.
        private const float W = 0.24f, H = 0.12f, D = 0.012f;
        private const float CanvasW = 240f, CanvasH = 120f, CanvasScale = 0.001f;
        private const float ScrewDiameter = 0.012f, ScrewInset = 0.018f;

        [MenuItem("Tools/ArtUnbound/Create Cedula In Scene")]
        public static void CreateCedula()
        {
            var root = new GameObject("Cedula");

            var mat = LoadMaterial();
            var tiered = new List<Renderer>(5);

            // Placa fisica.
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            Object.DestroyImmediate(plate.GetComponent<Collider>());
            plate.transform.SetParent(root.transform, false);
            plate.transform.localScale = new Vector3(W, H, D);
            if (mat != null) plate.GetComponent<Renderer>().sharedMaterial = mat;
            tiered.Add(plate.GetComponent<Renderer>());

            // World Space Canvas (cara -Z, identidad), igual convencion que la placa.
            var canvasGO = new GameObject("LabelCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(root.transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRT = (RectTransform)canvasGO.transform;
            canvasRT.sizeDelta = new Vector2(CanvasW, CanvasH);
            canvasRT.localScale = new Vector3(CanvasScale, CanvasScale, CanvasScale);
            canvasRT.localPosition = new Vector3(0f, 0f, -(D * 0.5f + 0.002f));
            canvasRT.localRotation = Quaternion.identity;

            var titleFont  = LoadFont(TitleFontPath, "titulo");
            var authorFont = LoadFont(AuthorFontPath, "autor");
            var infoFont   = LoadFont(InfoFontPath, "info");

            // 3 renglones apilados.
            var title = CreateUIText("TitleText", canvasRT, new Vector2(0f, 0.48f), new Vector2(1f, 0.97f),
                "Artwork Title", TitleColor, titleFont, 23f, FontStyles.Bold);
            var author = CreateUIText("AuthorText", canvasRT, new Vector2(0f, 0.30f), new Vector2(1f, 0.50f),
                "Author Name", AuthorColor, authorFont, 16f, FontStyles.Italic);
            var info = CreateUIText("InfoText", canvasRT, new Vector2(0f, 0.04f), new Vector2(1f, 0.30f),
                "1886 · Movement · Museum", InfoColor, infoFont, 11f, FontStyles.Normal);

            // Remaches semiesfericos en las 4 esquinas (mitad embebida en la placa).
            float zFront = -(D * 0.5f);
            float x = W * 0.5f - ScrewInset, y = H * 0.5f - ScrewInset;
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, -y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, -y, zFront)));

            // CedulaLabel cableado: 3 TMP + renderers para el acabado por tier.
            var label = root.AddComponent<CedulaLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("authorText").objectReferenceValue = author;
            so.FindProperty("infoText").objectReferenceValue = info;
            var arr = so.FindProperty("tieredRenderers");
            arr.arraySize = tiered.Count;
            for (int i = 0; i < tiered.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = tiered[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Cedula");
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("[CedulaBuilder] Cedula creada. Asigna los 4 materiales por tier en su CedulaLabel, " +
                      "arrastrala a Project para crear el prefab y asignalo al campo cedulaPrefab del GameBootstrap.");
        }

        private static Material LoadMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(PrimaryMaterialPath)
                      ?? AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
            if (mat == null)
                Debug.LogWarning($"[CedulaBuilder] No encontre el material en '{PrimaryMaterialPath}'. " +
                                 "Asignale a mano uno de Real Materials.");
            return mat;
        }

        private static TMP_FontAsset LoadFont(string path, string label)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) Debug.LogWarning($"[CedulaBuilder] Fuente de {label} no encontrada en '{path}'.");
            return f;
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
            Vector2 anchorMin, Vector2 anchorMax, string text, Color color, TMP_FontAsset font,
            float fontSize, FontStyles style)
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
            tmp.fontStyle = style;
            tmp.enableAutoSizing = false;
            tmp.fontSize = fontSize;
            return tmp;
        }

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
