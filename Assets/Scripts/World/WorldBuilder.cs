using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Builds the courses at start-up from code (no hand-made scene). Round 10: Paradise Plunge (course 0) and Coaster Coast (course 1); the
    // Round 1 transition strip and the four mode lanes are gone (their checks run on Paradise Plunge, CheckPilot.Round10). Kept here: the
    // shared helpers (the change signs with the coming form's model).
    public static class WorldBuilder
    {
        static Transform root;
        static Material M(string n) => Resources.Load<Material>("Materials/" + n);

        public static List<Course> Build()
        {
            root = new GameObject("World").transform;
            var list = new List<Course> { ParadisePlunge.Build(root), CoasterCoast.Build(root), BigTop.Build(root) };   // Round 15: Big Top Breakdown
            foreach (var c in list) c.Finish();
            return list;
        }

        // ---------- helpers ----------
        static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, string mat, int layer = 0, bool collider = true, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = center; g.transform.localScale = size; g.transform.localEulerAngles = euler; g.layer = layer;
            var m = M(mat); if (m) g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        // A sign: post, colored board facing the oncoming racer, and the vehicle in the coming form, side-on.
        public static void Sign(Transform parent, Vector3 at, Mode form, float postHeight = 4)
        {
            var s = new GameObject($"Sign {form}").transform; s.SetParent(parent, false); s.localPosition = at;
            Box(s, "post", new Vector3(0, postHeight * .5f, 0), new Vector3(.3f, postHeight, .3f), "Gate", 0, false);
            string board = form == Mode.Drive ? "SignCar" : form == Mode.Fly ? "SignWing" : form == Mode.Boat ? "SignBoat" : "SignSub";
            Box(s, "board", new Vector3(0, postHeight + 1.6f, 0), new Vector3(6.5f, 3.2f, .2f), board, 0, false);
            var holder = new GameObject("symbol").transform; holder.SetParent(s, false); holder.localPosition = new Vector3(0, postHeight + 1.5f, -.6f); holder.localEulerAngles = new Vector3(0, 90, 0);
            VehicleFactory.MakeVisual(holder, form, form == Mode.Fly ? .62f : .8f);
        }
    }
}
