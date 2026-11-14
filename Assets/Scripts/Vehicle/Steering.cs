using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 4: a visible steering mechanism per form, in front of the driver, moved by the racer's own input, with the driver's hands
    // on it (Driver: two-bone arm IK to the two grips). One per form, swapped with the model at the change's midpoint (they live under the
    // FormSet's pivot, so they squash and twirl with it):
    //   car   "wheel"   a steering wheel on a raked column: turns 120 deg each way with the steering
    //   boat  "helm"    a ship's wheel with spokes and handles, upright: turns 110 deg each way
    //   plane "yoke"    a control yoke on a column: rolls 35 deg with the bank (steering), pushes / pulls with climb / dive
    //   sub   "levers"  twin throttle levers, one per hand: forward together to dive, back to climb, apart to turn
    //   also  "stick" (a single centre stick) and "handlebars" (a motorcycle's bars that turn 30 deg) for future designs
    // A design can bring its own per form: designs.json "steering": ["wheel", "helm", "yoke", "levers"] (Car, Boat, Plane, Sub order), and an
    // empty named "Steering" in a form's model places it there; otherwise it sits where the driver's hands are in the Driving pose.
    [DefaultExecutionOrder(-20)]   // before the Driver's arm IK reads the grips
    public sealed class Steering : MonoBehaviour
    {
        public sealed class Mech
        {
            public string kind; public Transform root, moving, gripL, gripR, leverL, leverR;
            public float turn;   // the moving part's current angle (deg), for checks
        }
        ShiftVehicle v; FormSet forms; readonly Dictionary<Mode, Mech> mechs = new Dictionary<Mode, Mech>();
        float steer, pitch; string builtFor = "";
        public static readonly Dictionary<Mode, string> Defaults = new Dictionary<Mode, string> { { Mode.Drive, "wheel" }, { Mode.Boat, "helm" }, { Mode.Fly, "yoke" }, { Mode.Dive, "levers" } };
        public Mech Shown { get; private set; }
        public static bool ForceInput; public static float ForcedSteer, ForcedPitch;   // checks: the pictures at full left / centre / full right
        public Vector3 Anchor { get; private set; }   // in the pivot's space (the driver's hands in the Driving pose)
        public float Span { get; private set; } = .36f;   // the distance between her hands there

        public static Steering Make(ShiftVehicle v, Vector3 anchor, float span)
        {
            var s = v.gameObject.GetComponent<Steering>() ?? v.gameObject.AddComponent<Steering>(); s.v = v; s.forms = v.Forms;
            s.Anchor = anchor; s.Span = Mathf.Clamp(span, .25f, .5f); s.Build(); return s;
        }
        public string KindOf(Mode m) => mechs.TryGetValue(m, out var x) ? x.kind : "";
        public Mech Of(Mode m) => mechs.TryGetValue(m, out var x) ? x : null;

        public void Build()
        {
            foreach (var m in mechs.Values) if (m.root) Destroy(m.root.gameObject);
            mechs.Clear(); if (!forms) return; builtFor = forms.DesignName;
            var d = VehicleDesigns.Find(forms.DesignName);
            foreach (var m in FormSet.All)
            {
                int i = System.Array.IndexOf(FormSet.All, m);
                string kind = d != null && d.steering != null && i < d.steering.Length && !string.IsNullOrEmpty(d.steering[i]) ? d.steering[i] : Defaults[m];
                var at = Anchor + forms.SeatOf(m) - FormSet.GraySeat;
                var model = forms.ModelOf(m); if (model) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "Steering") at = forms.Pivot.InverseTransformPoint(t.position);
                var mech = Make(kind, forms.Pivot, at, Span); mech.root.gameObject.SetActive(false); mechs[m] = mech;
                foreach (var t in mech.root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = forms.gameObject.layer;
            }
        }

        static Material dark, metal, wood, grip, glow;
        static Material Dark => dark ? dark : dark = JurassicLook.Mat(new Color(.16f, .16f, .18f), true, default, .4f);
        static Material Metal => metal ? metal : metal = JurassicLook.Mat(new Color(.62f, .64f, .68f), true, default, .4f);
        static Material Wood => wood ? wood : wood = JurassicLook.Mat(new Color(.55f, .34f, .16f), true, default, .4f);
        static Material Grip => grip ? grip : grip = JurassicLook.Mat(new Color(.95f, .45f, .1f), true, default, .4f);
        static Material Glow => glow ? glow : glow = JurassicLook.Mat(new Color(.3f, .95f, 1f), false, new Color(.2f, .8f, 1f));
        static Transform Part(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Quaternion? rot = null, PrimitiveType shape = PrimitiveType.Cube)
        {
            var g = GameObject.CreatePrimitive(shape); g.name = name; Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localRotation = rot ?? Quaternion.identity; g.transform.localScale = size;
            var r = g.GetComponent<Renderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g.transform;
        }
        static Transform Empty(Transform parent, string name, Vector3 pos) { var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t; }
        // a ring of short bars (a wheel rim) in the local XY plane
        static void Ring(Transform parent, float r, float thick, int n, Material mat)
        {
            for (int i = 0; i < n; i++)
            {
                float a = (i + .5f) * 360f / n; var p = Quaternion.Euler(0, 0, a) * Vector3.right * r;
                Part(parent, "rim", p, new Vector3(thick, 2 * Mathf.PI * r / n * 1.08f, thick), mat, Quaternion.Euler(0, 0, a));
            }
        }

        // the mechanism's parts; its root sits at the hands' centre, facing the driver's forward (+Z), the grips at +-span/2
        public static Mech Make(string kind, Transform parent, Vector3 at, float span)
        {
            var m = new Mech { kind = kind }; float h = kind == "wheel" || kind == "helm" ? Mathf.Max(span * .5f, kind == "helm" ? .19f : .15f) : span * .5f;
            m.root = new GameObject("Steering " + kind).transform; m.root.SetParent(parent, false); m.root.localPosition = at;
            switch (kind)
            {
                case "helm":
                {
                    // upright ship's wheel a little ahead of the hands: rim through the grips, eight spokes with handles
                    var axle = Empty(m.root, "Helm axle", Vector3.zero); axle.localRotation = Quaternion.Euler(-8, 0, 0);
                    Part(axle, "Helm post", new Vector3(0, -.32f, .1f), new Vector3(.07f, .55f, .07f), Wood);
                    m.moving = Empty(axle, "Helm", new Vector3(0, 0, .02f));
                    Ring(m.moving, h, .035f, 20, Wood);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f; Part(m.moving, "spoke", Quaternion.Euler(0, 0, a) * Vector3.right * (h * .55f), new Vector3(h * 1.1f, .022f, .022f), Wood, Quaternion.Euler(0, 0, a));
                        Part(m.moving, "handle", Quaternion.Euler(0, 0, a) * Vector3.right * (h + .06f), new Vector3(.12f, .03f, .03f), Wood, Quaternion.Euler(0, 0, a));
                    }
                    Part(m.moving, "hub", Vector3.zero, new Vector3(.08f, .08f, .05f), Metal);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, 0, 0)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, 0, 0));
                    break;
                }
                case "yoke":
                {
                    // a column from the dash with a U-shaped yoke; rolls with the bank, slides with climb / dive
                    m.moving = Empty(m.root, "Yoke", Vector3.zero);
                    Part(m.root, "Yoke column", new Vector3(0, -.04f, .28f), new Vector3(.05f, .05f, .5f), Metal);
                    Part(m.moving, "Yoke bar", new Vector3(0, -.06f, 0), new Vector3(span, .045f, .045f), Dark);
                    foreach (float s in new[] { -1f, 1f }) Part(m.moving, "Yoke horn", new Vector3(s * h, 0, 0), new Vector3(.05f, .16f, .05f), Grip);
                    Part(m.moving, "Yoke badge", new Vector3(0, -.06f, -.03f), new Vector3(.08f, .05f, .01f), Glow);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, .02f, 0)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, .02f, 0));
                    break;
                }
                case "levers":
                {
                    // two throttle levers on a console between her knees, one per hand
                    Part(m.root, "Lever console", new Vector3(0, -.3f, .06f), new Vector3(span + .12f, .08f, .22f), Dark);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var pivot = Empty(m.root, s < 0 ? "Lever L" : "Lever R", new Vector3(s * h, -.27f, .06f));
                        Part(pivot, "lever", new Vector3(0, .14f, 0), new Vector3(.03f, .28f, .03f), Metal);
                        Part(pivot, "knob", new Vector3(0, .28f, 0), new Vector3(.07f, .07f, .07f), Grip, null, PrimitiveType.Sphere);
                        var g = Empty(pivot, s < 0 ? "Grip L" : "Grip R", new Vector3(0, .27f, 0));
                        if (s < 0) { m.leverL = pivot; m.gripL = g; } else { m.leverR = pivot; m.gripR = g; }
                    }
                    Part(m.root, "Sonar", new Vector3(0, -.22f, .18f), new Vector3(.12f, .08f, .01f), Glow);
                    m.moving = m.leverL;
                    break;
                }
                case "stick":
                {
                    // one centre stick between the knees, both hands on its grip
                    var pivot = Empty(m.root, "Stick", new Vector3(0, -.32f, 0)); m.moving = pivot;
                    Part(pivot, "stick", new Vector3(0, .15f, 0), new Vector3(.035f, .3f, .035f), Metal);
                    Part(pivot, "grip", new Vector3(0, .3f, 0), new Vector3(.06f, .12f, .06f), Grip);
                    m.gripL = Empty(pivot, "Grip L", new Vector3(-.04f, .3f, 0)); m.gripR = Empty(pivot, "Grip R", new Vector3(.04f, .32f, 0));
                    break;
                }
                case "handlebars":
                {
                    // a motorcycle's bars (Next Steps: the motorcycle design): turn about a raked head tube
                    var head = Empty(m.root, "Head tube", new Vector3(0, -.05f, .12f)); head.localRotation = Quaternion.Euler(-25, 0, 0);
                    m.moving = Empty(head, "Bars", Vector3.zero);
                    Part(m.moving, "bar", new Vector3(0, 0, -.1f), new Vector3(span + .1f, .03f, .03f), Metal);
                    foreach (float s in new[] { -1f, 1f }) Part(m.moving, "grip", new Vector3(s * h, 0, -.1f), new Vector3(.1f, .04f, .04f), Dark);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, 0, -.1f)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, 0, -.1f));
                    break;
                }
                default:   // "wheel"
                {
                    // a car steering wheel tilted back toward her on a column into the dash
                    m.kind = "wheel";
                    var col = Empty(m.root, "Wheel column", Vector3.zero); col.localRotation = Quaternion.Euler(28, 0, 0);   // the column runs forward and down into the dash
                    Part(col, "column", new Vector3(0, 0, .2f), new Vector3(.05f, .05f, .38f), Dark);
                    m.moving = Empty(col, "Wheel", Vector3.zero);
                    Ring(m.moving, h, .04f, 18, Dark);
                    foreach (float a in new[] { 0f, 180f, 270f }) Part(m.moving, "spoke", Quaternion.Euler(0, 0, a) * Vector3.right * (h * .5f), new Vector3(h, .03f, .02f), Metal, Quaternion.Euler(0, 0, a));
                    Part(m.moving, "hub", Vector3.zero, new Vector3(.1f, .1f, .04f), Grip);
                    Part(m.moving, "top mark", new Vector3(0, h, -.005f), new Vector3(.04f, .05f, .045f), Grip);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, .03f, 0)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, .03f, 0));
                    break;
                }
            }
            return m;
        }

        // which mechanism shows: the shown model's form (it swaps at the change's midpoint with the model)
        Mode ShownForm() { foreach (var m in FormSet.All) { var g = forms.ModelOf(m); if (g && g.activeSelf) return m; } return v.Mode; }

        void LateUpdate()
        {
            if (!v || !forms) return;
            if (builtFor != forms.DesignName) Build();
            var mode = ShownForm(); Mech now = null;
            foreach (var kv in mechs) { bool on = kv.Key == mode && forms.ShowDriver; if (kv.Value.root.gameObject.activeSelf != on) kv.Value.root.gameObject.SetActive(on); if (on) now = kv.Value; }
            Shown = now; if (now == null) return;
            float k = 1 - Mathf.Exp(-12f * Time.deltaTime);
            float wantSteer = v.Holding || v.Finished ? 0 : v.Input.Steer, wantPitch = v.Holding || v.Finished ? 0 : v.Input.Vertical;
            if (ForceInput && v.Human) { wantSteer = ForcedSteer; wantPitch = ForcedPitch; }
            steer = Mathf.Lerp(steer, wantSteer, k); pitch = Mathf.Lerp(pitch, wantPitch, k);
            switch (now.kind)
            {
                case "wheel": now.turn = -steer * 120f; now.moving.localRotation = Quaternion.Euler(0, 0, now.turn); break;
                case "helm": now.turn = -steer * 110f; now.moving.localRotation = Quaternion.Euler(0, 0, now.turn); break;
                case "handlebars": now.turn = steer * 30f; now.moving.localRotation = Quaternion.Euler(0, now.turn, 0); break;
                case "yoke": now.turn = -steer * 35f; now.moving.localRotation = Quaternion.Euler(-pitch * 12f, 0, now.turn); now.moving.localPosition = new Vector3(0, 0, pitch * .07f); break;
                case "stick": now.turn = steer * 22f; now.moving.localRotation = Quaternion.Euler(-pitch * 20f, 0, -now.turn); break;
                case "levers":
                {
                    // forward (a positive angle) to dive (pull back / K = up: vertical > 0 climbs), apart to turn
                    float l = Mathf.Clamp(-pitch + steer, -1, 1) * 22f, r = Mathf.Clamp(-pitch - steer, -1, 1) * 22f;
                    now.leverL.localRotation = Quaternion.Euler(l, 0, 0); now.leverR.localRotation = Quaternion.Euler(r, 0, 0); now.turn = l - r; break;
                }
            }
        }
    }
}
