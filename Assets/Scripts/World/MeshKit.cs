using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm
{
    // Small mesh helpers for Paradise Plunge, built at start-up:
    //  - Strip: a solid extruded ribbon that follows the course line (roads, river banks, cave roof / walls / floor), with a mesh collider;
    //  - Batch: many low-poly shapes (spheres, cylinders, boxes) with vertex colours merged into one mesh (scenery: one draw call per batch,
    //    the Steam Deck's budget). Same idea as Tee'd Off's MeshKit.Batch (TO Scripts/Course/MeshKit.cs), written fresh (no code copied).
    public static class MeshKit
    {
        // ---------------- strips ----------------
        // One cross-section sample: center, right (flat), left / right edge offsets (m, from center along right; left is negative),
        // top and bottom heights (absolute world y).
        public struct Sample { public Vector3 c, right; public float l, r, top, bottom; }

        // A closed extrusion through the samples: top, both sides, bottom, end caps. Split into pieces of at most 'piece' samples for culling.
        public static List<GameObject> Strip(Transform parent, string name, List<Sample> s, Material mat, Material side = null, bool collider = true, int layer = 0, int piece = 40, bool shadows = true)
        {
            var list = new List<GameObject>();
            for (int a = 0; a < s.Count - 1; a += piece)
            {
                int b = Mathf.Min(s.Count - 1, a + piece);
                var mesh = StripMesh(s, a, b, side != null);
                var g = new GameObject(name); g.transform.SetParent(parent, false); g.layer = layer;
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterials = side != null ? new[] { mat, side } : new[] { mat };
                mr.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (collider) g.AddComponent<MeshCollider>().sharedMesh = mesh;
                list.Add(g);
            }
            return list;
        }

        static Mesh StripMesh(List<Sample> s, int a, int b, bool twoMats)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var top = new List<int>(); var sides = new List<int>();
            float dist = 0;
            void Quad(List<int> tri, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, float u0, float u1, float w)
            {
                int i = v.Count; v.Add(p0); v.Add(p1); v.Add(p2); v.Add(p3); for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(new Vector2(0, u0)); uv.Add(new Vector2(w, u0)); uv.Add(new Vector2(w, u1)); uv.Add(new Vector2(0, u1));
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 1); tri.Add(i); tri.Add(i + 3); tri.Add(i + 2);
            }
            for (int i = a; i < b; i++)
            {
                var s0 = s[i]; var s1 = s[i + 1]; float d = Vector3.Distance(s0.c, s1.c); float u0 = dist, u1 = dist + d; dist = u1;
                Vector3 L0t = s0.c + s0.right * s0.l, R0t = s0.c + s0.right * s0.r, L1t = s1.c + s1.right * s1.l, R1t = s1.c + s1.right * s1.r;
                L0t.y = R0t.y = s0.top; L1t.y = R1t.y = s1.top;
                Vector3 L0b = L0t, R0b = R0t, L1b = L1t, R1b = R1t; L0b.y = R0b.y = s0.bottom; L1b.y = R1b.y = s1.bottom;
                var fwd = (s1.c - s0.c); fwd.y = 0; fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : Vector3.forward;
                var upN = Vector3.Cross(R1t - L0t, L1t - R0t).normalized; if (upN.y < 0) upN = -upN;
                // top: L0 R0 R1 L1 (wound so the face points up)
                Quad(top, L0t, R0t, R1t, L1t, upN, u0, u1, s0.r - s0.l);
                var tri = twoMats ? sides : top;
                var rightN = Vector3.Cross(Vector3.up, fwd);
                Quad(tri, R0t, R0b, R1b, R1t, rightN, u0, u1, s0.top - s0.bottom);
                Quad(tri, L1t, L1b, L0b, L0t, -rightN, u0, u1, s0.top - s0.bottom);
                Quad(tri, L1b, R1b, R0b, L0b, Vector3.down, u0, u1, s0.r - s0.l);
            }
            // end caps
            var tri2 = twoMats ? sides : top;
            { var s0 = s[a]; var d0 = s[Mathf.Min(a + 1, s.Count - 1)].c - s0.c; d0.y = 0; d0.Normalize(); Cap(s0, -d0, false); }
            { var s1 = s[b]; var d1 = s1.c - s[Mathf.Max(b - 1, 0)].c; d1.y = 0; d1.Normalize(); Cap(s1, d1, true); }
            void Cap(Sample c, Vector3 outN, bool end)
            {
                Vector3 Lt = c.c + c.right * c.l, Rt = c.c + c.right * c.r; Lt.y = Rt.y = c.top; Vector3 Lb = Lt, Rb = Rt; Lb.y = Rb.y = c.bottom;
                if (end) Quad(tri2, Lt, Rt, Rb, Lb, outN, 0, 1, 1); else Quad(tri2, Rt, Lt, Lb, Rb, outN, 0, 1, 1);
            }
            var m = new Mesh { name = "strip" }; if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
            if (twoMats) { m.subMeshCount = 2; m.SetTriangles(top, 0); m.SetTriangles(sides, 1); } else m.SetTriangles(top, 0);
            m.RecalculateBounds(); return m;
        }

        // ---------------- batches ----------------
        static Mesh sphere, cylinder, cube, cone;
        public static Mesh LowSphere => sphere ? sphere : (sphere = MakeSphere(8, 6));
        public static Mesh LowCylinder => cylinder ? cylinder : (cylinder = MakeCylinder(8, false));
        public static Mesh LowCone => cone ? cone : (cone = MakeCylinder(8, true));
        public static Mesh Cube => cube ? cube : (cube = MakeCube());

        public sealed class Batch
        {
            readonly List<Vector3> v = new List<Vector3>(); readonly List<Vector3> n = new List<Vector3>(); readonly List<Color> c = new List<Color>(); readonly List<int> t = new List<int>();
            public int VertexCount => v.Count;
            public void Add(Mesh m, Vector3 pos, Quaternion rot, Vector3 scale, Color col)
            {
                var mv = m.vertices; var mn = m.normals; var mt = m.triangles; int baseI = v.Count;
                var M = Matrix4x4.TRS(pos, rot, scale); var N = M.inverse.transpose;
                for (int i = 0; i < mv.Length; i++) { v.Add(M.MultiplyPoint3x4(mv[i])); n.Add(N.MultiplyVector(mn[i]).normalized); c.Add(col); }
                foreach (var i in mt) t.Add(baseI + i);
            }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name }; if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
                m.SetVertices(v); m.SetNormals(n); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
            }
            public GameObject Object(Transform parent, string name, Material mat, bool collider = false, bool shadows = true)
            {
                var g = new GameObject(name); g.transform.SetParent(parent, false);
                var mesh = Build(name); g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (collider) g.AddComponent<MeshCollider>().sharedMesh = mesh;
                return g;
            }
        }

        static Mesh MakeSphere(int lon, int lat)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int y = 0; y <= lat; y++) for (int x = 0; x <= lon; x++)
            {
                float a = x * Mathf.PI * 2 / lon, b = y * Mathf.PI / lat;
                v.Add(new Vector3(Mathf.Sin(b) * Mathf.Cos(a), Mathf.Cos(b), Mathf.Sin(b) * Mathf.Sin(a)) * .5f);
            }
            for (int y = 0; y < lat; y++) for (int x = 0; x < lon; x++)
            {
                int i = y * (lon + 1) + x; t.Add(i); t.Add(i + 1); t.Add(i + lon + 1); t.Add(i + 1); t.Add(i + lon + 2); t.Add(i + lon + 1);
            }
            var m = new Mesh { name = "low sphere" }; m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals();
            var nn = new Vector3[v.Count]; for (int i = 0; i < v.Count; i++) nn[i] = v[i].normalized; m.normals = nn; return m;
        }
        // Unit cylinder (or cone) of height 1 centered on its middle, radius 0.5, along Y.
        static Mesh MakeCylinder(int sides, bool pointed)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                v.Add(d * .5f + Vector3.down * .5f); n.Add(d); v.Add(pointed ? Vector3.up * .5f : d * .5f + Vector3.up * .5f); n.Add(pointed ? (d + Vector3.up).normalized : d);
            }
            for (int i = 0; i < sides; i++) { int k = i * 2; t.Add(k); t.Add(k + 1); t.Add(k + 2); t.Add(k + 1); t.Add(k + 3); t.Add(k + 2); }
            int c0 = v.Count; v.Add(Vector3.down * .5f); n.Add(Vector3.down);
            for (int i = 0; i <= sides; i++) { float a = i * Mathf.PI * 2 / sides; v.Add(new Vector3(Mathf.Cos(a), -1, Mathf.Sin(a)) * .5f); n.Add(Vector3.down); }
            for (int i = 0; i < sides; i++) { t.Add(c0); t.Add(c0 + 1 + i); t.Add(c0 + 2 + i); }
            if (!pointed)
            {
                int c1 = v.Count; v.Add(Vector3.up * .5f); n.Add(Vector3.up);
                for (int i = 0; i <= sides; i++) { float a = i * Mathf.PI * 2 / sides; v.Add(new Vector3(Mathf.Cos(a), 1, Mathf.Sin(a)) * .5f); n.Add(Vector3.up); }
                for (int i = 0; i < sides; i++) { t.Add(c1); t.Add(c1 + 2 + i); t.Add(c1 + 1 + i); }
            }
            var m = new Mesh { name = pointed ? "low cone" : "low cylinder" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(t, 0); return m;
        }
        static Mesh MakeCube()
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); var m = Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh); Object.DestroyImmediate(g); m.name = "cube"; return m;
        }
    }
}
