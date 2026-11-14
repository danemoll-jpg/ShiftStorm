using UnityEngine;

namespace ShiftStorm
{
    // Round 5 item 1 (Dan: "still feels like I am moving slow"): movement you can see, per form.
    //   Drive / Fly: air streaks (the player only): thin lines that hang still in the world for a moment and are stretched by the camera's
    //                own motion, so they streak past faster the faster you go; from 45 % of the top speed, thicker on boost.
    //   Boat: spray from both sides of the stern, more with speed; Dive: a bubble trail. (Rivals too: a boat's spray, a sub's bubbles.)
    // All unlit (the Beacon shader: vertex colour, no fog), small counts (Steam Deck budget: at most ~250 live particles per vehicle).
    [RequireComponent(typeof(ShiftVehicle))]
    public sealed class SpeedFx : MonoBehaviour
    {
        ShiftVehicle v; ParticleSystem streaks, sprayL, sprayR, bubbles;
        public static bool Off;   // checks: the build 5 look
        public int Live => (streaks ? streaks.particleCount : 0) + (sprayL ? sprayL.particleCount + sprayR.particleCount : 0) + (bubbles ? bubbles.particleCount : 0);

        static Material mat;
        static Material Mat { get { if (!mat && GateMarker.Beacon) { mat = new Material(GateMarker.Beacon) { name = "SpeedFx" }; mat.SetColor("_Color", Color.white); mat.SetFloat("_Pulse", 0); } return mat; } }

        void Start()
        {
            v = GetComponent<ShiftVehicle>();
            if (v.Human)   // Round 6: both players in split screen
            {
                streaks = Make("Streaks", new Vector3(0, 1.8f, 16f), 300, false);
                var sh = streaks.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(18f, 9f, 26f);
                var m = streaks.main; m.startLifetime = .4f; m.startSpeed = 0; m.startSize3D = false; m.startSize = .14f; m.startColor = new Color(1, 1, 1, .6f);
                var r = streaks.GetComponent<ParticleSystemRenderer>(); r.renderMode = ParticleSystemRenderMode.Stretch; r.cameraVelocityScale = .06f; r.velocityScale = 0; r.lengthScale = 1f;
                Fade(streaks, .6f);
            }
            sprayL = Spray(-1); sprayR = Spray(1);
            bubbles = Make("Bubbles", new Vector3(0, 0, -2.1f), 160, true);
            { var sh = bubbles.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = .5f;
              var m = bubbles.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.4f); m.startSpeed = new ParticleSystem.MinMaxCurve(.5f, 2f); m.gravityModifier = -.25f; m.startSize = new ParticleSystem.MinMaxCurve(.12f, .35f); m.startColor = new Color(.8f, .97f, 1f, .55f);
              Fade(bubbles, .55f); }
        }

        ParticleSystem Spray(float side)
        {
            var p = Make(side < 0 ? "Spray L" : "Spray R", new Vector3(side * 1.0f, -.35f, -.9f), 120, true);
            p.transform.localRotation = Quaternion.Euler(-35, side * 120, 0);   // up, out and back
            var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 18; sh.radius = .25f;
            var m = p.main; m.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .65f); m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f); m.gravityModifier = 1.6f;
            m.startSize = new ParticleSystem.MinMaxCurve(.25f, .6f); m.startColor = new Color(.93f, .98f, 1f, .85f);
            Fade(p, .85f);
            return p;
        }

        ParticleSystem Make(string name, Vector3 local, int max, bool mesh)
        {
            var g = new GameObject(name); g.transform.SetParent(transform, false); g.transform.localPosition = local;
            var p = g.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = p.main; m.simulationSpace = ParticleSystemSimulationSpace.World; m.maxParticles = max; m.playOnAwake = false; m.loop = true; m.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var e = p.emission; e.rateOverTime = 0;
            var r = g.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (mesh) { r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = MeshKit.LowSphere; }
            p.Play();
            return p;
        }
        static void Fade(ParticleSystem p, float a)
        {
            var col = p.colorOverLifetime; col.enabled = true; var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(a, .15f), new GradientAlphaKey(0, 1) });
            col.color = gr;
        }

        void LateUpdate()
        {
            if (!v || !v.Body) return;
            float frac = v.Speed / Mathf.Max(1f, v.TopSpeedNow); bool racing = !v.Holding && !Off;
            if (streaks)
            {
                bool air = v.Mode == Mode.Drive || v.Mode == Mode.Fly;
                float k = Mathf.Clamp01((frac - .45f) / .55f);
                var e = streaks.emission; e.rateOverTime = racing && air ? k * (v.Mode == Mode.Fly ? 260 : 200) * (v.Boosting ? 1.6f : 1f) : 0;
                var m = streaks.main; m.startSize = v.Boosting ? .2f : .14f;
            }
            bool onWater = v.Mode == Mode.Boat && WaterVolume.SurfaceAt(v.Body.position, out float s, 2f) && v.Body.position.y < s + 1.6f;
            float spray = racing && onWater ? Mathf.Clamp01((v.Speed - 4f) / 40f) : 0;
            foreach (var sp in new[] { sprayL, sprayR }) { var e = sp.emission; e.rateOverTime = spray * 90 * (v.Boosting ? 1.4f : 1f); var m = sp.main; m.startSpeed = new ParticleSystem.MinMaxCurve(3f + 4f * spray, 6f + 8f * spray); }
            var be = bubbles.emission; be.rateOverTime = racing && v.Mode == Mode.Dive ? 25 + 70 * Mathf.Clamp01(frac) : 0;
        }
    }
}
