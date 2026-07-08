using UnityEngine;

namespace RingFlow.Gameplay
{
    public static class RingMeshGenerator
    {
        public static Mesh CreateTorus(float radius, float tubeRadius, int radialSegments = 24, int tubularSegments = 12)
        {
            var mesh = new Mesh { name = "ProceduralTorus" };

            int vertexCount = (radialSegments + 1) * (tubularSegments + 1);
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];
            var colors = new Color[vertexCount];

            float radialStep = 2f * Mathf.PI / radialSegments;
            float tubularStep = 2f * Mathf.PI / tubularSegments;

            int idx = 0;
            for (int i = 0; i <= radialSegments; i++)
            {
                float u = i * radialStep;
                float cosU = Mathf.Cos(u);
                float sinU = Mathf.Sin(u);

                for (int j = 0; j <= tubularSegments; j++)
                {
                    float v = j * tubularStep;
                    float cosV = Mathf.Cos(v);
                    float sinV = Mathf.Sin(v);

                    float x = (radius + tubeRadius * cosV) * cosU;
                    float y = tubeRadius * sinV;
                    float z = (radius + tubeRadius * cosV) * sinU;

                    vertices[idx] = new Vector3(x, y, z);
                    normals[idx] = new Vector3(cosV * cosU, sinV, cosV * sinU).normalized;
                    uvs[idx] = new Vector2((float)i / radialSegments, (float)j / tubularSegments);
                    colors[idx] = Color.white;
                    idx++;
                }
            }

            int triangleCount = radialSegments * tubularSegments * 6;
            var triangles = new int[triangleCount];
            idx = 0;
            for (int i = 0; i < radialSegments; i++)
            {
                for (int j = 0; j < tubularSegments; j++)
                {
                    int current = i * (tubularSegments + 1) + j;
                    int next = current + tubularSegments + 1;

                    triangles[idx++] = current;
                    triangles[idx++] = next;
                    triangles[idx++] = current + 1;

                    triangles[idx++] = current + 1;
                    triangles[idx++] = next;
                    triangles[idx++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
