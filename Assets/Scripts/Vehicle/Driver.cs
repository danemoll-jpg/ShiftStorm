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
        public Transform Head => head;   // Round 6: the first-person view (CameraViews) sits at her eyes
        ShiftVehicle v; FormSet forms; Animator anim; Transform model;
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

        // voice (Round 8 item 3: every driver speaks, in their own Tee'd Off voice: copied clips + new ElevenLabs lines in Resources/Audio/Voice/<who>)
        AudioSource voice; readonly Dictionary<string, AudioClip[]> lines = new Dictionary<string, AudioClip[]>(); readonly Dictionary<string, float> nextOf = new Dictionary<string, float>();
        float nextAny; public readonly List<string> Spoken = new List<string>();
        public static readonly List<string> AllSpoken = new List<string>();   // every driver's lines in this run, "t who pool clip length" (checks)
        public float SpeakingUntil { get; private set; } = -1;
        public static float AnySpeakingUntil;   // the latest a driver's line runs to (the Caller treats it as talking)
        // pool -> clip names, per driver (the folder's lower-case name); mess-ups: Round 8 item 3 (Dan: "Noooo" or "oh no!", or in character)
        static readonly Dictionary<string, Dictionary<string, string[]>> LineNames = new Dictionary<string, Dictionary<string, string[]>>
        {
            ["abigail"] = new Dictionary<string, string[]> {
                { "win", new[] { "win_1", "quipgood_1", "quipgood_2" } }, { "last", new[] { "lose_1", "quipbad_1" } },
                { "messup", new[] { "messup_1", "messup_2", "messup_3", "disaster_1", "mishit_1", "gasp_1", "bogey_1", "line_1" } }, { "water", new[] { "water_1", "messup_1" } },
                { "waterfall", new[] { "laugh_1", "lucky_1" } } },
            // Round 9 item 2: golf-only lines out (Abi "That's not where I aimed", Rae "Too much muscle!", "Who planted that?!", "The lake can
            // have it!", Vivi "Second place suits no one" for last place), racing lines in the same voices in (Tools/Audio/driver_lines.py)
            ["rae"] = new Dictionary<string, string[]> {
                { "win", new[] { "win_1", "quipgood_1" } }, { "last", new[] { "lose_1" } },
                { "messup", new[] { "messup_1", "messup_2", "messup_3", "disaster_1", "throttle_1", "gasp_1", "wall_1" } }, { "water", new[] { "swim_1", "messup_1" } } },
            ["chip"] = new Dictionary<string, string[]> {
                { "win", new[] { "win_1", "quipgood_1" } }, { "last", new[] { "lose_1" } },
                { "messup", new[] { "messup_1", "messup_2", "messup_3", "disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_2" } }, { "water", new[] { "messup_2", "disaster_1" } } },
            ["vivi"] = new Dictionary<string, string[]> {
                { "win", new[] { "win_1", "quipgood_1" } }, { "last", new[] { "last_1" } },
                { "messup", new[] { "messup_1", "messup_2", "messup_3", "disaster_1", "disaster_2", "mishit_1", "gasp_1", "trouble_1" } }, { "water", new[] { "water_1", "messup_2" } } },
        };
        public static string[] MessupNames(string folder) => LineNames.TryGetValue(folder, out var t) && t.TryGetValue("messup", out var m) ? m : new string[0];
        const float LineCooldown = 8f, AnyCooldown = 3f, MessupCooldown = 5f, NearPlayer = 70f;
        Steering steering; float ik; public float IkWeight => ik;
        public Steering Steering => steering;
        public int FinishPlace { get; private set; }   // the place this driver finished in (0: not finished / no race)
        public string FinishReaction { get; private set; } = "";
        public static readonly List<Driver> All = new List<Driver>();
        public string Who => who;
        public int LinesLoaded { get; private set; }
        public int LinesIn(string pool) => lines.TryGetValue(pool, out var l) ? l.Length : 0;

        // Round 3: the rivals' drivers too (Tee'd Off's Chip, Rae and Vivi, copied to Resources/Drivers/<name>), with Abi's shared seated
        // clips and no voice (only the player's driver speaks).
        string modelPath = "Driver/Abigail", texPath = "Driver/Abigail_basecolor", who = "Abigail";
        public static Driver Make(ShiftVehicle v, string character = null)
        {
            var d = v.gameObject.AddComponent<Driver>(); d.v = v; d.forms = v.Forms;
            if (character != null) { d.who = character; d.modelPath = $"Drivers/{character}/{character}"; d.texPath = $"Drivers/{character}/{character}_basecolor"; }
            else Instance = d;
            d.Seat(); d.Voice(); All.Add(d);
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
            model = Instantiate(prefab, forms.Pivot, false).transform; model.name = who + " (driver)";
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
            Place(seat, s); Scale = s; baseScale = s; HeadTop = MaxY();
            if (!Animated) foreach (var b in new[] { hips, spine, chest, head, uaL, uaR, laL, laR, ulL, ulR, llL, llR }) if (b) seated[b] = b.localRotation;
            Source = Animated ? $"{who}.fbx (Tee'd Off), Humanoid, {ClipCount} shared Mixamo clips (Driving loop), scale {s:0.00}" : $"{who}.fbx (Tee'd Off), Humanoid, fixed seated pose (no clips found), scale {s:0.00}";
            // Round 8 item 4: the steering mechanism; Round 9 item 1: placed from her eyes in the Driving pose (where the first-person camera
            // sits, CameraViews.FirstPerson) at real proportions to her, low in the view, where her hands reach (Steering.Place)
            if (handL && handR && head)
            {
                var eye = forms.Pivot.InverseTransformPoint(head.position + transform.up * (.1f * s) + transform.forward * (.05f * s)); eye.x = 0;
                // where her hands reach comfortably: the Driving clip's hands, a little up and forward (Round 8's measured place)
                Vector3 a = forms.Pivot.InverseTransformPoint(handL.position), b = forms.Pivot.InverseTransformPoint(handR.position);
                var mid = (a + b) * .5f; var hp = forms.Pivot.InverseTransformPoint(hips.position);
                mid.y = Mathf.Clamp(mid.y + .12f, hp.y + .2f * s, hp.y + .6f * s); mid.z = Mathf.Clamp(mid.z + .13f, hp.z + .22f * s, hp.z + .6f * s); mid.x = 0;
                steering = Steering.Make(v, eye, mid, s);
                Debug.Log($"[Driver] {who}: eyes at {eye.ToString("F2")}, wheel at {steering.Anchor.ToString("F2")} (grips {steering.Span:0.00} m apart)");
            }
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
            if (Astride) { LegsAstride(); return; }
            foreach (var (u, l, f, sd) in new[] { (ulL, llL, footL, -1f), (ulR, llR, footR, 1f) })
            { Aim(u, l, W(new Vector3(sd * .12f, .08f, 1))); Aim(l, f, W(new Vector3(0, -.75f, .65f))); }
        }
        // Round 9 item 3c: astride (the Scrambler: designs.json "astride"): thighs down and forward either side of the tank, knees bent, feet
        // back on the pegs, the back leaning forward over the bars
        bool Astride { get { var d = forms ? forms.Design : null; return d != null && d.Astride(forms.ShownMode); } }
        public bool RidingAstride => Astride;
        Vector3 WP(Vector3 localDir) => forms.Pivot.TransformDirection(localDir.normalized);   // the pivot's frame: she leans with the bike
        void LegsAstride()
        {
            foreach (var (u, l, f, sd) in new[] { (ulL, llL, footL, -1f), (ulR, llR, footR, 1f) })
            { Aim(u, l, WP(new Vector3(sd * .34f, -.55f, .78f))); Aim(l, f, WP(new Vector3(sd * .06f, -.95f, -.32f))); }
            if (spine) Aim(spine, chest ? chest : head, WP(new Vector3(0, 1, .75f)));
            if (chest) Aim(chest, head, WP(new Vector3(0, 1, .4f)));
            if (head) head.rotation = Quaternion.AngleAxis(-25f, forms.Pivot.right) * head.rotation;   // eyes up the road again
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

        Vector3 seatApplied; float baseScale;
        // Round 9 item 3c: her size on a design that asks for it (designs.json "riderScale": the Scrambler's bikes, 1.4x); the hips stay on the
        // seat (re-anchored every frame), the eyes / first-person camera follow her head
        void DesignScale()
        {
            if (!model || baseScale <= 0) return;
            var d = forms ? forms.Design : null; float want = baseScale * (d != null && d.riderScale > 0 ? d.riderScale : 1f);
            if (Mathf.Abs(model.localScale.x - want) < 1e-4f) return;
            model.localScale = Vector3.one * want; Scale = want;
            if (!Animated) { model.localPosition += seat + forms.SeatOffset - forms.Pivot.InverseTransformPoint(hips.position); seatApplied = forms.SeatOffset; }
        }
        void Update()
        {
            DesignScale();
            if (model && forms && model.gameObject.activeSelf != forms.ShowDriver) model.gameObject.SetActive(forms.ShowDriver);   // a closed design hides her
            if (Animated && model && v) UpdateGraph();
            TryPending();
        }

        void LateUpdate()
        {
            if (!model || !v || (!Animated && seated.Count == 0)) return;
            float k = 1 - Mathf.Exp(-6f * Time.deltaTime);
            lean = Mathf.Lerp(lean, v.Holding || v.Finished ? 0 : v.Input.Steer, k);
            if (Animated)   // the clip has posed her upper body this frame; legs stay forward in the racing seat (the clips sit on a chair), lean and look on top
            {
                // hips stay on the seat whatever height the clip sits at; Round 5: measured in the FormSet's pivot (she squashes and twirls with
                // the change) and on the shown model's own Seat point
                model.localPosition += seat + forms.SeatOffset - forms.Pivot.InverseTransformPoint(hips.position);
                Legs();
                if (spine) spine.rotation = Quaternion.AngleAxis(-lean * 6f, transform.forward) * spine.rotation;
                if (head) head.rotation = Quaternion.AngleAxis(lean * 12f, transform.up) * head.rotation;
                ArmsToGrips();
                return;
            }
            model.localPosition += forms.SeatOffset - seatApplied; seatApplied = forms.SeatOffset;
            cheer = Mathf.MoveTowards(cheer, v.Finished ? 1 : 0, Time.deltaTime * 2.5f);
            foreach (var kv in seated) kv.Key.localRotation = kv.Value;
            if (Astride) LegsAstride();   // Round 9
            if (spine) spine.rotation = Quaternion.AngleAxis(-lean * 9f, transform.forward) * spine.rotation;
            if (head) head.rotation = Quaternion.AngleAxis(lean * 14f, transform.up) * head.rotation;
            ArmsToGrips();
            if (cheer > .01f)
                foreach (var (u, l, s) in new[] { (uaL, laL, -1f), (uaR, laR, 1f) })
                {
                    if (!u || !l) continue; var to = W(new Vector3(s * .45f, 1, .1f)); var cur = l.position - u.position;
                    u.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(cur, to), cheer) * u.rotation;
                }
        }

        // ---------------- Round 8 item 4: hands on the steering ----------------
        // Two-bone IK per arm (upper arm, forearm) to the shown mechanism's grips, the elbows bent down and out; eased off for the one-shot
        // gestures (clap, thumbs up, honk, disappointed) and after the finish.
        void ArmsToGrips()
        {
            var m = steering ? steering.Shown : null;
            bool want = m != null && oneShot < 0 && !v.Finished && forms.ShowDriver;
            ik = Mathf.MoveTowards(ik, want ? 1 : 0, Time.deltaTime / .25f);
            if (ik <= 0 || m == null) return;
            TwoBone(uaL, laL, handL, m.gripL.position, -1, ik); TwoBone(uaR, laR, handR, m.gripR.position, 1, ik);
        }
        void TwoBone(Transform upper, Transform lower, Transform hand, Vector3 target, float side, float w)
        {
            if (!upper || !lower || !hand) return;
            Vector3 s0 = upper.position; float a = Vector3.Distance(s0, lower.position), b = Vector3.Distance(lower.position, hand.position);
            var to = target - s0; float d = Mathf.Clamp(to.magnitude, Mathf.Abs(a - b) + 1e-3f, (a + b) * .999f); var dir = to.normalized;
            var pole = transform.right * side * .7f - transform.up * .7f - transform.forward * .2f;   // elbows down and out
            var n = Vector3.Cross(dir, pole); if (n.sqrMagnitude < 1e-6f) n = transform.right; n.Normalize();
            float cosA = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1), sinA = Mathf.Sqrt(1 - cosA * cosA);
            var elbow = s0 + (dir * cosA + Vector3.Cross(n, dir) * sinA) * a;
            var full = Quaternion.FromToRotation(lower.position - s0, elbow - s0) * upper.rotation;
            upper.rotation = Quaternion.Slerp(upper.rotation, full, w);
            var fullL = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            lower.rotation = Quaternion.Slerp(lower.rotation, fullL, w);
        }
        // check helper: how far each hand is from its grip (m)
        public (float l, float r) GripError() { var m = steering ? steering.Shown : null; if (m == null || !handL || !handR) return (-1, -1); return (Vector3.Distance(handL.position, m.gripL.position), Vector3.Distance(handR.position, m.gripR.position)); }

        // ---------------- voice ----------------
        string Folder => who == "Abigail" ? "abigail" : who.ToLowerInvariant();
        void Voice()
        {
            voice = gameObject.AddComponent<AudioSource>(); voice.spatialBlend = 0; voice.volume = .95f; voice.playOnAwake = false;
            int n = 0;
            if (LineNames.TryGetValue(Folder, out var table))
                foreach (var kv in table)
                {
                    var list = new List<AudioClip>(); foreach (var name in kv.Value) { var c = Resources.Load<AudioClip>($"Audio/Voice/{Folder}/{name}"); if (c && !list.Contains(c)) list.Add(c); }
                    lines[kv.Key] = list.ToArray(); n += list.Count;
                }
            LinesLoaded = n;
            v.Event += (kind, text) =>
            {
                if (kind == "finish") StartCoroutine(FinishReact());
                else if (kind == "reset") { MessUp(text.Contains("water") || text.Contains("creek") ? "water" : "messup", "reset: " + text); PlayOnce(Grumble); }
                else if (kind == "go") PlayOnce(Horn);
                else if (kind == "start") { FinishPlace = 0; FinishReaction = ""; }
                else if (kind == "gate" && text.Contains("near miss")) MessUp("messup", "penalty");
                else if (kind == "missed") MessUp("messup", "missed gate");
                else if (kind == "change") { if (v.ChangeFrom == Mode.Boat && v.Mode == Mode.Drive && v.Human) Say("waterfall"); PlayOnce(Thumbs); }
            };
            v.Stunt += st => { if (st == "loopfail") MessUp("messup", "loop failed"); };
            v.WallHit += (angle, keep, what) => { if (keep < .7f && !v.Finished && !v.Holding) MessUp("messup", $"wall hit ({angle:0} deg)"); };
            Debug.Log($"[Driver] voice: {n} {who} lines loaded");
        }
        // the finish: a reaction by place (Round 8 item 5: 1st claps, 2nd-3rd thumbs up, last is disappointed, never clapping)
        System.Collections.IEnumerator FinishReact()
        {
            yield return null; yield return null;   // the race has added this finisher
            var g = Game.Instance; var race = g ? g.Race : null; bool raced = race && race.Active && race.Racers.Contains(v);
            int place = raced ? race.Finishers.FindIndex(f => f.v == v) + 1 : 1, field = raced ? race.Racers.Count : 1;
            if (raced && place <= 0) place = race.Place(v);
            FinishPlace = raced ? place : 0;
            if (!raced) { var tt = g ? g.TrialOf(v) : null; yield return null; bool best = tt && tt.NewBest; FinishReaction = best ? "clap (time trial, new best)" : "thumbs up (time trial)"; PlayOnce(best ? Clap : Thumbs); if (best && v.Human) Say("win"); }
            else if (place == 1) { FinishReaction = "clap (1st)"; PlayOnce(Clap); if (v.Human) Say("win"); }
            else if (place < field) { FinishReaction = $"thumbs up ({Race.Ordinal(place)})"; PlayOnce(Thumbs); }
            else { FinishReaction = $"disappointed ({Race.Ordinal(place)}, last)"; PlayOnce(Grumble); if (v.Human) Say("last"); }
            v.Note("reaction", $"{who} finished {(raced ? Race.Ordinal(place) + " of " + field : "the time trial")}: {FinishReaction}");
        }
        // a mess-up yell: the people's own drivers always, a rival only near a person's racer; never over TeedOff Barker (he narrates)
        public void MessUp(string pool, string why)
        {
            if (!v.Course || !v.Course.timeTrial || v.Holding) return;
            if (!v.Human)
            {
                var g = Game.Instance; bool near = false;
                if (g) foreach (var h in g.Humans) if (h && h.Body && Vector3.Distance(h.Body.position, v.Body.position) < NearPlayer) near = true;
                if (!near) return;
            }
            if (Time.time < AnySpeakingUntil) { Spoken.Add($"{pool}: skipped (another driver, {why})"); return; }
            // Barker mid-line: the yell waits (up to 6 s) for his line to end, and he starts nothing new meanwhile (PendingYell)
            if (Caller.Instance && Caller.Instance.Talking) { pendPool = pool; pendWhy = why; pendUntil = Time.time + 6f; PendingYell = true; return; }
            if (Say(pool, false, MessupCooldown)) v.Note("messup", $"{who} reacts: {why}");
        }
        string pendPool, pendWhy; float pendUntil = -1;
        public static bool PendingYell { get; private set; }
        void TryPending()
        {
            if (pendUntil < 0) return;
            if (Time.time > pendUntil) { Spoken.Add($"{pendPool}: skipped (Barker talking, {pendWhy})"); pendUntil = -1; PendingYell = false; return; }
            if (Caller.Instance && Caller.Instance.Talking) return;
            pendUntil = -1; PendingYell = false; nextAny = 0;
            if (Say(pendPool, false, MessupCooldown)) v.Note("messup", $"{who} reacts (after Barker's line): {pendWhy}");
        }
        public bool Say(string what, bool force = false, float cooldown = LineCooldown)
        {
            if (!lines.TryGetValue(what, out var set) || set.Length == 0) return false;
            float now = Time.time;
            if (!force && (now < nextAny || (nextOf.TryGetValue(what, out var t) && now < t))) { Spoken.Add($"{what}: skipped (cooldown)"); return false; }
            if (Caller.Instance && Caller.Instance.Talking && !force) { Spoken.Add($"{what}: skipped (Barker talking)"); return false; }
            AudioClip clip = set[Random.Range(0, set.Length)];
            for (int k = 0; k < 4 && set.Length > 1 && Spoken.Count > 0 && Spoken[Spoken.Count - 1].EndsWith(clip.name); k++) clip = set[Random.Range(0, set.Length)];
            voice.Stop(); voice.clip = clip; voice.volume = .95f * Settings.Voices * (v.Human ? 1f : .85f); voice.Play(); SoundLog.Add("voice", who + " " + clip.name);
            nextAny = now + AnyCooldown; nextOf[what] = now + cooldown; Spoken.Add($"{what}: {clip.name}");
            SpeakingUntil = now + clip.length; AnySpeakingUntil = Mathf.Max(AnySpeakingUntil, SpeakingUntil);
            AllSpoken.Add($"{now:0.00} {who} {what} {clip.name} {clip.length:0.00}");
            v.Note("voice", $"{who} says {clip.name} ({what})");
            return true;
        }
        // TeedOff Barker cuts in: a driver's line stops (he has priority)
        public static void Hush()
        {
            foreach (var d in All) if (d && d.voice && d.voice.isPlaying) { d.voice.Stop(); if (d.SpeakingUntil > Time.time) { AllSpoken.Add($"{Time.time:0.00} {d.who} cut by Barker"); d.SpeakingUntil = Time.time; } }
            AnySpeakingUntil = Mathf.Min(AnySpeakingUntil, Time.time);
        }
        public AudioSource VoiceSource => voice;
        void OnDestroy() { All.Remove(this); if (graph.IsValid()) graph.Destroy(); }
    }
}
