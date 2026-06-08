using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Generador parametrico de salas de museo para VR (Meta Quest, URP).
    ///
    /// Construye la estructura de una sala (piso, techo, 4 paredes con grosor), una
    /// pared central divisoria con vanos para circular y una rejilla de iluminacion
    /// general en el techo. NO genera PlayerSpawnPoint: el jugador conserva su posicion
    /// (junto al menu). Es idempotente: si ya existe "GalleryRoot" lo borra y reconstruye.
    ///
    /// NO genera anchors ni placeholders de cuadros. El colgado de obras es DINAMICO
    /// en runtime: VRWallHangingController raycastea contra el layer "VRWall" donde el
    /// usuario apunta, cuelga la obra ahi y persiste posicion/rotacion via
    /// GalleryPersistenceService. VRGalleryController.SpawnSavedPaintings re-instancia
    /// las obras guardadas al re-entrar. Por eso la sala solo necesita aportar:
    ///   - Paredes en layer "VRWall" (blanco del raycast de colgado).
    ///   - Piso en layer "Teleportable" (destino del teleport).
    ///   - Iluminacion (horneada) para que la sala se vea.
    ///
    /// PRIORIDAD: rendimiento en Quest.
    ///  - Geometria minima: cada superficie es UN cubo escalado (6 meshes en total).
    ///  - Toda la estructura se marca STATIC (Contribute GI) para hornear lightmaps.
    ///  - Las luces se crean como Baked (NO Realtime).
    ///
    /// QUE DEBES HACER TU DESPUES (manual):
    ///  1. Asignar materiales bonitos (ver bloque CONFIG: rutas opcionales o reemplaza
    ///     el material en los objetos generados; son compartidos por superficie).
    ///  2. Window > Rendering > Lighting > Generate Lighting para hornear los lightmaps.
    /// </summary>
    public static class GalleryGenerator
    {
        // ════════════════════════════════════════════════════════════════════════
        //  CONFIG  — edita estos valores libremente y vuelve a ejecutar el menu.
        // ════════════════════════════════════════════════════════════════════════

        // --- Dimensiones de la sala (metros) ---
        private const float roomLength   = 30f;   // eje X
        private const float roomWidth    = 12f;   // eje Z
        private const float roomHeight   = 4f;    // eje Y
        private const float wallThickness = 0.2f;

        // --- Pared central divisoria (a lo largo de X, en Z=0) ---
        //     Parte la sala en 2 naves de roomLength x (roomWidth/2). Va en layer VRWall:
        //     se pueden colgar obras en sus DOS caras. Lleva aberturas para circular entre
        //     las naves; el resto se reparte en segmentos de pared.
        private const bool  buildCentralDivider  = true;
        private const int   dividerOpenings      = 5;     // numero de vanos para pasar (impar => vano central en X=0)
        private const float dividerOpeningWidth  = 2.5f;  // ancho de cada vano (m)

        // --- Colores placeholder (editables; en el futuro re-tinta la sala aqui) ---
        private static readonly Color floorColor   = new Color(0.18f, 0.18f, 0.20f, 1f); // gris oscuro
        private static readonly Color wallColor     = new Color(0.85f, 0.83f, 0.78f, 1f); // hueso (re-tintable)
        private static readonly Color ceilingColor  = new Color(0.92f, 0.92f, 0.92f, 1f); // casi blanco

        // --- Materiales opcionales por RUTA (Assets/.../MiMaterial.mat) ---
        //     Si la ruta esta vacia o no existe, se crea un material URP/Lit por defecto
        //     con el color de arriba (asi nunca sale rosa por shader perdido).
        //     Para asignar uno tuyo: arrastra el .mat al proyecto, copia su ruta aqui.
        private const string floorMaterialPath   = "Assets/ArtUnbound/Materials/Duela.mat";
        private const string wallMaterialPath     = "";
        private const string ceilingMaterialPath  = "";

        // Carpeta donde se guardan los materiales generados como .mat (para que
        // PERSISTAN y el prefab no salga rosa). Se crea sola si no existe.
        private const string generatedMaterialsFolder = "Assets/ArtUnbound/Materials/Gallery";

        // --- Layers (DEBEN existir en Project Settings > Tags and Layers) ---
        //  - El piso va en "Teleportable" para que el jugador pueda teletransportarse.
        //  - Las paredes van en "VRWall" para que el sistema de colgado las detecte
        //    (VRWallHangingController raycastea contra ese layer).
        //  - El techo se deja en Default (no se cuelga ni se camina en el).
        private const string floorLayerName   = "Teleportable";
        private const string wallLayerName     = "VRWall";
        private const string ceilingLayerName  = "Default";

        // --- Toggles ---
        private const bool generateLights = true;  // rejilla de luces baked en el techo
        // Agrega un TeleportationArea (XRI) al piso. El auto-wiring del proyecto solo
        // cubre MeshColliders; como el piso es un cubo (BoxCollider), lo agregamos aqui
        // para que el teleport funcione sin pasos manuales. Se auto-conecta al provider
        // del rig en runtime.
        private const bool addFloorTeleportationArea = true;

        // --- Rejilla de iluminacion general (BAKED) ---
        //     Spots apuntando hacia abajo, repartidos uniformemente en el techo.
        //     NO estan atados a posiciones de obras: solo iluminan la sala.
        private const int   lightColumns        = 6;    // luces a lo largo de X
        private const int   lightRows           = 2;    // luces a lo largo de Z
        private const float lightDropFromCeiling = 0.3f; // cuanto baja desde el techo
        private const float lightSpotAngle       = 90f;
        private const float lightRange           = 8f;
        private const float lightIntensity       = 2.5f;

        // --- Nombres de jerarquia ---
        private const string RootName       = "GalleryRoot";
        private const string StructureName  = "Structure";
        private const string LightsName     = "Lights";

        // ════════════════════════════════════════════════════════════════════════
        //  MENU PRINCIPAL
        // ════════════════════════════════════════════════════════════════════════

        [MenuItem("Tools/ArtUnbound/Generate Gallery")]
        public static void GenerateGallery()
        {
            // Idempotencia: borra cualquier GalleryRoot previo antes de reconstruir.
            DestroyExistingRoot();

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            var structure = new GameObject(StructureName);
            structure.transform.SetParent(root.transform, false);

            // Materiales (compartidos por superficie para permitir batching estatico).
            Material floorMat   = ResolveMaterial(floorMaterialPath,   "GalleryFloor_Mat",   floorColor);
            Material wallMat     = ResolveMaterial(wallMaterialPath,     "GalleryWall_Mat",     wallColor);
            Material ceilingMat  = ResolveMaterial(ceilingMaterialPath,  "GalleryCeiling_Mat",  ceilingColor);

            BuildShell(structure.transform, floorMat, wallMat, ceilingMat);

            if (buildCentralDivider)
                BuildCentralDivider(structure.transform, wallMat);

            if (generateLights)
            {
                var lights = new GameObject(LightsName);
                lights.transform.SetParent(root.transform, false);
                BuildLightingGrid(lights.transform);
            }

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log($"[GalleryGenerator] Sala {roomLength}x{roomWidth}x{roomHeight} generada " +
                      $"(estructura + pared central con {dividerOpenings} vanos + iluminacion). " +
                      "El colgado de obras es dinamico en runtime (VRWallHangingController). " +
                      "RECUERDA: asignar materiales finales y hornear " +
                      "(Window > Rendering > Lighting > Generate Lighting).");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  MENUS AUXILIARES
        // ════════════════════════════════════════════════════════════════════════

        [MenuItem("Tools/ArtUnbound/Clear Gallery")]
        public static void ClearGallery()
        {
            if (DestroyExistingRoot())
                Debug.Log("[GalleryGenerator] GalleryRoot eliminado.");
            else
                Debug.LogWarning("[GalleryGenerator] No habia GalleryRoot que borrar.");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  CONSTRUCCION DE LA ESTRUCTURA (cubos escalados, todo static)
        // ════════════════════════════════════════════════════════════════════════

        private static void BuildShell(Transform parent, Material floorMat, Material wallMat, Material ceilingMat)
        {
            float halfL = roomLength * 0.5f;
            float halfW = roomWidth  * 0.5f;
            // El piso/techo cubren tambien debajo de las paredes (footprint + grosor).
            float footprintX = roomLength + wallThickness * 2f;
            float footprintZ = roomWidth  + wallThickness * 2f;

            // Piso: su cara superior queda exactamente en Y = 0. Layer Teleportable.
            var floor = CreateBox(parent, "Floor",
                position: new Vector3(0f, -wallThickness * 0.5f, 0f),
                scale:    new Vector3(footprintX, wallThickness, footprintZ),
                material: floorMat,
                layerName: floorLayerName);
            if (addFloorTeleportationArea)
                AddFloorTeleport(floor);

            // Techo: su cara inferior queda en Y = roomHeight.
            CreateBox(parent, "Ceiling",
                position: new Vector3(0f, roomHeight + wallThickness * 0.5f, 0f),
                scale:    new Vector3(footprintX, wallThickness, footprintZ),
                material: ceilingMat,
                layerName: ceilingLayerName);

            // Paredes Norte/Sur (largas, a lo largo de X). Cara interior en Z = +-halfW.
            // Layer VRWall para que el sistema de colgado las detecte.
            CreateBox(parent, "Wall_North",
                position: new Vector3(0f, roomHeight * 0.5f, halfW + wallThickness * 0.5f),
                scale:    new Vector3(footprintX, roomHeight, wallThickness),
                material: wallMat,
                layerName: wallLayerName);

            CreateBox(parent, "Wall_South",
                position: new Vector3(0f, roomHeight * 0.5f, -halfW - wallThickness * 0.5f),
                scale:    new Vector3(footprintX, roomHeight, wallThickness),
                material: wallMat,
                layerName: wallLayerName);

            // Paredes Este/Oeste (cortas, a lo largo de Z). Cara interior en X = +-halfL.
            CreateBox(parent, "Wall_East",
                position: new Vector3(halfL + wallThickness * 0.5f, roomHeight * 0.5f, 0f),
                scale:    new Vector3(wallThickness, roomHeight, roomWidth),
                material: wallMat,
                layerName: wallLayerName);

            CreateBox(parent, "Wall_West",
                position: new Vector3(-halfL - wallThickness * 0.5f, roomHeight * 0.5f, 0f),
                scale:    new Vector3(wallThickness, roomHeight, roomWidth),
                material: wallMat,
                layerName: wallLayerName);
        }

        /// <summary>Crea un cubo escalado, le asigna material/layer y lo marca STATIC (Contribute GI).</summary>
        private static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale,
                                            Material material, string layerName)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;

            ApplyLayer(go, layerName);

            // STATIC: imprescindible para hornear lightmaps en Quest. isStatic = true
            // activa todos los flags (Contribute GI, Batching, Occluder/Occludee, etc.).
            go.isStatic = true;
            return go;
        }

        /// <summary>
        /// Agrega un TeleportationArea (XRI) al piso para que el jugador pueda teletransportarse.
        /// Usa el BoxCollider del cubo como superficie y la Interaction Layer "Teleport".
        /// El TeleportationProvider se auto-resuelve en runtime si no esta asignado.
        /// </summary>
        private static void AddFloorTeleport(GameObject floor)
        {
            if (floor.GetComponent<BaseTeleportationInteractable>() != null) return;

            var area = floor.AddComponent<TeleportationArea>();
            int teleportInteractionLayer = InteractionLayerMask.GetMask("Teleport");
            if (teleportInteractionLayer != 0)
                area.interactionLayers = teleportInteractionLayer;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  PARED CENTRAL DIVISORIA (con vanos para circular)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Construye una pared a lo largo de X (en Z=0) partida en segmentos con vanos.
        /// Patron: [segmento][vano][segmento]...[segmento]. Con G vanos hay G+1 segmentos,
        /// repartiendo el largo restante por igual. Cada segmento es un cubo en layer VRWall
        /// (se puede colgar en ambas caras) y se marca STATIC para hornear.
        /// </summary>
        private static void BuildCentralDivider(Transform parent, Material wallMat)
        {
            int openings = Mathf.Max(0, dividerOpenings);
            int segments = openings + 1;

            float openingsTotal = openings * dividerOpeningWidth;
            float wallTotal = roomLength - openingsTotal;
            if (wallTotal <= 0f)
            {
                Debug.LogWarning("[GalleryGenerator] Los vanos no caben en la pared central. " +
                                 "Reduce dividerOpenings o dividerOpeningWidth.");
                return;
            }

            float segmentWidth = wallTotal / segments;

            // Recorre de -roomLength/2 a +roomLength/2 alternando segmento y vano.
            float cursor = -roomLength * 0.5f;
            for (int s = 0; s < segments; s++)
            {
                float centerX = cursor + segmentWidth * 0.5f;
                CreateBox(parent, $"WallCenter_{s:00}",
                    position: new Vector3(centerX, roomHeight * 0.5f, 0f),
                    scale:    new Vector3(segmentWidth, roomHeight, wallThickness),
                    material: wallMat,
                    layerName: wallLayerName);

                cursor += segmentWidth;
                if (s < segments - 1) cursor += dividerOpeningWidth; // salta el vano
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ILUMINACION GENERAL (rejilla baked en el techo, no atada a obras)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Reparte spots baked apuntando hacia abajo en una rejilla uniforme bajo el techo.
        /// Iluminacion ambiental de la sala — NO marca posiciones de cuadros.
        /// </summary>
        private static void BuildLightingGrid(Transform parent)
        {
            float lightY = roomHeight - lightDropFromCeiling;
            int cols = Mathf.Max(1, lightColumns);
            int rows = Mathf.Max(1, lightRows);

            // Reparte los centros uniformemente dejando medio paso de margen en los bordes.
            int index = 0;
            for (int r = 0; r < rows; r++)
            {
                float tz = rows == 1 ? 0.5f : (r + 0.5f) / rows;
                float z = Mathf.Lerp(-roomWidth * 0.5f, roomWidth * 0.5f, tz);

                for (int c = 0; c < cols; c++)
                {
                    float tx = cols == 1 ? 0.5f : (c + 0.5f) / cols;
                    float x = Mathf.Lerp(-roomLength * 0.5f, roomLength * 0.5f, tx);

                    CreateBakedDownSpot(parent, new Vector3(x, lightY, z), index);
                    index++;
                }
            }
        }

        /// <summary>
        /// Crea un spot apuntando recto hacia abajo. Marcado BAKED: la iluminacion final
        /// se hornea, NO se calcula en runtime (clave para Quest).
        /// </summary>
        private static void CreateBakedDownSpot(Transform parent, Vector3 position, int index)
        {
            var go = new GameObject($"CeilingSpot_{index:00}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            var light = go.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = lightSpotAngle;
            light.range = lightRange;
            light.intensity = lightIntensity;
            light.color = Color.white;
            // BAKED: no Realtime. Cambia a LightmapBakeType.Mixed si quieres sombras
            // dinamicas, pero para Quest lo mas barato es Baked.
            light.lightmapBakeType = LightmapBakeType.Baked;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  MATERIALES (URP, con fallback para no salir rosa)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Carga el material de la ruta dada; si no hay, crea uno URP/Lit por defecto.</summary>
        private static Material ResolveMaterial(string assetPath, string defaultName, Color fallbackColor)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                var loaded = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                if (loaded != null) return loaded;
                Debug.LogWarning($"[GalleryGenerator] No se encontro material en '{assetPath}'. " +
                                 "Usando material URP por defecto.");
            }
            return CreateUrpMaterial(defaultName, fallbackColor);
        }

        /// <summary>
        /// Crea (o actualiza) un material URP/Lit GUARDADO como .mat en el proyecto.
        /// Guardarlo como asset es lo que evita el rosa al hacer prefab/recargar:
        /// un material 'new Material()' en memoria no persiste y su referencia se rompe.
        /// </summary>
        private static Material CreateUrpMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("[GalleryGenerator] No se encontro 'Universal Render Pipeline/Lit'. " +
                                 "El proyecto deberia estar en URP. Cayendo a 'Standard'.");
                shader = Shader.Find("Standard");
            }

            EnsureFolder(generatedMaterialsFolder);
            string path = $"{generatedMaterialsFolder}/{name}.mat";

            // Reusa el .mat si ya existe (idempotente); si no, lo crea como asset.
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            // URP/Lit usa _BaseColor; Standard usa _Color. Asignamos ambos por seguridad.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        /// <summary>Crea la carpeta (recursivamente) si no existe, en formato AssetDatabase.</summary>
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Asigna el layer por nombre. Si no existe en el proyecto, avisa y deja Default.</summary>
        private static void ApplyLayer(GameObject go, string layerName)
        {
            if (string.IsNullOrEmpty(layerName)) return;
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"[GalleryGenerator] El layer '{layerName}' no existe en " +
                                 $"Project Settings > Tags and Layers. '{go.name}' queda en Default.");
                return;
            }
            go.layer = layer;
        }

        /// <summary>Borra el GalleryRoot existente (idempotencia). Devuelve true si borro algo.</summary>
        private static bool DestroyExistingRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing == null) return false;
            Object.DestroyImmediate(existing);
            return true;
        }
    }
}
