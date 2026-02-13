using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Generates procedural meshes for puzzle pieces with triangular morphology.
    /// Each edge can be Flat, Positive (tab), or Negative (blank).
    /// </summary>
    public static class PieceMeshGenerator
    {
        private const float TAB_HEIGHT = 0.15f; // Height of tab as fraction of piece size
        private const float TAB_WIDTH = 0.4f;   // Width of tab base as fraction of edge length

        /// <summary>
        /// Generates a mesh for a puzzle piece with the given morphology.
        /// </summary>
        /// <param name="morphology">The edge morphology of the piece.</param>
        /// <param name="pieceSize">The size of the piece in world units.</param>
        /// <param name="uvMin">The minimum UV coordinates for this piece's texture region.</param>
        /// <param name="uvMax">The maximum UV coordinates for this piece's texture region.</param>
        /// <returns>A mesh with proper vertices, triangles, and UVs.</returns>
        public static Mesh GeneratePieceMesh(PieceMorphology morphology, float pieceSize, Vector2 uvMin, Vector2 uvMax)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float halfSize = pieceSize * 0.5f;
            float tabHeight = pieceSize * TAB_HEIGHT;
            float tabHalfWidth = pieceSize * TAB_WIDTH * 0.5f;
            float thickness = 0.005f; // 5mm thickness

            // 1. Generate Front Face (Z = +halfThickness)
            // We use the existing logic but offset Z
            // Note: If Z+ is towards camera, Front Face should be at +Z? 
            // Standard object: Local origin 0. Front face +Z/2, Back -Z/2?
            // Let's assume piece origin is center.

            float zFront = 0f; // Let's simplify: Front is at 0, extrude BACK (-Z) so pivot remains on surface?
                               // User requested explicit Z positioning (0.025). If pivot is center, maybe Thickness/2?
                               // Let's assume pivot is Back Face = 0, Front Face = Thickness?
                               // Or Pivot Center?
                               // User said "Son planas". Standard Plane has normal +Y. Quad normal -Z?
                               // Let's generate Front at Z=0 (Pivot) and extrude backwards to -Thickness.
                               // Or Front at Thickness/2, Back at -Thickness/2.

            // Let's go with: Front Face at Z=0. Back Face at Z = -Thickness.
            // Wait, if "Z+ is towards user", then Front Face (visible) should be the most Z+ part.
            // If Object is at 0.025, that's its pivot.
            // Be safe: Center geometry around Z=0. Extrude -HalfThick to +HalfThick.

            float halfThick = thickness * 0.5f;
            int frontStart = vertices.Count;
            GeneratePieceGeometry(vertices, triangles, uvs, morphology, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax, halfThick);

            // 2. Generate Back Face (Z = -halfThickness)
            int backStart = vertices.Count;
            // Back face needs mirrored X to match front when looking from back?
            // Or just same XY points, different Z.
            // We want same shape.
            // Reuse logic but with Z offset and FLIPPED winding.

            // We can manually copy vertices/tris?
            // Let's copy vertices from Front but change Z
            int vertexCount = backStart - frontStart;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 v = vertices[frontStart + i];
                v.z = -halfThick;
                vertices.Add(v);

                // UVs? Back face might use a generic "cardboard" UV or same image mirrored?
                // Let's reuse same UVs for now, or clamped 0,0 if we want blank back.
                uvs.Add(uvs[frontStart + i]);
            }

            // Add Back Face Triangles (Inverted winding of Front)
            // Front geometry adds tris: (Center, A, B)
            // We need (Center', B', A')
            int triCount = triangles.Count;
            for (int i = 0; i < triCount; i += 3)
            {
                // Retrieve indices relative to frontStart
                int a = triangles[i] - frontStart;
                int b = triangles[i + 1] - frontStart;
                int c = triangles[i + 2] - frontStart;

                // Add back face triangle
                triangles.Add(backStart + a);
                triangles.Add(backStart + c); // Swap for reverse winding
                triangles.Add(backStart + b);
            }

            // 3. Generate Sides (Connecting Front to Back)
            // We need to walk the perimeter.
            // Our GeneratePieceGeometry creates a center fan. It doesn't give us a clean perimeter list easily.
            // However, the vertices were added in order: Center, TopEdge..., RightEdge...
            // Actually, GeneratePieceGeometry adds Center first (Index 0).
            // Then edges. 
            // We need a perimeter loop.
            // The edge generation functions add vertices sequentially.
            // Order: BottomEdge -> RightEdge -> TopEdge -> LeftEdge.
            // We can iterate from 1 to Count-1.

            int perimeterStart = frontStart + 1; // Skip center
            int perimeterCount = vertexCount - 1;

            for (int i = 0; i < perimeterCount; i++)
            {
                int current = perimeterStart + i;
                int next = perimeterStart + ((i + 1) % perimeterCount); // Wrap around

                // But wait, the vertices list has duplicates? 
                // GenerateEdgeVertices adds Start, Points..., End.
                // Next Edge adds Start (same as Prev End), Points..., End.
                // We have duplicate vertices at corners?
                // Let's check GPG:
                // Bottom: Start(-,-) -> End(+,-)
                // Right: Start(+,-) -> End(+,+)
                // Yes, corner(+,-) is added twice.
                // This creates "hard" normals which is good for boxes.
                // But identifying the loop is harder.

                // Simpler approach:
                // Just creating quads between every adjacent pair in the list implies we might bridge the center?
                // No, the list order in 'vertices' after Center is:
                // BottomEdge (Start..End), RightEdge (Start..End)...
                // Note that BottomEnd == RightStart geometrically, but they are separate vertices in list?
                // Yes.
                // So we can stitch them sequentially.
                // But we must NOT stitch BottomEnd to RightStart if they are separate in list but same pos... 
                // Actually we CAN stitch them, it will just be a degenerate quad (length 0), invisible. That's fine!

                int topV = current;
                int topNext = (current + 1);

                // If current is last vertex, next is first (perimeterStart)?
                // The list order is solid. 
                // BottomEdge list indices... RightEdge list indices...
                // Vertices are added linearly.

                if (i == perimeterCount - 1) topNext = perimeterStart; // Close loop

                // Indices in Back Face
                int botV = topV + vertexCount; // Corresponding back vertex
                int botNext = topNext + vertexCount;

                // Add Quad (Two Triangles)
                // Facing OUT. 
                // Top is Front(+Z), Bot is Back(-Z).
                // Sequence: TopV -> TopNext -> BotNext -> BotV
                // Tris: (TopV, BotV, TopNext), (BotV, BotNext, TopNext)?
                // Let's verify winding.
                // Front face winding is CCW? Check GPG.
                // Previous step I flipped winding to A->C->B (CW?).
                // Let's assume standard CCW (Right Hand Rule).
                // Side Normal should point out.

                triangles.Add(topV);
                triangles.Add(topNext);
                triangles.Add(botV);

                triangles.Add(botV);
                triangles.Add(topNext);
                triangles.Add(botNext);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);

            // Recalculate normals to smooth (or flat)
            mesh.RecalculateNormals(); // Will smooth the sides if shared verts? No, corners are duped, so hard edges. Good.
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Generates a mesh for a piece at a specific grid position.
        /// </summary>
        public static Mesh GeneratePieceMesh(PieceMorphology morphology, float pieceSize, int col, int row, int gridCols, int gridRows)
        {
            // Calculate UV coordinates based on grid position
            float uMin = (float)col / gridCols;
            float uMax = (float)(col + 1) / gridCols;
            float vMin = 1f - (float)(row + 1) / gridRows; // Flip V for typical texture coordinates
            float vMax = 1f - (float)row / gridRows;

            return GeneratePieceMesh(morphology, pieceSize, new Vector2(uMin, vMin), new Vector2(uMax, vMax));
        }

        private static void GeneratePieceGeometry(
            List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
            PieceMorphology morphology, float halfSize, float tabHeight, float tabHalfWidth,
            Vector2 uvMin, Vector2 uvMax, float zOffset)
        {
            // Center vertex (for fan triangulation)
            int centerIndex = vertices.Count;
            vertices.Add(new Vector3(0, 0, zOffset));
            uvs.Add(new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f));

            // Generate edge vertices for each side
            List<int> bottomEdge = GenerateEdgeVertices(vertices, uvs, morphology.bottom, EdgeSide.Bottom, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax, zOffset);
            List<int> rightEdge = GenerateEdgeVertices(vertices, uvs, morphology.right, EdgeSide.Right, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax, zOffset);
            List<int> topEdge = GenerateEdgeVertices(vertices, uvs, morphology.top, EdgeSide.Top, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax, zOffset);
            List<int> leftEdge = GenerateEdgeVertices(vertices, uvs, morphology.left, EdgeSide.Left, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax, zOffset);

            // Create triangles using fan from center
            // Create triangles using fan from center
            // FLIPPED WINDING: To face the camera (which looks from +Z/-Z depending on tray), we swap vertex order.
            // Previous order was invisible.

            // Bottom edge
            for (int i = 0; i < bottomEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(bottomEdge[i]);     // Swapped back? No, let's try (center, i, i+1)
                triangles.Add(bottomEdge[i + 1]);
            }

            // Right edge
            for (int i = 0; i < rightEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(rightEdge[i]);
                triangles.Add(rightEdge[i + 1]);
            }

            // Top edge
            for (int i = 0; i < topEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(topEdge[i]);
                triangles.Add(topEdge[i + 1]);
            }

            // Left edge
            for (int i = 0; i < leftEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(leftEdge[i]);
                triangles.Add(leftEdge[i + 1]);
            }

            // Connect last vertex of left edge to first vertex of bottom edge
            triangles.Add(centerIndex);
            triangles.Add(leftEdge[leftEdge.Count - 1]);
            triangles.Add(bottomEdge[0]);
        }

        private static List<int> GenerateEdgeVertices(
            List<Vector3> vertices, List<Vector2> uvs,
            PieceEdgeState edgeState, EdgeSide side,
            float halfSize, float tabHeight, float tabHalfWidth,
            Vector2 uvMin, Vector2 uvMax, float zOffset)
        {
            List<int> indices = new List<int>();

            // Get edge direction and normal
            Vector3 edgeStart, edgeEnd, outward;
            GetEdgeVectors(side, halfSize, zOffset, out edgeStart, out edgeEnd, out outward);

            Vector3 edgeDir = (edgeEnd - edgeStart).normalized;

            if (edgeState == PieceEdgeState.Flat)
            {
                // Simple flat edge - just start and end vertices
                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, halfSize));
            }
            else if (edgeState == PieceEdgeState.Positive)
            {
                // Tab (protrusion) - triangle pointing outward
                Vector3 tabStart = edgeStart + edgeDir * (halfSize - tabHalfWidth);
                Vector3 tabEnd = edgeStart + edgeDir * (halfSize + tabHalfWidth);
                Vector3 tabTip = (tabStart + tabEnd) * 0.5f + outward * tabHeight;

                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, tabStart, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, tabTip, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, tabEnd, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, halfSize));
            }
            else // Negative
            {
                // Blank (indentation) - triangle cutting into piece
                Vector3 blankStart = edgeStart + edgeDir * (halfSize - tabHalfWidth);
                Vector3 blankEnd = edgeStart + edgeDir * (halfSize + tabHalfWidth);
                Vector3 blankTip = (blankStart + blankEnd) * 0.5f - outward * tabHeight;

                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, blankStart, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, blankTip, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, blankEnd, uvMin, uvMax, halfSize));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, halfSize));
            }

            return indices;
        }

        private static void GetEdgeVectors(EdgeSide side, float halfSize, float z, out Vector3 start, out Vector3 end, out Vector3 outward)
        {
            switch (side)
            {
                case EdgeSide.Bottom:
                    start = new Vector3(-halfSize, -halfSize, z);
                    end = new Vector3(halfSize, -halfSize, z);
                    outward = Vector3.down;
                    break;
                case EdgeSide.Right:
                    start = new Vector3(halfSize, -halfSize, z);
                    end = new Vector3(halfSize, halfSize, z);
                    outward = Vector3.right;
                    break;
                case EdgeSide.Top:
                    start = new Vector3(halfSize, halfSize, z);
                    end = new Vector3(-halfSize, halfSize, z);
                    outward = Vector3.up;
                    break;
                case EdgeSide.Left:
                default:
                    start = new Vector3(-halfSize, halfSize, z);
                    end = new Vector3(-halfSize, -halfSize, z);
                    outward = Vector3.left;
                    break;
            }
        }

        private static int AddVertex(List<Vector3> vertices, List<Vector2> uvs, Vector3 position, Vector2 uvMin, Vector2 uvMax, float halfSize)
        {
            vertices.Add(position);

            // Calculate UV based on position within piece bounds
            // IMPORTANT: Use UNCLAMPED mapping based on the nominal cell size (halfSize * 2).
            // This allows vertices outside the core cell (the tabs) to correctly map to UVs outside the uvMin-uvMax range.
            // This is crucial for puzzle pieces to seamlessly connect visually to neighbors.

            float pieceSize = halfSize * 2f;

            // Map position (-halfSize..halfSize) to 0..1
            // Use simple math instead of InverseLerp which clamps to [0,1]
            float u = (position.x + halfSize) / pieceSize;
            float v = (position.y + halfSize) / pieceSize;

            // Map to piece's texture region (Unclamped)
            float finalU = uvMin.x + (uvMax.x - uvMin.x) * u;
            float finalV = uvMin.y + (uvMax.y - uvMin.y) * v;

            uvs.Add(new Vector2(finalU, finalV));

            return vertices.Count - 1;
        }

        /// <summary>
        /// Generates all 81 possible morphology variants for reference/debugging.
        /// </summary>
        public static PieceMorphology[] GenerateAllMorphologyVariants()
        {
            PieceMorphology[] variants = new PieceMorphology[81];
            PieceEdgeState[] states = { PieceEdgeState.Flat, PieceEdgeState.Positive, PieceEdgeState.Negative };

            int index = 0;
            foreach (var top in states)
            {
                foreach (var right in states)
                {
                    foreach (var bottom in states)
                    {
                        foreach (var left in states)
                        {
                            variants[index++] = new PieceMorphology(top, right, bottom, left);
                        }
                    }
                }
            }

            return variants;
        }

        /// <summary>
        /// Gets a unique index (0-80) for a morphology configuration.
        /// </summary>
        public static int GetMorphologyIndex(PieceMorphology morphology)
        {
            int top = (int)morphology.top;
            int right = (int)morphology.right;
            int bottom = (int)morphology.bottom;
            int left = (int)morphology.left;

            return top * 27 + right * 9 + bottom * 3 + left;
        }
    }
}
