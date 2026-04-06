using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArtUnbound.Feedback
{
    /// <summary>
    /// Glowing golden particle trail that guides the user's gaze from a selected
    /// artwork card to the detail panel.
    ///
    /// Visual approach:
    ///   - Runtime-generated radial-gradient texture (bright centre → transparent edge)
    ///   - Billboard quads (always face the camera) with additive blending → glow effect
    ///   - Dual-layer per particle: small bright core + larger soft halo
    ///   - Starburst at the origin and a flash at the destination
    /// </summary>
    public class SelectionTrailController : MonoBehaviour
    {
        public static SelectionTrailController Instance { get; private set; }

        [Header("Trail")]
        [SerializeField] private int   particleCount  = 20;
        [SerializeField] private float travelDuration = 0.45f;  // each particle's travel time
        [SerializeField] private float spawnInterval  = 0.018f; // delay between launches
        [SerializeField] private float arcHeight      = 0.10f;  // Bézier mid-point Y lift
        [SerializeField] private float scatter        = 0.012f; // random XY scatter off the arc

        [Header("Particle Size")]
        [SerializeField] private float coreStartSize  = 0.022f;
        [SerializeField] private float coreEndSize    = 0.006f;
        [SerializeField] private float haloMultiplier = 2.8f;   // halo is this × core size

        [Header("Colours")]
        [SerializeField] private Color coreColor  = new Color(1.00f, 0.98f, 0.85f, 1f); // near-white
        [SerializeField] private Color haloColor  = new Color(1.00f, 0.72f, 0.10f, 0.55f); // gold
        [SerializeField] private Color burstColor = new Color(1.00f, 0.88f, 0.40f, 1f); // bright gold

        [Header("Starburst (origin)")]
        [SerializeField] private float burstDuration = 0.35f;
        [SerializeField] private float burstMaxSize  = 0.18f;
        [SerializeField] private int   burstRayCount = 6;

        [Header("Arrival Flash (destination)")]
        [SerializeField] private float flashDuration = 0.40f;
        [SerializeField] private float flashMaxSize  = 0.20f;

        // ── shared glow texture (created once, reused for all particles) ──────
        private static Texture2D _glowTex;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else                  Destroy(gameObject);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Public API
        // ─────────────────────────────────────────────────────────────────────

        public void PlayTrail(Vector3 origin, Vector3 destination)
        {
            StartCoroutine(RunTrail(origin, destination));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Coroutine
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator RunTrail(Vector3 origin, Vector3 destination)
        {
            EnsureGlowTexture();

            // Starburst fires immediately at origin
            SpawnStarburst(origin);

            // Bézier arc control point
            Vector3 ctrl = Vector3.Lerp(origin, destination, 0.5f);
            ctrl.y += arcHeight;

            for (int i = 0; i < particleCount; i++)
            {
                // Scatter: small random XY offset baked into each particle's control point
                Vector3 scatter3 = new Vector3(
                    Random.Range(-scatter, scatter),
                    Random.Range(-scatter, scatter),
                    0f);

                SpawnParticle(origin, ctrl + scatter3, destination);
                yield return new WaitForSeconds(spawnInterval);
            }

            // Wait for first particle to arrive, then flash
            float remaining = travelDuration - spawnInterval * particleCount;
            if (remaining > 0f) yield return new WaitForSeconds(remaining);

            SpawnArrivalFlash(destination);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Spawn helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SpawnParticle(Vector3 from, Vector3 ctrl, Vector3 to)
        {
            // Halo layer (larger, gold, semi-transparent)
            var halo = MakeGlowQuad("TrailHalo", from, haloColor, coreStartSize * haloMultiplier);
            var haAnim = halo.AddComponent<TrailParticleAnimator>();
            haAnim.Initialize(from, ctrl, to, travelDuration,
                              coreStartSize * haloMultiplier, coreEndSize * haloMultiplier,
                              fadeStart: 0.5f);

            // Core layer (smaller, near-white, fully opaque)
            var core = MakeGlowQuad("TrailCore", from, coreColor, coreStartSize);
            var coAnim = core.AddComponent<TrailParticleAnimator>();
            coAnim.Initialize(from, ctrl, to, travelDuration,
                              coreStartSize, coreEndSize,
                              fadeStart: 0.65f);
        }

        private void SpawnStarburst(Vector3 position)
        {
            // Central glow sphere
            var glow = MakeGlowQuad("BurstGlow", position, burstColor, 0f);
            var ga = glow.AddComponent<ScaleAndFadeAnimator>();
            ga.Initialize(burstDuration, burstMaxSize * 1.5f, delay: 0f);

            // Rays: thin elongated quads radiating outward
            for (int i = 0; i < burstRayCount; i++)
            {
                float angle = i * (360f / burstRayCount);
                var ray = MakeGlowQuad("BurstRay", position, burstColor, 0f);
                ray.transform.localScale = new Vector3(burstMaxSize * 0.08f, burstMaxSize * 0.6f, 1f);

                var ra = ray.AddComponent<BurstRayAnimator>();
                ra.Initialize(burstDuration, burstMaxSize, angle);
            }
        }

        private void SpawnArrivalFlash(Vector3 position)
        {
            // Two-ring flash: inner bright + outer wide soft
            var inner = MakeGlowQuad("FlashInner", position, coreColor,  0f);
            var io = inner.AddComponent<ScaleAndFadeAnimator>();
            io.Initialize(flashDuration, flashMaxSize * 0.6f, delay: 0f);

            var outer = MakeGlowQuad("FlashOuter", position, burstColor, 0f);
            var oo = outer.AddComponent<ScaleAndFadeAnimator>();
            oo.Initialize(flashDuration, flashMaxSize, delay: 0.04f);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Quad / material factory
        // ─────────────────────────────────────────────────────────────────────

        private static GameObject MakeGlowQuad(string goName, Vector3 worldPos, Color color, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = worldPos;
            go.transform.localScale = size > 0f ? Vector3.one * size : Vector3.zero;

            var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));

            // Additive blending: SrcBlend=One, DstBlend=One → particles ADD light to the scene
            mat.SetInt("_SrcBlend",   (int)BlendMode.One);
            mat.SetInt("_DstBlend",   (int)BlendMode.One);
            mat.SetInt("_ZWrite",     0);
            mat.renderQueue = 3000;

            mat.color = color;
            if (_glowTex != null)
            {
                mat.mainTexture = _glowTex;
                if (mat.HasProperty("_MainTex"))    mat.SetTexture("_MainTex",  _glowTex);
                if (mat.HasProperty("_BaseMap"))     mat.SetTexture("_BaseMap",  _glowTex);
            }

            go.GetComponent<Renderer>().material = mat;

            // Billboard component makes the quad always face the camera
            go.AddComponent<BillboardToCamera>();

            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Glow texture (32×32 radial gradient)
        // ─────────────────────────────────────────────────────────────────────

        private static void EnsureGlowTexture()
        {
            if (_glowTex != null) return;

            const int size = 32;
            _glowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _glowTex.wrapMode = TextureWrapMode.Clamp;
            float centre = size * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(centre, centre));
                float norm  = Mathf.Clamp01(dist / centre);
                float alpha = Mathf.Pow(1f - norm, 2.2f); // smooth quadratic falloff
                _glowTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            _glowTex.Apply();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Inner behaviour components
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Makes the quad face the camera every frame.</summary>
        private class BillboardToCamera : MonoBehaviour
        {
            private void LateUpdate()
            {
                var cam = Camera.main;
                if (cam == null) return;
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }

        /// <summary>Moves along a quadratic Bézier, shrinks and fades.</summary>
        private class TrailParticleAnimator : MonoBehaviour
        {
            private Vector3 p0, p1, p2;
            private float   duration, startSize, endSize, fadeStart, elapsed;
            private Material mat;

            public void Initialize(Vector3 from, Vector3 ctrl, Vector3 to,
                                   float dur, float sStart, float sEnd, float fadeStart)
            {
                p0 = from; p1 = ctrl; p2 = to;
                duration = dur; startSize = sStart; endSize = sEnd;
                this.fadeStart = fadeStart;
                elapsed = 0f;
                mat = GetComponent<Renderer>().material;
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float u = 1f - t;

                transform.position   = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                transform.localScale = Vector3.one * Mathf.Lerp(startSize, endSize, t);

                if (t > fadeStart)
                {
                    Color c = mat.color;
                    c.a = Mathf.Lerp(1f, 0f, (t - fadeStart) / (1f - fadeStart));
                    mat.color = c;
                }

                if (t >= 1f) Destroy(gameObject);
            }
        }

        /// <summary>Scales up and fades — used for arrival flash rings.</summary>
        private class ScaleAndFadeAnimator : MonoBehaviour
        {
            private float duration, maxSize, delay, elapsed;
            private Material mat;

            public void Initialize(float dur, float size, float delay)
            {
                duration = dur; maxSize = size; this.delay = delay;
                elapsed = -delay;
                mat = GetComponent<Renderer>().material;
            }

            private void Update()
            {
                elapsed += Time.deltaTime;
                if (elapsed < 0f) return;

                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.SmoothStep(0f, maxSize, Mathf.Clamp01(t * 2.5f));
                transform.localScale = Vector3.one * scale;

                Color c = mat.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                mat.color = c;

                if (t >= 1f) Destroy(gameObject);
            }
        }

        /// <summary>Starburst ray: billboard quad that elongates and fades outward.</summary>
        private class BurstRayAnimator : MonoBehaviour
        {
            private float duration, maxSize, elapsed;
            private float angleZ;
            private Material mat;

            public void Initialize(float dur, float size, float angleDeg)
            {
                duration = dur; maxSize = size; angleZ = angleDeg;
                elapsed = 0f;
                mat = GetComponent<Renderer>().material;
            }

            private void LateUpdate()
            {
                // Billboard first, then rotate the ray angle on top
                var cam = Camera.main;
                if (cam != null)
                    transform.rotation = Quaternion.LookRotation(
                        transform.position - cam.transform.position) *
                        Quaternion.Euler(0f, 0f, angleZ);

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float length = Mathf.SmoothStep(0f, maxSize * 0.65f, Mathf.Clamp01(t * 3f));
                float width  = maxSize * 0.04f;
                transform.localScale = new Vector3(width, length, 1f);

                Color c = mat.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                mat.color = c;

                if (t >= 1f) Destroy(gameObject);
            }
        }
    }
}
