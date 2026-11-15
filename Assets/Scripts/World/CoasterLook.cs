using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm
{
    // Round 10: which course is shown, and its look. Only the course being raced is active (the other is hidden: no drawing, no colliders,
    // no volumes); Paradise Plunge uses JurassicLook, Coaster Coast CoasterLook (a sunset over the sea that turns to night during the lap).
    public static class CourseLook
    {
        public static Course Shown { get; private set; }
        public static void Apply(Course c)
        {
            var g = Game.Instance; if (g != null && g.Courses != null) foreach (var k in g.Courses) if (k) k.gameObject.SetActive(k == c);
            Shown = c;
            if (c && c.look == "coaster") CoasterLook.Apply(); else JurassicLook.Apply(true);
        }
    }

    // Coaster Coast's look: Tee'd Off's toon shaders (the same thresholds and outline as Paradise Plunge), a seaside palette (sun-bleached
    // boards, candy stripes, coaster red and gold, deep sea blues) and a sky that starts as a golden sunset and darkens to a violet night as
    // the leading person's racer goes round (CoasterSky); the park's lights glow either way (emissive), so the night shows them off.
    public static class CoasterLook
    {
        public static readonly Color Plank = new Color(.72f, .58f, .42f), PlankDark = new Color(.58f, .45f, .32f), Pier = new Color(.46f, .36f, .27f);
        public static readonly Color CoasterRed = new Color(.82f, .16f, .2f), CoasterGold = new Color(1f, .78f, .2f), Steel = new Color(.86f, .88f, .9f);
        public static readonly Color Candy = new Color(.98f, .42f, .62f), Mint = new Color(.4f, .9f, .78f), Lemon = new Color(1f, .9f, .35f), Sky = new Color(.38f, .7f, .98f), Cream = new Color(.98f, .94f, .84f);
        public static readonly Color SeaDeep = new Color(.08f, .24f, .42f), SeaLight = new Color(.2f, .45f, .62f), SeaCrest = new Color(.9f, .8f, .72f);
        public static readonly Color Sand = new Color(.86f, .76f, .58f), Rock = new Color(.45f, .43f, .45f), Neon = new Color(1f, .3f, .75f), NeonB = new Color(.2f, .95f, 1f);
        // sky at sunset (t = 0) and at night (t = 1)
        public static readonly Color SunsetTop = new Color(.32f, .42f, .72f), SunsetHorizon = new Color(1f, .58f, .32f), SunsetBelow = new Color(.95f, .55f, .4f);
        public static readonly Color NightTop = new Color(.05f, .05f, .18f), NightHorizon = new Color(.42f, .2f, .45f), NightBelow = new Color(.2f, .12f, .3f);
        public static readonly Color FogSunset = new Color(.95f, .66f, .52f), FogNight = new Color(.2f, .13f, .3f);
        public static readonly Color UnderwaterFog = new Color(.04f, .2f, .32f); public const float UnderwaterDensity = .022f;
        static Material sky;

        public static Material Mat(Color c, bool outline = false, Color emission = default, float scale = 1f) => JurassicLook.Mat(c, outline, emission, scale);
        public static Material Vc(bool outline = true, Color emission = default) => JurassicLook.VertexColored(outline, emission);
        public static Material Glow(Color c) => JurassicLook.Mat(c, false, c * 1.4f);

        public static void Apply()
        {
            JurassicLook.Apply(true);   // the toon thresholds, the outline, the sun's shadows
            if (!sky) { var b = Resources.Load<Material>("Materials/Jurassic/Sky"); if (b) sky = new Material(b) { name = "Coaster sky" }; }
            if (sky) RenderSettings.skybox = sky;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 260f; RenderSettings.fogEndDistance = 1500f;
            ChaseCam.UnderwaterColor = UnderwaterFog; ChaseCam.UnderwaterDensity = UnderwaterDensity;
            Evening(0); ChaseCam.ResetSkyFog();
        }
        public static float Now { get; private set; }
        // 0 = sunset, 1 = night
        public static void Evening(float t)
        {
            Now = t; float k = Mathf.SmoothStep(0, 1, t);
            if (sky) { sky.SetColor("_Top", Color.Lerp(SunsetTop, NightTop, k)); sky.SetColor("_Horizon", Color.Lerp(SunsetHorizon, NightHorizon, k)); sky.SetColor("_Below", Color.Lerp(SunsetBelow, NightBelow, k)); }
            RenderSettings.fogColor = Color.Lerp(FogSunset, FogNight, k);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.Lerp(new Color(.7f, .6f, .58f), new Color(.34f, .32f, .46f), k);
            var sun = RenderSettings.sun; if (!sun) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) { sun.color = Color.Lerp(new Color(1f, .7f, .45f), new Color(.55f, .6f, 1f), k); sun.intensity = Mathf.Lerp(1.05f, .42f, k); sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(14f, 32f, k), Mathf.Lerp(250f, 200f, k), 0); }
        }
    }

    // Coaster Coast's evening: the sky follows the leading person's racer round the lap (sunset at the start, night by the finish), eased
    public sealed class CoasterSky : MonoBehaviour
    {
        public Course course; float shown = -1;
        public static float Forced = -1;   // checks: a fixed time of the evening
        void Update()
        {
            var g = Game.Instance; if (!g || !course || CourseLook.Shown != course) return;
            float best = 0; foreach (var h in g.Humans) if (h && h.Course == course) best = Mathf.Max(best, h.Finished ? course.Length : h.Progress);
            float want = Forced >= 0 ? Forced : Mathf.Clamp01(best / Mathf.Max(1, course.Length) * 1.05f);
            shown = shown < 0 ? want : Mathf.MoveTowards(shown, want, Time.deltaTime * .04f);
            if (Mathf.Abs(shown - CoasterLook.Now) > .002f) CoasterLook.Evening(shown);
        }
    }

    // Round 10: a big moving thing along the lap that should be seen (Round 8b's rule: >= 8 % of the screen's height for >= 1 s): its box
    // (local centre and size), the segment it belongs to, the Barker pool that names it. Like DinoActor.ScreenShare: the box's corners on
    // the screen, 0 when behind the camera, too far in the fog / underwater, hidden behind solid ground or not showing (a firework between
    // bursts).
    public sealed class SetPiece : MonoBehaviour
    {
        public static readonly List<SetPiece> All = new List<SetPiece>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        public string label, segment, pool; public Vector3 centre, size = Vector3.one * 10; public float range = 900f;
        public System.Func<bool> showing;
        public float BigFor { get; private set; }
        public float ShareNow { get; private set; }
        public float ScreenShare(Camera cam)
        {
            if (!cam || !isActiveAndEnabled || (showing != null && !showing())) return 0;
            var cp = cam.transform.position; var tr = transform; var c0 = tr.TransformPoint(centre);
            bool camUnder = WaterVolume.SurfaceAt(cp, out float surf, 0f) && cp.y < surf;
            var lp = c0 - cp; var loc = tr.InverseTransformDirection(lp); var half = Vector3.Scale(size, tr.lossyScale) * .5f;
            var near = new Vector3(Mathf.Max(0, Mathf.Abs(loc.x) - half.x), Mathf.Max(0, Mathf.Abs(loc.y) - half.y), Mathf.Max(0, Mathf.Abs(loc.z) - half.z));
            float dist = near.magnitude; if (dist > range) return 0;
            if (camUnder && dist > DinoActor.UnderwaterSight) return 0;
            bool pieceUnder = WaterVolume.SurfaceAt(c0, out float ws, 0f) && c0.y + half.y < ws;
            if (!camUnder && pieceUnder) return 0;
            float minY = 9, maxY = -9, minX = 9, maxX = -9; int front = 0;
            for (int i = 0; i < 8; i++)
            {
                var p = tr.TransformPoint(centre + Vector3.Scale(new Vector3((i & 1) == 0 ? -.5f : .5f, (i & 2) == 0 ? -.5f : .5f, (i & 4) == 0 ? -.5f : .5f), size));
                var v = cam.WorldToViewportPoint(p); if (v.z < .3f) continue; front++;
                minX = Mathf.Min(minX, v.x); maxX = Mathf.Max(maxX, v.x); minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y);
            }
            if (front == 0 || maxX < 0 || minX > 1 || maxY < 0 || minY > 1) return 0;
            float share = Mathf.Clamp01(maxY) - Mathf.Clamp01(minY); if (share < .02f) return share;
            float dd = Vector3.Distance(cp, c0);
            if (!Physics.Linecast(cp, c0, out var hit, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) || hit.distance > dd - Mathf.Max(half.x, half.z)) return share;
            return 0;
        }
        static int trackedFrame = -1; static float trackedAt;
        public static void Track(Camera cam)
        {
            if (trackedFrame == Time.frameCount) return; trackedFrame = Time.frameCount;
            float dt = Mathf.Clamp(Time.time - trackedAt, 0f, .25f); trackedAt = Time.time;
            foreach (var s in All) { s.ShareNow = s.ScreenShare(cam); s.BigFor = s.ShareNow >= DinoActor.BigShare ? s.BigFor + dt : 0; }
        }
        public static SetPiece BigOnScreen(string pool, float held)
        {
            SetPiece best = null;
            foreach (var s in All) if (s.pool == pool && s.ShareNow >= DinoActor.BigShare && s.BigFor >= held && (!best || s.ShareNow > best.ShareNow)) best = s;
            return best;
        }
        public static SetPiece Make(GameObject g, string label, string segment, string pool, Vector3 centre, Vector3 size)
        {
            var s = g.AddComponent<SetPiece>(); s.label = label; s.segment = segment; s.pool = pool; s.centre = centre; s.size = size; return s;
        }
    }

    // ---- looks only ----
    // turns about its own axis
    public sealed class Spin : MonoBehaviour { public Vector3 axis = Vector3.up; public float degPerSec = 10; void Update() => transform.Rotate(axis, degPerSec * Time.deltaTime, Space.Self); }
    // the hubless Ferris wheel: the rim turns, the gondolas hang level from it
    public sealed class WheelTurn : MonoBehaviour
    {
        public Transform rim; public Transform[] gondolas; public float radius = 30, degPerSec = 5; float a;
        void Update()
        {
            a += degPerSec * Time.deltaTime; if (rim) rim.localRotation = Quaternion.Euler(0, 0, a);
            if (gondolas == null) return;
            for (int i = 0; i < gondolas.Length; i++) { float t = (a + i * 360f / gondolas.Length) * Mathf.Deg2Rad; gondolas[i].localPosition = new Vector3(Mathf.Cos(t), Mathf.Sin(t), 0) * (radius + 2.2f) + Vector3.down * 2.2f; gondolas[i].localRotation = Quaternion.identity; }
        }
    }
    // bobs up and down and sways (the mascot balloon, buoys)
    public sealed class Bob : MonoBehaviour
    {
        public float height = 1.5f, period = 4f, sway = 4f; Vector3 p0; Quaternion r0; float ph;
        void Start() { p0 = transform.localPosition; r0 = transform.localRotation; ph = Random.value * 10; }
        void Update() { float t = Time.time + ph; transform.localPosition = p0 + Vector3.up * Mathf.Sin(t * 2 * Mathf.PI / period) * height; transform.localRotation = r0 * Quaternion.Euler(Mathf.Sin(t * .7f) * sway * .5f, Mathf.Sin(t * .31f) * sway * 2, Mathf.Sin(t * .53f) * sway); }
    }
    // fireworks: a burst of glowing sparks flying out from a point every few seconds, fading as they spread
    public sealed class Firework : MonoBehaviour
    {
        public float every = 2.2f, radius = 18f, delay; public Color colour = Color.white; Transform[] sparks; float t0 = -1; Vector3[] dirs;
        public bool Bursting => t0 >= 0 && Time.time - t0 < 1.4f;
        void Start()
        {
            var mat = CoasterLook.Glow(colour); sparks = new Transform[28]; dirs = new Vector3[sparks.Length];
            for (int i = 0; i < sparks.Length; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(g.GetComponent<Collider>()); g.transform.SetParent(transform, false);
                var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; sparks[i] = g.transform; g.SetActive(false);
                dirs[i] = Random.onUnitSphere;
            }
            t0 = -1; nextAt = Time.time + delay;
        }
        float nextAt;
        void Update()
        {
            if (Time.time >= nextAt) { t0 = Time.time; nextAt = Time.time + every; for (int i = 0; i < dirs.Length; i++) dirs[i] = Random.onUnitSphere; }
            float u = t0 < 0 ? 9 : (Time.time - t0) / 1.4f;
            for (int i = 0; i < sparks.Length; i++)
            {
                bool on = u < 1; if (sparks[i].gameObject.activeSelf != on) sparks[i].gameObject.SetActive(on); if (!on) continue;
                float e = 1 - (1 - u) * (1 - u); sparks[i].localPosition = dirs[i] * radius * e + Vector3.down * 4f * u * u; sparks[i].localScale = Vector3.one * Mathf.Lerp(1.6f, .2f, u);
            }
        }
    }
    // swims / flies along beside the nearest person's racer while it is in a stretch of the lap (the whale, the dolphins): a little ahead,
    // to one side, never faster than `speed`; elsewhere it idles where it is
    public sealed class Pacer : MonoBehaviour
    {
        public Course course; public float from, to, side = 30, lead = 20, y, speed = 70, hop; public bool waterY = true;
        Vector3 vel; float hopT;
        void Update()
        {
            var g = Game.Instance; if (!g || !course || CourseLook.Shown != course) return;
            ShiftVehicle near = null; foreach (var h in g.Humans) if (h && h.Course == course && h.Progress > from && h.Progress < to) near = h;
            Vector3 want; Vector3 dir;
            if (near) { var at = course.PointAt(Mathf.Min(near.Progress + lead, course.Length), out dir); var fl = new Vector3(dir.x, 0, dir.z).normalized; want = at + Vector3.Cross(Vector3.up, fl) * side; want.y = y; }
            else { want = transform.position; dir = transform.forward; }
            if (hop > 0) { hopT += Time.deltaTime; want.y = y + Mathf.Max(0, Mathf.Sin(hopT * 2.2f)) * hop; }
            var d = want - transform.position; var step = Vector3.ClampMagnitude(d, speed * Time.deltaTime);
            transform.position += step;
            var look = step.sqrMagnitude > 1e-4f ? step : new Vector3(dir.x, 0, dir.z);
            if (look.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), Time.deltaTime * 3f);
        }
    }
    // the octopus's arms: each a chain of segments waving
    public sealed class Tentacles : MonoBehaviour
    {
        public Transform[] roots; float t;
        void Update()
        {
            t += Time.deltaTime; if (roots == null) return;
            for (int i = 0; i < roots.Length; i++)
            {
                var r = roots[i]; int k = 0;
                for (var s = r; s != null && s.childCount > 0; s = s.GetChild(0), k++) s.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f + i * .8f + k * .6f) * 14f, 0, Mathf.Cos(t * .9f + i + k * .5f) * 9f);
            }
        }
    }
}
