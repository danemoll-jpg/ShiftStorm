using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftStorm
{
    // Round 14 item 3 (Dan: "For the cycle sub version, I wanted a bubble that enveloped the whole bike, not just a dome"): the Scrambler's
    // sub rides inside a complete clear glass sphere, the bike and its rider wholly inside it (a hamster ball / diving bell), a chrome ring
    // round its equator. Build 15's "sphere" was a 0.9 m ball over the seat whose lower half sat inside the bike: it read as a dome.
    //   - size: from the model's own mesh bounds (every renderer, in the vehicle's frame) and the rider's seated box over the seat (scaled by
    //     designs.json "riderScale"), the ball round that box's corners + 8 cm: nothing can poke through (Make)
    //   - the change into the sub: the ring flashes in first, then the ball grows out from the seat and closes over the bike (0.45 s, a
    //     small overshoot)
    //   - the glass: Resources/Shaders/BubbleGlass.shader (rim, highlights, the sky's reflection, a little refraction). The refraction samples
    //     the camera's opaque copy, which is off in the pipeline for the Deck; it is switched on per camera only while a bubble is within
    //     80 m of it (Refraction), so it costs nothing anywhere else on the lap.
    //   - the hull collider is unchanged (one hull for every form, ShiftVehicle)
    public sealed class BubbleSphere : MonoBehaviour
    {
        public static readonly List<BubbleSphere> All = new List<BubbleSphere>();
        public float Radius { get; private set; }
        public Vector3 CenterLocal { get; private set; }   // in the form model's parent (the FormSet pivot) frame
        public Transform Ring { get; private set; }
        float shownAt = -1; FormSet set; Transform pivot;
        public static int Grows { get; private set; }

        static Mesh sphereMesh, ringMesh; static Material glass, chrome;
        static Mesh SphereMesh { get { if (!sphereMesh) { var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphereMesh = g.GetComponent<MeshFilter>().sharedMesh; Destroy(g); } return sphereMesh; } }
        public static Material Glass
        {
            get
            {
                if (glass) return glass;
                var sh = Resources.Load<Shader>("Shaders/BubbleGlass");
                if (sh && sh.isSupported) glass = new Material(sh) { name = "Bubble glass" };
                else { var b = Resources.Load<Material>("Materials/VehicleGlass"); glass = new Material(b) { name = "Bubble glass (fallback)" }; glass.SetColor("_BaseColor", new Color(.7f, .88f, 1f, .2f)); glass.SetFloat("_Smoothness", .97f); Debug.LogWarning("[Bubble] BubbleGlass shader missing or unsupported: plain glass"); }
                return glass;
            }
        }
        static Material Chrome => chrome ? chrome : (chrome = JurassicLook.Mat(new Color(.9f, .92f, .96f), false, new Color(.3f, .32f, .36f)));

        // the ball round the model (already instantiated under parent) and a rider seated at `seat` (parent frame)
        public static BubbleSphere Make(GameObject model, Transform parent, Vector3 seat, float riderScale)
        {
            var b = new Bounds(seat, Vector3.zero); bool any = false;
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!mf.sharedMesh) continue; var mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = parent.InverseTransformPoint(mf.transform.TransformPoint(corner)); if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            // the rider: seated, hands forward on the grips, head ~1.05 m over the seat at full size (riderScale: the Scrambler's 1.4)
            float rs = Mathf.Max(.5f, riderScale);
            b.Encapsulate(seat + new Vector3(-.38f, -.1f, -.5f) * rs); b.Encapsulate(seat + new Vector3(.38f, 1.08f, .62f) * rs);
            float r = 0; for (int i = 0; i < 8; i++) { var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)); r = Mathf.Max(r, (c - b.center).magnitude); }
            // a box's corner sphere is generous; the real shape fills far less of the corners: the ball's radius is the longest half axis
            // + 60 % of the rest of the way to the corner (everything checked inside below)
            float half = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));
            r = Mathf.Lerp(half, r, .6f) + .08f;
            var go = new GameObject("Glass sphere (game, whole bike)"); go.transform.SetParent(model.transform, false);
            go.transform.position = parent.TransformPoint(b.center); go.transform.rotation = parent.rotation;
            var s = go.AddComponent<BubbleSphere>(); s.Radius = r; s.CenterLocal = b.center; s.pivot = parent;
            var ball = new GameObject("glass"); ball.transform.SetParent(go.transform, false);
            ball.transform.localScale = Vector3.one * (2 * r);   // the sphere's own frame is the pivot's scale (Place)
            ball.AddComponent<MeshFilter>().sharedMesh = SphereMesh; var br = ball.AddComponent<MeshRenderer>(); br.sharedMaterial = Glass;
            br.shadowCastingMode = ShadowCastingMode.Off; br.receiveShadows = false;
            // the chrome ring round its equator (the model's own chrome ring sits there: Dan's design)
            var ring = new GameObject("chrome ring (equator)"); ring.transform.SetParent(go.transform, false);
            ring.transform.localScale = Vector3.one;
            ring.AddComponent<MeshFilter>().sharedMesh = RingMesh(r + .01f, Mathf.Clamp(r * .02f, .025f, .04f)); var rr = ring.AddComponent<MeshRenderer>(); rr.sharedMaterial = Chrome;
            rr.shadowCastingMode = ShadowCastingMode.On; s.Ring = ring.transform;
            Debug.Log($"[Bubble] {model.name}: bike + rider box {b.size.x:0.00} x {b.size.y:0.00} x {b.size.z:0.00} m, sphere radius {r:0.00} m, centre {b.center.y - seat.y:+0.00;-0.00} m over the seat");
            return s;
        }
        static Vector3 Inv(Vector3 v) => new Vector3(1 / Mathf.Max(1e-4f, v.x), 1 / Mathf.Max(1e-4f, v.y), 1 / Mathf.Max(1e-4f, v.z));
        static readonly Dictionary<string, Mesh> rings = new Dictionary<string, Mesh>();
        static Mesh RingMesh(float R, float t)
        {
            string key = $"{R:0.000}/{t:0.000}"; if (rings.TryGetValue(key, out var m) && m) return m;
            const int seg = 64, side = 10; var v = new List<Vector3>(); var n = new List<Vector3>(); var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2 / seg; var c = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (int k = 0; k <= side; k++) { float b = k * Mathf.PI * 2 / side; var dir = c * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b); v.Add(c * R + dir * t); n.Add(dir); }
            }
            for (int i = 0; i < seg; i++) for (int k = 0; k < side; k++) { int a0 = i * (side + 1) + k, a1 = a0 + side + 1; tri.AddRange(new[] { a0, a1, a0 + 1, a0 + 1, a1, a1 + 1 }); }
            m = new Mesh { name = "Bubble ring" }; m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0); m.RecalculateBounds(); rings[key] = m; return m;
        }

        void OnEnable() { All.Add(this); set = GetComponentInParent<FormSet>(); shownAt = Now; Grows++; Apply(0); Refraction.Ensure(); }
        void OnDisable() { All.Remove(this); }
        float Now => set && set.Unscaled ? Time.unscaledTime : Time.time;
        public float Grown { get; private set; } = 1;
        // the change into the sub: the ring first (0-0.15 s), then the ball grows from the seat and closes over the bike (0.1-0.45 s)
        void LateUpdate() { float t = Now - shownAt; Apply(t < .5f ? t : 1); }
        void Apply(float t)
        {
            if (FormSet.NoEffect) t = 1;
            float k = Mathf.Clamp01((t - .1f) / .35f), e = 1 + 2.2f * Mathf.Pow(k - 1, 3) + 1.2f * Mathf.Pow(k - 1, 2);   // ease out, a small overshoot
            float ring = Mathf.Clamp01(t / .15f);
            Grown = k >= 1 ? 1 : Mathf.Max(.05f, e);
            // placed from the pivot every frame (the model's own scale is not to be trusted: build 16's first try kept the import's 1/20
            // from the moment it was made and drew a 69 m ball the camera sat inside)
            var pv = pivot ? pivot : set ? set.Pivot : null; var par = transform.parent;
            if (pv && par)
            {
                transform.SetPositionAndRotation(pv.TransformPoint(CenterLocal), pv.rotation);
                var pl = par.lossyScale; float want = pv.lossyScale.x * Grown; transform.localScale = new Vector3(want / Mathf.Max(1e-5f, pl.x), want / Mathf.Max(1e-5f, pl.y), want / Mathf.Max(1e-5f, pl.z));
            }
            else transform.localScale = Vector3.one * Grown;
            if (Ring) Ring.localScale = Vector3.one * Mathf.Lerp(.7f, 1f, ring) / Mathf.Max(.05f, Grown);
        }

        // per camera: the opaque copy (for the glass's refraction) only while a bubble is near it
        sealed class Refraction : MonoBehaviour
        {
            static Refraction inst; static readonly int IdOn = Shader.PropertyToID("_BubbleRefractOn");
            public static void Ensure() { if (inst) return; var g = new GameObject("Bubble refraction"); DontDestroyOnLoad(g); inst = g.AddComponent<Refraction>(); }
            readonly Dictionary<Camera, bool> on = new Dictionary<Camera, bool>();
            void OnEnable() => RenderPipelineManager.beginCameraRendering += Begin;
            void OnDisable() => RenderPipelineManager.beginCameraRendering -= Begin;
            void LateUpdate()
            {
                foreach (var c in Camera.allCameras)
                {
                    bool near = false; foreach (var b in All) if (b && b.isActiveAndEnabled && (b.transform.position - c.transform.position).sqrMagnitude < 80f * 80f) { near = true; break; }
                    on.TryGetValue(c, out bool was); if (near == was) continue;
                    var d = c.GetUniversalAdditionalCameraData(); if (d) d.requiresColorOption = near ? CameraOverrideOption.On : CameraOverrideOption.UsePipelineSettings;
                    on[c] = near; RefractCameras += near ? 1 : 0;
                }
            }
            void Begin(ScriptableRenderContext ctx, Camera c) { on.TryGetValue(c, out bool x); Shader.SetGlobalFloat(IdOn, x ? 1 : 0); }
        }
        public static int RefractCameras { get; private set; }
    }
}
