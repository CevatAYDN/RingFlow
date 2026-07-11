using UnityEngine;

namespace RingFlow.Gameplay
{
    /// <summary>
    /// Static cache for procedural meshes shared across all VFX systems.
    /// Meshes are generated once on first access, then reused forever.
    /// Zero runtime allocation after initialization.
    /// </summary>
    public static class VfxMeshCache
    {
        private static Mesh _donutMesh;
        private static Mesh _quadMesh;
        private static Mesh _ringSegmentMesh;
        private static Mesh _sparkMesh;
        private static bool _initialized;

        public static Mesh DonutMesh
        {
            get
            {
                if (_donutMesh == null) GenerateDonutMesh();
                return _donutMesh;
            }
        }

        public static Mesh QuadMesh
        {
            get
            {
                if (_quadMesh == null) GenerateQuadMesh();
                return _quadMesh;
            }
        }

        public static Mesh RingSegmentMesh
        {
            get
            {
                if (_ringSegmentMesh == null) GenerateRingSegmentMesh();
                return _ringSegmentMesh;
            }
        }

        public static Mesh SparkMesh
        {
            get
            {
                if (_sparkMesh == null) GenerateSparkMesh();
                return _sparkMesh;
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            // Touch all getters to force generation
            _ = DonutMesh;
            _ = QuadMesh;
            _ = RingSegmentMesh;
            _ = SparkMesh;
            _initialized = true;
        }

        private static void GenerateDonutMesh()
        {
            _donutMesh = CreateTorusMesh(0.18f, 0.06f, 16, 12);
            _donutMesh.name = "VfxCache_Donut";
            _donutMesh.MarkDynamic();
        }

        private static void GenerateQuadMesh()
        {
            _quadMesh = new Mesh
            {
                name = "VfxCache_Quad",
                vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, 0f),
                    new Vector3(0.5f, -0.5f, 0f),
                    new Vector3(-0.5f, 0.5f, 0f),
                    new Vector3(0.5f, 0.5f, 0f)
                },
                triangles = new[] { 0, 1, 2, 1, 3, 2 },
                normals = new[]
                {
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                }
            };
            _quadMesh.MarkDynamic();
        }

        private static void GenerateRingSegmentMesh()
        {
            // A ring arc segment (1/8 of a torus) for merge absorption particles
            _ringSegmentMesh = CreateTorusMesh(0.22f, 0.04f, 8, 6);
            _ringSegmentMesh.name = "VfxCache_RingSegment";
            _ringSegmentMesh.MarkDynamic();
        }

        private static void GenerateSparkMesh()
        {
            // A simple diamond-shaped spark
            _sparkMesh = new Mesh
            {
                name = "VfxCache_Spark",
                vertices = new[]
                {
                    new Vector3(0f, -0.5f, 0f),
                    new Vector3(0.3f, 0f, 0f),
                    new Vector3(0f, 0.5f, 0f),
                    new Vector3(-0.3f, 0f, 0f)
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                normals = new[]
                {
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0.5f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0.5f)
                }
            };
            _sparkMesh.MarkDynamic();
        }

        private static Mesh CreateTorusMesh(float majorRadius, float minorRadius, int radialSegments, int tubularSegments)
        {
            var mesh = new Mesh();
            int numVertices = (radialSegments + 1) * (tubularSegments + 1);
            var vertices = new Vector3[numVertices];
            var normals = new Vector3[numVertices];
            var uv = new Vector2[numVertices];
            var triangles = new int[radialSegments * tubularSegments * 6];

            int vIdx = 0;
            for (int radial = 0; radial <= radialSegments; radial++)
            {
                float u = (float)radial / radialSegments * Mathf.PI * 2f;
                float cosU = Mathf.Cos(u);
                float sinU = Mathf.Sin(u);

                for (int tubular = 0; tubular <= tubularSegments; tubular++)
                {
                    float v = (float)tubular / tubularSegments * Mathf.PI * 2f;
                    float cosV = Mathf.Cos(v);
                    float sinV = Mathf.Sin(v);

                    float centerX = majorRadius * cosU;
                    float centerZ = majorRadius * sinU;

                    vertices[vIdx] = new Vector3(
                        centerX + minorRadius * cosV * cosU,
                        minorRadius * sinV,
                        centerZ + minorRadius * cosV * sinU
                    );

                    normals[vIdx] = new Vector3(
                        cosV * cosU,
                        sinV,
                        cosV * sinU
                    ).normalized;

                    uv[vIdx] = new Vector2((float)radial / radialSegments, (float)tubular / tubularSegments);
                    vIdx++;
                }
            }

            int triIdx = 0;
            for (int radial = 0; radial < radialSegments; radial++)
            {
                for (int tubular = 0; tubular < tubularSegments; tubular++)
                {
                    int current = radial * (tubularSegments + 1) + tubular;
                    int next = current + tubularSegments + 1;

                    triangles[triIdx++] = current;
                    triangles[triIdx++] = next;
                    triangles[triIdx++] = current + 1;

                    triangles[triIdx++] = current + 1;
                    triangles[triIdx++] = next;
                    triangles[triIdx++] = next + 1;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            return mesh;
        }
    }
}
