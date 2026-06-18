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

            var lamp = new GameObject(kName);
            lamp.transform.SetParent(parent, false);
            lamp.transform.localPosition = new Vector3(0f, h / 2f + 0.12f, 0.18f);
            lamp.transform.localRotation = Quaternion.Euler(55f, 0f, 0f); // apunta abajo-atras al cuadro

            var fixture = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fixture.name = "LamparaFixture";
            Object.Destroy(fixture.GetComponent<Collider>());
            fixture.transform.SetParent(lamp.transform, false);
            fixture.transform.localScale = new Vector3(Mathf.Min(0.18f, w * 0.5f), 0.03f, 0.04f);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color c = FrameBarColor(tier);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                fixture.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var light = lamp.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 45f;
            light.range = 1.2f;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.96f, 0.88f); // calido museo
            light.shadows = LightShadows.None;          // barato para Quest
        }
    }
}
