using System.Collections.Generic;
using ArtUnbound.MR;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Crea la PLACA DE ESTATUS (GDD 8.4) en la escena para arrastrarla a Project y generar el prefab.
    /// Mismo molde que las placas (cubo + World Space Canvas + 4 remaches), con layout de 3 partes:
    ///   - Overline ESTATICO: "Your museum at a glance" (Hanken, versalitas espaciadas).
    ///   - Hero DINAMICO: el rango (Visitor -> Patron), Cormorant SemiBold grande.
    ///   - Divisor + Agregados DINAMICOS: "47 / 252 artworks  .  8 plaques", Hanken.
    /// El hero y los agregados se cablean a un CollectibleLabel (titleText/subtitleText): en runtime
    /// CollectibleFactory (caso Status) inyecta rango+agregados y aplica el material del tier de estatus.
    /// Asignar el prefab al campo `visualPrefab` del asset `status`.
    /// </summary>
    public static class StatusPlaqueBuilder
    {
        private const string PrimaryMaterialPath = "Assets/Real Materials/RM Brass common.mat";
        private const string FallbackMaterialPath = "Assets/Real Materials/RM Gold freckles.mat";

        private const string CormorantSemiBoldPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/CormorantGaramond-SemiBold SDF.asset";
        private const string HankenMediumPath =
            "Assets/ArtUnbound/UI/Fonts/art-unbound-assets/fonts/HankenGrotesk-Medium SDF.asset";

        private static readonly Color OverlineColor   = Hex(0x4A, 0x3A, 0x28); // 4A3A28
        private static readonly Color HeroColor       = Hex(0x24, 0x1B, 0x12); // 241B12
        private static readonly Color AggregateColor  = Hex(0x3A, 0x2C, 0x1C); // 3A2C1C
        private static readonly Color DividerColor    = new Color(0.14f, 0.11f, 0.07f, 0.55f);

        // Dimensiones (m). Algo mas amplia que las placas para alojar 3 renglones + overline.
        private const float W = 0.32f, H = 0.20f, D = 0.02f;
        private const float CanvasW = 320f, CanvasH = 200f, CanvasScale = 0.001f;
        private const float ScrewDiameter = 0.018f, ScrewInset = 0.024f;

        [MenuItem("Tools/ArtUnbound/Create Status Plaque In Scene")]
        public static void CreateStatusPlaque()
        {
            var root = new GameObject("StatusPlaque");
            TrySetTag(root, "PlacedArtwork");
            int layer = LayerMask.NameToLayer("PuzzlePiece");
            if (layer >= 0) root.layer = layer;

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

            // World Space Canvas (cara -Z, identidad), misma convencion que las placas.
            var canvasGO = new GameObject("LabelCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(root.transform, false);
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRT = (RectTransform)canvasGO.transform;
            canvasRT.sizeDelta = new Vector2(CanvasW, CanvasH);
            canvasRT.localScale = new Vector3(CanvasScale, CanvasScale, CanvasScale);
            canvasRT.localPosition = new Vector3(0f, 0f, -(D * 0.5f + 0.002f));
            canvasRT.localRotation = Quaternion.identity;

            var cormorant = LoadFont(CormorantSemiBoldPath, "Cormorant SemiBold");
            var hanken = LoadFont(HankenMediumPath, "Hanken Medium");

            // Overline ESTATICO (versalitas espaciadas), no se cablea al label.
            var overline = CreateUIText("Overline", canvasRT, new Vector2(0f, 0.74f), new Vector2(1f, 0.90f),
                "Your museum at a glance", OverlineColor, hanken, 13f, FontStyles.UpperCase);
            overline.characterSpacing = 10f;
            overline.enableWordWrapping = false;

            // Hero DINAMICO (rango). Autosize para que "Connoisseur"/"Patron" quepan igual.
            var hero = CreateUIText("HeroText", canvasRT, new Vector2(0f, 0.40f), new Vector2(1f, 0.72f),
                "Connoisseur", HeroColor, cormorant, 52f, FontStyles.Normal);
            hero.enableAutoSizing = true;
            hero.fontSizeMin = 24f;
            hero.fontSizeMax = 52f;
            hero.enableWordWrapping = false;

            // Divisor decorativo (linea corta bajo el hero).
            CreateDivider(canvasRT, new Vector2(0.5f, 0.37f), new Vector2(70f, 2f));

            // Agregados DINAMICOS.
            var aggregates = CreateUIText("AggregatesText", canvasRT, new Vector2(0f, 0.14f), new Vector2(1f, 0.34f),
                "47 / 252 artworks  ·  8 plaques", AggregateColor, hanken, 20f, FontStyles.Normal);
            aggregates.enableAutoSizing = true;
            aggregates.fontSizeMin = 11f;
            aggregates.fontSizeMax = 20f;
            aggregates.enableWordWrapping = false;

            // Remaches semiesfericos en las 4 esquinas.
            float zFront = -(D * 0.5f);
            float x = W * 0.5f - ScrewInset, y = H * 0.5f - ScrewInset;
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(-x, -y, zFront)));
            tiered.Add(CreateScrew(root.transform, mat, new Vector3(x, -y, zFront)));

            // Collider en la raiz para el flujo de grab/colgado.
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(W, H, D);

            // CollectibleLabel: hero -> titleText, agregados -> subtitleText, renderers para el tier.
            var label = root.AddComponent<CollectibleLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("titleText").objectReferenceValue = hero;
            so.FindProperty("subtitleText").objectReferenceValue = aggregates;
            var arr = so.FindProperty("tieredRenderers");
            arr.arraySize = tiered.Count;
            for (int i = 0; i < tiered.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = tiered[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Status Plaque");
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            Debug.Log("[StatusPlaqueBuilder] Placa de estatus creada. Asigna los 4 materiales por tier en su " +
                      "CollectibleLabel, arrastrala a Project y asigna el prefab al campo visualPrefab del asset 'status'.");
        }

        private static Material LoadMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(PrimaryMaterialPath)
                      ?? AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
            if (mat == null)
                Debug.LogWarning($"[StatusPlaqueBuilder] No encontre el material en '{PrimaryMaterialPath}'.");
            return mat;
        }

        private static TMP_FontAsset LoadFont(string path, string label)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (f == null) Debug.LogWarning($"[StatusPlaqueBuilder] Fuente {label} no encontrada en '{path}'.");
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

        private static void CreateDivider(RectTransform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Divider", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            var img = go.AddComponent<Image>(); // sin sprite = rectangulo solido
            img.color = DividerColor;
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

        private static void TrySetTag(GameObject go, string tag)
        {
            try { go.tag = tag; }
            catch { Debug.LogWarning($"[StatusPlaqueBuilder] El tag '{tag}' no existe; la placa queda Untagged."); }
        }

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
