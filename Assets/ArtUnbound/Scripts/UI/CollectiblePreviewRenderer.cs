using System.Collections.Generic;
using ArtUnbound.Data;
using ArtUnbound.MR;
using TMPro;
using UnityEngine;

namespace ArtUnbound.UI
{
    /// <summary>
    /// Renderiza una miniatura 3D de un coleccionable COLGABLE (placa / estatus) a una RenderTexture
    /// para mostrarla en las tarjetas de Collection. Reusa CollectibleFactory.Build (mismo modelo,
    /// texto y material por tier que al colgar), coloca la instancia en un rig aislado lejos del juego,
    /// la fotografia con una camara ortografica dedicada (fondo transparente) y cachea por id+tier.
    ///
    /// No requiere capa ni prefab especiales: el rig vive en RigOrigin (muy lejos), su luz es puntual
    /// con rango corto (no afecta la escena), y la instancia se destruye tras renderizar.
    /// </summary>
    public class CollectiblePreviewRenderer : MonoBehaviour
    {
        private static CollectiblePreviewRenderer _instance;
        public static CollectiblePreviewRenderer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CollectiblePreviewRenderer");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CollectiblePreviewRenderer>();
                    _instance.Init();
                }
                return _instance;
            }
        }

        private const int DefaultSize = 256;
        private static readonly Vector3 RigOrigin = new Vector3(0f, 5000f, 0f);

        private Camera _cam;
        private readonly Dictionary<string, RenderTexture> _cache = new Dictionary<string, RenderTexture>();

        private void Init()
        {
            transform.position = RigOrigin;

            var camGO = new GameObject("PreviewCamera");
            camGO.transform.SetParent(transform, false);
            _cam = camGO.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // fondo transparente
            _cam.cullingMask = ~0;
            _cam.nearClipPlane = 0.01f;
            _cam.farClipPlane = 10f;
            _cam.enabled = false; // se renderiza a mano

            var lightGO = new GameObject("PreviewLight");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0.2f, 0.3f, -0.6f);
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Point;   // rango corto: no ilumina la escena principal (lejana)
            light.range = 8f;
            light.intensity = 2.2f;
        }

        /// <summary>Miniatura (cacheada) del coleccionable, o null si no se pudo construir.</summary>
        public Texture GetPreview(CollectibleDefinition def, int size = DefaultSize)
        {
            if (def == null) return null;
            FrameTier tier = PresentationDecorator.CurrentFrameTier();
            string key = $"{def.id}:{tier}";
            if (_cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var rt = Render(def, tier, size);
            if (rt != null) _cache[key] = rt;
            return rt;
        }

        private RenderTexture Render(CollectibleDefinition def, FrameTier tier, int size)
        {
            GameObject inst = CollectibleFactory.Build(def, tier);
            if (inst == null) return null;

            inst.transform.SetParent(transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            StripColliders(inst);

            // Asegura que el texto TMP tenga geometria antes de fotografiar.
            foreach (var t in inst.GetComponentsInChildren<TMP_Text>(true)) t.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();

            // Encuadre por bounds de los renderers (placa + remaches).
            Bounds b = ComputeBounds(inst);

            // Orientacion robusta: la camara encara la cara LEGIBLE (la del canvas/texto), sea cual
            // sea su rotacion en el prefab. Asi el status (canvas en una direccion) y las placas (con
            // su canvas en otra) se ven todas de frente; no depende de una convencion -Z fija.
            Transform face = null;
            var canvas = inst.GetComponentInChildren<Canvas>(true);
            if (canvas != null) face = canvas.transform;
            else { var t = inst.GetComponentInChildren<TMP_Text>(true); if (t != null) face = t.transform; }
            Vector3 fwd = face != null ? face.forward : Vector3.forward;
            Vector3 up  = face != null ? face.up : Vector3.up;

            float extent = Mathf.Max(b.extents.x, b.extents.y);
            _cam.orthographicSize = Mathf.Max(0.001f, extent * 1.18f);
            float dist = b.extents.magnitude + 0.5f;
            _cam.transform.position = b.center - fwd * dist;
            _cam.transform.rotation = Quaternion.LookRotation(fwd, up);
            _cam.nearClipPlane = 0.01f;
            _cam.farClipPlane = dist * 2f + 1f;

            var rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = $"Preview_{def.id}" };
            var prev = _cam.targetTexture;
            _cam.targetTexture = rt;
            _cam.Render();
            _cam.targetTexture = prev;

            // DestroyImmediate (no Destroy): PopulateCollection renderiza TODAS las placas en el
            // mismo frame y Destroy se difiere al final del frame. Con Destroy, las instancias previas
            // siguen en el rig al renderizar la siguiente y la camara las fotografia encimadas (se veian
            // los tornillos "dobles" por el offset entre placas de distinto tamano). DestroyImmediate
            // garantiza que el rig solo contenga la instancia actual en cada captura.
            DestroyImmediate(inst);
            return rt;
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, new Vector3(0.3f, 0.2f, 0.05f));
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
        }
    }
}
