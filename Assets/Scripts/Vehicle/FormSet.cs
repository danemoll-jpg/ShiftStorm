using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 5 item 5: the vehicle as four whole models, one per form, swapped with an effect. Tripo gives one solid mesh per form, so a
    // change is no longer parts folding: over the change (ShiftTuning.changeSeconds, 0.6 s) the old model squashes and twirls a quarter
    // turn, at the midpoint it is swapped for the new one (with a burst: panels flying off in the body colour, plus sparkles in the air,
    // spray on the water, bubbles under it, dust on the road), and the new one stretches back out of the twirl with a small overshoot.
    // The physics hands over at the change's start as before (ShiftVehicle.Change, t = 0); this is looks only. One hull collider for
    // every form (ShiftVehicle), Abi sits on each model's Seat point (Driver follows SeatOffset).
    // A design (VehicleDesigns) gives each form a model; a form it does not have yet is the gray-box placeholder (the Blender part model of
    // Rounds 1-4, posed for that form by a FormAnimator, which also spins its wheels / propeller).
    public sealed class FormSet : MonoBehaviour
    {
        public Mode Current { get; private set; }
        public Mode Target { get; private set; }
        public bool Done { get; private set; } = true;
        public float StartedAt { get; private set; }
        public float VisualDoneAt { get; private set; }
        public float Seconds { get; private set; }
        public float T { get; private set; } = 1;
        public bool Swapped { get; private set; } = true;   // the model shown is the target's (from the midpoint on)
        public string DesignName { get; private set; } = VehicleDesigns.GrayBox;
        public Transform Pivot { get; private set; }        // squash / twirl happen here; the models and the driver sit under it
        public Vector3 SeatOffset { get; private set; }     // the shown model's seat, relative to the gray box's (Driver follows it)
        public static bool NoEffect;
        public bool Unscaled;                               // the Garage (game paused): the effect runs on unscaled time
        float Now => Unscaled ? Time.unscaledTime : Time.time;                        // checks: compare the change without the effect

        readonly Dictionary<Mode, GameObject> models = new Dictionary<Mode, GameObject>();
        readonly Dictionary<Mode, FormAnimator> grayParts = new Dictionary<Mode, FormAnimator>();
        readonly Dictionary<Mode, Vector3> seats = new Dictionary<Mode, Vector3>();
        Color body = new Color(.95f, .45f, .1f); bool showDriver = true; ParticleSystem burst;
        // Round 9 item 3c: a rival racing its own design (designs.json "rival"): that design always, in its own colours (Dan's texture),
        // recoloured to the rival's colour only while the player races the same design
        public string OwnDesign { get; private set; }
        public void SetOwnDesign(string design) { OwnDesign = design; Restyle(); }
        public void Restyle() { if (string.IsNullOrEmpty(OwnDesign)) Build(Settings.Design); else Build(OwnDesign); }
        Color TextureTint => !string.IsNullOrEmpty(OwnDesign) && OwnDesign != Settings.Design ? VehicleDesigns.PlayerTint : body;
        public VehicleDesigns.Design Design { get; private set; }
        public bool ShowDriver => showDriver;
        public static readonly Vector3 GraySeat = new Vector3(0, .17f, -.05f);
        public static readonly Mode[] All = { Mode.Drive, Mode.Boat, Mode.Fly, Mode.Dive };

        public static FormSet Make(Transform parent, string design, Color tint, Mode mode, float scale = 1f, bool effects = true)
        {
            var g = new GameObject("Forms"); g.transform.SetParent(parent, false); g.transform.localScale = Vector3.one * scale;
            var set = g.AddComponent<FormSet>(); set.body = tint;
            set.Pivot = new GameObject("Pivot").transform; set.Pivot.SetParent(g.transform, false);
            set.Build(design);
            if (effects) set.MakeBurst();
            set.Snap(mode);
            return set;
        }

        // (re)build the four models for a design; keeps the form shown
        public void Build(string design)
        {
            foreach (var m in models.Values) if (m) Destroy(m);
            models.Clear(); grayParts.Clear(); seats.Clear();
            var d = VehicleDesigns.Find(design); DesignName = d != null ? d.name : VehicleDesigns.GrayBox; showDriver = d == null || d.driver; Design = d;
            foreach (var m in All)
            {
                GameObject go = null; Vector3 seat = GraySeat;
                if (d != null && d.Has(m)) go = VehicleDesigns.Instantiate(d, m, Pivot, TextureTint, out seat);
                if (!go) { var fa = VehicleFactory.MakeGrayForm(Pivot, m, body); go = fa.gameObject; grayParts[m] = fa; seat = GraySeat; }
                go.name = $"Form {m}"; go.SetActive(false); models[m] = go; seats[m] = seat;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            int layer = gameObject.layer; foreach (var t in Pivot.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            Show(Swapped ? Target : Current);
        }

        // the gray box's named parts (the Driver measures the canopy on the Dive form, which always has the canopy closed)
        public Transform PartTransform(string name) => grayParts.TryGetValue(Mode.Dive, out var fa) ? fa.PartTransform(name) : (grayParts.Count > 0 ? First().PartTransform(name) : null);
        FormAnimator First() { foreach (var v in grayParts.Values) return v; return null; }
        public int PartCount => grayParts.TryGetValue(Mode.Drive, out var fa) ? fa.PartCount : 0;
        public GameObject ModelOf(Mode m) => models.TryGetValue(m, out var g) ? g : null;
        public bool IsImported(Mode m) => !grayParts.ContainsKey(m);
        public Vector3 SeatOf(Mode m) => seats.TryGetValue(m, out var s) ? s : GraySeat;   // Round 8: each form's seat (the steering mechanism)
        public Mode ShownMode { get { foreach (var kv in models) if (kv.Value && kv.Value.activeSelf) return kv.Key; return Swapped ? Target : Current; } }   // Round 9

        void Show(Mode m)
        {
            foreach (var kv in models) if (kv.Value) kv.Value.SetActive(kv.Key == m);
            SeatOffset = (seats.TryGetValue(m, out var s) ? s : GraySeat) - GraySeat;
        }

        public void Snap(Mode m)
        {
            Current = Target = m; Done = true; Swapped = true; T = 1; Seconds = 0; VisualDoneAt = Now;
            Show(m); Pivot.localScale = Vector3.one; Pivot.localRotation = Quaternion.identity;
        }

        public void Begin(Mode m, float seconds)
        {
            if (!Done && !Swapped) Show(Current);
            Current = Swapped ? Target : Current; Target = m; Seconds = seconds; StartedAt = Now; T = 0; Done = false; Swapped = false;
            if (NoEffect) { Show(m); Swapped = true; }
        }

        void LateUpdate()
        {
            if (Done) return;
            T = Seconds > 0 ? Mathf.Clamp01((Now - StartedAt) / Seconds) : 1;
            if (!Swapped && T >= .5f) { Show(Target); Swapped = true; Burst(Target); }
            if (NoEffect) { Pivot.localScale = Vector3.one; Pivot.localRotation = Quaternion.identity; }
            else if (T < .5f)
            {
                float k = T / .5f, e = k * k;                         // ease in: squash down and twirl out
                Pivot.localScale = new Vector3(1 + .25f * e, 1 - .45f * e, 1 + .1f * e);
                Pivot.localRotation = Quaternion.Euler(0, 90f * e, 0);
            }
            else
            {
                float k = (T - .5f) / .5f, e = 1 - (1 - k) * (1 - k);  // ease out, with a small overshoot on the stretch
                float over = Mathf.Sin(k * Mathf.PI) * .12f;
                Pivot.localScale = new Vector3(Mathf.Lerp(.8f, 1, e) - over * .5f, Mathf.Lerp(1.35f, 1, e) + over, Mathf.Lerp(.9f, 1, e));
                Pivot.localRotation = Quaternion.Euler(0, -90f * (1 - e), 0);
            }
            if (T >= 1) { Done = true; Current = Target; VisualDoneAt = Now; Pivot.localScale = Vector3.one; Pivot.localRotation = Quaternion.identity; }
        }

        // ---- the burst at the swap ----
        void MakeBurst()
        {
            var g = new GameObject("Change burst"); g.transform.SetParent(transform, false);
            burst = g.AddComponent<ParticleSystem>(); burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = burst.main; m.simulationSpace = ParticleSystemSimulationSpace.World; m.useUnscaledTime = Unscaled; m.maxParticles = 120; m.playOnAwake = false; m.loop = false; m.startLifetime = .7f;
            m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var e = burst.emission; e.rateOverTime = 0;
            var sh = burst.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1.2f;
            var rot = burst.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            var col = burst.colorOverLifetime; col.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .6f), new GradientAlphaKey(0, 1) }); col.color = gr;
            var sz = burst.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, .2f));
            var r = g.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = MeshKit.Cube; r.alignment = ParticleSystemRenderSpace.Local;
            r.sharedMaterial = SpeedFxMaterial; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            burst.Play();
        }
        static Material fxMat;
        static Material SpeedFxMaterial { get { if (!fxMat && GateMarker.Beacon) { fxMat = new Material(GateMarker.Beacon) { name = "Change burst" }; fxMat.SetColor("_Color", Color.white); } return fxMat; } }
        public int BurstsPlayed { get; private set; }

        void Burst(Mode to)
        {
            if (!burst || NoEffect) return; BurstsPlayed++; var mm = burst.main; mm.useUnscaledTime = Unscaled;
            var ep = new ParticleSystem.EmitParams(); var vel = GetComponentInParent<Rigidbody>() is Rigidbody rb ? rb.linearVelocity : Vector3.zero;
            // panels: body-coloured plates thrown off and tumbling
            for (int i = 0; i < 10; i++)
            {
                var dir = Random.onUnitSphere; dir.y = Mathf.Abs(dir.y) * .7f + .2f;
                ep.position = transform.position + dir * 1.1f; ep.velocity = vel * .85f + dir * Random.Range(4f, 8f); ep.startColor = i % 3 == 0 ? new Color(.97f, .95f, .9f) : body;
                ep.startSize3D = new Vector3(Random.Range(.5f, .9f), .06f, Random.Range(.35f, .6f)); ep.rotation3D = Random.insideUnitSphere * 180; ep.startLifetime = .55f;
                burst.Emit(ep, 1);
            }
            // per form: sparkles (air, road), spray (water), bubbles (under it)
            Color c = to == Mode.Fly ? new Color(1f, .9f, .45f) : to == Mode.Boat ? new Color(.92f, .98f, 1f) : to == Mode.Dive ? new Color(.75f, .95f, 1f, .7f) : new Color(1f, .8f, .45f);
            int n = to == Mode.Boat ? 40 : 30;
            for (int i = 0; i < n; i++)
            {
                var dir = Random.onUnitSphere; if (to == Mode.Boat) dir = new Vector3(dir.x, Mathf.Abs(dir.y) + .6f, dir.z).normalized;
                ep.position = transform.position + dir * .8f;
                ep.velocity = vel * .9f + dir * (to == Mode.Dive ? Random.Range(1f, 3f) : Random.Range(5f, 11f)) + (to == Mode.Dive ? Vector3.up * 2f : Vector3.zero);
                float s = to == Mode.Dive ? Random.Range(.15f, .35f) : Random.Range(.12f, .25f); ep.startSize3D = new Vector3(s, s, s);
                ep.startColor = c; ep.rotation3D = Random.insideUnitSphere * 180; ep.startLifetime = Random.Range(.4f, .8f);
                burst.Emit(ep, 1);
            }
        }
    }
}
