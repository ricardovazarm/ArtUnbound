using System.Collections.Generic;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Gameplay
{
    /// <summary>
    /// Generates procedural meshes for puzzle pieces with triangular morphology.
    /// Each edge can be Flat, Positive (tab), or Negative (blank).
    ///
    /// Pieces can be rectangular: pieceWidth and pieceHeight are independent.
    /// Tab geometry scales per edge orientation so adjacent pieces interlock
    /// correctly even when the grid spacing differs along X vs Y.
    /// </summary>
    public static class PieceMeshGenerator
    {
        // Tab spread along the edge (fraction of the edge length).
        private const float TAB_WIDTH = 0.4f;
        // Tab protrusion perpendicular to the edge (fraction of the perpendicular piece dimension).
        private const float TAB_HEIGHT = 0.15f;

        /// <summary>
        /// Generates a mesh for a puzzle piece with the given morphology and rectangular dimensions.
        /// </summary>
        public static Mesh GeneratePieceMesh(PieceMorphology morphology, float pieceWidth, float pieceHeight, Vector2 uvMin, Vector2 uvMax)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            float halfWidth = pieceWidth * 0.5f;
            float halfHeight = pieceHeight * 0.5f;
            float thickness = 0.005f; // 5mm thickness

            float halfThick = thickness * 0.5f;
            int frontStart = vertices.Count;
            GeneratePieceGeometry(vertices, triangles, uvs, morphology, pieceWidth, pieceHeight, uvMin, uvMax, halfThick);

            // Back face: same XY, mirror Z, reverse winding for outward normals.
            int backStart = vertices.Count;
            int vertexCount = backStart - frontStart;
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 v = vertices[frontStart + i];
                v.z = -halfThick;
                vertices.Add(v);
                uvs.Add(uvs[frontStart + i]);
            }

            int triCount = triangles.Count;
            for (int i = 0; i < triCount; i += 3)
            {
                int a = triangles[i] - frontStart;
                int b = triangles[i + 1] - frontStart;
                int c = triangles[i + 2] - frontStart;
                triangles.Add(backStart + a);
                triangles.Add(backStart + c);
                triangles.Add(backStart + b);
            }

            // Side strips connecting front to back along the perimeter.
            int perimeterStart = frontStart + 1; // skip the center vertex
            int perimeterCount = vertexCount - 1;
            for (int i = 0; i < perimeterCount; i++)
            {
                int current = perimeterStart + i;
                int next = (i == perimeterCount - 1) ? perimeterStart : current + 1;

                int topV = current;
                int botV = topV + vertexCount;
                int botNext = next + vertexCount;

                triangles.Add(topV);
                triangles.Add(next);
                triangles.Add(botV);

                triangles.Add(botV);
                triangles.Add(next);
                triangles.Add(botNext);
            }

            WeldVertices(vertices, uvs, triangles);

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
        /// UVs are flipped horizontally to compensate for the board's 180° Y rotation.
        /// </summary>
        public static Mesh GeneratePieceMesh(PieceMorphology morphology, float pieceWidth, float pieceHeight, int col, int row, int gridCols, int gridRows)
        {
            float uLeft = (float)col / gridCols;
            float uRight = (float)(col + 1) / gridCols;
            float vBottom = (float)row / gridRows;
            float vTop = (float)(row + 1) / gridRows;
            return GeneratePieceMesh(morphology, pieceWidth, pieceHeight, new Vector2(uRight, 1f - vTop), new Vector2(uLeft, 1f - vBottom));
        }

        private static void GeneratePieceGeometry(
            List<Vector3> vertices, List<int> triangles, List<Vector2> uvs,
            PieceMorphology morphology, float pieceWidth, float pieceHeight,
            Vector2 uvMin, Vector2 uvMax, float zOffset)
        {
            // Center vertex (for fan triangulation).
            int centerIndex = vertices.Count;
            vertices.Add(new Vector3(0, 0, zOffset));
            uvs.Add(new Vector2((uvMin.x + uvMax.x) * 0.5f, (uvMin.y + uvMax.y) * 0.5f));

            List<int> bottomEdge = GenerateEdgeVertices(vertices, uvs, morphology.bottom, EdgeSide.Bottom, pieceWidth, pieceHeight, uvMin, uvMax, zOffset);
            List<int> rightEdge = GenerateEdgeVertices(vertices, uvs, morphology.right, EdgeSide.Right, pieceWidth, pieceHeight, uvMin, uvMax, zOffset);
            List<int> topEdge = GenerateEdgeVertices(vertices, uvs, morphology.top, EdgeSide.Top, pieceWidth, pieceHeight, uvMin, uvMax, zOffset);
            List<int> leftEdge = GenerateEdgeVertices(vertices, uvs, morphology.left, EdgeSide.Left, pieceWidth, pieceHeight, uvMin, uvMax, zOffset);

            for (int i = 0; i < bottomEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(bottomEdge[i]);
                triangles.Add(bottomEdge[i + 1]);
            }
            for (int i = 0; i < rightEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(rightEdge[i]);
                triangles.Add(rightEdge[i + 1]);
            }
            for (int i = 0; i < topEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(topEdge[i]);
                triangles.Add(topEdge[i + 1]);
            }
            for (int i = 0; i < leftEdge.Count - 1; i++)
            {
                triangles.Add(centerIndex);
                triangles.Add(leftEdge[i]);
                triangles.Add(leftEdge[i + 1]);
            }
            triangles.Add(centerIndex);
            triangles.Add(leftEdge[leftEdge.Count - 1]);
            triangles.Add(bottomEdge[0]);
        }

        private static List<int> GenerateEdgeVertices(
            List<Vector3> vertices, List<Vector2> uvs,
            PieceEdgeState edgeState, EdgeSide side,
            float pieceWidth, float pieceHeight,
            Vector2 uvMin, Vector2 uvMax, float zOffset)
        {
            List<int> indices = new List<int>();

            Vector3 edgeStart, edgeEnd, outward;
            GetEdgeVectors(side, pieceWidth, pieceHeight, zOffset, out edgeStart, out edgeEnd, out outward);

            Vector3 edgeDir = (edgeEnd - edgeStart).normalized;

            // Horizontal edges (Top/Bottom) span pieceWidth and protrude along Y (scaled by pieceHeight).
            // Vertical edges (Left/Right) span pieceHeight and protrude along X (scaled by pieceWidth).
            bool horizontal = side == EdgeSide.Top || side == EdgeSide.Bottom;
            float edgeLen = horizontal ? pieceWidth : pieceHeight;
            float perpDim = horizontal ? pieceHeight : pieceWidth;
            float halfEdgeLen = edgeLen * 0.5f;
            float tabHalfSpread = edgeLen * TAB_WIDTH * 0.5f;
            float tabExtension = perpDim * TAB_HEIGHT;

            if (edgeState == PieceEdgeState.Flat)
            {
                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, pieceWidth, pieceHeight));
            }
            else if (edgeState == PieceEdgeState.Positive)
            {
                Vector3 tabStart = edgeStart + edgeDir * (halfEdgeLen - tabHalfSpread);
                Vector3 tabEnd = edgeStart + edgeDir * (halfEdgeLen + tabHalfSpread);
                Vector3 tabTip = (tabStart + tabEnd) * 0.5f + outward * tabExtension;

                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, tabStart, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, tabTip, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, tabEnd, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, pieceWidth, pieceHeight));
            }
            else // Negative
            {
                Vector3 blankStart = edgeStart + edgeDir * (halfEdgeLen - tabHalfSpread);
                Vector3 blankEnd = edgeStart + edgeDir * (halfEdgeLen + tabHalfSpread);
                Vector3 blankTip = (blankStart + blankEnd) * 0.5f - outward * tabExtension;

                indices.Add(AddVertex(vertices, uvs, edgeStart, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, blankStart, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, blankTip, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, blankEnd, uvMin, uvMax, pieceWidth, pieceHeight));
                indices.Add(AddVertex(vertices, uvs, edgeEnd, uvMin, uvMax, pieceWidth, pieceHeight));
            }

            return indices;
        }

        private static void GetEdgeVectors(EdgeSide side, float pieceWidth, float pieceHeight, float z, out Vector3 start, out Vector3 end, out Vector3 outward)
        {
            float halfWidth = pieceWidth * 0.5f;
            float halfHeight = pieceHeight * 0.5f;
            switch (side)
            {
                case EdgeSide.Bottom:
                    start = new Vector3(-halfWidth, -halfHeight, z);
                    end = new Vector3(halfWidth, -halfHeight, z);
                    outward = Vector3.down;
                    break;
                case EdgeSide.Right:
                    start = new Vector3(halfWidth, -halfHeight, z);
                    end = new Vector3(halfWidth, halfHeight, z);
                    outward = Vector3.right;
                    break;
                case EdgeSide.Top:
                    start = new Vector3(halfWidth, halfHeight, z);
                    end = new Vector3(-halfWidth, halfHeight, z);
                    outward = Vector3.up;
                    break;
                case EdgeSide.Left:
                default:
                    start = new Vector3(-halfWidth, halfHeight, z);
                    end = new Vector3(-halfWidth, -halfHeight, z);
                    outward = Vector3.left;
                    break;
            }
        }

        /// <summary>
        /// Welds vertices at the same position so normals smooth across edges, hiding triangular facets.
        /// </summary>
        private static void WeldVertices(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            const float epsilon = 0.00001f;
            int n = vertices.Count;
            int[] remap = new int[n];

            List<Vector3> newVertices = new List<Vector3>();
            List<Vector2> newUvs = new List<Vector2>();

            for (int i = 0; i < n; i++)
            {
                Vector3 v = vertices[i];
                int found = -1;
                for (int j = 0; j < newVertices.Count; j++)
                {
                    if (Vector3.Distance(newVertices[j], v) < epsilon)
                    {
                        found = j;
                        break;
                    }
                }
                if (found >= 0)
                {
                    remap[i] = found;
                }
                else
                {
                    remap[i] = newVertices.Count;
                    newVertices.Add(v);
                    newUvs.Add(uvs[i]);
                }
            }

            for (int t = 0; t < triangles.Count; t++)
                triangles[t] = remap[triangles[t]];

            vertices.Clear();
            vertices.AddRange(newVertices);
            uvs.Clear();
            uvs.AddRange(newUvs);
        }

        private static int AddVertex(List<Vector3> vertices, List<Vector2> uvs, Vector3 position, Vector2 uvMin, Vector2 uvMax, float pieceWidth, float pieceHeight)
        {
            vertices.Add(position);

            // Map position (-halfDim..halfDim) to 0..1 per axis. Unclamped so tab vertices outside
            // the cell map to UVs outside the uvMin-uvMax range — neighboring pieces share textures.
            float halfWidth = pieceWidth * 0.5f;
            float halfHeight = pieceHeight * 0.5f;
            float u = (position.x + halfWidth) / pieceWidth;
            float v = (position.y + halfHeight) / pieceHeight;

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
