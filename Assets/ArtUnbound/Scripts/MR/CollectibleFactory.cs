using ArtUnbound.Data;
using TMPro;
using UnityEngine;

namespace ArtUnbound.MR
{
    /// <summary>
    /// Construye un coleccionable COLGABLE (placa) a partir de un CollectibleDefinition. Si el
    /// asset tiene `visualPrefab`, lo instancia; si no, arma un molde procedural (quad coloreado por
    /// tier global + texto TMP titulo/condicion/fecha, grabado #241B12). Tag "PlacedArtwork" para
    /// reutilizar el flujo de grab/colgado.
    /// </summary>
    public static class CollectibleFactory
    {
        public static GameObject Build(CollectibleDefinition def, FrameTier tier, string earnedDate = null)
        {
            // Texto a mostrar: normalmente el del asset; la placa de estatus muestra rango + agregados
            // dinamicos (GDD 8.4), no su texto estatico.
            ResolveDisplayText(def, out string dispTitle, out string dispSub);

            // 1) Prefab visual asignado en el asset -> usarlo tal cual.
            if (def != null && def.visualPrefab != null)
            {
                var inst = Object.Instantiate(def.visualPrefab);
                inst.name = $"Collectible_{def.id}";
                inst.tag = "PlacedArtwork";
                int lyr = LayerMask.NameToLayer("PuzzlePiece");
                if (lyr >= 0) inst.layer = lyr;
                // Vuelca titulo/condicion/fecha en los TMP del prefab (si tiene CollectibleLabel),
                // asi un mismo prefab de placa sirve para todos los coleccionables.
                var label = inst.GetComponentInChildren<CollectibleLabel>(true);
                if (label != null)
                {
                    label.Apply(dispTitle, dispSub, earnedDate);
                    var data = ArtUnbound.Core.GameBootstrap.Instance != null
                        ? ArtUnbound.Core.GameBootstrap.Instance.SaveData : null;
                    if (data != null) label.ApplyTier(data.GetPlayerTier());
                }
                if (inst.GetComponent<Collider>() == null)
                {
                    var bc = inst.AddComponent<BoxCollider>();
                    bc.size = new Vector3(0.3f, 0.18f, 0.04f);
                }
                FaceContentTowardPlusZ(inst.transform);
                return inst;
            }

            // 2) Molde procedural (fallback).
            var root = new GameObject($"Collectible_{(def != null ? def.id : "unknown")}");
            root.tag = "PlacedArtwork";
            int layer = LayerMask.NameToLayer("PuzzlePiece");
            if (layer >= 0) root.layer = layer;

            const float w = 0.30f, h = 0.18f;

            var plate = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plate.name = "PlaquePlate";
            Object.Destroy(plate.GetComponent<Collider>());
            plate.transform.SetParent(root.transform, false);
            plate.transform.localScale = new Vector3(w, h, 1f);
            plate.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                Color c = PresentationDecorator.FrameBarColor(tier);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); else mat.color = c;
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.85f);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.65f);
                plate.GetComponent<Renderer>().sharedMaterial = mat;
            }

            var textGO = new GameObject("PlaqueText");
            textGO.transform.SetParent(root.transform, false);
            textGO.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            var tmp = textGO.AddComponent<TextMeshPro>();
            string date  = string.IsNullOrEmpty(earnedDate) ? string.Empty : $"\n<size=45%>{earnedDate}</size>";
            tmp.text = $"<b>{dispTitle}</b>\n<size=55%>{dispSub}</size>{date}";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.14f, 0.11f, 0.07f);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.01f;
            tmp.fontSizeMax = 0.06f;
            var rt = tmp.rectTransform;
            rt.localScale = Vector3.one;
            rt.sizeDelta = new Vector2(w * 0.9f, h * 0.85f);

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, 0.04f);
            FaceContentTowardPlusZ(root.transform);
            return root;
        }

        /// <summary>
        /// La cara de la placa (label, tornillos, texto) se arma sobre el lado -Z del prefab/molde,
        /// pero el flujo de colgado compartido (CheckNearbyWall/TryRepositionWallArtwork) orienta los
        /// objetos colocados con su +Z hacia el usuario — convencion correcta para las OBRAS, cuya cara
        /// es +Z. Sin compensar, la placa quedaria viendo a la pared. Volteamos el contenido 180° sobre
        /// Y (giro rigido: posicion + rotacion) para que la cara de la placa quede en +Z como las obras,
        /// asi se cuelga mirando al usuario y persiste/recarga consistente.
        /// La vista flotante de PlaqueView compensa este giro en GameBootstrap.OnHangPlaqueFromCollection.
        /// </summary>
        private static void FaceContentTowardPlusZ(Transform root)
        {
            var flip = Quaternion.Euler(0f, 180f, 0f);
            foreach (Transform child in root)
            {
                child.localPosition = flip * child.localPosition;
                child.localRotation = flip * child.localRotation;
            }
        }

        /// <summary>
        /// Resuelve el texto a mostrar. Normalmente es el del asset (title/conditionText). Para la
        /// placa de ESTATUS (GDD 8.4) muestra solo estado global: rango (hero, Visitor -> Patron) +
        /// agregados (obras y placas), calculados en runtime via CollectibleService.
        /// </summary>
        private static void ResolveDisplayText(CollectibleDefinition def, out string title, out string sub)
        {
            title = def != null ? def.title : string.Empty;
            sub   = def != null ? def.conditionText : string.Empty;
            if (def == null || def.kind != CollectibleKind.Status) return;

            var svc = ArtUnbound.Core.GameBootstrap.Instance != null
                ? ArtUnbound.Core.GameBootstrap.Instance.CollectibleService : null;
            if (svc == null) return;
            title = svc.GetStatusRank();
            sub   = svc.GetStatusAggregates();
        }
    }
}
