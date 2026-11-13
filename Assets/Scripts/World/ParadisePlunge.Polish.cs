using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 5 item 6: the first polished stretch, Cliffside Climb -> Sky Arch -> Lagoon Landing, in Tee'd Off's Jurassic Par look (its
    // JurassicBuilder "cliff", "boulder", "fern", "waterfall" and "ptnest" decorations and Waterfall effect, rebuilt here at ShiftStorm's
    // scale; colours from ArtStyle_Jurassic via JurassicLook). Looks only: every collider stays the gray box's (the cliff strip, the arch's
    // boxes, the lagoon banks), so handling, walls and the checks' routes are unchanged; the rock dressing sits on or behind those faces,
    // never inside the route. Readability first: nothing over the road, the flight corridor (11 m round the line), the slit or the landing
    // corridor; gates, rings and buoys are unlit beacons that read through all of it. Gray box stays everywhere else.
    public static partial class ParadisePlunge
    {
        public const int DetailLayer = 11; public const float DetailCullDistance = 800f;   // the polish is drawn only this close (the arch from the ramp: ~500 m)
        static readonly Color Cloud = new Color(1f, .96f, .9f), CloudShade = new Color(.9f, .8f, .72f), Egg = new Color(.93f, .9f, .78f);

        static void Polish()
        {
            var rnd = new System.Random(505);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            // per 200 m cell (like Scenery's plants), so the camera culls what it does not see (one batch over the whole stretch was
            // drawn, with its shadows, from everywhere on the lap)
            var rock = new CellBatch(); var green = new CellBatch();

            // ---- Cliffside Climb: the hillside's road face becomes stacked mossy rock ledges (TO "cliff"), ferns in the cracks ----
            float c0 = M("climb0") + 10, c1 = M("rampStart") - 20;
            for (float p = c0; p < c1; p += 8f)
            {
                var q = At(p); var r = Right(p); var f = Facing(p); float top = q.y + 9f;
                for (int row = 0; row < 4; row++)
                {
                    float y = top - 1.2f - row * 2.6f + R(-.4f, .4f); if (y < q.y - 1.6f) continue;
                    // the road's edge is 12.5 m out and the barrier stands at the face (13.3 m): a ledge more than 2.5 m over the road may
                    // overhang the barrier by up to ~1 m (never over the driving space); lower ones stay flush with the face
                    bool high = y - (q.y - .9f) > 2.5f;
                    var at = new Vector3(q.x, y, q.z) - r * (high ? 15.1f + R(0, .3f) : 15.6f + R(0, .3f));
                    rock.Add(MeshKit.LowSphere, at, f * Quaternion.Euler(R(-8, 8), R(-15, 15), 0), new Vector3(high ? R(4.8f, 5.4f) : R(3.6f, 4.4f), R(2.8f, 3.8f), R(5f, 8f)), (row + (int)(p / 8f)) % 3 == 0 ? JurassicLook.RockDark : JurassicLook.Rock);
                }
                // moss lip along the top edge, ferns hanging in the cracks every few metres
                rock.Add(MeshKit.LowSphere, new Vector3(q.x, top + .1f, q.z) - r * 16f, f, new Vector3(7.5f, 1.4f, 8f), JurassicLook.Moss);
                if (rnd.NextDouble() < .45) Fern(green, new Vector3(q.x, top - R(2.5f, 5f), q.z) - r * 13.9f, R(.45f, .7f), R(0, 360));
                if (rnd.NextDouble() < .5) Plant(green, new Vector3(q.x, top + .6f, q.z) - r * R(18, 30), rnd);
            }
            // boulders at the foot of the cliff between the barrier and the face (never on the road)
            for (float p = c0 + 12; p < c1; p += R(30, 50))
            {
                var q = At(p); var r = Right(p); var at = new Vector3(q.x, q.y - 1f, q.z) - r * (HalfWidth(p) + 2.4f);
                if (Vector3.Distance(at, new Vector3(q.x, at.y, q.z)) > 13f) continue;   // only where there is room behind the barrier
                Boulder(rock, at, .45f, R(0, 360));
            }
            // waterfalls off the hillside, pouring down beside the climb into the gap behind the barrier
            foreach (float frac in new[] { .28f, .62f })
            {
                float p = Mathf.Lerp(c0, c1, frac); var q = At(p); var r = Right(p);
                Fall(new Vector3(q.x, q.y - 1.3f, q.z) - r * 13.6f, Quaternion.LookRotation(r, Vector3.up), 6f, 10.5f);
            }
            // the valley side of the climb (right, below the road): rock and ferns on the cliff the road is cut into
            for (float p = M("corner1") + 30; p < c1; p += 11f)
            {
                var q = At(p); var r = Right(p); float y = q.y - 6f;
                rock.Add(MeshKit.LowSphere, new Vector3(q.x, y, q.z) + r * (HalfWidth(p) + 4.5f), Facing(p), new Vector3(6f, R(4f, 6f), 9f), rnd.NextDouble() < .5 ? JurassicLook.Rock : JurassicLook.RockDark);
                if (rnd.NextDouble() < .35) Fern(green, new Vector3(q.x, y + 3.5f, q.z) + r * (HalfWidth(p) + 4f), R(.5f, .8f), R(0, 360));
            }

            // ---- Sky Arch: rock pillars and span as stacked lumps round the gray-box colliders (their boxes stay, hidden) ----
            {
                float p = M("arch"); var q = At(p); var f = Facing(p); var r = Right(p);
                foreach (Transform t in root) if (t.name.StartsWith("Arch pillar") || t.name.StartsWith("Arch span")) { var mr = t.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false; }
                foreach (float s in new[] { -1f, 1f })
                {
                    var basePt = new Vector3(q.x, 0, q.z) + r * s * 44;
                    for (float y = 4; y < 80; y += 7f)
                        for (int k = 0; k < 4; k++)
                        {
                            var off = r * ((k % 2) * 9f - 4.5f + R(-1, 1)) + f * Vector3.forward * ((k / 2) * 10f - 5f + R(-1, 1));
                            rock.Add(MeshKit.LowSphere, basePt + Vector3.up * (y + R(-1, 1)) + off, f * Quaternion.Euler(0, R(0, 90), 0), new Vector3(R(12, 15), R(8, 10), R(13, 16)), (k + (int)y) % 3 == 0 ? JurassicLook.RockDark : JurassicLook.Rock);
                        }
                    // moss bands and ferns on the ledges
                    foreach (float y in new[] { 22f, 47f, 66f }) rock.Add(MeshKit.LowSphere, basePt + Vector3.up * y, f, new Vector3(25, 3, 27), JurassicLook.Moss);
                    for (int i = 0; i < 6; i++) Fern(green, basePt + Vector3.up * (12 + i * 11) + r * s * R(10, 12) + f * Vector3.forward * R(-8, 8), R(.8f, 1.2f), R(0, 360));
                }
                // the span: lumps along it, a mossy top with cycads and a pterodactyl nest (TO "ptnest") on it
                for (float x = -52; x <= 52; x += 8)
                    for (int k = 0; k < 2; k++)
                        rock.Add(MeshKit.LowSphere, new Vector3(q.x, 82 + (k - .5f) * 6 + R(-.8f, .8f), q.z) + r * (x + R(-1.5f, 1.5f)), f, new Vector3(R(12, 15), R(9, 11), R(26, 29)), k == 0 ? JurassicLook.RockDark : JurassicLook.Rock);
                rock.Add(MeshKit.LowSphere, new Vector3(q.x, 91.6f, q.z), f, new Vector3(118, 4, 30), JurassicLook.Moss);
                for (int i = 0; i < 7; i++) Plant(green, new Vector3(q.x, 93f, q.z) + r * (-45 + i * 15 + R(-3, 3)) + f * Vector3.forward * R(-9, 9), rnd);
                var nest = new Vector3(q.x, 93.4f, q.z) + r * 30f;
                for (int i = 0; i < 24; i++) { float a = i * Mathf.PI * 2 / 24; green.Add(MeshKit.LowCylinder, nest + new Vector3(Mathf.Cos(a) * 6f, .8f, Mathf.Sin(a) * 6f), Quaternion.Euler(78, -a * Mathf.Rad2Deg, 0), new Vector3(.4f, 2.6f, .4f), i % 3 == 0 ? new Color(.5f, .37f, .22f) : new Color(.62f, .47f, .28f)); }
                foreach (var (ex, ez) in new[] { (-1.6f, .8f), (1.2f, -.6f), (.2f, 1.6f) }) green.Add(MeshKit.LowSphere, nest + new Vector3(ex, 1.6f, ez), Quaternion.identity, new Vector3(1.6f, 2.2f, 1.6f), Egg);
                Dino("ptero", nest + new Vector3(4.5f, 1.2f, 2f), 200, 4.5f);
            }

            // ---- Lagoon Landing: sand beaches and rocks round the banks, palms (cycads) and ferns, waterfalls into the lagoon,
            //      foam round the landing (drifting toon foam ribbons) ----
            {
                var sand = JurassicLook.Sand; var wet = new Color(.74f, .66f, .48f);
                // beach: a sloped strip just inside each bank, from the water to the bank top
                void Beach(Vector3 a, Vector3 b, Vector3 inward)
                {
                    float len = Vector3.Distance(a, b); var dir = (b - a).normalized; int n = Mathf.CeilToInt(len / 22f);
                    for (int i = 0; i <= n; i++)
                    {
                        var at = Vector3.Lerp(a, b, i / (float)n);
                        rock.Add(MeshKit.LowSphere, at + inward * 6f + Vector3.up * .2f, Quaternion.LookRotation(dir, Vector3.up), new Vector3(16f, 2.2f, 26f), i % 2 == 0 ? sand : wet);
                        if (i % 3 == 1) Boulder(rock, at - inward * 3f + Vector3.up * 1.5f, R(.5f, .8f), R(0, 360));
                        if (i % 2 == 0) Plant(green, at - inward * R(4, 12) + Vector3.up * 1.6f, rnd);
                    }
                }
                Beach(W(300, 0, -300), W(525, 0, -300), Vector3.back);
                Beach(W(500, 0, -295), W(500, 0, -640), Vector3.left);
                Beach(W(300, 0, -620), W(525, 0, -620), Vector3.forward);
                Beach(W(300, 0, -330), W(300, 0, -500), Vector3.right);
                // waterfalls off a rock cliff on the east bank into the lagoon (well beside the landing corridor)
                Fall(W(500, 0, -420), Quaternion.LookRotation(Vector3.left), 9f, 16f, true);
                Fall(W(500, 0, -545), Quaternion.LookRotation(Vector3.left), 7f, 12f, true);
                // foam: ribbons drifting along the landing corridor's edges and a ring of foam where the planes come down
                float sp = M("splash"); var foam = JurassicLook.Mat(new Color(.95f, .98f, 1f), false, new Color(.2f, .22f, .22f));
                for (int i = -2; i <= 8; i++) foreach (float side in new[] { -1f, 1f })
                {
                    float p = sp + i * 18; var pt = At(p); pt.y = Water + .06f; var d = Flat(Dir(p));
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(g.GetComponent<Collider>()); g.name = "foam ribbon"; g.transform.SetParent(scen, false);
                    g.transform.position = pt + Right(p) * side * R(10, 24); g.transform.rotation = Quaternion.LookRotation(d); g.transform.localScale = new Vector3(R(2, 4), .03f, R(1, 1.6f));
                    var mr = g.GetComponent<MeshRenderer>(); mr.sharedMaterial = foam; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    g.AddComponent<Drift>().velocity = d * R(1.5f, 3f);
                }
            }

            // ---- sky: Tee'd Off's puffy clouds (ArtStyle_Jurassic cloudCount 9) over the stretch, high above the flight ----
            var clouds = new MeshKit.Batch();
            for (int i = 0; i < 9; i++)
            {
                var at = W(R(80, 520), R(190, 260), R(-560, 520));
                for (int k = 0; k < 5; k++) clouds.Add(MeshKit.LowSphere, at + new Vector3(R(-28, 28), R(-4, 6), R(-14, 14)), Quaternion.identity, new Vector3(R(24, 40), R(12, 18), R(18, 28)), k == 0 ? CloudShade : Cloud);
            }
            clouds.Object(scen, "Clouds", JurassicLook.VertexColored(false, new Color(.35f, .32f, .28f)), false, false);

            rock.Objects(scen, "Polished rock", JurassicLook.VertexColored(true), false);   // no shadow casting: big lumps in every cascade cost ~1 M triangles
            green.Objects(scen, "Polished plants", JurassicLook.VertexColored(true), false);
        }

        // a MeshKit.Batch per 200 m cell of the ground plan, chosen by where each piece is added
        sealed class CellBatch
        {
            readonly Dictionary<Vector2Int, MeshKit.Batch> cells = new Dictionary<Vector2Int, MeshKit.Batch>();
            public MeshKit.Batch At(Vector3 p) { var k = new Vector2Int(Mathf.FloorToInt(p.x / 200f), Mathf.FloorToInt(p.z / 200f)); if (!cells.TryGetValue(k, out var b)) cells[k] = b = new MeshKit.Batch(); return b; }
            public void Add(Mesh m, Vector3 pos, Quaternion rot, Vector3 scale, Color col) => At(pos).Add(m, pos, rot, scale, col);
            // on the Detail layer: the camera draws it only within DetailCullDistance (Game sets Camera.layerCullDistances)
            public void Objects(Transform parent, string name, Material mat, bool shadows) { foreach (var kv in cells) kv.Value.Object(parent, $"{name} {kv.Key}", mat, false, shadows).layer = DetailLayer; }
        }
        static void Fern(CellBatch b, Vector3 p, float s, float yaw) => Fern(b.At(p), p, s, yaw);
        static void Cycad(CellBatch b, Vector3 p, float s, float yaw) => Cycad(b.At(p), p, s, yaw);
        static void Boulder(CellBatch b, Vector3 p, float s, float yaw) => Boulder(b.At(p), p, s, yaw);
        static void Plant(CellBatch b, Vector3 p, System.Random rnd)
        {
            float yaw = (float)rnd.NextDouble() * 360, s = .9f + (float)rnd.NextDouble() * .6f;
            if (rnd.NextDouble() < .55) Cycad(b, p, s, yaw); else Fern(b, p, s, yaw);
        }

        // Tee'd Off's Waterfall: a sheet off a mossy lip, white streaks running down it, a foam pool at its foot (looks only)
        static void Fall(Vector3 foot, Quaternion facing, float width, float height, bool cliff = false)
        {
            var go = new GameObject("Waterfall (polish)"); go.transform.SetParent(scen, false); go.transform.SetPositionAndRotation(foot, facing);
            if (cliff)   // a rock face behind it (TO's Waterfall cliff), mossy on top, built in the world batch's colours
            {
                var cb = new MeshKit.Batch();
                for (float y = 1.5f; y < height; y += 3.4f) for (int i = -1; i <= 1; i++) cb.Add(MeshKit.LowSphere, new Vector3(i * (width * .7f + 2f), y, -3.5f - Mathf.Abs(i) * .8f), Quaternion.Euler(0, i * 20, 0), new Vector3(width * .8f + 3f, 4.2f, 6f), (i + (int)y) % 2 == 0 ? JurassicLook.Rock : JurassicLook.RockDark);
                cb.Add(MeshKit.LowSphere, new Vector3(0, height + .6f, -3.5f), Quaternion.identity, new Vector3(width * 2.4f + 6f, 2f, 7f), JurassicLook.Moss);
                cb.Object(go.transform, "cliff", JurassicLook.VertexColored(true), false, true);
            }
            var sheet = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(sheet.GetComponent<Collider>()); sheet.transform.SetParent(go.transform, false);
            sheet.transform.localPosition = new Vector3(0, height * .5f, .3f); sheet.transform.localScale = new Vector3(width, height, 1); sheet.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var sr = sheet.GetComponent<MeshRenderer>(); sr.sharedMaterial = JurassicLook.FallMat(.7f); sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var b = new MeshKit.Batch();
            b.Add(MeshKit.LowSphere, new Vector3(0, height + .2f, -.6f), Quaternion.identity, new Vector3(width + 2.5f, 1.4f, 3.2f), JurassicLook.Moss);
            b.Add(MeshKit.LowCylinder, new Vector3(0, .05f, 1.8f), Quaternion.identity, new Vector3(width * .9f, .12f, 3.4f), new Color(.92f, .97f, 1f));
            b.Object(go.transform, "lip and pool", JurassicLook.VertexColored(true), false, false);
            var fx = go.AddComponent<FallStreaks>(); fx.width = width; fx.height = height;
        }
    }

    // looks only: white water streaking down a waterfall's face (Tee'd Off's Waterfall.Update)
    public sealed class FallStreaks : MonoBehaviour
    {
        public float width = 6, height = 10; readonly List<Transform> s = new List<Transform>();
        void Start()
        {
            var mat = JurassicLook.Mat(Color.white, false, new Color(.3f, .3f, .3f));
            for (int i = 0; i < 10; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                g.transform.localScale = new Vector3(.45f, Mathf.Clamp(height * .14f, 1.2f, 6f), .25f); var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; s.Add(g.transform);
            }
        }
        void Update()
        {
            for (int i = 0; i < s.Count; i++) { float u = Mathf.Repeat(Time.time * (9f / Mathf.Max(4f, height)) + i * .37f, 1f); s[i].localPosition = new Vector3(((i * .61f) % 1f - .5f) * width * .85f, height * (1f - u), .5f); }
        }
    }
}
