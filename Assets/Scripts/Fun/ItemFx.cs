using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 17 item 1: what the items look like and the things they send out: projectiles that chase along the course (the Wrong Form zap,
    // the Bubble Trap, the dino egg's baby raptor (a baby pterodactyl at a plane, a baby plesiosaur at a boat or a sub), the firework rocket,
    // the human cannonball), the Pocket Storm's cloud, the Mini Flood's wave, a Slick on the road / water (glitter in the air, ink under
    // water), the Rainbow Bridge's lane, the Bubble Shield, the Mirage's three copies, the Hype Horn's rings, a confetti / bubble puff.
    // Built from MeshKit's low-poly pieces in vertex colours (the toon look; glows on the Beacon shader, no fog: they read from far).
    public static class ItemFx
    {
        static Material glow, glowSoft, toon;
        // the Beacon shader's own colour is gold (the route's rings): white here, so the vertex colours show as they are
        static Material Glow { get { if (!glow) { glow = new Material(GateMarker.Beacon) { name = "item glow" }; glow.SetColor("_Color", Color.white); } return glow; } }
        public static Material GlowMat => Glow;
        static Material GlowSoft { get { if (!glowSoft) { glowSoft = new Material(GateMarker.Beacon) { name = "item glow soft" }; glowSoft.SetColor("_Color", new Color(1, 1, 1, .7f)); } return glowSoft; } }
        static Material Toon => toon ? toon : (toon = JurassicLook.VertexColored(true));
        public static GameObject Build(Transform parent, string name, System.Action<MeshKit.Batch> add, Material mat)
        { var b = new MeshKit.Batch(); add(b); var g = b.Object(parent, name, mat, false, false); return g; }
        static Transform Root(string name, Vector3 at) { var g = new GameObject(name); g.transform.position = at; return g.transform; }
        static void Life(GameObject g, float s) => Object.Destroy(g, s);
        static readonly Color[] Rainbow = { new Color(1f, .25f, .25f), new Color(1f, .6f, .15f), new Color(1f, .95f, .25f), new Color(.3f, .95f, .35f), new Color(.3f, .6f, 1f), new Color(.65f, .35f, 1f) };

        // a new race (or a restart): nothing an item sent out or left behind carries over
        public static void ClearAll()
        {
            foreach (var x in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
            foreach (var x in Object.FindObjectsByType<CloudFx>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
            foreach (var x in Object.FindObjectsByType<WaveFx>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
            foreach (var x in Object.FindObjectsByType<SlickFx>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
            foreach (var x in Object.FindObjectsByType<FollowFx>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
            foreach (var x in Object.FindObjectsByType<MirageFx>(FindObjectsSortMode.None)) Object.Destroy(x.gameObject);
        }
        // the item box: a spinning rainbow cube with a white core
        public static Transform BoxMesh(Transform parent)
        {
            var g = Build(parent, "box", b =>
            {
                for (int i = 0; i < 6; i++) { var n = Quaternion.Euler(i < 4 ? 0 : (i == 4 ? 90 : -90), i < 4 ? i * 90 : 0, 0); b.Add(MeshKit.Cube, n * Vector3.forward * .5f, n, new Vector3(.96f, .96f, .04f), Rainbow[i] * new Color(1, 1, 1, .72f)); }
                b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, Vector3.one * .45f, new Color(1, 1, 1, .95f));
            }, Glow);
            return g.transform;
        }

        // a burst: little glowing cubes flying out and fading (confetti, bubbles, sparks)
        public static void Puff(Vector3 at, Color c, float size)
        {
            var r = Root("item puff", at); var p = r.gameObject.AddComponent<PuffFx>(); p.size = size; p.color = c;
        }
        // ---- held on a racer ----
        public static void TrapBubble(ShiftVehicle v, float secs) => Follow(v, "bubble trap", secs, b => b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, Vector3.one * 7f, new Color(.6f, .9f, 1f, .55f)), GlowSoft, true);
        public static void Shield(ShiftVehicle v, float secs) => Follow(v, "bubble shield", secs, b => b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, Vector3.one * 6.2f, new Color(.5f, 1f, 1f, .45f)), GlowSoft, false, () => Time.time < v.ShieldUntil);
        public static void Horn(ShiftVehicle v)
        {
            Follow(v, "hype horn", 1.2f, b => { for (int i = 0; i < 20; i++) { float a = i * 18f; b.Add(MeshKit.Cube, Quaternion.Euler(0, a, 0) * Vector3.forward * 4f, Quaternion.Euler(0, a, 0), new Vector3(1.2f, .35f, .2f), new Color(1f, .7f, .2f)); } }, Glow, false, null, true);
            GameSounds.Instance?.Play("go", .5f); GameSounds.Instance?.Play("finish", .5f);
        }
        static void Follow(ShiftVehicle v, string name, float secs, System.Action<MeshKit.Batch> add, Material m, bool wobble, System.Func<bool> alive = null, bool grow = false)
        {
            var g = Build(null, name, add, m); var f = g.AddComponent<FollowFx>(); f.v = v; f.until = Time.time + secs; f.alive = alive; f.wobble = wobble; f.grow = grow;
        }
        public static void Mirage(ShiftVehicle v, float secs) { var g = new GameObject("mirage"); var m = g.AddComponent<MirageFx>(); m.v = v; m.until = Time.time + secs; GameSounds.Instance?.At("it_shimmer", v.Body.position, .6f); }

        // ---- sent out ----
        public static void Fire(ItemKind k, ShiftVehicle owner, ShiftVehicle target)
        {
            var g = new GameObject("item " + k); g.transform.position = owner.Body.position + Vector3.up * 1.5f + owner.transform.forward * 4f;
            var p = g.AddComponent<Projectile>(); p.kind = k; p.owner = owner; p.target = target; p.prog = owner.Progress; p.course = owner.Course;
            p.Make();
            GameSounds.Instance?.At(k == ItemKind.WrongForm ? "it_zap" : k == ItemKind.BubbleTrap ? "it_bubble" : k == ItemKind.FireworkRocket ? "it_rocket" : k == ItemKind.HumanCannonball ? "it_boom" : "it_squeak", owner.Body.position, .6f);
        }
        public static void Storm(ShiftVehicle owner) { var g = new GameObject("pocket storm"); var c = g.AddComponent<CloudFx>(); c.owner = owner; c.prog = owner.Progress + 15f; c.Make(); GameSounds.Instance?.At("it_rain", owner.Body.position, .6f); }
        public static void Wave(ShiftVehicle owner) { var g = new GameObject("mini flood"); var w = g.AddComponent<WaveFx>(); w.owner = owner; w.prog = owner.Progress + 6f; w.Make(); GameSounds.Instance?.At("it_wave", owner.Body.position, .65f); }
        public static void Slick(ShiftVehicle owner)
        {
            var at = owner.Body.position - Vector3.ProjectOnPlane(owner.transform.forward, Vector3.up).normalized * 7f;
            var g = new GameObject("slick"); g.transform.position = at; var s = g.AddComponent<SlickFx>(); s.owner = owner; s.form = owner.Mode; s.Make(); GameSounds.Instance?.At("it_pop", at, .4f);
        }
        public static Lane MakeLane(ShiftVehicle v)
        {
            var g = new GameObject("rainbow bridge"); var l = g.AddComponent<Lane>(); l.v = v; l.Make(); GameSounds.Instance?.Play("ring", .6f, 1.2f); return l;
        }

        // the dino egg's chaser, the rocket, the clown, the zap, the bubble: by how it looks
        public static GameObject Look(ItemKind k, Transform parent, Mode targetForm)
        {
            switch (k)
            {
                case ItemKind.WrongForm:
                    return Build(parent, "zap", b => { b.Add(MeshKit.Cube, Vector3.zero, Quaternion.identity, new Vector3(.5f, .5f, 4.5f), new Color(.8f, .45f, 1f)); b.Add(MeshKit.LowSphere, Vector3.forward * 2.2f, Quaternion.identity, Vector3.one * 1.6f, new Color(1f, .85f, 1f)); }, Glow);
                case ItemKind.BubbleTrap:
                    return Build(parent, "bubble", b => { b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, Vector3.one * 3f, new Color(.6f, .9f, 1f, .6f)); b.Add(MeshKit.LowSphere, new Vector3(.6f, .7f, -.6f), Quaternion.identity, Vector3.one * .6f, new Color(1, 1, 1, .9f)); }, GlowSoft);
                case ItemKind.FireworkRocket:
                    return Build(parent, "rocket", b =>
                    {
                        var along = Quaternion.Euler(90, 0, 0);
                        b.Add(MeshKit.LowCylinder, Vector3.zero, along, new Vector3(.7f, 2.4f, .7f), new Color(.9f, .15f, .15f));
                        b.Add(MeshKit.LowCone, Vector3.forward * 1.6f, along, new Vector3(.75f, .9f, .75f), new Color(1f, .9f, .3f));
                        for (int i = 0; i < 3; i++) b.Add(MeshKit.Cube, Quaternion.Euler(0, 0, i * 120) * Vector3.up * .5f - Vector3.forward * 1f, Quaternion.Euler(0, 0, i * 120), new Vector3(.1f, .7f, .6f), Color.white);
                    }, Toon);
                case ItemKind.HumanCannonball:
                    return Build(parent, "clown", b =>
                    {
                        b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(1.2f, 1.2f, 1.7f), new Color(.95f, .2f, .3f));
                        b.Add(MeshKit.LowSphere, Vector3.forward * 1f, Quaternion.identity, Vector3.one * .8f, new Color(1f, .85f, .7f));
                        b.Add(MeshKit.LowSphere, Vector3.forward * 1.05f + Vector3.up * .25f, Quaternion.identity, new Vector3(.95f, .5f, .95f), new Color(1f, .85f, .15f));
                        b.Add(MeshKit.LowSphere, Vector3.forward * 1.45f, Quaternion.identity, Vector3.one * .28f, new Color(1f, .1f, .1f));
                        foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * .55f, .1f, 1.2f), Quaternion.identity, new Vector3(.25f, .25f, 1.1f), Color.white);
                    }, Toon);
                default:   // the dino egg: a baby raptor (ground), pterodactyl (air), plesiosaur (water)
                    var g = new GameObject("baby dino"); g.transform.SetParent(parent, false);
                    var green = new Color(.35f, .7f, .3f); var belly = new Color(.85f, .8f, .55f);
                    var raptor = Build(g.transform, "raptor", b =>
                    {
                        b.Add(MeshKit.LowSphere, Vector3.up * 1.1f, Quaternion.identity, new Vector3(.9f, .9f, 1.8f), green);
                        b.Add(MeshKit.LowSphere, new Vector3(0, 1.7f, 1f), Quaternion.identity, new Vector3(.6f, .55f, .9f), green);
                        b.Add(MeshKit.LowCone, new Vector3(0, 1.15f, -1.3f), Quaternion.Euler(-95, 0, 0), new Vector3(.45f, 1.4f, .45f), green);
                        foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * .3f, .45f, 0), Quaternion.identity, new Vector3(.22f, .9f, .3f), belly);
                        foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.LowSphere, new Vector3(s * .2f, 1.85f, 1.35f), Quaternion.identity, Vector3.one * .14f, Color.white);
                    }, Toon);
                    var ptero = Build(g.transform, "ptero", b =>
                    {
                        b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(.6f, .5f, 1.4f), new Color(.75f, .45f, .3f));
                        b.Add(MeshKit.LowCone, new Vector3(0, .15f, 1.1f), Quaternion.Euler(90, 0, 0), new Vector3(.3f, .9f, .3f), new Color(.95f, .8f, .45f));
                        foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * 1.4f, .1f, 0), Quaternion.Euler(0, 0, s * 8), new Vector3(2.6f, .08f, .9f), new Color(.85f, .55f, .35f));
                    }, Toon);
                    var plesio = Build(g.transform, "plesio", b =>
                    {
                        b.Add(MeshKit.LowSphere, Vector3.zero, Quaternion.identity, new Vector3(1.1f, .7f, 1.8f), new Color(.3f, .55f, .65f));
                        b.Add(MeshKit.LowCylinder, new Vector3(0, .7f, 1.1f), Quaternion.Euler(40, 0, 0), new Vector3(.3f, 1.6f, .3f), new Color(.3f, .55f, .65f));
                        b.Add(MeshKit.LowSphere, new Vector3(0, 1.3f, 1.7f), Quaternion.identity, new Vector3(.4f, .35f, .55f), new Color(.35f, .62f, .7f));
                        foreach (float s in new[] { -1f, 1f }) b.Add(MeshKit.Cube, new Vector3(s * .7f, -.2f, .3f), Quaternion.Euler(0, s * 25, 0), new Vector3(1f, .08f, .4f), belly);
                    }, Toon);
                    raptor.SetActive(targetForm == Mode.Drive); ptero.SetActive(targetForm == Mode.Fly); plesio.SetActive(targetForm == Mode.Boat || targetForm == Mode.Dive);
                    return g;
            }
        }

        // ---- the Rainbow Bridge: a lane of six coloured strips along the route ahead (the road, the water) or rainbow rings (the air, under
        // water); only its owner gets the push (ShiftVehicle.AfterEffects) ----
        public sealed class Lane : MonoBehaviour
        {
            public ShiftVehicle v; readonly List<Vector3> pts = new List<Vector3>(); float until; Mode form; int near;
            public bool Done => near >= pts.Count - 2 || Time.time > until;
            public int Points => pts.Count;
            public void Make()
            {
                form = v.Mode; until = Time.time + 9f; var c = v.Course;
                for (float d = 8f; d <= 250f; d += 4f)
                {
                    Vector3 q;
                    if (v.OnShortcut) { if (v.ShortcutS + d > v.OnShortcut.Length) break; q = v.OnShortcut.PointAt(v.ShortcutS + d, out _); }
                    else { if (v.Progress + d > c.Length - 5) break; q = c.PointAt(v.Progress + d, out _); }
                    pts.Add(q);
                }
                bool flat = form == Mode.Drive || form == Mode.Boat;
                float surf = 0; bool water = form == Mode.Boat && pts.Count > 0 && WaterVolume.SurfaceAt(pts[0], out surf, 4f);
                Build(transform, "lane", b =>
                {
                    for (int i = 1; i < pts.Count; i++)
                    {
                        var a = pts[i - 1]; var e = pts[i]; var d = e - a; if (d.sqrMagnitude < .01f) continue;
                        var mid = (a + e) * .5f; var fl = Vector3.ProjectOnPlane(d, Vector3.up); var right = Vector3.Cross(Vector3.up, fl.normalized);
                        if (flat)
                        {
                            float y = form == Mode.Drive ? mid.y - .84f : (water ? surf + .12f : mid.y - .45f);
                            var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
                            for (int k = 0; k < 6; k++) b.Add(MeshKit.Cube, new Vector3(mid.x, form == Mode.Drive ? y : y, mid.z) + right * ((k - 2.5f) * 1.15f), rot, new Vector3(1.12f, .06f, d.magnitude + .05f), Rainbow[k]);
                        }
                        else if (i % 6 == 0)
                        {
                            var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
                            for (int s = 0; s < 18; s++) { float an = s * 20f; b.Add(MeshKit.Cube, mid + rot * (Quaternion.Euler(0, 0, an) * Vector3.up * 6f), rot * Quaternion.Euler(0, 0, an), new Vector3(2.2f, .5f, .5f), Rainbow[(s / 3) % 6]); }
                        }
                    }
                }, Glow);
            }
            public bool On(ShiftVehicle who)
            {
                if (who != v || pts.Count < 2 || Done) return false;
                var p = who.Body.position; float best = 1e9f; int bi = near;
                for (int i = Mathf.Max(0, near - 3); i < Mathf.Min(pts.Count, near + 25); i++)
                {
                    var d = pts[i] - p; if (form == Mode.Drive || form == Mode.Boat) d.y = 0; float m = d.sqrMagnitude;
                    if (m < best) { best = m; bi = i; }
                }
                near = Mathf.Max(near, bi);
                return best < 7.5f * 7.5f && who.Mode == form;
            }
        }
    }

    public sealed class PuffFx : MonoBehaviour
    {
        public float size = 1; public Color color = Color.white; readonly List<(Transform t, Vector3 v)> bits = new List<(Transform, Vector3)>(); float t0;
        void Start()
        {
            t0 = Time.time; var rnd = new System.Random(GetHashCode());
            for (int i = 0; i < 18; i++)
            {
                var g = ItemFx.Build(transform, "bit", b => b.Add(MeshKit.Cube, Vector3.zero, Quaternion.identity, Vector3.one * (.35f + .25f * size * .3f), Color.Lerp(color, Color.white, (float)rnd.NextDouble() * .5f)), ItemFx.GlowMat);
                var d = new Vector3((float)rnd.NextDouble() - .5f, (float)rnd.NextDouble() * .8f, (float)rnd.NextDouble() - .5f).normalized * (6f + 6f * size * (float)rnd.NextDouble());
                bits.Add((g.transform, d));
            }
            Destroy(gameObject, 1.1f);
        }
        void Update() { float k = Time.time - t0; foreach (var (t, v) in bits) { t.localPosition = v * k + Vector3.down * 4f * k * k; t.localScale = Vector3.one * Mathf.Max(0, 1 - k); t.localRotation = Quaternion.Euler(k * 400, k * 300, 0); } }
    }

    public sealed class FollowFx : MonoBehaviour
    {
        public ShiftVehicle v; public float until; public System.Func<bool> alive; public bool wobble, grow; float t0;
        void Start() => t0 = Time.time;
        void LateUpdate()
        {
            if (!v || Time.time > until || (alive != null && !alive())) { if (v && name.StartsWith("bubble")) { ItemFx.Puff(transform.position, new Color(.7f, .95f, 1f), 1.5f); GameSounds.Instance?.At("it_pop", transform.position, .6f); } Destroy(gameObject); return; }
            transform.position = v.transform.position + Vector3.up * .4f;
            float k = Time.time - t0;
            transform.localScale = grow ? Vector3.one * (.5f + 3f * k) : Vector3.one * (1f + (wobble ? .06f * Mathf.Sin(Time.time * 9f) : .03f * Mathf.Sin(Time.time * 4f)));
        }
    }

    // the Mirage: three copies of the racer's own model, a little behind on its own path (from its history), flickering
    public sealed class MirageFx : MonoBehaviour
    {
        public ShiftVehicle v; public float until; readonly List<Transform> copies = new List<Transform>(); Mode made = (Mode)(-1);
        void Build()
        {
            foreach (var c in copies) if (c) Destroy(c.gameObject); copies.Clear();
            var model = v.Forms ? v.Forms.ModelOf(v.Mode) : null; made = v.Mode;
            for (int i = 0; i < 3; i++)
            {
                Transform t;
                if (model) { var g = Instantiate(model, transform); g.SetActive(true); foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb); foreach (var co in g.GetComponentsInChildren<Collider>(true)) Destroy(co); t = g.transform; }
                else t = ItemFx.Build(transform, "copy", b => b.Add(MeshKit.Cube, Vector3.zero, Quaternion.identity, new Vector3(2f, 1f, 4.2f), new Color(.7f, .95f, 1f, .5f)), ItemFx.GlowMat).transform;
                copies.Add(t);
            }
        }
        void LateUpdate()
        {
            if (!v || Time.time > until || v.Decoys <= 0) { Destroy(gameObject); return; }
            if (made != v.Mode) Build();
            var h = v.History;
            for (int i = 0; i < copies.Count; i++)
            {
                var c = copies[i]; bool on = i < v.Decoys; if (!c) continue;
                float back = .3f * (i + 1); int k = h.Count - 1; while (k > 0 && h[k].t > Time.time - back) k--;
                if (k < 0 || h.Count == 0) { c.gameObject.SetActive(false); continue; }
                c.gameObject.SetActive(on && ((Time.frameCount + i * 3) % 9) != 0);
                c.SetPositionAndRotation(h[k].pos + Vector3.right * Mathf.Sin(Time.time * 3f + i) * .6f, h[k].rot);
                c.localScale = Vector3.one * (v.Forms ? v.Forms.transform.lossyScale.x : 1f);
            }
        }
    }

    // a projectile: along the course line toward its target's place on it, then straight at it
    public sealed class Projectile : MonoBehaviour
    {
        public ItemKind kind; public ShiftVehicle owner, target; public Course course; public float prog; float life; Transform look; Mode shown;
        float Extra => kind == ItemKind.WrongForm ? 90f : kind == ItemKind.BubbleTrap ? 30f : 38f;
        float Min => kind == ItemKind.WrongForm ? 160f : 70f;
        public void Make() { shown = target ? target.Mode : Mode.Drive; look = ItemFx.Look(kind, transform, shown).transform; if (target) target.Warn(Items.Name(kind).ToUpper() + " INCOMING!", 1.5f); }
        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime; life += dt;
            if (life > 14f || !target || target.Finished || !target.gameObject.activeInHierarchy || target.Course != course) { ItemFx.Puff(transform.position, Items.Tint(kind), 1f); Destroy(gameObject); return; }
            var tp = target.Body.position + Vector3.up * .5f; var pos = transform.position;
            float sp = Mathf.Max(target.Speed + Extra, Min);
            bool close = Mathf.Abs(target.Progress - prog) < 70f || (tp - pos).sqrMagnitude < 80f * 80f;
            Vector3 next;
            if (close) next = Vector3.MoveTowards(pos, tp, sp * dt);
            else
            {
                prog += Mathf.Sign(target.Progress - prog) * sp * dt; var q = course.PointAt(prog, out _) + Vector3.up * 1.2f;
                next = Vector3.MoveTowards(pos, q, sp * dt * 1.4f);
            }
            if ((next - pos).sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(next - pos, Vector3.up);
            transform.position = next;
            if (kind == ItemKind.DinoEgg && target.Mode != shown && look) { shown = target.Mode; Destroy(look.gameObject); look = ItemFx.Look(kind, transform, shown).transform; }
            if (look && kind == ItemKind.DinoEgg) look.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(life * 14f)) * .5f;
            if ((tp - next).sqrMagnitude < 80f * 80f) target.Warn(Items.Name(kind).ToUpper() + " INCOMING!", .4f);
            if ((tp - next).sqrMagnitude < 4.5f * 4.5f)
            {
                bool took = target.Hit(kind, owner); GameSounds.Instance?.At(kind == ItemKind.BubbleTrap ? "it_bubble" : kind == ItemKind.WrongForm ? "it_zap" : kind == ItemKind.FireworkRocket ? "it_rocket" : "land", tp, .7f);
                ItemFx.Puff(tp, Items.Tint(kind), kind == ItemKind.FireworkRocket ? 3f : 1.6f);
                if (kind == ItemKind.FireworkRocket) ItemFx.Puff(tp + Vector3.up * 3f, new Color(1f, .9f, .3f), 3f);
                Items.Note(owner, $"{Items.Name(kind)} reached {target.RacerName} ({target.Mode}): {(took ? "hit" : "no effect")}");
                Destroy(gameObject);
            }
        }
    }

    // the Pocket Storm: a rain cloud drifting ahead along the course from its owner, soaking each racer it passes over (once)
    public sealed class CloudFx : MonoBehaviour
    {
        public ShiftVehicle owner; public float prog; float life; readonly HashSet<ShiftVehicle> soaked = new HashSet<ShiftVehicle>(); Transform rain;
        public void Make()
        {
            ItemFx.Build(transform, "cloud", b => { var rnd = new System.Random(5); for (int i = 0; i < 7; i++) b.Add(MeshKit.LowSphere, new Vector3((float)rnd.NextDouble() * 14 - 7, (float)rnd.NextDouble() * 3, (float)rnd.NextDouble() * 10 - 5), Quaternion.identity, Vector3.one * (6f + (float)rnd.NextDouble() * 4f), new Color(.42f, .45f, .55f)); }, JurassicLook.VertexColored(true));
            rain = ItemFx.Build(transform, "rain", b => { var rnd = new System.Random(9); for (int i = 0; i < 60; i++) b.Add(MeshKit.Cube, new Vector3((float)rnd.NextDouble() * 18 - 9, -(float)rnd.NextDouble() * 14, (float)rnd.NextDouble() * 14 - 7), Quaternion.identity, new Vector3(.08f, 1.4f, .08f), new Color(.7f, .85f, 1f, .7f)); }, ItemFx.GlowMat).transform;
            transform.position = owner.Course.PointAt(prog, out _) + Vector3.up * 14f;
        }
        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime; life += dt; if (life > 6.5f || !owner || !owner.Course) { Destroy(gameObject); return; }
            prog += (Mathf.Max(owner.Speed, 40f) + 38f) * dt; var q = owner.Course.PointAt(prog, out _);
            transform.position = Vector3.Lerp(transform.position, q + Vector3.up * 14f, 6f * dt);
            if (rain) rain.localPosition = Vector3.down * ((Time.time * 18f) % 4f);
            var race = Game.Instance ? Game.Instance.Race : null; if (!race) return;
            foreach (var r in race.Racers)
            {
                if (!r || r == owner || soaked.Contains(r) || !r.gameObject.activeInHierarchy) continue;
                var d = r.Body.position - transform.position; if (new Vector2(d.x, d.z).sqrMagnitude < 30f * 30f && d.y > -45f && d.y < 10f) { soaked.Add(r); r.Warn("SOAKED!", 1.2f); bool took = r.Hit(ItemKind.PocketStorm, owner); Items.Note(owner, $"Pocket Storm over {r.RacerName} ({r.Mode}): {(took ? "soaked" : "no effect")}"); }
            }
        }
    }

    // the Mini Flood: a wave rolling forward along the course at 120 m/s for 260 m, knocking each racer it reaches (once)
    public sealed class WaveFx : MonoBehaviour
    {
        public ShiftVehicle owner; public float prog; float start, life; readonly HashSet<ShiftVehicle> hit = new HashSet<ShiftVehicle>();
        public void Make()
        {
            start = prog;
            ItemFx.Build(transform, "wave", b =>
            {
                for (int i = 0; i < 9; i++) { float x = (i - 4) * 5f; b.Add(MeshKit.LowSphere, new Vector3(x, 1.5f, 0), Quaternion.identity, new Vector3(7f, 5f, 4f), new Color(.3f, .6f, 1f, .75f)); b.Add(MeshKit.LowSphere, new Vector3(x, 4f, .8f), Quaternion.identity, new Vector3(5f, 1.6f, 2.4f), new Color(.95f, .98f, 1f, .85f)); }
            }, ItemFx.GlowMat);
        }
        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime; life += dt; if (life > 2.4f || !owner || !owner.Course) { Destroy(gameObject); return; }
            prog += 120f * dt; var q = owner.Course.PointAt(prog, out var dir);
            transform.SetPositionAndRotation(q - Vector3.up * .8f, Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up).sqrMagnitude > 1e-4f ? Vector3.ProjectOnPlane(dir, Vector3.up) : Vector3.forward, Vector3.up));
            var race = Game.Instance ? Game.Instance.Race : null; if (!race) return;
            foreach (var r in race.Racers)
            {
                if (!r || r == owner || hit.Contains(r) || !r.gameObject.activeInHierarchy || r.Lap != owner.Lap) continue;
                if (r.Progress > start && Mathf.Abs(r.Progress - prog) < 12f && Vector3.Distance(r.Body.position, q) < 70f) { hit.Add(r); bool took = r.Hit(ItemKind.FloodWave, owner); Items.Note(owner, $"Mini Flood reached {r.RacerName} ({r.Mode}): {(took ? "knocked" : "no effect")}"); }
            }
        }
    }

    // a Slick: a puddle on the road / water, glitter in the air, ink under water; the first racer through it spins
    public sealed class SlickFx : MonoBehaviour
    {
        public ShiftVehicle owner; public Mode form; float born;
        public void Make()
        {
            born = Time.time;
            if (form == Mode.Drive || form == Mode.Boat)
            {
                var p = transform.position; if (Physics.Raycast(p + Vector3.up * 2f, Vector3.down, out var h, 6f, ShiftVehicle.GroundMask, QueryTriggerInteraction.Ignore)) p.y = h.point.y + .06f; else if (WaterVolume.SurfaceAt(p, out float s, 3f)) p.y = s + .08f;
                transform.position = p;
                ItemFx.Build(transform, "puddle", b => { for (int i = 0; i < 5; i++) b.Add(MeshKit.LowCylinder, new Vector3(Mathf.Sin(i * 2.4f) * 1.6f, 0, Mathf.Cos(i * 2.4f) * 1.6f), Quaternion.identity, new Vector3(3.2f, .04f, 3.2f), new Color(.75f, .45f, 1f, .8f)); }, ItemFx.GlowMat);
            }
            else ItemFx.Build(transform, form == Mode.Fly ? "glitter" : "ink", b => { var rnd = new System.Random(3); for (int i = 0; i < 30; i++) b.Add(MeshKit.LowSphere, new Vector3((float)rnd.NextDouble() * 8 - 4, (float)rnd.NextDouble() * 6 - 3, (float)rnd.NextDouble() * 8 - 4), Quaternion.identity, Vector3.one * (form == Mode.Fly ? .5f : 1.8f), form == Mode.Fly ? new Color(1f, .8f, 1f) : new Color(.1f, .08f, .2f, .85f)); }, ItemFx.GlowMat);
            Destroy(gameObject, 20f);
        }
        void FixedUpdate()
        {
            var race = Game.Instance ? Game.Instance.Race : null; if (!race) return; float r2 = form == Mode.Drive || form == Mode.Boat ? 4.5f : 6f;
            foreach (var r in race.Racers)
            {
                if (!r || !r.gameObject.activeInHierarchy || (r == owner && Time.time - born < 2f)) continue;
                if ((r.Body.position - transform.position).sqrMagnitude < r2 * r2) { bool took = r.Hit(ItemKind.Slick, owner); GameSounds.Instance?.At("it_slick", transform.position, .7f); Items.Note(owner, $"Slick caught {r.RacerName} ({r.Mode}): {(took ? "spun" : "no effect")}"); ItemFx.Puff(transform.position, new Color(.8f, .5f, 1f), 1.5f); Destroy(gameObject); return; }
            }
        }
    }
}
