using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 9 item 3b (Dan: "I didn't even notice a crowd. But can we take the crowd work from Gridiron Glory? There was already extensive
    // work to make the crowd look more real."): the grandstand crowd, ported from Gridiron Glory's CrowdRenderer (C:\Users\danmo\footballGame,
    // Scripts/Gameplay/Field/CrowdRenderer.cs, read-only: copied and changed; Docs/REUSE-AUDIT.md). Every seat is one GPU-instanced,
    // camera-facing fan sprite from Gridiron Glory's fan atlas (Resources/Crowd/FanAtlas.png: the Y Bot in Dan's Mixamo clips, 14 poses x 6
    // frames: sitting, clapping, cheering, arms up, jumping, fist pumps, towels, foam fingers), each with his own shirt, skin, shorts, hair or
    // hat, size, mirror and beat (Resources/Shaders/CrowdInstanced.shader, also copied). Changed for ShiftStorm:
    //   - one Crowd per grandstand, with its own material: a racer passing within reach makes that stand cheer, the cheer rippling along the
    //     stand from where the racer is at the racer's speed (fans stand up, jump, wave arms, towels and foam fingers); a person's racer gets
    //     the full cheer, an AI's a smaller one; everyone sways and claps a little meanwhile
    //   - Jurassic Par colours (tropical shirts, sun hats), cartoon size (x1.35: a 2.4 m fan, like the cartoon-huge dinosaurs)
    //   - URP; the light from the sun; drawn by every camera (split screen) within DrawDistance
    //   - sound: a murmur bed per stand (Gridiron Glory's crowd_bed_engaged, 3D); a roar at GO from the start stands; a cheer when a person's
    //     racer passes the waterfall-landing stand (crowd_cheer_small, 3D). No cheer sound at the finish (GameSounds plays the finish's one
    //     crowd sound) or the loop (its own "ooh"): Round 9 item 3, one sound per event
    //   Presentation only; no football-match mood (boos, the wave round a bowl, ponchos, empty seats).
    public sealed class Crowd : MonoBehaviour
    {
        public static readonly List<Crowd> All = new List<Crowd>();
        public static bool Off = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noCrowd") >= 0;   // checks: the frame time without it
        public const float DrawDistance = 700f;
        public static float Density = 1f;   // Round 13 item 5: the share of each stand's fans drawn (GraphicsPreset)
        public string StandName { get; private set; }
        public int Fans { get; private set; }
        public int Cheers { get; private set; }
        public float LastCheer { get; private set; } = -99;
        public Vector3 Center { get; private set; }
        public static double CpuMs, CpuTotal; public static int CpuFrames;   // the crowd's own CPU time (all stands), for the report

        Mesh mesh; Material mat; readonly List<Matrix4x4[]> batches = new List<Matrix4x4[]>(); readonly List<int> counts = new List<int>();
        readonly List<MaterialPropertyBlock> blocks = new List<MaterialPropertyBlock>();
        Vector3 axis, a0, a1; float reach; bool passSound, goSound; Vector4 burst, sweep;
        readonly Dictionary<ShiftVehicle, float> lastFor = new Dictionary<ShiftVehicle, float>();
        AudioSource bed, cheer; static AudioClip[] small, big; static AudioClip bedClip;
        static float Now => Time.unscaledTime;
        static readonly int IdEnergy = Shader.PropertyToID("_CrowdEnergy"), IdTime = Shader.PropertyToID("_CrowdTime"), IdBurst = Shader.PropertyToID("_CrowdBurst"),
            IdSweep = Shader.PropertyToID("_CrowdSweep"), IdShirt = Shader.PropertyToID("_Shirt"), IdParams = Shader.PropertyToID("_Params"), IdParams2 = Shader.PropertyToID("_Params2"),
            IdNeck = Shader.PropertyToID("_CrowdNeck"), IdGrid = Shader.PropertyToID("_CrowdGrid"), IdAtlas = Shader.PropertyToID("_Atlas"), IdLight = Shader.PropertyToID("_CrowdLight"), IdAmb = Shader.PropertyToID("_CrowdAmbient");

        [System.Serializable] sealed class AtlasInfo { public int frames, poses, cellW, cellH; public float boxW, boxH; public string[] names; public float[] neck, rect; }
        static Material baseMat; static AtlasInfo info; static bool tried;

        // the Jurassic Par crowd's shirts: tropical prints in sunset orange, lagoon teal, palm green, hibiscus pink, sand, sky blue, white
        public static readonly Color[] Tropical = { new Color(.98f, .55f, .15f), new Color(.15f, .7f, .68f), new Color(.35f, .72f, .28f), new Color(.95f, .38f, .55f),
            new Color(.95f, .86f, .6f), new Color(.35f, .6f, .95f), new Color(.96f, .95f, .9f), new Color(.98f, .82f, .2f) };

        // seats: positions on the stand (feet); along: the stand's long axis; a0 / a1: its two ends (the racer is "passing" within reach of the segment)
        public static Crowd Build(Transform parent, string name, List<Vector3> seats, Vector3 along, float reach, bool passSound, bool goSound, int seed, float scale = 1.35f)
        {
            var go = new GameObject("Crowd " + name); go.transform.SetParent(parent, false);
            var c = go.AddComponent<Crowd>(); c.StandName = name; c.passSound = passSound; c.goSound = goSound; c.Create(seats, along, reach, seed, scale); All.Add(c); return c;
        }
        void OnDestroy() { All.Remove(this); }

        void Create(List<Vector3> seats, Vector3 along, float reachM, int seed, float scale)
        {
            if (!tried)
            {
                tried = true; baseMat = Resources.Load<Material>("Materials/CrowdInstanced");
                var ta = Resources.Load<TextAsset>("Crowd/fan_atlas"); info = ta ? JsonUtility.FromJson<AtlasInfo>(ta.text) : null;
                small = new[] { L("gg_crowd_cheer_small_1"), L("gg_crowd_cheer_small_2"), L("gg_crowd_cheer_small_3") }; big = new[] { L("gg_crowd_cheer_big_1"), L("gg_crowd_cheer_big_2"), L("gg_crowd_roar_1") };
                bedClip = L("gg_crowd_bed_engaged");
            }
            var atlas = Resources.Load<Texture2D>("Crowd/FanAtlas");
            if (Off || !baseMat || !baseMat.shader.isSupported || !SystemInfo.supportsInstancing || !atlas || info == null || info.neck == null)
            {
                Debug.Log($"[Crowd] {StandName}: not drawn ({(Off ? "-noCrowd" : $"material {(baseMat ? "ok" : "missing")}, shader {(baseMat && baseMat.shader.isSupported ? "ok" : "unsupported")}, instancing {SystemInfo.supportsInstancing}, atlas {(atlas ? "ok" : "missing")}")})");
                enabled = false; return;
            }
            mat = new Material(baseMat) { name = "Crowd " + StandName, enableInstancing = true };
            mat.SetTexture(IdAtlas, atlas);
            var neck = new float[84]; for (int i = 0; i < neck.Length && i < info.neck.Length; i++) neck[i] = info.neck[i];
            mat.SetFloatArray(IdNeck, neck);
            var rects = new Vector4[14];
            for (int i = 0; i < rects.Length; i++) rects[i] = info.rect != null && info.rect.Length >= (i + 1) * 3 ? new Vector4(info.rect[i * 3], info.rect[i * 3 + 1], info.rect[i * 3 + 2], 0f) : new Vector4(0f, 1f, 1f, 0f);
            mat.SetVectorArray("_CrowdRect", rects);
            mat.SetVector(IdGrid, new Vector4(info.frames, info.poses, info.boxW, info.boxH));
            mesh = QuadMesh();
            axis = new Vector3(along.x, 0, along.z).normalized; reach = reachM;
            var rng = new System.Random(seed); Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity; float amin = float.MaxValue, amax = float.MinValue; var sum = Vector3.zero;
            for (int start = 0; start < seats.Count; start += 1023)
            {
                int n = Mathf.Min(1023, seats.Count - start);
                var mats = new Matrix4x4[n]; var shirts = new Vector4[n]; var prms = new Vector4[n]; var prms2 = new Vector4[n];
                for (int i = 0; i < n; i++)
                {
                    var s = seats[start + i]; sum += s;
                    float sc = scale * (.9f + (float)rng.NextDouble() * .18f);
                    var pos = s + new Vector3(((float)rng.NextDouble() - .5f) * .12f, 0f, ((float)rng.NextDouble() - .5f) * .1f);
                    mats[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * sc);
                    Color col = Tropical[rng.Next(Tropical.Length)] * (.85f + (float)rng.NextDouble() * .25f);
                    shirts[i] = col;
                    prms[i] = new Vector4((float)rng.NextDouble(), 1f, 1f, (float)rng.NextDouble());
                    prms2[i] = new Vector4((float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble(), (float)rng.NextDouble());
                    float a = Vector3.Dot(s, axis); if (a < amin) { amin = a; a0 = s; } if (a > amax) { amax = a; a1 = s; }
                }
                var block = new MaterialPropertyBlock(); block.SetVectorArray(IdShirt, shirts); block.SetVectorArray(IdParams, prms); block.SetVectorArray(IdParams2, prms2);
                batches.Add(mats); counts.Add(n); blocks.Add(block); Fans += n;
            }
            Center = seats.Count > 0 ? sum / seats.Count : transform.position;
            transform.position = Center;
            if (bedClip)
            {
                bed = gameObject.AddComponent<AudioSource>(); bed.clip = bedClip; bed.loop = true; bed.spatialBlend = 1; bed.rolloffMode = AudioRolloffMode.Linear; bed.minDistance = 20; bed.maxDistance = 170; bed.dopplerLevel = 0;
                bed.volume = .5f * Settings.Effects; bed.timeSamples = rng.Next(0, Mathf.Max(1, bedClip.samples - 1)); bed.Play();
                cheer = gameObject.AddComponent<AudioSource>(); cheer.playOnAwake = false; cheer.spatialBlend = 1; cheer.rolloffMode = AudioRolloffMode.Linear; cheer.minDistance = 25; cheer.maxDistance = 200; cheer.dopplerLevel = 0;
            }
            Debug.Log($"[Crowd] {StandName}: {Fans} fans in {batches.Count} batch(es), {(a1 - a0).magnitude:0} m long, cheers heard: {(passSound ? "when a person passes" : "")}{(goSound ? " at GO" : "")}{(!passSound && !goSound ? "none (murmur only)" : "")}");
        }
        static AudioClip L(string n) => Resources.Load<AudioClip>("Audio/Sfx/" + n);

        // a cheer for a racer at `at` going `speed` m/s (amount 1 for a person's racer); a bigger one only replaces what is left of the last
        public void Cheer(Vector3 at, float speed, float amount, float hold = 2.6f, bool sound = true, bool roar = false)
        {
            float t = Now - burst.x, left = burst.y <= 0 ? 0 : burst.y * Mathf.Clamp01(1 - (t - burst.z) / 2.5f);
            if (amount < left - .05f) return;
            burst = new Vector4(Now, amount, hold, 0); sweep = new Vector4(axis.x, axis.z, Vector3.Dot(at, axis), Mathf.Max(4f, speed));
            Cheers++; LastCheer = Time.time;
            if (sound && cheer && Time.time - GameSounds.LastFinishSound > 3f && !SoundLog.Busy("crowd", 1.5f))   // one crowd sound at a time (both start stands at GO: one roar)
            {
                var set = roar ? big : small; var c = set[Random.Range(0, set.Length)];
                if (c) { cheer.PlayOneShot(c, (roar ? .9f : .75f) * Settings.Effects); SoundLog.Add("crowd", $"{StandName} {c.name}"); }
            }
        }
        // the closest point of the stand's long axis to p
        Vector3 Nearest(Vector3 p) { var d = a1 - a0; float k = d.sqrMagnitude < 1e-4f ? 0 : Mathf.Clamp01(Vector3.Dot(p - a0, d) / d.sqrMagnitude); return a0 + d * k; }
        public float DistanceTo(Vector3 p) { var q = Nearest(p); return Vector3.Distance(q, p); }   // 3D: the Coral Vault runs under the waterfall-landing stand

        void Update()
        {
            if (!mat) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var g = Game.Instance;
            // racers within reach set the stand off (each once per pass: again only after it has been away 20 s)
            if (g && g.Race)
                foreach (var r in g.Race.Racers)
                {
                    if (!r) continue;
                    bool near = DistanceTo(r.Body ? r.Body.position : r.transform.position) < reach;
                    if (!near) continue;
                    if (lastFor.TryGetValue(r, out var last) && Time.time - last < 20f) { lastFor[r] = Time.time; continue; }
                    lastFor[r] = Time.time;
                    if (r.Holding) continue;   // the grid: the GO cheer (below) does it
                    Cheer(r.transform.position, r.Speed, r.Human ? 1f : .65f, 2.6f, r.Human && passSound);
                }
            if (bed) bed.volume = .5f * Settings.Effects;
            sw.Stop(); CpuMs += sw.Elapsed.TotalMilliseconds;
        }
        // the start: every stand within reach of the grid cheers at GO; the finish: a person's racer finishing in front of a stand (a roar for 1st)
        public static void OnRace(string kind, ShiftVehicle v, int place)
        {
            foreach (var c in All)
            {
                if (!c.mat || !v) continue;
                float d = c.DistanceTo(v.transform.position);
                if (kind == "go" && d < c.reach + 30f) c.Cheer(v.transform.position, 12f, 1f, 4f, c.goSound && v == Game.Instance.Player, true);
                else if (kind == "finish" && d < c.reach + 40f) { c.Cheer(v.transform.position, Mathf.Max(8f, v.Speed), v.Human ? 1f : .7f, place == 1 ? 6f : 3f, false); c.lastFor[v] = Time.time; }
            }
        }

        void LateUpdate()
        {
            if (!mat) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Shader.SetGlobalFloat(IdEnergy, .45f);
            Shader.SetGlobalFloat(IdTime, Now);
            var sun = RenderSettings.sun; Shader.SetGlobalColor(IdLight, sun ? sun.color * sun.intensity : JurassicLook.SunColor * JurassicLook.SunIntensity);
            Shader.SetGlobalColor(IdAmb, RenderSettings.ambientLight);
            mat.SetVector(IdBurst, burst); mat.SetVector(IdSweep, sweep);
            sw.Stop(); CpuMs += sw.Elapsed.TotalMilliseconds;
            if (this == All[0]) { CpuTotal += CpuMs; CpuMs = 0; CpuFrames++; if (CpuFrames % 1200 == 0) Debug.Log($"[Crowd] {All.Count} stands, {Total} fans, CPU {CpuTotal / CpuFrames:0.000} ms a frame on average"); }
        }
        // drawn per camera as it renders (split screen's two, and a check's own renders too: Gridiron Glory queued them once a frame in
        // LateUpdate, which a camera rendered by hand earlier in the frame misses), only by a camera within DrawDistance
        void OnEnable() { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnCamera; }
        void OnDisable() { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnCamera; }
        void OnCamera(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera cam)
        {
            if (!mat || !cam || (cam.transform.position - Center).sqrMagnitude > DrawDistance * DrawDistance) return;
            if ((cam.cullingMask & (1 << gameObject.layer)) == 0) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int drawn = 0;
            for (int i = 0; i < batches.Count; i++) { int n = Mathf.Clamp(Mathf.CeilToInt(counts[i] * Density), 1, counts[i]); drawn += n; Graphics.DrawMeshInstanced(mesh, 0, mat, batches[i], n, blocks[i], UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer, cam); }
            DrawnLastFrame = drawn; sw.Stop(); CpuMs += sw.Elapsed.TotalMilliseconds;
        }
        public int DrawnLastFrame { get; private set; }
        public static int Total { get { int n = 0; foreach (var c in All) n += c.Fans; return n; } }

        // one fan: a quad from x -0.5..0.5, y 0..1 (the shader sizes it, turns it to the camera and picks the atlas cell)
        static Mesh QuadMesh()
        {
            var m = new Mesh { name = "Fan" };
            m.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 1f, 0f), new Vector3(-0.5f, 1f, 0f) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            m.bounds = new Bounds(new Vector3(0, 1.6f, 0), new Vector3(3.5f, 3.6f, 3.5f));   // generous: the shader turns and sizes the quad (box up to 2.2 x 3.3 m at x1.35)
            return m;
        }
    }
}
