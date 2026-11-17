using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 14 item 1: behind the front menus (the title, the main menu and the pages it opens, not the Garage's turntable) the camera
    // flies slowly along the course the menus are on (the one chosen on the course page: it is shown there as the cards change), high
    // and to one side, looking ahead down the route. Unscaled time: the race behind is frozen.
    [DefaultExecutionOrder(100)]
    public sealed class MenuCam : MonoBehaviour
    {
        public static bool On { get; private set; }
        public static bool Hold; public static float Step = -1;   // checks: held still; a fixed time step per frame (1/60: the Deck's frames)
        Camera cam; float s = -1; int course = -1; Vector3 pos; Quaternion rot; bool fresh = true;
        void Awake() => cam = GetComponent<Camera>();
        void LateUpdate()
        {
            var g = Game.Instance; bool on = g && g.MenuOpen && g.InFront && !Garage.Active && g.Courses != null;
            if (!on) { if (On) fresh = true; On = false; return; }
            On = true;
            var c = g.Courses[Mathf.Clamp(g.MenuCourse, 0, g.Courses.Count - 1)];
            if (CourseLook.Shown != c) CourseLook.Apply(c);
            if (course != g.MenuCourse) { course = g.MenuCourse; s = Mathf.Min(120f, c.Length * .05f); fresh = true; }
            float dt = Step > 0 ? Step : Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (Hold) { transform.SetPositionAndRotation(pos, rot); return; }
            s += 22f * dt; if (s > c.Length - 260f) { s = 60f; fresh = true; }
            var p = c.PointAt(s, out var d); var flat = new Vector3(d.x, 0, d.z); flat = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward; var right = Vector3.Cross(Vector3.up, flat);
            var want = p + Vector3.up * 34f + right * 26f - flat * 30f; want.y = Mathf.Max(want.y, Mathf.Max(p.y, 0f) + 24f);
            var look = c.PointAt(s + 140f, out _); look.y = Mathf.Max(look.y, -2f);
            var wantRot = Quaternion.LookRotation(look - want, Vector3.up);
            if (fresh) { pos = want; rot = wantRot; fresh = false; }
            float k = 1 - Mathf.Exp(-1.2f * dt);
            pos = Vector3.Lerp(pos, want, k); rot = Quaternion.Slerp(rot, wantRot, k);
            transform.SetPositionAndRotation(pos, rot);
            if (cam) { cam.fieldOfView = 58f; cam.nearClipPlane = .2f; }
        }
    }

    // Round 14 item 1: each course's picture for the course cards (rendered once at start-up from a viewpoint over its signature stretch:
    // Paradise Plunge's stunt strip and loop, Coaster Coast's Ferris wheel), and the forms it uses
    public static class CourseCards
    {
        static readonly Dictionary<int, RenderTexture> shots = new Dictionary<int, RenderTexture>();
        public static Texture Get(int i) => shots.TryGetValue(i, out var t) ? t : null;
        public static void Make(Game g)
        {
            var go = new GameObject("Course card camera"); var cam = go.AddComponent<Camera>(); cam.enabled = false;
            cam.CopyFrom(g.Cam); cam.targetTexture = null; cam.fieldOfView = 55; cam.farClipPlane = 2400;
            var keep = CourseLook.Shown;
            for (int i = 0; i < g.Courses.Count; i++)
            {
                var c = g.Courses[i]; CourseLook.Apply(c);
                float L = c.Length, s; Vector3 target;
                var m = c.marks;
                if (m != null && m.ContainsKey("wheel") && m.ContainsKey("jM")) { s = m["jM"] - 150; target = CoasterCoast.WheelCentre; }
                else if (m != null && m.ContainsKey("loopIn")) { s = m["loopIn"] - 170; target = c.PointAt(m["loopIn"], out _) + Vector3.up * 14; }
                else { s = L * .1f; target = c.PointAt(s + 200, out _); }
                var p = c.PointAt(s, out var d); var flat = new Vector3(d.x, 0, d.z).normalized; var right = Vector3.Cross(Vector3.up, flat);
                var at = p + Vector3.up * 16 + right * 22 - flat * 10;
                go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(target - at, Vector3.up));
                var rt = new RenderTexture(640, 360, 24) { name = "Course card " + c.courseName }; cam.targetTexture = rt;
                RenderSettings.fog = RenderSettings.fog;   // the course's own above-water fog (CourseLook set it)
                cam.Render(); cam.targetTexture = null; shots[i] = rt;
            }
            CourseLook.Apply(keep ? keep : g.Courses[g.CourseIndex]);
            Object.Destroy(go);
            Debug.Log($"[Menu] course cards rendered: {shots.Count}");
        }
        public static string Forms(Course c)
        {
            var set = new HashSet<Mode> { c.startMode };
            foreach (var z in c.GetComponentsInChildren<TransformZone>(true)) set.Add(z.target);
            var l = new List<string>(); foreach (var f in FormSet.All) if (set.Contains(f)) l.Add(Game.FormName(f));
            return string.Join("  •  ", l);
        }
    }
}
