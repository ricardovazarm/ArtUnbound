using ArtUnbound.Data;
using ArtUnbound.MR;
using UnityEngine;

namespace ArtUnbound.VR
{
    /// <summary>
    /// Builds the placed-artwork GameObject (quad + frame) used by both
    /// VRWallHangingController (at hang time) and VRGalleryController (at app startup).
    /// Mirrors the approach in WallAnchorManager.SpawnArtworkAtAnchor for consistency with MR.
    /// </summary>
    public static class PlacedArtworkFactory
    {
        public static GameObject Build(string artworkId, FrameTier frameTier,
                                       float w, float h, ArtworkCatalog catalog)
        {
            var root = new GameObject($"PlacedArtwork_{artworkId}");
            root.tag = "PlacedArtwork";
            int puzzlePieceLayer = LayerMask.NameToLayer("PuzzlePiece");
            if (puzzlePieceLayer >= 0) root.layer = puzzlePieceLayer;
            var identifier = root.AddComponent<PlacedArtworkIdentifier>();
            identifier.artworkId = artworkId;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FullImageReveal";
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, 0.0155f);
            quad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale    = new Vector3(w, h, 1f);

            if (catalog == null)
            {
                Debug.LogError($"[PlacedArtworkFactory] ArtworkCatalog is null for '{artworkId}' — image will not show. Wire the ArtworkCatalog field in the Inspector.");
            }

            var artworkDef = catalog?.artworks?.Find(a => a.artworkId == artworkId);
            if (artworkDef == null)
            {
                Debug.LogWarning($"[PlacedArtworkFactory] ArtworkDefinition not found for '{artworkId}' — image will not show.");
            }
            else
            {
                Texture texture = artworkDef.puzzleTexture != null
                    ? (Texture)artworkDef.puzzleTexture
                    : artworkDef.fullImage?.texture;

                if (texture == null)
                {
                    Debug.LogWarning($"[PlacedArtworkFactory] No texture on ArtworkDefinition for '{artworkId}' (puzzleTexture={artworkDef.puzzleTexture != null}, fullImage={artworkDef.fullImage != null})");
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Unlit/Texture")
                                 ?? Shader.Find("Sprites/Default");
                    if (shader != null)
                    {
                        var mat = new Material(shader);
                        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", texture);
                        if (mat.HasProperty("_BaseMap_ST")) mat.SetVector("_BaseMap_ST", new Vector4(1f, 1f, 0f, 0f));
                        if (mat.HasProperty("_MainTex_ST")) mat.SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
                        quad.GetComponent<Renderer>().material = mat;
                        Debug.Log($"[PlacedArtworkFactory] Image material assigned (shader='{shader.name}') for '{artworkId}'");
                    }
                    else
                    {
                        Debug.LogError($"[PlacedArtworkFactory] No usable shader found for artwork image '{artworkId}'");
                    }
                }
            }

            BuildFrameBars(root.transform, w, h, frameTier);

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, 0.05f);

            return root;
        }

        private static void BuildFrameBars(Transform parent, float w, float h, FrameTier tier)
        {
            Color color = tier switch
            {
                FrameTier.Bronce => new Color(0.55f, 0.35f, 0.15f),
                FrameTier.Plata  => new Color(0.75f, 0.75f, 0.78f),
                FrameTier.Oro    => new Color(0.85f, 0.72f, 0.20f),
                _                => new Color(0.55f, 0.35f, 0.15f),
            };
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = null;
            if (shader != null)
            {
                mat = new Material(shader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
            }

            const float thickness = 0.02f;
            const float depth     = 0.02f;

            var frameRoot = new GameObject("FullImageRevealFrame");
            frameRoot.transform.SetParent(parent, false);
            frameRoot.transform.localPosition = new Vector3(0f, 0f, 0.015f);
            frameRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CreateBar("FrameTop",    frameRoot.transform, w + thickness * 2f, thickness, depth, 0f,                      h / 2f + thickness / 2f, mat);
            CreateBar("FrameBottom", frameRoot.transform, w + thickness * 2f, thickness, depth, 0f,                     -h / 2f - thickness / 2f, mat);
            CreateBar("FrameLeft",   frameRoot.transform, thickness,          h,          depth, -w / 2f - thickness / 2f, 0f,                      mat);
            CreateBar("FrameRight",  frameRoot.transform, thickness,          h,          depth,  w / 2f + thickness / 2f, 0f,                      mat);
        }

        private static void CreateBar(string barName, Transform parent,
            float width, float height, float depth, float x, float y, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = barName;
            Object.Destroy(cube.GetComponent<Collider>());
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = new Vector3(x, y, 0f);
            cube.transform.localScale    = new Vector3(width, height, depth);
            if (mat != null)
                cube.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
