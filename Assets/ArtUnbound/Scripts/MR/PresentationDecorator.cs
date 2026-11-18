using System.Collections.Generic;
using ArtUnbound.Core;
using ArtUnbound.Data;
using TMPro;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Aplica las mejoras de presentacion (GDD 8.2) a una obra colgada: marco (gating),
    /// cedula y lampara. El desbloqueo y el toggle se consultan en el SaveData GLOBAL
    /// (GameBootstrap.Instance), de modo que la presentacion refleja el tier actual del jugador
    /// y se "re-aplica" cada vez que una obra se instancia (al colgar o al cargar la galeria).
    /// El acabado/material lo determina el tier global (Cobre -> Platino).
    ///
    /// Procedural a proposito: no requiere prefabs hechos a mano. Comun a MR y VR.
    /// </summary>
    public static class PresentationDecorator
    {
        /// <summary>
        /// Tier de MARCO efectivo: Madera (base de madera lisa) si el Marco no esta activo
        /// (desbloqueo a 25 obras + toggle), o el material del tier global si lo esta.
        /// </summary>
        public static FrameTier CurrentFrameTier()
        {
            var data = GameBootstrap.Instance != null ? GameBootstrap.Instance.SaveData : null;
            if (data == null || !data.IsMarcoActive()) return FrameTier.Madera;
            return data.GetPlayerTier() switch
            {
                PlayerTier.Platino => FrameTier.Platino,
                PlayerTier.Oro     => FrameTier.Oro,
                PlayerTier.Plata   => FrameTier.Plata,
                _                  => FrameTier.Bronce // Cobre
            };
        }

        /// <summary>Color del marco/acabado por tier (incluye Madera = base de madera lisa).</summary>
        public static Color FrameBarColor(FrameTier tier) => tier switch
        {
            FrameTier.Madera  => new Color(0.45f, 0.30f, 0.18f), // madera
            FrameTier.Bronce  => new Color(0.55f, 0.35f, 0.15f),
            FrameTier.Plata   => new Color(0.75f, 0.75f, 0.78f),
            FrameTier.Oro     => new Color(0.85f, 0.72f, 0.20f),
            FrameTier.Platino => new Color(0.90f, 0.91f, 0.95f),
            _                 => new Color(0.45f, 0.30f, 0.18f),
        };

        /// <summary>
        /// Anade cedula y/o lampara a la obra colgada segun el estado global. Llamar despues de
        /// construir la obra (quad + marco), tanto en MR como en VR. Idempotente por nombre de hijo.
        /// </summary>
        public static void Decorate(GameObject artworkRoot, string title, string author, float w, float h,
                                    int year = 0, string movement = null, string museum = null)
        {
            if (artworkRoot == null) return;
            var data = GameBootstrap.Instance != null ? GameBootstrap.Instance.SaveData : null;
            if (data == null) return;

            FrameTier tier = CurrentFrameTier();

            if (data.IsCedulaActive())
                BuildCedula(artworkRoot.transform, title, author, ComposeInfo(year, movement, museum), w, h, tier);

            if (data.IsLamparaActive())
                BuildLampara(artworkRoot.transform, w, h, tier);
        }

        /// <summary>Linea compuesta "Ano - Movimiento - Museo" (separada por " . "), omitiendo vacios.</summary>
        private static string ComposeInfo(int year, string movement, string museum)
        {
            var parts = new List<string>(3);
            if (year > 0) parts.Add(year.ToString());
            if (!string.IsNullOrEmpty(movement)) parts.Add(movement);
            if (!string.IsNullOrEmpty(museum)) parts.Add(museum);
            return string.Join(" · ", parts); // middle dot
        }

        // ── Cedula: placa-cedula tipo museo (titulo + autor + ano/movimiento/museo) bajo el cuadro ──
        private static void BuildCedula(Transform parent, string title, string author, string info,
                                        float w, float h, FrameTier tier)
        {
            const string kName = "Cedula";
            if (parent.Find(kName) != null) return; // idempotente

            // 1) Prefab editable asignado en GameBootstrap -> clonarlo bajo la obra e inyectar datos.
            var prefab = GameBootstrap.Instance != null ? GameBootstrap.Instance.CedulaPrefab : null;
            if (prefab != null)
            {
                var inst = Object.Instantiate(prefab, parent);
                inst.name = kName;
                // Misma colocacion que el molde procedural: bajo el cuadro, girada para mirar al frente.
                inst.transform.localPosition = new Vector3(0f, -h / 2f - 0.10f, 0.012f);
                inst.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                var label = inst.GetComponentInChildren<CedulaLabel>(true);
                if (label != null)
                {
                    label.Apply(title, author, info);
                    var data = GameBootstrap.Instance.SaveData;
                    if (data != null) label.ApplyTier(data.GetPlayerTier());
                }
                return;
            }

            // 2) Molde procedural (fallback sin prefab).
            var cedula = new GameObject(kName);
            cedula.transform.SetParent(parent, false);
            cedula.transform.localPosition = new Vector3(0f, -h / 2f - 0.09f, 0.012f);
            cedula.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            float plateW = Mathf.Min(0.22f, w * 0.6f);
            float plateH = 0.06f;

            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = "CedulaPlate";
            Object.Destroy(plate.GetComponent<Collider>());
            plate.transform.SetParent(cedula.transform, false);
            plate.transform.localScale = new Vector3(plateW, plateH, 1f);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color c = FrameBarColor(tier);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.8f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
                plate.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // Texto TMP 3D, grabado oscuro (#241B12). Titulo + autor (mas pequeno).
            var textGO = new GameObject("CedulaText");
            textGO.transform.SetParent(cedula.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            var tmp = textGO.AddComponent<TextMeshPro>();
            string infoLine = string.IsNullOrEmpty(info) ? string.Empty : $"\n<size=40%>{info}</size>";
            tmp.text = $"{title}\n<size=55%>{author}</size>{infoLine}";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.14f, 0.11f, 0.07f); // grabado oscuro
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.01f;
            tmp.fontSizeMax = 0.05f;
            var rt = tmp.rectTransform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(plateW * 0.92f, plateH * 0.85f);
        }

        // ── Lampara: luz tipo museo sobre el cuadro ──
        private static void BuildLampara(Transform parent, float w, float h, FrameTier tier)
        {
            const string kName = "Lampara";
            if (parent.Find(kName) != null) return; // idempotente

            // Raiz sin offset: cada pieza se coloca en el espacio de la obra (+Z = hacia la sala,
            // la pared queda en z~0, el borde superior de la obra en y = h/2).
            var lamp = new GameObject(kName);
            lamp.transform.SetParent(parent, false);

            Material metal = null;
            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (litShader != null)
            {
                metal = new Material(litShader);
                Color c = FrameBarColor(tier);
                if (metal.HasProperty("_BaseColor")) metal.SetColor("_BaseColor", c); else metal.color = c;
            }

            float top = h / 2f;
            float shadeWidth = Mathf.Min(0.18f, w * 0.5f);

            // 1) Placa de fijacion pegada a la pared, arriba del cuadro: de aqui "cuelga" todo
            //    (antes la lampara era una barra suelta a 18 cm de la pared y parecia flotar).
            Vector3 plateCenter = new Vector3(0f, top + 0.07f, 0.006f);
            CreateLampPart(PrimitiveType.Cube, "LamparaPlaca", lamp.transform, plateCenter,
                           Quaternion.identity, new Vector3(0.07f, 0.05f, 0.012f), metal);

            // 2) Pantalla: barra inclinada cuya boca mira abajo-atras, hacia la pintura.
            Vector3 shadeCenter = new Vector3(0f, top + 0.12f, 0.16f);
            Quaternion shadeRot = Quaternion.Euler(55f, 0f, 0f);
            var shade = CreateLampPart(PrimitiveType.Cube, "LamparaFixture", lamp.transform, shadeCenter,
                                       shadeRot, new Vector3(shadeWidth, 0.03f, 0.045f), metal);

            // 3) Brazo: tubo de la placa a la pantalla.
            Vector3 armStart = new Vector3(0f, top + 0.07f, 0.012f);
            Vector3 armDir = shadeCenter - armStart;
            float armLen = armDir.magnitude;
            if (armLen > 0.001f)
            {
                // El cilindro de Unity mide 2 de alto sobre su eje Y y 1 de diametro.
                CreateLampPart(PrimitiveType.Cylinder, "LamparaBrazo", lamp.transform,
                               (armStart + shadeCenter) * 0.5f,
                               Quaternion.FromToRotation(Vector3.up, armDir / armLen),
                               new Vector3(0.012f, armLen * 0.5f, 0.012f), metal);
            }

            // 4) Boca encendida: tira clara bajo la pantalla (su -Y local mira a la pintura).
            Material glow = LampGlowMaterial();
            if (glow != null && shade != null)
            {
                var strip = CreateLampPart(PrimitiveType.Cube, "LamparaBoca", shade.transform,
                                           Vector3.zero, Quaternion.identity, Vector3.one, glow);
                // Hijo de la pantalla (escalada): valores en fracciones de la pantalla.
                strip.transform.localPosition = new Vector3(0f, -0.52f, 0f);
                strip.transform.localScale = new Vector3(0.9f, 0.06f, 0.7f);
            }

            // Sin bano de luz sobre la pintura: se probo un quad con degradado calido encima de la
            // obra y el usuario lo retiro (2026-11-18). La lampara solo lleva su boca encendida.
        }

        private static GameObject CreateLampPart(PrimitiveType type, string name, Transform parent,
                                                 Vector3 localPos, Quaternion localRot, Vector3 localScale,
                                                 Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            // Sin collider: la lampara no debe estorbar a los rayos de seleccion ni de pared.
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            if (material != null) go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        // Material compartido de la boca encendida. Usa Sprites/Default (o UI/Default): estan en
        // "Always Included Shaders", no dependen de keywords de URP (cuyas variantes pueden no
        // entrar al build) y no tienen culling.
        private static Material _lampGlowMaterial;

        private static Shader LampOverlayShader() =>
            Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");

        private static Material LampGlowMaterial()
        {
            if (_lampGlowMaterial != null) return _lampGlowMaterial;
            var shader = LampOverlayShader();
            if (shader == null) return null;
            _lampGlowMaterial = new Material(shader) { name = "LampGlow" };
            _lampGlowMaterial.color = new Color(1f, 0.97f, 0.86f, 1f);
            return _lampGlowMaterial;
        }
    }
}
