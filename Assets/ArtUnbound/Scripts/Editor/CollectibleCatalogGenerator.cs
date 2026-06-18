using System;
using System.Collections.Generic;
using System.Linq;
using ArtUnbound.Data;
using UnityEditor;
using UnityEngine;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Genera (o actualiza) los assets de coleccionables y el CollectibleCatalog a partir del
    /// catalogo de obras real + las reglas del GDD 8.6. Texto en INGLES y umbral FIJO (calculado
    /// del catalogo al momento de generar). Idempotente: actualiza por id, no duplica; conserva el
    /// orden existente del catalogo y solo agrega los coleccionables nuevos al final.
    ///
    /// Tras correrlo, el usuario edita textos/iconos/prefabs y reordena libremente.
    /// </summary>
    public static class CollectibleCatalogGenerator
    {
        private const string DefinitionsFolder = "Assets/ArtUnbound/Data/Collectibles";
        private const string CatalogPath = "Assets/ArtUnbound/Data/CollectibleCatalog.asset";

        private class Spec
        {
            public string id, title, condition, themeKey;
            public CollectibleKind kind;
            public int threshold;
            public bool hangable;
        }

        [MenuItem("Tools/ArtUnbound/Generate Collectibles")]
        public static void Generate()
        {
            var catalog = FindArtworkCatalog();
            if (catalog == null || catalog.artworks == null)
            {
                Debug.LogError("[Collectibles] No se encontro un ArtworkCatalog con obras. Aborta.");
                return;
            }

            var specs = BuildSpecs(catalog);

            EnsureFolder(DefinitionsFolder);
            var defs = new List<CollectibleDefinition>();
            foreach (var s in specs)
                defs.Add(CreateOrUpdate(s));

            // Catalogo: conserva el orden existente, agrega los nuevos al final.
            var cat = AssetDatabase.LoadAssetAtPath<CollectibleCatalog>(CatalogPath);
            bool newCatalog = cat == null;
            if (newCatalog)
            {
                cat = ScriptableObject.CreateInstance<CollectibleCatalog>();
                AssetDatabase.CreateAsset(cat, CatalogPath);
            }
            cat.collectibles ??= new List<CollectibleDefinition>();
            cat.collectibles.RemoveAll(c => c == null);
            foreach (var d in defs)
                if (!cat.collectibles.Contains(d))
                    cat.collectibles.Add(d);

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = cat;
            Debug.Log($"[Collectibles] {defs.Count} coleccionables generados/actualizados. Catalogo: {CatalogPath}" +
                      (newCatalog ? " (nuevo)" : " (orden existente conservado)") +
                      ". RECUERDA: asignar el CollectibleCatalog al campo 'collectibleCatalog' de GameBootstrap.");
        }

        private static List<Spec> BuildSpecs(ArtworkCatalog catalog)
        {
            var all = catalog.artworks.Where(a => a != null).ToList();
            var specs = new List<Spec>();

            // 1) Estatus (GDD 8.4): el hero muestra el RANGO dinamico (Visitor -> Patron) + agregados;
            // el texto del asset es solo etiqueta para Collection / fallback.
            specs.Add(new Spec { id = "status", kind = CollectibleKind.Status, themeKey = "",
                threshold = 1, hangable = true, title = "Museum Status", condition = "Your museum at a glance" });

            // 2) Comportamiento. "On Display" mide obras exhibidas A LA VEZ (no acumuladas); "Curator"
            // quedo reservado como rango de la placa de estatus (GDD 8.6).
            specs.Add(new Spec { id = "behavior_hung_first", kind = CollectibleKind.Behavior, themeKey = "hung_first",
                threshold = 1, hangable = true, title = "First Hung", condition = "Hung your first artwork" });
            specs.Add(new Spec { id = "behavior_curator", kind = CollectibleKind.Behavior, themeKey = "on_display",
                threshold = 5, hangable = true, title = "On Display", condition = "5 artworks on display at once" });

            // 3) Autor (Maestro + Gran Maestro)
            foreach (var g in all.Where(a => !string.IsNullOrEmpty(a.author))
                                 .GroupBy(a => a.author, StringComparer.OrdinalIgnoreCase))
            {
                int count = g.Count();
                string author = g.Key;
                bool isVanGogh = author.IndexOf("van Gogh", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isDaVinci = author.IndexOf("da Vinci", StringComparison.OrdinalIgnoreCase) >= 0;
                if (count >= 6 || isDaVinci)
                {
                    int maestro = isVanGogh ? 15 : count;
                    specs.Add(new Spec { id = $"master_author_{Slug(author)}", kind = CollectibleKind.AuthorMaster,
                        themeKey = author, threshold = maestro, hangable = true,
                        title = $"{ShortAuthor(author)} — Master",
                        condition = isVanGogh ? "Completed 15 works" : $"Completed all {count} works" });
                }
                if (isVanGogh)
                {
                    specs.Add(new Spec { id = $"grandmaster_author_{Slug(author)}", kind = CollectibleKind.AuthorGrandMaster,
                        themeKey = author, threshold = count, hangable = true,
                        title = $"{ShortAuthor(author)} — Grand Master", condition = $"Completed all {count} works" });
                }
            }

            // 4) Movimiento (Maestro + Gran Maestro)
            foreach (var g in all.Where(a => !string.IsNullOrEmpty(a.artMovement))
                                 .GroupBy(a => a.artMovement, StringComparer.OrdinalIgnoreCase))
            {
                int count = g.Count();
                if (count <= 10) continue;
                string mv = g.Key;
                int maestro = count > 25 ? 25 : count;
                specs.Add(new Spec { id = $"master_movement_{Slug(mv)}", kind = CollectibleKind.MovementMaster,
                    themeKey = mv, threshold = maestro, hangable = true,
                    title = $"{mv} — Master", condition = count > 25 ? "Reached 25 works" : $"Completed all {count} works" });
                if (count > 25)
                {
                    specs.Add(new Spec { id = $"grandmaster_movement_{Slug(mv)}", kind = CollectibleKind.MovementGrandMaster,
                        themeKey = mv, threshold = count, hangable = true,
                        title = $"{mv} — Grand Master", condition = $"Completed all {count} works" });
                }
            }

            // 5) Mejoras de presentacion (nombres EN acordados). Ademas de desbloquear la funcion
            //    (cedula/marco/lampara) al cruzar el umbral, otorgan una PLACA colgable (hangable=true)
            //    que el jugador decide si colgar; se otorga en CollectibleService por umbral de obras.
            specs.Add(new Spec { id = "upgrade_cedula", kind = CollectibleKind.Cedula, themeKey = "",
                threshold = 10, hangable = true, title = "Museum Label", condition = "Complete 10 artworks" });
            specs.Add(new Spec { id = "upgrade_frame", kind = CollectibleKind.Frame, themeKey = "",
                threshold = 25, hangable = true, title = "Frame", condition = "Complete 25 artworks" });
            specs.Add(new Spec { id = "upgrade_lamp", kind = CollectibleKind.Lamp, themeKey = "",
                threshold = 50, hangable = true, title = "Picture Light", condition = "Complete 50 artworks" });

            return specs;
        }

        private static CollectibleDefinition CreateOrUpdate(Spec s)
        {
            string path = $"{DefinitionsFolder}/{s.id}.asset";
            var def = AssetDatabase.LoadAssetAtPath<CollectibleDefinition>(path);
            bool isNew = def == null;
            if (isNew) def = ScriptableObject.CreateInstance<CollectibleDefinition>();

            // Datos derivados de reglas (se actualizan). icon/visualPrefab se PRESERVAN al regenerar.
            def.id = s.id;
            def.kind = s.kind;
            def.title = s.title;
            def.conditionText = s.condition;
            def.themeKey = s.themeKey;
            def.threshold = s.threshold;
            def.hangable = s.hangable;

            if (isNew) AssetDatabase.CreateAsset(def, path);
            EditorUtility.SetDirty(def);
            return def;
        }

        private static ArtworkCatalog FindArtworkCatalog()
        {
            var guids = AssetDatabase.FindAssets("t:ArtworkCatalog");
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<ArtworkCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static string Slug(string s) =>
            new string((s ?? string.Empty).ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        private static string ShortAuthor(string author)
        {
            if (author.IndexOf("van Gogh", StringComparison.OrdinalIgnoreCase) >= 0) return "Van Gogh";
            if (author.IndexOf("da Vinci", StringComparison.OrdinalIgnoreCase) >= 0) return "Da Vinci";
            if (author.IndexOf("Rembrandt", StringComparison.OrdinalIgnoreCase) >= 0) return "Rembrandt";
            var parts = author.Split(' ');
            return parts.Length > 0 ? parts[parts.Length - 1] : author;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
