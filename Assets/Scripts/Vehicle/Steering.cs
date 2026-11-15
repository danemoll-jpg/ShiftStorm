using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 4: a visible steering mechanism per form, in front of the driver, moved by the racer's own input, with the driver's hands
    // on it (Driver: two-bone arm IK to the two grips). One per form, swapped with the model at the change's midpoint (they live under the
    // FormSet's pivot, so they squash and twirl with it):
    //   car   "wheel"   a steering wheel on a raked column: turns 120 deg each way with the steering
    //   boat  "helm"    a ship's wheel with spokes and handles: turns 110 deg each way
    //   plane "yoke"    a control yoke on a column: rolls 35 deg with the bank (steering), pushes / pulls with climb / dive
    //   sub   "levers"  twin throttle levers, one per hand: forward together to dive, back to climb, apart to turn
    //   also  "stick" (a single centre stick), "handlebars" (a motorcycle's bars that turn 30 deg) and "grips" (only the grips, on a model's own
    //         bars: the Scrambler, Round 9)
    // A design can bring its own per form: designs.json "steering": ["wheel", "helm", "yoke", "levers"] (Car, Boat, Plane, Sub order), and an
    // empty named "Steering" in a form's model places it there; otherwise it sits where it is placed from the driver's eyes (below).
    // Round 9 item 1 (Dan: "The steering wheel is far too big. It takes up too much of the screen when you choose first person pov"): every
    // mechanism at real proportions to the driver (the sizes here are for Abi at her scale 0.69, ~1.2 m tall: a 0.26 m wheel is a real
    // 0.38 m one) and placed from her eyes, low in the view: the wheel's top ~30 deg under the first-person camera's axis, so it covers
    // ~15-20 % of the screen height and the road stays clear. Build 10: a 0.30 m wheel at eye level ~0.35 m ahead, two thirds of the screen.
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
        public Vector3 Eye { get; private set; }      // in the pivot's space: the driver's eyes in the Driving pose (reports)
        public Vector3 Reach { get; private set; }    // in the pivot's space: where her hands reach comfortably (the Driving clip's hands, Round 8)
        public float DriverScale { get; private set; } = .69f;
        public Vector3 Anchor => Reach + Offset("wheel", DriverScale);
        public float Span => 2 * Half("wheel") * DriverScale / .69f;

        public static Steering Make(ShiftVehicle v, Vector3 eye, Vector3 reach, float driverScale)
        {
            var s = v.gameObject.GetComponent<Steering>() ?? v.gameObject.AddComponent<Steering>(); s.v = v; s.forms = v.Forms;
            s.Eye = eye; s.Reach = reach; s.DriverScale = Mathf.Clamp(driverScale, .45f, 1.1f); s.Build(); return s;
        }
        // per kind: the mechanism's centre from where her hands reach, for a 0.69 driver: a little lower and nearer than build 10's, so its top
        // sits ~27 deg under the first-person camera's axis (the camera now 12 cm over her eyes: CameraViews.FirstPerson) and her short cartoon
        // arms still hold it
        static Vector3 Place(string kind)
        {
            switch (kind)
            {
                case "helm": return new Vector3(0, -.16f, -.02f);
                case "yoke": return new Vector3(0, -.06f, .05f);
                case "levers": return new Vector3(0, -.05f, .04f);
                case "stick": return new Vector3(0, -.06f, 0);
                case "handlebars": return new Vector3(0, -.03f, 0);
                default: return new Vector3(0, -.08f, -.04f);
            }
        }
        // the grips' half spacing (m, for a 0.69 driver)
        public static float Half(string kind)
        {
            switch (kind) { case "grips": return .28f; case "helm": return .125f; case "yoke": return .14f; case "levers": return .1f; case "stick": return .03f; case "handlebars": return .2f; default: return .13f; }
        }
        public static Vector3 Offset(string kind, float scale) => Place(kind) * (scale / .69f);
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
                if (kind == "none") continue;
                var at = Reach + Offset(kind, DriverScale) + forms.SeatOf(m) - FormSet.GraySeat;
                var model = forms.ModelOf(m); if (model) foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "Steering") at = forms.Pivot.InverseTransformPoint(t.position);
                var mech = Make(kind, forms.Pivot, at, DriverScale / .69f * (d != null && d.riderScale > 0 ? d.riderScale : 1f)); mech.root.gameObject.SetActive(false); mechs[m] = mech;   // Round 9: sized with the rider
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

        // the mechanism's parts; its root sits at its centre, facing the driver's forward (+Z); k scales it with the driver
        public static Mech Make(string kind, Transform parent, Vector3 at, float k)
        {
            var m = new Mech { kind = kind }; float h = Half(kind), span = 2 * h;
            m.root = new GameObject("Steering " + kind).transform; m.root.SetParent(parent, false); m.root.localPosition = at; m.root.localScale = Vector3.one * k;
            // the round ones are held at "ten to two" (the hands stay in view, low on the screen)
            var ten = new Vector3(-h * .9063f, h * .4226f, 0); var two = new Vector3(h * .9063f, h * .4226f, 0);
            switch (kind)
            {
                case "helm":
                {
                    // a ship's wheel a little ahead of the hands: rim through the grips, eight spokes with handles
                    var axle = Empty(m.root, "Helm axle", Vector3.zero); axle.localRotation = Quaternion.Euler(14, 0, 0);
                    Part(axle, "Helm post", new Vector3(0, -.2f, .08f), new Vector3(.05f, .36f, .05f), Wood);
                    m.moving = Empty(axle, "Helm", new Vector3(0, 0, .02f));
                    Ring(m.moving, h, .025f, 20, Wood);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * 45f; Part(m.moving, "spoke", Quaternion.Euler(0, 0, a) * Vector3.right * (h * .55f), new Vector3(h * 1.1f, .016f, .016f), Wood, Quaternion.Euler(0, 0, a));
                        Part(m.moving, "handle", Quaternion.Euler(0, 0, a) * Vector3.right * (h + .025f), new Vector3(.05f, .018f, .018f), Wood, Quaternion.Euler(0, 0, a));
                    }
                    Part(m.moving, "hub", Vector3.zero, new Vector3(.06f, .06f, .04f), Metal);
                    m.gripL = Empty(m.moving, "Grip L", ten); m.gripR = Empty(m.moving, "Grip R", two);
                    break;
                }
                case "yoke":
                {
                    // a column from the dash with a U-shaped yoke; rolls with the bank, slides with climb / dive
                    m.moving = Empty(m.root, "Yoke", Vector3.zero);
                    Part(m.root, "Yoke column", new Vector3(0, -.03f, .09f), new Vector3(.035f, .035f, .14f), Dark);   // short and dark: build 10's long metal one read as a road line through the glass
                    Part(m.moving, "Yoke bar", new Vector3(0, -.045f, 0), new Vector3(span, .032f, .032f), Dark);
                    foreach (float s in new[] { -1f, 1f }) Part(m.moving, "Yoke horn", new Vector3(s * h, 0, 0), new Vector3(.035f, .11f, .035f), Grip);
                    Part(m.moving, "Yoke badge", new Vector3(0, -.045f, -.02f), new Vector3(.06f, .035f, .01f), Glow);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, .015f, 0)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, .015f, 0));
                    break;
                }
                case "levers":
                {
                    // two throttle levers on a console between her knees, one per hand
                    Part(m.root, "Lever console", new Vector3(0, -.17f, .04f), new Vector3(span + .08f, .05f, .14f), Dark);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var pivot = Empty(m.root, s < 0 ? "Lever L" : "Lever R", new Vector3(s * h, -.15f, .04f));
                        Part(pivot, "lever", new Vector3(0, .08f, 0), new Vector3(.02f, .16f, .02f), Metal);
                        Part(pivot, "knob", new Vector3(0, .16f, 0), new Vector3(.045f, .045f, .045f), Grip, null, PrimitiveType.Sphere);
                        var g = Empty(pivot, s < 0 ? "Grip L" : "Grip R", new Vector3(0, .155f, 0));
                        if (s < 0) { m.leverL = pivot; m.gripL = g; } else { m.leverR = pivot; m.gripR = g; }
                    }
                    Part(m.root, "Sonar", new Vector3(0, -.13f, .11f), new Vector3(.08f, .05f, .01f), Glow);
                    m.moving = m.leverL;
                    break;
                }
                case "stick":
                {
                    // one centre stick between the knees, both hands on its grip
                    var pivot = Empty(m.root, "Stick", new Vector3(0, -.2f, 0)); m.moving = pivot;
                    Part(pivot, "stick", new Vector3(0, .1f, 0), new Vector3(.025f, .2f, .025f), Metal);
                    Part(pivot, "grip", new Vector3(0, .2f, 0), new Vector3(.04f, .08f, .04f), Grip);
                    m.gripL = Empty(pivot, "Grip L", new Vector3(-.03f, .2f, 0)); m.gripR = Empty(pivot, "Grip R", new Vector3(.03f, .22f, 0));
                    break;
                }
                case "grips":
                {
                    // Round 9: the model's own handlebars (the Scrambler: Dan's bikes have them): no parts of ours, only the two grips her hands
                    // hold, turning a little about the bars' centre with the steering (designs.json "steering" "grips"; the "Steering" point
                    // of each form is the grips' centre)
                    m.moving = Empty(m.root, "Grips", Vector3.zero);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, 0, 0)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, 0, 0));
                    break;
                }
                case "handlebars":
                {
                    // a motorcycle's bars (the Scrambler, Round 9): a raked head tube, the bars swept back to the grips, turning about the tube
                    var head = Empty(m.root, "Head tube", new Vector3(0, -.04f, .1f)); head.localRotation = Quaternion.Euler(-25, 0, 0);
                    Part(head, "head tube", new Vector3(0, -.12f, 0), new Vector3(.04f, .22f, .04f), Dark);
                    m.moving = Empty(head, "Bars", Vector3.zero);
                    Part(m.moving, "bar", new Vector3(0, 0, -.06f), new Vector3(span * .8f, .022f, .022f), Metal);
                    foreach (float s in new[] { -1f, 1f })
                    {
                        Part(m.moving, "bar end", new Vector3(s * h * .9f, 0, -.09f), new Vector3(.022f, .022f, .07f), Metal, Quaternion.Euler(0, s * 30f, 0));
                        Part(m.moving, "grip", new Vector3(s * h, 0, -.11f), new Vector3(.08f, .03f, .03f), Grip);
                    }
                    Part(m.moving, "clocks", new Vector3(0, .03f, -.02f), new Vector3(.06f, .04f, .02f), Glow);
                    m.gripL = Empty(m.moving, "Grip L", new Vector3(-h, 0, -.11f)); m.gripR = Empty(m.moving, "Grip R", new Vector3(h, 0, -.11f));
                    break;
                }
                default:   // "wheel"
                {
                    // a car steering wheel tilted back toward her on a column into the dash
                    m.kind = "wheel";
                    var col = Empty(m.root, "Wheel column", Vector3.zero); col.localRotation = Quaternion.Euler(28, 0, 0);   // the column runs forward and down into the dash
                    Part(col, "column", new Vector3(0, 0, .15f), new Vector3(.035f, .035f, .28f), Dark);
                    m.moving = Empty(col, "Wheel", Vector3.zero);
                    Ring(m.moving, h, .026f, 20, Dark);
                    foreach (float a in new[] { 0f, 180f, 270f }) Part(m.moving, "spoke", Quaternion.Euler(0, 0, a) * Vector3.right * (h * .5f), new Vector3(h, .02f, .014f), Metal, Quaternion.Euler(0, 0, a));
                    Part(m.moving, "hub", Vector3.zero, new Vector3(.065f, .065f, .03f), Grip);
                    Part(m.moving, "top mark", new Vector3(0, h, -.004f), new Vector3(.025f, .032f, .03f), Grip);
                    m.gripL = Empty(m.moving, "Grip L", ten); m.gripR = Empty(m.moving, "Grip R", two);
                    break;
                }
            }
            return m;
        }

        // checks: the share of the camera's screen height the shown mechanism covers (its parts' boxes projected, clipped to the screen)
        public float ScreenShare(Camera cam)
        {
            if (Shown == null || !cam) return 0;
            float lo = 1, hi = 0; bool any = false;
            foreach (var r in Shown.root.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) continue; var b = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var vp = cam.WorldToViewportPoint(r.transform.TransformPoint(c)); if (vp.z <= 0) continue;
                    if (vp.x < -.2f || vp.x > 1.2f) continue;
                    lo = Mathf.Min(lo, vp.y); hi = Mathf.Max(hi, vp.y); any = true;
                }
            }
            if (!any) return 0;
            return Mathf.Clamp01(Mathf.Min(hi, 1) - Mathf.Max(lo, 0));
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
                case "grips": now.turn = steer * 8f; now.moving.localRotation = Quaternion.Euler(0, now.turn, 0); break;
                case "yoke": now.turn = -steer * 35f; now.moving.localRotation = Quaternion.Euler(-pitch * 12f, 0, now.turn); now.moving.localPosition = new Vector3(0, 0, pitch * .05f); break;
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
