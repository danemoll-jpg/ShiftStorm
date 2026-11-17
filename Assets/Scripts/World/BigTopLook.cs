using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm
{
    // Round 15 item 3: Big Top Breakdown's look: Tee'd Off's toon shaders (the same thresholds and outline), a circus palette (big-top red
    // and white stripes, circus gold, royal blue, meadow green, sawdust), bright gusty daylight: a high warm sun, a clear blue sky with a pale
    // horizon, light far fog. Fixed (no time of day: nothing changes while parked).
    public static class BigTopLook
    {
        public static readonly Color TopRed = new Color(.86f, .14f, .16f), TopWhite = new Color(.98f, .96f, .9f), Gold = new Color(1f, .78f, .18f), Royal = new Color(.16f, .3f, .82f);
        public static readonly Color Meadow = new Color(.42f, .7f, .3f), MeadowDark = new Color(.33f, .58f, .25f), Sawdust = new Color(.86f, .7f, .45f), Dirt = new Color(.66f, .52f, .36f);
        public static readonly Color Wood = new Color(.6f, .42f, .26f), WoodDark = new Color(.45f, .31f, .2f), Steel = new Color(.8f, .82f, .86f), Teal = new Color(.2f, .72f, .7f);
        public static readonly Color Purple = new Color(.55f, .28f, .78f), Orange = new Color(1f, .55f, .15f), Pink = new Color(1f, .5f, .68f), River = new Color(.16f, .42f, .5f), RiverLight = new Color(.3f, .62f, .66f);
        public static readonly Color Grey = new Color(.6f, .6f, .64f), Skin = new Color(.96f, .76f, .6f);
        public static readonly Color SkyTop = new Color(.22f, .5f, .95f), SkyHorizon = new Color(.78f, .9f, 1f), SkyBelow = new Color(.62f, .78f, .7f), Fog = new Color(.78f, .88f, .96f);
        public static readonly Color UnderwaterFog = new Color(.08f, .3f, .32f); public const float UnderwaterDensity = .02f;
        static Material sky;

        public static Material Mat(Color c, bool outline = false, Color emission = default) => JurassicLook.Mat(c, outline, emission);
        public static Material Vc(bool outline = true, Color emission = default) => JurassicLook.VertexColored(outline, emission);
        public static Material Glow(Color c) => JurassicLook.Mat(c, false, c * 1.4f);

        public static void Apply()
        {
            JurassicLook.Apply(true);   // the toon thresholds, the outline, the sun's shadows
            if (!sky) { var b = Resources.Load<Material>("Materials/Jurassic/Sky"); if (b) sky = new Material(b) { name = "Big Top sky" }; }
            if (sky) { RenderSettings.skybox = sky; sky.SetColor("_Top", SkyTop); sky.SetColor("_Horizon", SkyHorizon); sky.SetColor("_Below", SkyBelow); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 420f; RenderSettings.fogEndDistance = 1900f; RenderSettings.fogColor = Fog;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.74f, .74f, .72f);
            var sun = RenderSettings.sun; if (!sun) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) { sun.color = new Color(1f, .96f, .86f); sun.intensity = 1.15f; sun.transform.rotation = Quaternion.Euler(52f, 215f, 0); }
            ChaseCam.UnderwaterColor = UnderwaterFog; ChaseCam.UnderwaterDensity = UnderwaterDensity;
            ChaseCam.ResetSkyFog();
        }
    }

    // ---- looks only ----
    // a pennant / banner / canvas flap in the gusty wind: swings about its own axis, harder in a gust (every few seconds)
    public sealed class Flap : MonoBehaviour
    {
        public Vector3 axis = Vector3.up; public float deg = 18, speed = 5; float ph; Quaternion r0;
        void Start() { r0 = transform.localRotation; ph = Random.value * 20; }
        void Update()
        {
            float t = Time.time + ph, gust = .55f + .45f * Mathf.Clamp01(Mathf.Sin(t * .35f) * 2.2f);
            transform.localRotation = r0 * Quaternion.AngleAxis(Mathf.Sin(t * speed) * deg * gust + Mathf.Sin(t * speed * 2.3f) * deg * .25f, axis);
        }
    }
    // swings like a pendulum about its local x axis (the trapeze, the tightrope walker's lean)
    public sealed class Swing : MonoBehaviour
    {
        public float deg = 35, period = 3.4f, offset; Quaternion r0;
        void Start() => r0 = transform.localRotation;
        void Update() => transform.localRotation = r0 * Quaternion.Euler(Mathf.Sin((Time.time + offset) * 2 * Mathf.PI / period) * deg, 0, 0);
    }
    // goes back and forth along its local x between -half and +half (the tightrope walker)
    public sealed class Pace : MonoBehaviour
    {
        public float half = 10, speed = 2.5f; Vector3 p0; float ph;
        void Start() { p0 = transform.localPosition; ph = Random.value * 10; }
        void Update() { float u = Mathf.PingPong((Time.time + ph) * speed, half * 2) - half; transform.localPosition = p0 + Vector3.right * u; transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2.1f) * 7f); }
    }
    // things thrown in arcs from a point and falling back (the clowns tumbling out of the tiny car, the elephants' water)
    public sealed class Arcs : MonoBehaviour
    {
        public Transform[] items; public Vector3 throwVel = new Vector3(0, 12, 6); public float spread = 3, gravity = 18, life = 1.6f, spin = 360; float[] t0; Vector3[] vel;
        void Start()
        {
            if (items == null) return; t0 = new float[items.Length]; vel = new Vector3[items.Length];
            for (int i = 0; i < items.Length; i++) { t0[i] = Time.time - life * i / items.Length; vel[i] = Throw(); }
        }
        Vector3 Throw() => throwVel + new Vector3(Random.Range(-spread, spread), Random.Range(-spread, spread) * .3f, Random.Range(-spread, spread));
        void Update()
        {
            if (items == null) return;
            for (int i = 0; i < items.Length; i++)
            {
                float t = Time.time - t0[i]; if (t > life) { t0[i] = Time.time; vel[i] = Throw(); t = 0; }
                items[i].localPosition = vel[i] * t + Vector3.down * (.5f * gravity * t * t);
                items[i].localRotation = Quaternion.Euler(spin * t, spin * .6f * t, 0);
            }
        }
    }
    // Round 15: the circus wagons rolling along the midway's side lanes (kinematic: a car that runs into one glances off it like a wall). Each
    // runs from `from` to `to` along the line at `speed`, off the line by `side`, then starts again at `from` (both ends are under a canvas
    // gateway arch, so it is never seen to appear).
    public sealed class Wagon : MonoBehaviour
    {
        public Course course; public float from, to, side, speed = 11, at; Rigidbody rb;
        void Start() { rb = GetComponent<Rigidbody>(); Place(true); }
        void FixedUpdate() { at += speed * Time.fixedDeltaTime; if (at > to) at = from + (at - to); Place(false); }
        void Place(bool snap)
        {
            if (!course) return; var p = course.PointAt(at, out var d); var f = new Vector3(d.x, 0, d.z).normalized; var pos = p + Vector3.Cross(Vector3.up, f) * side; pos.y = BigTop.G;
            var rot = Quaternion.LookRotation(f, Vector3.up);
            if (snap || !rb) { transform.SetPositionAndRotation(pos, rot); if (rb) { rb.position = pos; rb.rotation = rot; } } else { rb.MovePosition(pos); rb.MoveRotation(rot); }
        }
    }
}
