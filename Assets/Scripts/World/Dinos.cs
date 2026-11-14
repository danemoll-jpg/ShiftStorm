using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 1: Jurassic Par's dinosaurs, alive. Tee'd Off's rigged dinosaurs (Art/Characters/Dinos/<style>/<style>_rig.bytes: its
    // Meshy mesh with the skeleton and weights from its Tools/Characters/rig_dino.py; copied, read-only, into Resources/Dinos) are built
    // here at start-up into a skinned mesh (the same steps as Tee'd Off's editor DinoImport, done at run time because ShiftStorm builds its
    // world in code), with their texture and the game's toon material. DinoPose is Tee'd Off's ModelCreature animation, copied: a walk cycle
    // on the legs by speed (diagonal pairs on four legs, alternating on two), tail sway, a slow look round, breathing, the T. rex's roar and
    // chomp, the brontosaurus lowering its neck, the pterodactyl's wing beats. DinoActor gives each one a behaviour along the lap.
    // Stand-ins (no model anywhere yet; Tripo prompts in Docs/MODELS-NEEDED.md): the raptor is the T. rex at 2.6 m, the plesiosaur the
    // brontosaurus swimming with its body under water.
    public static class DinoRig
    {
        sealed class Template { public int tris; public Mesh mesh; public string[] names; public int[] parents; public Vector3[] heads; public Material mat; public Bounds bounds; }
        static readonly Dictionary<string, Template> cache = new Dictionary<string, Template>();
        public static readonly string[] Styles = { "bronto", "trex", "trike", "ptero" };

        static Template Load(string style)
        {
            if (cache.TryGetValue(style, out var t)) return t;
            var ta = Resources.Load<TextAsset>($"Dinos/{style}_rig");
            if (!ta) { cache[style] = null; Debug.LogWarning($"[Dinos] no Resources/Dinos/{style}_rig: stand-in shapes"); return null; }
            using var br = new BinaryReader(new MemoryStream(ta.bytes));
            if (new string(br.ReadChars(4)) != "BDOG") { cache[style] = null; return null; }
            br.ReadInt32();
            int nv = br.ReadInt32(), nt = br.ReadInt32(), nb = br.ReadInt32();
            t = new Template { names = new string[nb], parents = new int[nb], heads = new Vector3[nb] };
            for (int i = 0; i < nb; i++)
            {
                t.names[i] = System.Text.Encoding.ASCII.GetString(br.ReadBytes(32)).TrimEnd('\0');
                t.parents[i] = br.ReadInt32(); t.heads[i] = V3(br); V3(br);
            }
            var pos = new Vector3[nv]; var nrm = new Vector3[nv]; var uv = new Vector2[nv];
            for (int i = 0; i < nv; i++) pos[i] = V3(br);
            for (int i = 0; i < nv; i++) nrm[i] = V3(br);
            for (int i = 0; i < nv; i++) uv[i] = new Vector2(br.ReadSingle(), 1f - br.ReadSingle());
            var idx = new int[nv * 4]; for (int i = 0; i < idx.Length; i++) idx[i] = br.ReadInt32();
            var wt = new float[nv * 4]; for (int i = 0; i < wt.Length; i++) wt[i] = br.ReadSingle();
            var tris = new int[nt * 3]; for (int i = 0; i < tris.Length; i++) tris[i] = br.ReadInt32();
            var mesh = new Mesh { name = style + " (Tee'd Off rig)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = pos; mesh.normals = nrm; mesh.uv = uv; mesh.triangles = tris;
            var bw = new BoneWeight[nv];
            for (int i = 0; i < nv; i++)
                bw[i] = new BoneWeight { boneIndex0 = idx[i * 4], weight0 = wt[i * 4], boneIndex1 = idx[i * 4 + 1], weight1 = wt[i * 4 + 1], boneIndex2 = idx[i * 4 + 2], weight2 = wt[i * 4 + 2], boneIndex3 = idx[i * 4 + 3], weight3 = wt[i * 4 + 3] };
            mesh.boneWeights = bw;
            var bind = new Matrix4x4[nb]; for (int i = 0; i < nb; i++) bind[i] = Matrix4x4.Translate(-t.heads[i]);   // the bones have no rest rotation
            mesh.bindposes = bind; mesh.RecalculateBounds(); mesh.UploadMeshData(true);
            t.mesh = mesh; t.bounds = mesh.bounds; t.tris = nt;
            var tex = Resources.Load<Texture2D>($"Dinos/{style}_basecolor");
            t.mat = tex ? JurassicLook.Textured(tex, true) : JurassicLook.Mat(JurassicLook.Moss, true); t.mat.name = "Dino " + style;
            if (t.mat.HasProperty("_OutlineScale")) t.mat.SetFloat("_OutlineScale", .5f);
            cache[style] = t;
            Debug.Log($"[Dinos] {style}: {nv} vertices, {nt} triangles, {nb} bones (Tee'd Off rig)");
            return t;
        }
        static Vector3 V3(BinaryReader br) => new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        public static int Triangles(string style) { var t = Load(style); return t != null ? t.tris : 0; }

        // a new skinned dinosaur under parent, scaled so its body is `height` tall (pterodactyl: `height` = wingspan); null without the rig
        public static GameObject Spawn(string style, Transform parent, float height, out SkinnedMeshRenderer smr)
        {
            smr = null; var t = Load(style); if (t == null) return null;
            var root = new GameObject("Dino " + style); root.transform.SetParent(parent, false);
            var model = new GameObject("model").transform; model.SetParent(root.transform, false);
            var bones = new Transform[t.names.Length];
            for (int i = 0; i < bones.Length; i++) bones[i] = new GameObject(t.names[i]).transform;
            for (int i = 0; i < bones.Length; i++) { bones[i].SetParent(t.parents[i] >= 0 ? bones[t.parents[i]] : model, false); }
            for (int i = 0; i < bones.Length; i++) bones[i].localPosition = t.heads[i] - (t.parents[i] >= 0 ? t.heads[t.parents[i]] : Vector3.zero);
            var body = new GameObject("Body"); body.transform.SetParent(model, false);
            smr = body.AddComponent<SkinnedMeshRenderer>(); smr.sharedMesh = t.mesh; smr.bones = bones; smr.rootBone = bones[0]; smr.sharedMaterial = t.mat;
            smr.quality = SkinQuality.Bone2; smr.updateWhenOffscreen = false; smr.localBounds = new Bounds(t.bounds.center - t.heads[0], t.bounds.size * 1.8f);
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // on only near the camera (DinoActor)
            var b = t.bounds; float size = style == "ptero" ? b.size.x : b.size.y;
            float k = height / Mathf.Max(.01f, size); model.localScale = Vector3.one * k;
            model.localPosition = new Vector3(-b.center.x * k, -b.min.y * k, -b.center.z * k);
            return root;
        }
    }

    // Tee'd Off's ModelCreature animation (Scripts/Characters/ModelCreature.cs), copied (Docs/REUSE-AUDIT.md): bones found by name, the
    // walk / tail / neck / head / jaw / wings, the brontosaurus's neck down, the T. rex's roar
    public sealed class DinoPose
    {
        Transform head, jaw, spine, chest, hips;
        readonly List<Transform> neck = new List<Transform>(), tail = new List<Transform>();
        readonly List<(Transform upper, Transform lower, bool front, bool left)> legs = new List<(Transform, Transform, bool, bool)>();
        readonly List<(Transform t, bool left, int link)> wings = new List<(Transform, bool, int)>();
        Vector3 hipsRest; readonly string style; float gait; readonly float seed;
        public Transform Head => head;
        public DinoPose(Transform root, string style, float seed)
        {
            this.style = style; this.seed = seed;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == "Head") head = t; else if (n == "Jaw") jaw = t; else if (n == "Spine") spine = t; else if (n == "Chest") chest = t; else if (n == "Hips") hips = t;
                else if (n.StartsWith("Neck")) neck.Add(t); else if (n.StartsWith("Tail")) tail.Add(t);
                else if (n.StartsWith("Wing")) wings.Add((t, n[4] == 'L', n[n.Length - 1] - '0'));
            }
            neck.Sort((a, b) => string.CompareOrdinal(a.name, b.name)); tail.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.Contains("Upper")) continue;
                Transform lower = null; foreach (Transform c in t) if (c.name.Contains("Lower")) lower = c;
                legs.Add((t, lower, t.name.StartsWith("Front"), t.name.EndsWith("L")));
            }
            if (hips) hipsRest = hips.localPosition;
        }
        // speed in body lengths per second-ish (Tee'd Off's units: 0.6 = a full walk), chomp / roar / neckDown 0..1, flap: the wing beat's strength
        public void Tick(float clock, float speed, float dt, float chomp, float roar, float neckDown, float flap = 1f)
        {
            float t = clock + seed;
            if (style == "ptero")
            {
                float beat = (Mathf.Repeat(t, 5f) < 1.6f ? Mathf.Sin(t * 6f) : Mathf.Sin(t * .8f) * .15f) * flap;
                foreach (var (w, left, link) in wings) w.localRotation = Quaternion.Euler(0f, 0f, (left ? -1f : 1f) * (link == 0 ? 28f : 18f) * beat);
                if (head) head.localRotation = Quaternion.Euler(Mathf.Sin(t * .7f) * 6f, Mathf.Sin(t * .4f) * 15f, 0f);
                return;
            }
            float stride = style == "trex" ? 3.2f : style == "bronto" ? 3.0f : 2.2f;
            gait += dt * speed / stride * Mathf.PI * 2f;
            float walk = Mathf.Clamp01(speed / .6f);
            foreach (var (upper, lower, front, left) in legs)
            {
                float ph = gait + (left ? 0f : Mathf.PI) + (front ? Mathf.PI : 0f);
                upper.localRotation = Quaternion.Euler(Mathf.Sin(ph) * 22f * walk, 0f, 0f);
                if (lower) lower.localRotation = Quaternion.Euler(-Mathf.Max(0f, Mathf.Cos(ph)) * 24f * walk, 0f, 0f);
            }
            if (hips) hips.localPosition = hipsRest + Vector3.up * (Mathf.Abs(Mathf.Sin(gait)) * .012f * walk);
            float breathe = Mathf.Sin(t * 1.3f);
            if (spine) spine.localRotation = Quaternion.Euler(breathe * 1.2f, Mathf.Sin(gait) * 2f * walk, 0f);
            if (chest) chest.localRotation = Quaternion.Euler(-breathe * 1.0f, 0f, 0f);
            for (int i = 0; i < tail.Count; i++) tail[i].localRotation = Quaternion.Euler(Mathf.Sin(t * .9f - i) * 2f, Mathf.Sin(t * 1.3f - i * .6f) * (5f + 6f * walk), 0f);
            float look = Mathf.Sin(t * .35f) * 12f * (1f - neckDown) * (1f - roar);
            int n = Mathf.Max(1, neck.Count);
            float down = style == "bronto" ? Mathf.Lerp(Mathf.Sin(t * .5f) * 3f, 62f, neckDown) : Mathf.Sin(t * .7f) * 2f;
            for (int i = 0; i < neck.Count; i++) neck[i].localRotation = Quaternion.Euler(down / n - roar * 12f / n + chomp * 10f / n, look / n, 0f);
            if (head) head.localRotation = Quaternion.Euler(-roar * 18f + chomp * 14f + (style == "bronto" ? -neckDown * 20f : 0f), look * .4f, 0f);
            if (jaw) jaw.localRotation = Quaternion.Euler(Mathf.Max(chomp, roar * 1.2f) * 28f, 0f, 0f);
        }
    }

    // one dinosaur along the lap: its behaviour, its sound, and its cost (animated only while seen and near; not drawn beyond its range)
    public sealed class DinoActor : MonoBehaviour
    {
        public enum Kind { Idle, Graze, Wade, Swim, Run, Roar, Swoop }
        public static readonly List<DinoActor> All = new List<DinoActor>();
        public static bool NoDinos => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-noDinos") >= 0;   // checks: the frame cost without them
        public string style, segment = "", label = ""; public Kind kind; public bool standIn;
        public Vector3 a, b;                // Graze / Wade / Swim: walks between a and b; Run: runs from a to b; Swoop: circle centre a, radius b.x, height b.y
        public float speed = 1.2f, height = 10f, range = 650f, roarRange = 230f;
        public const float ShadowRange = 140f;
        public ShiftVehicle runWith;        // Run: paces this racer while it is in the stretch
        public float runFrom, runTo;        // Run: the stretch (progress)
        SkinnedMeshRenderer smr; DinoPose pose; float u, dir = 1, clock, roar, roarNext, chomp, neckDown, phase; int frame; AudioSource voice;
        public int Roars { get; private set; }
        public bool Seen => smr && smr.isVisible;
        public Renderer Rend => smr;
        static readonly Dictionary<string, AudioClip[]> calls = new Dictionary<string, AudioClip[]>();
        static AudioClip[] Calls(string style)
        {
            if (calls.TryGetValue(style, out var c)) return c;
            string[] names = style == "trex" ? new[] { "trex_roar_1", "trex_roar_2" } : style == "bronto" ? new[] { "bronto_moo_1", "bronto_moo_2" } : style == "ptero" ? new[] { "amb_dino_call_2", "amb_dino_call_3" } : new[] { "dino_grunt_1", "dino_grunt_2" };
            var l = new List<AudioClip>(); foreach (var n in names) { var x = Resources.Load<AudioClip>("Audio/Sfx/" + n); if (x) l.Add(x); }
            return calls[style] = l.ToArray();
        }

        public static DinoActor Make(Transform parent, string style, Kind kind, Vector3 a, Vector3 b, float height, string segment, string label, float yaw = 0)
        {
            string model = style == "raptor" ? "trex" : style == "plesio" ? "bronto" : style;
            SkinnedMeshRenderer smr = null; GameObject g = null;
            if (!NoDinos) g = DinoRig.Spawn(model, parent, height, out smr);
            else { g = new GameObject("Dino off"); g.transform.SetParent(parent, false); }
            bool standIn = g == null || style == "raptor" || style == "plesio";
            if (g == null && !NoDinos) { g = new GameObject("Dino " + style + " (shape)"); g.transform.SetParent(parent, false); var s = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(s.GetComponent<Collider>()); s.transform.SetParent(g.transform, false); s.transform.localScale = new Vector3(height * .5f, height * .6f, height); s.transform.localPosition = Vector3.up * height * .4f; s.GetComponent<Renderer>().sharedMaterial = JurassicLook.Mat(JurassicLook.Moss, true); }
            g.name = $"Dino {style} ({label})";
            var d = g.AddComponent<DinoActor>(); d.style = style; d.kind = kind; d.a = a; d.b = b; d.height = height; d.segment = segment; d.label = label; d.smr = smr; d.standIn = standIn;
            d.phase = Mathf.Repeat(a.x * .13f + a.z * .07f, 6.28f); d.u = Mathf.Repeat(d.phase, 1f);
            if (smr) d.pose = new DinoPose(g.transform, model, d.phase);
            g.transform.SetPositionAndRotation(a, Quaternion.Euler(0, yaw, 0));
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ParadisePlunge.DinoLayer;
            if (kind == Kind.Roar || (kind != Kind.Swoop && Random.value < .5f) || kind == Kind.Swoop)
            {
                d.voice = g.AddComponent<AudioSource>(); d.voice.spatialBlend = 1; d.voice.rolloffMode = AudioRolloffMode.Linear; d.voice.playOnAwake = false;
                d.voice.minDistance = kind == Kind.Roar ? 60 : 25; d.voice.maxDistance = kind == Kind.Roar ? 700 : 260; d.voice.dopplerLevel = 0;
            }
            d.roarNext = 4f + d.phase * 3f;
            All.Add(d); return d;
        }
        void OnDestroy() => All.Remove(this);

        // the nearest camera (both halves in split screen)
        static float CamDistance(Vector3 p)
        {
            float best = 1e9f; var g = Game.Instance;
            if (g && g.Cam) best = Vector3.Distance(g.Cam.transform.position, p);
            if (SplitScreen.Session && SplitScreen.Session.Cam2) best = Mathf.Min(best, Vector3.Distance(SplitScreen.Session.Cam2.transform.position, p));
            return best;
        }
        ShiftVehicle NearestHuman(out float dist)
        {
            dist = 1e9f; ShiftVehicle best = null; var g = Game.Instance; if (!g) return null;
            foreach (var h in g.Humans) if (h && h.Body) { float x = Vector3.Distance(h.Body.position, transform.position); if (x < dist) { dist = x; best = h; } }
            return best;
        }

        void Update()
        {
            float dt = Time.deltaTime; clock += dt; frame++;
            float cd = CamDistance(transform.position);
            bool draw = cd < range; if (smr && smr.enabled != draw) smr.enabled = draw;
            if (smr) { var sh = cd < ShadowRange ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off; if (smr.shadowCastingMode != sh) smr.shadowCastingMode = sh; }
            float moveSpeed = 0;
            switch (kind)
            {
                case Kind.Graze: case Kind.Wade: case Kind.Swim:
                {
                    float len = Mathf.Max(1f, Vector3.Distance(a, b));
                    // walk a little, stop to graze (the brontosaurus lowers its neck), walk on; turn round at the ends
                    float cycle = Mathf.Repeat(clock * .07f + phase, 1f); bool walking = kind == Kind.Swim || cycle < .55f;
                    neckDown = Mathf.MoveTowards(neckDown, walking || kind == Kind.Swim ? 0 : 1, dt * .5f);
                    moveSpeed = walking ? speed : 0;
                    u += dir * moveSpeed * dt / len; if (u > 1) { u = 1; dir = -1; } else if (u < 0) { u = 0; dir = 1; }
                    var p = Vector3.Lerp(a, b, u); var fwd = (b - a).normalized * dir; fwd.y = 0;
                    transform.position = p;
                    if (fwd.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd), dt * 1.2f);
                    break;
                }
                case Kind.Run:
                {
                    // raptors: run alongside the road while a racer is in their stretch (a little slower than it), then wait at the far end
                    var h = runWith ? runWith : NearestHuman(out _);
                    if (h && h.Course && h.Progress > runFrom - 60 && h.Progress < runTo && !h.Holding)
                    {
                        float want = Mathf.Clamp(h.Speed * .8f, 6f, 26f); speed = Mathf.MoveTowards(speed, want, dt * 12f);
                        u = Mathf.Clamp01(u + speed * dt / Mathf.Max(1f, Vector3.Distance(a, b)));
                    }
                    else if (h && (h.Progress < runFrom - 200 || h.Progress > runTo + 600)) { u = 0; speed = 0; }   // back at the start for the next lap
                    else speed = Mathf.MoveTowards(speed, 0, dt * 10f);
                    moveSpeed = speed;
                    transform.position = Vector3.Lerp(a, b, u); var f = (b - a); f.y = 0; transform.rotation = Quaternion.LookRotation(f);
                    // a little bob and weave (they are running, not sliding)
                    transform.position += Vector3.up * Mathf.Abs(Mathf.Sin(clock * 9f + phase)) * .15f * Mathf.Clamp01(speed / 8f) + transform.right * Mathf.Sin(clock * 1.7f + phase) * .6f;
                    break;
                }
                case Kind.Roar:
                {
                    // the T. rex on the cliff: it roars (head up, jaw wide, the camera shakes) as a racer comes by
                    var h = NearestHuman(out float hd);
                    if (h && hd < roarRange && clock > roarNext) { roar = 1; roarNext = clock + 9f; Roars++; Call(1f); if (hd < 320f) ChaseCam.Shake(.35f * Mathf.Clamp01(1.3f - hd / 320f), 1.4f); h.Note("dino", $"T. rex roars ({hd:0} m away)"); }
                    roar = Mathf.MoveTowards(roar, 0, dt / 2.2f);
                    chomp = Mathf.Max(0, Mathf.Sin(clock * 1.4f + phase)) * .3f * (1 - roar);
                    if (h) { var to = h.Body.position - transform.position; to.y = 0; if (to.sqrMagnitude > 1) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * .6f); }
                    break;
                }
                case Kind.Swoop:
                {
                    // pterodactyls: wide circles with a dip, banking into the turn
                    float r = b.x, ang = phase + clock * speed / Mathf.Max(5f, r);
                    var p = a + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang * 2f) * b.z, Mathf.Sin(ang) * r);
                    var tan = new Vector3(-Mathf.Sin(ang), Mathf.Cos(ang * 2f) * 2f * b.z / r, Mathf.Cos(ang));
                    transform.SetPositionAndRotation(p, Quaternion.LookRotation(tan.normalized) * Quaternion.Euler(0, 0, 22f));
                    break;
                }
            }
            // a call now and then when someone is near (a moo, a grunt, a screech)
            if (voice && kind != Kind.Roar && clock > roarNext && cd < voice.maxDistance) { Call(.8f); roarNext = clock + Random.Range(10f, 22f); }
            // the pose: every frame near and seen, every 3rd far, none when not seen
            if (pose == null || !draw || !smr.isVisible) return;
            if (cd > 260f && frame % 3 != 0) return;
            float step = cd > 260f ? dt * 3 : dt;
            float bodySpeed = moveSpeed / Mathf.Max(1f, height * .6f);
            pose.Tick(clock, kind == Kind.Run ? Mathf.Min(2.5f, bodySpeed * 1.4f) : Mathf.Min(1f, bodySpeed * 2.2f), step, chomp, roar, neckDown, kind == Kind.Swoop ? 1 : .6f);
        }
        void Call(float vol)
        {
            if (!voice) return; var cs = Calls(style == "raptor" ? "trike" : style == "plesio" ? "bronto" : style); if (cs.Length == 0) return;
            voice.pitch = style == "raptor" ? 1.5f : style == "plesio" ? .8f : 1f;
            voice.PlayOneShot(cs[Random.Range(0, cs.Length)], vol * Settings.Effects);
        }
    }
}
