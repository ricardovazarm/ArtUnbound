using UnityEngine;
using UnityEditor;
using System.IO;

namespace ArtUnbound.Editor
{
    /// <summary>
    /// Bakes frame materials to 2D sprite PNGs for grid and detail views.
    /// Uses the material's _BaseColor to draw a frame shape (avoids URP render issues in Editor).
    /// Sprites match the material colors used in 3D (wall placement).
    /// </summary>
    public static class FrameSpriteBaker
    {
        private const int OutputSize = 512;
        private const string OutputFolder = "Assets/ArtUnbound/UI/Sprites";
        private const string WoodTexturePath = "Assets/Real Materials/Textures/RM wood unplaned boards D alpha R.png";
        private const string MetalTexturePath = "Assets/Real Materials/Textures/RM aluminum used D.png";

        [MenuItem("Art Unbound/Bake Frame Sprites from Materials")]
        public static void BakeAllFrameSprites()
        {
            var materials = new[]
            {
                ("Frame_Madera", "FrameMadera.png"),
                ("Frame_Madera", "FrameDetail.png"),
                ("Frame_Madera", "FrameThumbnail.png"),
                ("Frame_Bronce", "FrameBronce.png"),
                ("Frame_Plata", "FramePlata.png"),
                ("Frame_Oro", "FrameOro.png"),
            };

            int total = materials.Length;
            for (int i = 0; i < total; i++)
            {
                var (matName, outName) = materials[i];
                if (EditorUtility.DisplayCancelableProgressBar("Bake Frame Sprites", $"Baking {outName}... ({i + 1}/{total})", (float)i / total))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning("[FrameSpriteBaker] Cancelled by user.");
                    return;
                }
                var mat = AssetDatabase.LoadAssetAtPath<Material>(
                    $"Assets/ArtUnbound/Materials/{matName}.mat");
                if (mat == null)
                {
                    Debug.LogWarning($"[FrameSpriteBaker] Material {matName} not found, skipping.");
                    continue;
                }

                BakeFrameSprite(mat, Path.Combine(OutputFolder, outName));
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            var outputNames = new[] { "FrameMadera.png", "FrameDetail.png", "FrameThumbnail.png", "FrameBronce.png", "FramePlata.png", "FrameOro.png" };
            foreach (var outName in outputNames)
            {
                var path = Path.Combine(OutputFolder, outName);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log("[FrameSpriteBaker] Done. Sprites saved to " + OutputFolder);
        }

        private static void BakeFrameSprite(Material material, string outputPath)
        {
            Color baseColor = Color.white;
            if (material.HasProperty("_BaseColor"))
                baseColor = material.GetColor("_BaseColor");
            else if (material.HasProperty("_Color"))
                baseColor = material.GetColor("_Color");

            var matName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(material)) ?? "";
            var tex = CreateFrameTexture(OutputSize, baseColor, matName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllBytes(outputPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>
        /// Creates frame texture. Wood/Metals: use RM textures if available. Fallback: procedural.
        /// </summary>
        private static Texture2D CreateFrameTexture(int size, Color baseColor, string materialName)
        {
            Texture2D sourceTexture = null;
            string texturePath = null;
            bool useMetalTexture = false;
            if (materialName.Contains("Madera"))
                texturePath = WoodTexturePath;
            else if (materialName.Contains("Oro") || materialName.Contains("Plata") || materialName.Contains("Bronce"))
            {
                texturePath = MetalTexturePath;
                useMetalTexture = true;
            }

            if (!string.IsNullOrEmpty(texturePath))
            {
                sourceTexture = LoadTextureForRead(texturePath);
                if (sourceTexture != null)
                    Debug.Log($"[FrameSpriteBaker] Using RM texture for {materialName}: {Path.GetFileName(texturePath)}");
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            const float borderRatio = 0.12f;
            int border = Mathf.Max(1, Mathf.RoundToInt(size * borderRatio));
            int inner = size - border;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inFrame = (y < border || y >= inner || x < border || x >= inner);
                    if (!inFrame)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    Color c;
                    if (sourceTexture != null)
                    {
                        bool useSmooth = useMetalTexture;
                        c = SampleFrameTexture(sourceTexture, x, y, size, border, inner, useSmooth, useSmooth);
                        if (useMetalTexture)
                            c *= baseColor;
                    }
                    else
                    {
                        int distToInner = border;
                        if (y < border) distToInner = Mathf.Min(distToInner, border - 1 - y);
                        else if (y >= inner) distToInner = Mathf.Min(distToInner, y - inner);
                        if (x < border) distToInner = Mathf.Min(distToInner, border - 1 - x);
                        else if (x >= inner) distToInner = Mathf.Min(distToInner, x - inner);
                        float t = Mathf.Clamp01(distToInner / (float)(border * 0.5f));
                        c = Color.Lerp(baseColor * 0.88f, baseColor, t);
                        if (materialName.Contains("Madera"))
                            c = ApplyWoodGrain(c, x, y, y < border || y >= inner);
                        else
                            c = ApplyMetalMottling(c, x, y);
                    }

                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(c.r * 255, 0, 255),
                        (byte)Mathf.Clamp(c.g * 255, 0, 255),
                        (byte)Mathf.Clamp(c.b * 255, 0, 255), 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D LoadTextureForRead(string assetPath)
        {
            var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            if (!File.Exists(fullPath)) return null;
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return null;
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Color SampleFrameTexture(Texture2D source, int x, int y, int size, int border, int inner, bool bilinear = false, bool lowTile = false)
        {
            float u, v;
            float tile = lowTile ? 1f : 2f;
            if (y < border)
            {
                u = ((float)x / size * tile) % 1f;
                v = (float)y / border;
            }
            else if (y >= inner)
            {
                u = ((float)x / size * tile) % 1f;
                v = (float)(y - inner) / border;
            }
            else if (x < border)
            {
                u = (float)x / border;
                v = ((float)(y - border) / (inner - border) * tile) % 1f;
            }
            else
            {
                u = (float)(x - inner) / border;
                v = ((float)(y - border) / (inner - border) * tile) % 1f;
            }

            if (bilinear)
            {
                int cx = Mathf.FloorToInt(u * source.width);
                int cy = Mathf.FloorToInt(v * source.height);
                int r = 3;
                Color c = Color.clear;
                int count = 0;
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        int nx = Mathf.Clamp(cx + dx, 0, source.width - 1);
                        int ny = Mathf.Clamp(cy + dy, 0, source.height - 1);
                        c += source.GetPixel(nx, ny);
                        count++;
                    }
                return c / count;
            }

            int tx = Mathf.Clamp(Mathf.FloorToInt(u * source.width), 0, source.width - 1);
            int ty = Mathf.Clamp(Mathf.FloorToInt(v * source.height), 0, source.height - 1);
            return source.GetPixel(tx, ty);
        }

        private static float SmoothNoise(float x, float y)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        private static float SmoothNoise2(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            float a = SmoothNoise(ix, iy);
            float b = SmoothNoise(ix + 1, iy);
            float c = SmoothNoise(ix, iy + 1);
            float d = SmoothNoise(ix + 1, iy + 1);
            float u = fx * fx * (3f - 2f * fx);
            float v = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static Color ApplyWoodGrain(Color c, int x, int y, bool horizontalGrain)
        {
            float along = horizontalGrain ? x : y;
            float across = horizontalGrain ? y : x;
            float n = SmoothNoise2(along * 0.12f, across * 0.25f);
            n += 0.4f * SmoothNoise2(along * 0.25f, across * 0.5f);
            float grain = Mathf.Lerp(0.82f, 1f, n);
            return c * grain;
        }

        private static Color ApplyMetalMottling(Color c, int x, int y)
        {
            float n = SmoothNoise2(x * 0.04f, y * 0.04f);
            n += 0.5f * SmoothNoise2(x * 0.08f, y * 0.08f);
            n = (n / 1.5f - 0.5f) * 0.12f;
            return new Color(
                Mathf.Clamp01(c.r + n),
                Mathf.Clamp01(c.g + n),
                Mathf.Clamp01(c.b + n), c.a);
        }
    }
}
