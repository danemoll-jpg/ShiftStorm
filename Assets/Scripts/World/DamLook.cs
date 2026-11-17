using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm
{
    // Round 16 item 2: Dam Shame's look: Tee'd Off's toon shaders (the same thresholds and outline), a mountain palette (dam concrete, safety
    // orange and hazard yellow, pine greens, granite, cold green-blue water), fresh morning light: a low warm sun from the east, a pale blue
    // sky with a peach horizon, a light morning haze down the canyon. Fixed (nothing changes while parked).
    public static class DamLook
    {
        public static readonly Color Concrete = new Color(.78f, .76f, .72f), ConcreteDark = new Color(.55f, .54f, .52f), Asphalt = new Color(.34f, .35f, .38f);
        public static readonly Color Safety = new Color(1f, .45f, .1f), Yellow = new Color(1f, .82f, .16f), Navy = new Color(.16f, .24f, .42f), Steel = new Color(.7f, .74f, .78f);
        public static readonly Color Pine = new Color(.16f, .36f, .22f), PineLight = new Color(.3f, .52f, .3f), Bark = new Color(.38f, .26f, .18f);
        public static readonly Color Rock = new Color(.56f, .54f, .52f), RockDark = new Color(.42f, .41f, .41f);
        public static readonly Color Water = new Color(.12f, .34f, .38f), WaterLight = new Color(.28f, .55f, .56f), Foam = new Color(.9f, .95f, .95f), Lake = new Color(.1f, .3f, .42f);
        public static readonly Color Fur = new Color(.48f, .32f, .2f), FurDark = new Color(.34f, .22f, .14f), Heron = new Color(.66f, .7f, .76f), Skin = new Color(.96f, .76f, .6f);
        public static readonly Color SkyTop = new Color(.38f, .6f, .92f), SkyHorizon = new Color(1f, .86f, .72f), SkyBelow = new Color(.72f, .76f, .74f), Fog = new Color(.86f, .87f, .88f);
        public static readonly Color UnderwaterFog = new Color(.1f, .26f, .26f); public const float UnderwaterDensity = .028f;
        static Material sky;

        public static Material Mat(Color c, bool outline = false, Color emission = default) => JurassicLook.Mat(c, outline, emission);
        public static Material Vc(bool outline = true, Color emission = default) => JurassicLook.VertexColored(outline, emission);
        public static Material Glow(Color c) => JurassicLook.Mat(c, false, c * 1.4f);

        public static void Apply()
        {
            JurassicLook.Apply(true);   // the toon thresholds, the outline, the sun's shadows
            if (!sky) { var b = Resources.Load<Material>("Materials/Jurassic/Sky"); if (b) sky = new Material(b) { name = "Dam Shame sky" }; }
            if (sky) { RenderSettings.skybox = sky; sky.SetColor("_Top", SkyTop); sky.SetColor("_Horizon", SkyHorizon); sky.SetColor("_Below", SkyBelow); }
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 320f; RenderSettings.fogEndDistance = 1700f; RenderSettings.fogColor = Fog;
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.66f, .69f, .74f);
            var sun = RenderSettings.sun; if (!sun) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun) { sun.color = new Color(1f, .88f, .72f); sun.intensity = 1.1f; sun.transform.rotation = Quaternion.Euler(30f, 250f, 0); }   // low, from the east-south-east down the canyon
            ChaseCam.UnderwaterColor = UnderwaterFog; ChaseCam.UnderwaterDensity = UnderwaterDensity;
            ChaseCam.ResetSkyFog();
        }
    }

    // spray: a cone of white droplets (the torrent's lip, the mist over the plunge pool)
    public sealed class Spray : MonoBehaviour
    {
        public Vector2 speed = new Vector2(6, 14); ParticleSystem ps; float rate;
        static Material mat;
        static Material Mat { get { if (!mat && GateMarker.Beacon) { mat = new Material(GateMarker.Beacon) { name = "Spray" }; mat.SetColor("_Color", new Color(.95f, .98f, 1f, .7f)); mat.SetFloat("_Pulse", 0); } return mat; } }
        public static Spray Make(Transform parent, string name, Vector3 at, Quaternion rot, float width, float rate)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.SetPositionAndRotation(at, rot);
            var s = g.AddComponent<Spray>(); s.rate = rate;
            var p = s.ps = g.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = p.main; m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = 600; m.playOnAwake = false; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f); m.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.2f); m.gravityModifier = .6f; m.startColor = new Color(.95f, .98f, 1f, .75f);
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width, .5f, 1f);
            var e = p.emission; e.rateOverTime = rate;
            var col = p.colorOverLifetime; col.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(.7f, .15f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Mat; r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = MeshKit.LowSphere; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return s;
        }
        void OnEnable() { if (ps) { var m = ps.main; m.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y); ps.Play(); } }
        void Start() { var m = ps.main; m.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y); ps.Play(); }
    }

    // Round 16 item 2: Dam Shame's water. One level for the valley, by stage: 0 (lap 1: the river in its bed), 1 (lap 2: the spillway runs,
    // the valley floor awash), 2 (lap 3: the valley flooded 16 m deep). It rises when the leader (the most gates passed by any racer on the
    // course; in a time trial the one racer) starts lap 2 and lap 3: the siren, Buck over the tannoy and Barnaby, and a 10 s rise anyone can
    // watch. A new race (the leader back on lap 1) puts it back at once. Everything that depends on the water is set from the stage, never from
    // a racer's lap: the change zones (DamShame.dynZones), the recovery anchors (form and place), the chute's wet floor and its torrent, the
    // choice jump's lip (lap 1's water only), the plunge pool's reset zone. And every physics step, for every racer here: a car the water has
    // risen over becomes a boat, a boat held under (the turbine hall's roof) a sub, a boat left on a dry road a car.
    public sealed class DamWater : MonoBehaviour
    {
        public Course course;
        public static DamWater Instance { get; private set; }
        public static int Stage { get; private set; }
        public static float Level { get; private set; } = DamShame.ValleyLevel[0];
        public static bool Rising => Instance && Instance.riseT0 >= 0;
        public const float RiseSeconds = 10f;
        public readonly List<string> Events = new List<string>();   // checks: every rise and reset of the water, every form the water decided
        float riseFrom, riseTo, riseT0 = -1, nextScan; readonly List<ShiftVehicle> racers = new List<ShiftVehicle>();
        readonly Dictionary<ShiftVehicle, float> heldUnder = new Dictionary<ShiftVehicle, float>(), leftDry = new Dictionary<ShiftVehicle, float>();
        AudioSource siren; float gateOpen;

        void Awake() => Instance = this;
        void OnEnable() { Instance = this; Scan(); Apply(Target(), true, "shown"); }

        void Scan()
        {
            racers.Clear();
            foreach (var v in FindObjectsByType<ShiftVehicle>(FindObjectsSortMode.None)) if (v && v.isActiveAndEnabled && v.Course == course) racers.Add(v);
        }
        // the water the leader has earned: lap 2 from its lap line, lap 3 from the next
        int Target()
        {
            int best = 0; foreach (var v in racers) if (v) best = Mathf.Max(best, v.GatesPassed);
            return best >= DamShame.FirstGate(3) ? 2 : best >= DamShame.FirstGate(2) ? 1 : 0;
        }
        void Update()
        {
            if (Time.time >= nextScan) { nextScan = Time.time + .2f; Scan(); }
            int t = Target();
            if (t < Stage) Apply(t, true, "a new race");
            else if (t > Stage)
            {
                bool fresh = racers.Count == 0 || racers.TrueForAll(v => !v || v.Holding || Time.time - v.StartTime < 1.5f);
                Apply(t, fresh, fresh ? "started there" : "the leader's lap");
            }
            if (riseT0 >= 0)
            {
                float k = Mathf.Clamp01((Time.time - riseT0) / RiseSeconds); SetLevel(Mathf.Lerp(riseFrom, riseTo, k * k * (3 - 2 * k)));
                if (k >= 1) { riseT0 = -1; Events.Add($"{Time.time:0.0} risen to {Level:0.0} m"); }
            }
            // the spillway's radial gates swing up while it runs
            gateOpen = Mathf.MoveTowards(gateOpen, Stage >= 1 ? 1 : 0, Time.deltaTime / 4f);
            for (int i = 0; i < DamShame.GateLeaves.Count; i++) { var l = DamShame.GateLeaves[i]; if (!l) continue; var e = l.localEulerAngles; l.localEulerAngles = new Vector3(-55f * gateOpen * (i == 1 || Stage >= 2 ? 1 : .55f), e.y, e.z); }
        }

        void SetLevel(float y)
        {
            Level = y; var w = DamShame.ValleyWater; if (!w) return;
            w.size = new Vector3(w.size.x, y - DamShame.Floor, w.size.z); var p = w.transform.position; w.transform.position = new Vector3(p.x, (y + DamShame.Floor) * .5f, p.z);
            var s = DamShame.ValleySurface; if (s) { var q = s.position; s.position = new Vector3(q.x, y - .02f, q.z); }
        }

        void Apply(int stage, bool snap, string why)
        {
            int was = Stage; Stage = stage;
            foreach (var (z, t) in DamShame.dynZones) { if (!z) continue; var m = t[stage]; z.gameObject.SetActive(m.HasValue); if (m.HasValue) z.target = m.Value; }
            foreach (var (a, w, road, at) in DamShame.dynAnchors) { if (!a) continue; var (m, p) = DamShame.AnchorFor(w, road, at, stage); a.mode = m; a.transform.position = p; }
            if (DamShame.ChuteFloor) foreach (var r in DamShame.ChuteFloor.transform.parent.GetComponentsInChildren<WetRamp>(true)) r.enabled = stage >= 1;
            foreach (var g in DamShame.Torrent) if (g) g.SetActive(stage >= 1);
            foreach (var l in DamShame.lips) if (l) l.gameObject.SetActive(stage == 0);
            foreach (var (g, up) in DamShame.StageThings) if (g) g.SetActive(up[stage]);
            var cz = DamShame.PoolReset; if (cz) cz.gameObject.SetActive(stage == 0);
            var sc = DamShame.Canyon; if (sc && sc.entry) sc.entry.modes = stage == 0 ? new[] { Mode.Fly } : new[] { Mode.Dive };   // the Canyon flight: lap 1's water only (later the lip throws everyone up)
            float to = DamShame.ValleyLevel[stage];
            if (snap) { riseT0 = -1; SetLevel(to); gateOpen = stage >= 1 ? 1 : 0; }
            else { riseFrom = Level; riseTo = to; riseT0 = Time.time; Siren(); StartCoroutine(Announce(stage)); }
            Events.Add($"{Time.time:0.0} stage {was} -> {stage} ({why}){(snap ? ", at once" : $", rising {Level:0.0} -> {to:0.0} m over {RiseSeconds:0} s")}");
            Debug.Log($"[Dam] water stage {was} -> {stage} ({why}){(snap ? " at once" : " rising")}");
        }

        // Buck over the tannoy first, then Barnaby
        IEnumerator Announce(int stage)
        {
            var bk = Buck.Instance; var cl = Caller.Instance; var p1 = Game.Instance ? Game.Instance.Player : null;
            if (bk && Buck.Enabled && Settings.BuckBriefings != 2) bk.PA($"rise{stage}");
            float t0 = Time.time; while (bk && bk.Busy && Time.time - t0 < 12f) yield return null;
            if (cl && p1 && p1.Course == course && !p1.Finished) cl.Call($"dmrise{stage}", p1, true);
        }

        void Siren()
        {
            if (!siren) { siren = gameObject.AddComponent<AudioSource>(); siren.spatialBlend = 0; siren.playOnAwake = false; siren.clip = SirenClip(); }
            siren.volume = .32f * Settings.Effects; siren.Play(); SoundLog.Add("siren", "Dam siren");
        }
        static AudioClip sirenClip;
        static AudioClip SirenClip()
        {
            if (sirenClip) return sirenClip;
            const int rate = 22050; float len = 4.2f; int n = (int)(rate * len); var d = new float[n]; double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate, wail = .5f - .5f * Mathf.Cos(t * Mathf.PI * 2 / 2.1f), f = 380 + 420 * wail;
                ph += 2 * Mathf.PI * f / rate; float env = Mathf.Clamp01(t / .3f) * Mathf.Clamp01((len - t) / .6f);
                d[i] = env * .5f * (Mathf.Sin((float)ph) * .7f + Mathf.Sin((float)ph * 2) * .2f + Mathf.Sin((float)ph * 3) * .1f);
            }
            sirenClip = AudioClip.Create("dam_siren", n, 1, rate, false); sirenClip.SetData(d, 0); return sirenClip;
        }

        // every physics step: the forms the water decides for the racers it reaches
        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            foreach (var v in racers)
            {
                if (!v || !v.isActiveAndEnabled || v.Course != course || v.Holding || v.Changing || v.Finished || v.OnShortcut) continue;
                var p = v.Body.position; bool wet = WaterVolume.SurfaceAt(p, out float s, .8f); float depth = wet ? s - p.y : -99f;
                if (v.Mode == Mode.Drive && depth > .15f && Inside(p)) { Decide(v, Mode.Boat, $"the water rose over the road ({depth:0.0} m)"); continue; }
                if (v.Mode == Mode.Boat)
                {
                    heldUnder[v] = depth > 2.6f ? (heldUnder.TryGetValue(v, out var h) ? h : 0) + dt : 0;
                    if (heldUnder[v] > .4f) { heldUnder[v] = 0; Decide(v, Mode.Dive, $"held under by the flood ({depth:0.0} m)"); continue; }
                    bool dry = !v.OnWater && depth < -1f && v.Speed < 14f && Physics.Raycast(p, Vector3.down, out var hit, 2.5f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore) && !(hit.collider.GetComponentInParent<WetRamp>() is WetRamp wr && wr.enabled);
                    leftDry[v] = dry ? (leftDry.TryGetValue(v, out var d) ? d : 0) + dt : 0;
                    if (leftDry[v] > 1f) { leftDry[v] = 0; Decide(v, Mode.Drive, "left on a dry road"); }
                }
            }
        }
        // the valley only (not the reservoir behind the dam, not the plunge pool on lap 1)
        static bool Inside(Vector3 p) { var w = DamShame.ValleyWater; return w && w.ContainsXZ(p) && (Stage > 0 || !DamShame.InPool(p)); }
        void Decide(ShiftVehicle v, Mode m, string why) { v.Change(m, why); Events.Add($"{Time.time:0.0} {v.RacerName}: {m} ({why}) at stage {Stage}"); }
    }
}
