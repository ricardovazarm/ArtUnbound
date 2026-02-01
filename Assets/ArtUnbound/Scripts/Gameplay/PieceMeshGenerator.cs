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

            // Generate the base piece with edge modifications
            GeneratePieceGeometry(
                vertices, triangles, uvs,
                morphology, halfSize, tabHeight, tabHalfWidth,
                uvMin, uvMax
            );

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
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
            Vector2 uvMin, Vector2 uvMax)
        {
            // Center vertex (for fan triangulation)
            int centerIndex = vertices.Count;
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f));

            // Generate edge vertices for each side
            List<int> bottomEdge = GenerateEdgeVertices(vertices, uvs, morphology.bottom, EdgeSide.Bottom, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax);
            List<int> rightEdge = GenerateEdgeVertices(vertices, uvs, morphology.right, EdgeSide.Right, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax);
            List<int> topEdge = GenerateEdgeVertices(vertices, uvs, morphology.top, EdgeSide.Top, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax);
            List<int> leftEdge = GenerateEdgeVertices(vertices, uvs, morphology.left, EdgeSide.Left, halfSize, tabHeight, tabHalfWidth, uvMin, uvMax);

            // Create triangles using fan from center
            // Bottom edge
            for (int i = 0; i < bottomEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(bottomEdge[i]);
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
            Vector2 uvMin, Vector2 uvMax)
        {
            List<int> indices = new List<int>();

            // Get edge direction and normal
            Vector3 edgeStart, edgeEnd, outward;
            GetEdgeVectors(side, halfSize, out edgeStart, out edgeEnd, out outward);

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

        private static void GetEdgeVectors(EdgeSide side, float halfSize, out Vector3 start, out Vector3 end, out Vector3 outward)
        {
            switch (side)
            {
                case EdgeSide.Bottom:
                    start = new Vector3(-halfSize, -halfSize, 0);
                    end = new Vector3(halfSize, -halfSize, 0);
                    outward = Vector3.down;
                    break;
                case EdgeSide.Right:
                    start = new Vector3(halfSize, -halfSize, 0);
                    end = new Vector3(halfSize, halfSize, 0);
                    outward = Vector3.right;
                    break;
                case EdgeSide.Top:
                    start = new Vector3(halfSize, halfSize, 0);
                    end = new Vector3(-halfSize, halfSize, 0);
                    outward = Vector3.up;
                    break;
                case EdgeSide.Left:
                default:
                    start = new Vector3(-halfSize, halfSize, 0);
                    end = new Vector3(-halfSize, -halfSize, 0);
                    outward = Vector3.left;
                    break;
            }
        }

        private static int AddVertex(List<Vector3> vertices, List<Vector2> uvs, Vector3 position, Vector2 uvMin, Vector2 uvMax, float halfSize)
        {
            vertices.Add(position);

            // Calculate UV based on position within piece bounds
            // Extended bounds to account for tabs
            float extendedHalf = halfSize * (1f + TAB_HEIGHT);
            float u = Mathf.InverseLerp(-extendedHalf, extendedHalf, position.x);
            float v = Mathf.InverseLerp(-extendedHalf, extendedHalf, position.y);

            // Map to piece's texture region
            float finalU = Mathf.Lerp(uvMin.x, uvMax.x, u);
            float finalV = Mathf.Lerp(uvMin.y, uvMax.y, v);

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
