using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ShiftStorm
{
    // Abi (Abigail Wills, Tee'd Off's "The Competitor"; model and voice copied from Tee'd Off, Art/Characters/Abigail and
    // Resources/Audio/Voice/abigail) in the cockpit, sized to sit under the closed canopy. Her body language comes from the shared driver
    // animations (Mixamo, Resources/Drivers/Animations, Docs/MODELS-NEEDED.md) blended in a small playable graph: Driving while racing, Sitting
    // Idle on the grid and after the finish, Sitting Thumbs Up at a shape change, Sitting Disapproval after a reset, Sitting Clap at the
    // finish, Honking Horn at GO; she leans into the steering and looks where she turns on top. Without the clips she holds a fixed seated
    // pose made here by aiming her Humanoid bones (thighs forward, knees bent, arms to the wheel) and throws her arms up at the finish. Her voice: a line at the
    // finish, after a reset and on the leap through the waterfall, each with a cooldown so she never chatters.
    public sealed class Driver : MonoBehaviour
    {
        public static Driver Instance { get; private set; }
        public string Source { get; private set; } = "none";
        public float Scale { get; private set; }
        public float CanopyTop { get; private set; }
        public float HeadTop { get; private set; }
        ShiftVehicle v; FormAnimator forms; Animator anim; Transform model;
        Transform hips, spine, chest, head, uaL, uaR, laL, laR, handL, handR, ulL, ulR, llL, llR, footL, footR;
        readonly Dictionary<Transform, Quaternion> seated = new Dictionary<Transform, Quaternion>();
        float lean, cheer; SkinnedMeshRenderer[] skins; Vector3 seat;
        // the shared clips
        static readonly string[] ClipNames = { "Driving", "Sitting Idle", "Sitting Thumbs Up", "Sitting Disapproval", "Sitting Clap", "Honking Horn" };
        const int Drive = 0, Idle = 1, Thumbs = 2, Grumble = 3, Clap = 4, Horn = 5;
        PlayableGraph graph; AnimationMixerPlayable mixer; readonly AnimationClipPlayable[] plays = new AnimationClipPlayable[ClipNames.Length];
        readonly AnimationClip[] clips = new AnimationClip[ClipNames.Length]; readonly float[] weight = new float[ClipNames.Length];
        int oneShot = -1; float oneShotEnd; public bool Animated { get; private set; } public int ClipCount { get; private set; }
        public readonly List<string> Played = new List<string>();

        // voice
        AudioSource voice; readonly Dictionary<string, AudioClip[]> lines = new Dictionary<string, AudioClip[]>(); readonly Dictionary<string, float> nextOf = new Dictionary<string, float>();
        float nextAny; public readonly List<string> Spoken = new List<string>();
        static readonly Dictionary<string, string[]> LineNames = new Dictionary<string, string[]>
        {
            { "finish", new[] { "win_1", "quipgood_1", "quipgood_2" } },
            { "reset", new[] { "disaster_1", "disaster_2", "grunt_1", "trouble_1" } },
            { "waterfall", new[] { "laugh_1", "lucky_1" } },
        };
        const float LineCooldown = 8f, AnyCooldown = 3f;

        // Round 3: the rivals' drivers too (Tee'd Off's Chip, Rae and Vivi, copied to Resources/Drivers/<name>), with Abi's shared seated
        // clips and no voice (only the player's driver speaks).
        string modelPath = "Driver/Abigail", texPath = "Driver/Abigail_basecolor", who = "Abigail";
        public static Driver Make(ShiftVehicle v, string character = null)
        {
            var d = v.gameObject.AddComponent<Driver>(); d.v = v; d.forms = v.Forms;
            if (character != null) { d.who = character; d.modelPath = $"Drivers/{character}/{character}"; d.texPath = $"Drivers/{character}/{character}_basecolor"; }
            else Instance = d;
            d.Seat(); if (character == null) d.Voice();
            return d;
        }

        // ---------------- the seat ----------------
        void Seat()
        {
            var prefab = Resources.Load<GameObject>(modelPath);
            if (!prefab || !forms) { Debug.LogWarning($"[Driver] no {who} model (Resources/{modelPath}): the cockpit stays empty"); return; }
            // the closed canopy's top, in the vehicle's own space (measured, not assumed: the Blender model may change)
            forms.Snap(Mode.Dive);
            var canopy = forms.PartTransform("Canopy"); float top = .87f;
            if (canopy) { var r = canopy.GetComponentInChildren<Renderer>(); if (r) top = transform.InverseTransformPoint(r.bounds.max).y; }
            CanopyTop = top;
            model = Instantiate(prefab, forms.transform, false).transform; model.name = who + " (driver)";
            model.localPosition = Vector3.zero; model.localRotation = Quaternion.identity; model.localScale = Vector3.one;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
            anim = model.GetComponentInChildren<Animator>();
            if (!anim || !anim.isHuman) { Debug.LogWarning($"[Driver] {who} has no Humanoid avatar: no seated pose"); Destroy(model.gameObject); model = null; forms.Snap(Mode.Drive); return; }
            T(ref hips, HumanBodyBones.Hips); T(ref spine, HumanBodyBones.Spine); T(ref chest, HumanBodyBones.Chest); T(ref head, HumanBodyBones.Head);
            T(ref uaL, HumanBodyBones.LeftUpperArm); T(ref uaR, HumanBodyBones.RightUpperArm); T(ref laL, HumanBodyBones.LeftLowerArm); T(ref laR, HumanBodyBones.RightLowerArm);
            T(ref handL, HumanBodyBones.LeftHand); T(ref handR, HumanBodyBones.RightHand);
            T(ref ulL, HumanBodyBones.LeftUpperLeg); T(ref ulR, HumanBodyBones.RightUpperLeg); T(ref llL, HumanBodyBones.LeftLowerLeg); T(ref llR, HumanBodyBones.RightLowerLeg);
            T(ref footL, HumanBodyBones.LeftFoot); T(ref footR, HumanBodyBones.RightFoot);
            skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var sk in skins) { sk.updateWhenOffscreen = true; sk.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            Paint();
            for (int i = 0; i < ClipNames.Length; i++) { clips[i] = Resources.Load<AnimationClip>("Drivers/Animations/" + ClipNames[i]); if (clips[i]) ClipCount++; }
            if (clips[Drive] && clips[Idle]) Graph(); else { anim.enabled = false; Pose(); }
            // size: the seated head top sits under the closed canopy with a margin; hips on the seat
            seat = new Vector3(0, .17f, -.05f);
            Place(seat, 1f);
            float headTop = MaxY(), hipsY = Local(hips.position).y;
            float s = Mathf.Clamp((top - .07f - seat.y) / Mathf.Max(.1f, headTop - hipsY), .5f, 1.05f);
            Place(seat, s); Scale = s; HeadTop = MaxY();
            if (!Animated) foreach (var b in new[] { hips, spine, chest, head, uaL, uaR, laL, laR, ulL, ulR, llL, llR }) if (b) seated[b] = b.localRotation;
            Source = Animated ? $"{who}.fbx (Tee'd Off), Humanoid, {ClipCount} shared Mixamo clips (Driving loop), scale {s:0.00}" : $"{who}.fbx (Tee'd Off), Humanoid, fixed seated pose (no clips found), scale {s:0.00}";
            forms.Snap(Mode.Drive);
            Debug.Log($"[Driver] {who} seated: scale {s:0.00}, head top {HeadTop:0.00} m, closed canopy top {top:0.00} m");
        }
        void T(ref Transform t, HumanBodyBones b) => t = anim.GetBoneTransform(b);
        // the clip graph, posed on Driving (the seat and the size are measured in that pose)
        void Graph()
        {
            Animated = true; anim.enabled = true; anim.applyRootMotion = false; anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; anim.runtimeAnimatorController = null;
            graph = PlayableGraph.Create(who + " driver"); graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var output = AnimationPlayableOutput.Create(graph, "driver", anim);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i]) continue;
                plays[i] = AnimationClipPlayable.Create(graph, clips[i]); plays[i].SetApplyFootIK(false);
                graph.Connect(plays[i], 0, mixer, i);
            }
            output.SetSourcePlayable(mixer);
            weight[Drive] = 1; Apply();
            plays[Drive].SetTime(.5f); graph.Evaluate(0); Legs();
            graph.Play();
        }
        void Apply() { for (int i = 0; i < clips.Length; i++) if (clips[i]) mixer.SetInputWeight(i, weight[i]); }
        public void PlayOnce(int i)
        {
            if (!Animated || !clips[i]) return;
            oneShot = i; oneShotEnd = Time.time + clips[i].length; plays[i].SetTime(0); plays[i].SetDuration(clips[i].length); Played.Add(ClipNames[i]);
        }
        void UpdateGraph()
        {
            int baseClip = v.Holding || v.Finished ? Idle : Drive;
            if (oneShot >= 0 && Time.time > oneShotEnd - .25f) oneShot = -1;
            float k = 1 - Mathf.Exp(-8f * Time.deltaTime);
            for (int i = 0; i < clips.Length; i++) { float want = oneShot >= 0 ? (i == oneShot ? 1 : 0) : (i == baseClip ? 1 : 0); weight[i] = Mathf.Lerp(weight[i], want, k); }
            float sum = 0; foreach (var w in weight) sum += w; if (sum > 1e-4f) for (int i = 0; i < weight.Length; i++) weight[i] /= sum;
            Apply();
        }
        void OnDestroy() { if (graph.IsValid()) graph.Destroy(); }
        Vector3 Local(Vector3 world) => transform.InverseTransformPoint(world);
        Vector3 W(Vector3 localDir) => transform.TransformDirection(localDir.normalized);
        // point a bone so its child lies along a direction (vehicle space: x right, y up, z forward)
        static void Aim(Transform bone, Transform child, Vector3 worldDir)
        {
            if (!bone || !child) return; var cur = child.position - bone.position; if (cur.sqrMagnitude < 1e-8f) return;
            bone.rotation = Quaternion.FromToRotation(cur, worldDir) * bone.rotation;
        }
        void Pose()
        {
            // sit up, leaning back a little
            Aim(spine, chest ? chest : head, W(new Vector3(0, 1, -.12f)));
            if (chest) Aim(chest, head, W(new Vector3(0, 1, -.05f)));
            foreach (var (u, l, f, s) in new[] { (ulL, llL, footL, -1f), (ulR, llR, footR, 1f) })
            {
                Aim(u, l, W(new Vector3(s * .12f, .08f, 1)));      // thighs forward, a touch apart
                Aim(l, f, W(new Vector3(0, -.75f, .65f)));         // knees bent, feet forward and down
            }
            foreach (var (u, l, h, s) in new[] { (uaL, laL, handL, -1f), (uaR, laR, handR, 1f) })
            {
                Aim(u, l, W(new Vector3(s * .25f, -.8f, .55f)));   // upper arms down and forward
                Aim(l, h, W(new Vector3(-s * .35f, .3f, 1)));        // forearms forward to the wheel
            }
        }
        void Legs()
        {
            foreach (var (u, l, f, sd) in new[] { (ulL, llL, footL, -1f), (ulR, llR, footR, 1f) })
            { Aim(u, l, W(new Vector3(sd * .12f, .08f, 1))); Aim(l, f, W(new Vector3(0, -.75f, .65f))); }
        }
        void Place(Vector3 seat, float s)
        {
            model.localScale = Vector3.one * s;
            var hp = Local(hips.position); model.localPosition += seat - hp;
        }
        // the highest point of her mesh (baked in the pose), in the vehicle's space
        float MaxY()
        {
            float m = float.MinValue; var mesh = new Mesh();
            foreach (var sk in skins) { sk.BakeMesh(mesh, true); var tr = sk.transform; foreach (var p in mesh.vertices) m = Mathf.Max(m, Local(tr.TransformPoint(p)).y); }
            Destroy(mesh); return m;
        }
        void Paint()
        {
            var tex = Resources.Load<Texture2D>(texPath);
            var mat = JurassicLook.Textured(tex, true); mat.SetFloat("_OutlineScale", .5f);
            foreach (var r in model.GetComponentsInChildren<Renderer>(true)) { var ms = r.sharedMaterials; for (int i = 0; i < ms.Length; i++) ms[i] = mat; r.sharedMaterials = ms; }
        }

        // Check helper: of her mesh's vertices above the body's top, how many lie outside the closed canopy (an ellipsoid fitted to the
        // canopy's bounds)? 0 = nothing pokes through the glass. Also: how many lie below the floor of the body (poking out underneath).
        public (int outside, int total, int below) PokeTest(Mode form)
        {
            if (!model || !forms) return (0, 0, 0);
            var canopy = forms.PartTransform("Canopy"); var body = forms.PartTransform("Body");
            var cr = canopy ? canopy.GetComponentInChildren<MeshFilter>() : null; var br = body ? body.GetComponentInChildren<Renderer>() : null;
            if (!cr || !br) return (0, 0, 0);
            float bodyTop = Local(br.bounds.max).y, bodyBottom = Local(br.bounds.min).y;
            var mb = cr.sharedMesh.bounds; var ct = cr.transform;
            int outside = 0, total = 0, below = 0; var mesh = new Mesh();
            foreach (var sk in skins)
            {
                sk.BakeMesh(mesh, true); var tr = sk.transform;
                foreach (var p in mesh.vertices)
                {
                    var w = tr.TransformPoint(p); var l = Local(w); total++;
                    if (l.y < bodyBottom) below++;
                    if (l.y <= bodyTop || form != Mode.Dive) continue;
                    var q = ct.InverseTransformPoint(w) - mb.center; var e = mb.extents;   // canopy mesh space: an ellipsoid with the bounds' half sizes
                    float k = (q.x * q.x) / (e.x * e.x) + (q.y * q.y) / (e.y * e.y) + (q.z * q.z) / (e.z * e.z);
                    if (k > 1f) outside++;
                }
            }
            Destroy(mesh); return (outside, total, below);
        }

        void Update() { if (Animated && model && v) UpdateGraph(); }

        void LateUpdate()
        {
            if (!model || !v || (!Animated && seated.Count == 0)) return;
            float k = 1 - Mathf.Exp(-6f * Time.deltaTime);
            lean = Mathf.Lerp(lean, v.Holding || v.Finished ? 0 : v.Input.Steer, k);
            if (Animated)   // the clip has posed her upper body this frame; legs stay forward in the racing seat (the clips sit on a chair), lean and look on top
            {
                model.localPosition += seat - Local(hips.position);   // hips stay on the seat whatever height the clip sits at
                Legs();
                if (spine) spine.rotation = Quaternion.AngleAxis(-lean * 6f, transform.forward) * spine.rotation;
                if (head) head.rotation = Quaternion.AngleAxis(lean * 12f, transform.up) * head.rotation;
                return;
            }
            cheer = Mathf.MoveTowards(cheer, v.Finished ? 1 : 0, Time.deltaTime * 2.5f);
            foreach (var kv in seated) kv.Key.localRotation = kv.Value;
            if (spine) spine.rotation = Quaternion.AngleAxis(-lean * 9f, transform.forward) * spine.rotation;
            if (head) head.rotation = Quaternion.AngleAxis(lean * 14f, transform.up) * head.rotation;
            if (cheer > .01f)
                foreach (var (u, l, s) in new[] { (uaL, laL, -1f), (uaR, laR, 1f) })
                {
                    if (!u || !l) continue; var to = W(new Vector3(s * .45f, 1, .1f)); var cur = l.position - u.position;
                    u.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, to), cheer) * u.rotation;
                }
        }

        // ---------------- voice ----------------
        void Voice()
        {
            voice = gameObject.AddComponent<AudioSource>(); voice.spatialBlend = 0; voice.volume = .95f; voice.playOnAwake = false;
            int n = 0;
            foreach (var kv in LineNames)
            {
                var list = new List<AudioClip>(); foreach (var name in kv.Value) { var c = Resources.Load<AudioClip>("Audio/Voice/abigail/" + name); if (c) list.Add(c); }
                lines[kv.Key] = list.ToArray(); n += list.Count;
            }
            v.Event += (kind, text) =>
            {
                if (kind == "finish") { Say("finish"); PlayOnce(Clap); }
                else if (kind == "reset") { Say("reset"); PlayOnce(Grumble); }
                else if (kind == "go") PlayOnce(Horn);
                else if (kind == "change") { if (v.ChangeFrom == Mode.Boat && v.Mode == Mode.Drive) Say("waterfall"); PlayOnce(Thumbs); }
            };
            Debug.Log($"[Driver] voice: {n} Abigail lines loaded");
        }
        public bool Say(string what)
        {
            if (!lines.TryGetValue(what, out var set) || set.Length == 0) return false;
            float now = Time.time;
            if (now < nextAny || (nextOf.TryGetValue(what, out var t) && now < t)) { Spoken.Add($"{what}: skipped (cooldown)"); return false; }
            var clip = set[Random.Range(0, set.Length)]; voice.PlayOneShot(clip);
            nextAny = now + AnyCooldown; nextOf[what] = now + LineCooldown; Spoken.Add($"{what}: {clip.name}");
            v.Note("voice", $"Abi says {clip.name} ({what})");
            return true;
        }
    }
}
