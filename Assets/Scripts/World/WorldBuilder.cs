using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Builds the Round 1 gray boxes at start-up from code (no hand-made scene): the transition test strip (road -> launch ramp -> air -> water
    // landing corridor -> boat section with a current -> dive portal -> underwater stretch -> surfacing pool -> water ramp -> road landing) and
    // four flat lanes, one per mode. All sizes are in metres; the strip runs along +Z at x = 0. Signs before each change carry a small model of
    // the vehicle in the form it is about to take (car, wing, boat, submarine).
    public static class WorldBuilder
    {
        static Transform root, tests;
        public const float TestOffsetX = 3000f;
        static Material M(string n) => Resources.Load<Material>("Materials/" + n);

        public static List<Course> Build()
        {
            root = new GameObject("World (gray boxes)").transform;
            // Round 2: Paradise Plunge first (course 0); the Round 1 strip and lanes follow for testing, moved 3 km east, clear of the lap
            var lap = ParadisePlunge.Build(root);
            tests = new GameObject("Test courses (Round 1)").transform; tests.SetParent(root, false); tests.position = new Vector3(TestOffsetX, 0, 0);
            var list = new List<Course> { lap, BuildStrip(), DriveLane(new Vector3(400, 0, 0)), BoatLane(new Vector3(520, 0, 0)), FlyLane(new Vector3(640, 0, 0)), DiveLane(new Vector3(760, 0, 0)) };
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
        static GameObject Cyl(Transform parent, string name, Vector3 center, float radius, float height, string mat, int layer = 0, bool collider = true)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = center; g.transform.localScale = new Vector3(radius * 2, height * .5f, radius * 2); g.layer = layer;
            var m = M(mat); if (m) g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.Destroy(g.GetComponent<Collider>()); else { Object.Destroy(g.GetComponent<Collider>()); var c = g.AddComponent<CapsuleCollider>(); c.radius = .5f; c.height = 2; }
            return g;
        }
        // A slab from a to b (both its top-center points), width w, thickness 1.
        static GameObject Slab(Transform parent, string name, Vector3 a, Vector3 b, float w, string mat)
        {
            var d = b - a; float len = d.magnitude; var rot = Quaternion.LookRotation(d / len, Vector3.up);
            var g = Box(parent, name, Vector3.zero, new Vector3(w, 1, len + .4f), mat);
            g.transform.localRotation = rot; g.transform.localPosition = (a + b) * .5f - rot * Vector3.up * .5f;
            return g;
        }
        static T Vol<T>(Transform parent, string name, Vector3 center, Vector3 size) where T : BoxVolume
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false); g.transform.localPosition = center;
            var v = g.AddComponent<T>(); v.size = size; return v;
        }
        static void Water(Transform parent, string name, Vector3 center, Vector3 size, bool surfaceVisible = true)
        {
            Vol<WaterVolume>(parent, name, center, size);
            if (!surfaceVisible) return;
            var top = Box(parent, name + " surface", center + Vector3.up * (size.y * .5f - .02f), new Vector3(size.x, .04f, size.z), "Water", 0, false);
            top.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Zone(Transform parent, string name, Vector3 center, Vector3 size, Mode target, bool corridor = false)
        {
            var z = Vol<TransformZone>(parent, name, center, size); z.target = target; z.landingCorridor = corridor; z.label = name;
        }
        static void Anchor(Course c, Vector3 at, Mode mode, float yaw = 0)
        {
            var g = new GameObject($"Anchor {mode} {at.z:0}"); g.transform.SetParent(c.transform, false); g.transform.localPosition = at; g.transform.localEulerAngles = new Vector3(0, yaw, 0);
            var a = g.AddComponent<RecoveryAnchor>(); a.mode = mode; c.anchors.Add(a);
        }
        static void Gate(Course c, int index, string label, Vector3 at, Vector2 half)
        {
            var g = new GameObject("Gate " + label); g.transform.SetParent(c.transform, false); g.transform.localPosition = at;
            var pg = g.AddComponent<ProgressGate>(); pg.index = index; pg.label = label; pg.halfSize = half; c.gates.Add(pg);
            // two thin posts mark it (visual only)
            Box(g.transform, "post L", new Vector3(-half.x + 1, 0, 0), new Vector3(.4f, Mathf.Min(half.y * 2, 14), .4f), "Gate", 0, false);
            Box(g.transform, "post R", new Vector3(half.x - 1, 0, 0), new Vector3(.4f, Mathf.Min(half.y * 2, 14), .4f), "Gate", 0, false);
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
        static void Ring(Transform parent, Vector3 at, float r)
        {
            for (int i = 0; i < 12; i++) { float a = i * 30 * Mathf.Deg2Rad; Box(parent, "ring", at + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0), new Vector3(.6f, r * .55f, .6f), "Gate", 0, false, new Vector3(0, 0, i * 30)); }
        }
        static Course NewCourse(string name, Vector3 origin)
        {
            var g = new GameObject("Course " + name); g.transform.SetParent(tests, false); g.transform.localPosition = origin;
            var c = g.AddComponent<Course>(); c.courseName = name; return c;
        }
        static void Line(Course c, params Vector3[] pts) { foreach (var p in pts) c.line.Add(c.transform.TransformPoint(p)); }

        // ---------- the transition test strip ----------
        static Course BuildStrip()
        {
            var c = NewCourse("Transition strip", Vector3.zero); var t = c.transform;
            c.halfWidth = 46; c.minY = -20; c.maxY = 45; c.startMode = Mode.Drive;
            Line(c, new Vector3(0, .9f, -10), new Vector3(0, .9f, 200), new Vector3(0, 10.9f, 240), new Vector3(0, 14, 330), new Vector3(0, 10, 430),
                    new Vector3(0, -1.5f, 540), new Vector3(0, -1.5f, 862), new Vector3(0, -8.5f, 905), new Vector3(0, -9, 1070), new Vector3(0, -2.5f, 1100),
                    new Vector3(0, -1.5f, 1160), new Vector3(0, 3.5f, 1200), new Vector3(0, 1, 1240), new Vector3(0, 1, 1500));
            // 1 road (z -20..200) with grass either side and low barriers
            Box(t, "Road", new Vector3(0, -.5f, 90), new Vector3(16, 1, 220), "Road");
            Box(t, "Grass", new Vector3(0, -.56f, 110), new Vector3(92, 1, 260), "Ground");
            Box(t, "Barrier L", new Vector3(-9, .4f, 90), new Vector3(.6f, .8f, 220), "Barrier", ShiftVehicle.ObstacleLayer);
            Box(t, "Barrier R", new Vector3(9, .4f, 90), new Vector3(.6f, .8f, 220), "Barrier", ShiftVehicle.ObstacleLayer);
            Sign(t, new Vector3(-12, 0, 150), Mode.Fly);
            // 2 launch ramp (z 200 -> 240, up to y 10: 14 degrees) and wind streamers
            Slab(t, "Launch ramp", new Vector3(0, -.05f, 199.5f), new Vector3(0, 10, 240), 16, "Ramp");
            Box(t, "Ramp support", new Vector3(0, 4, 237), new Vector3(14, 8, 4), "Rock");
            for (int i = 0; i < 4; i++) { Box(t, "streamer", new Vector3(-9, 6 + i, 205 + i * 9), new Vector3(.15f, .15f, 6), "SignWing", 0, false); Box(t, "streamer", new Vector3(9, 6 + i, 205 + i * 9), new Vector3(.15f, .15f, 6), "SignWing", 0, false); }
            Zone(t, "Wings out (ramp lip)", new Vector3(0, 16, 247), new Vector3(30, 16, 14), Mode.Fly);
            // 3 air over a canyon (z 240..520), two rings to aim through
            Box(t, "Canyon floor", new Vector3(0, -25, 380), new Vector3(120, 2, 290), "Rock");
            for (int i = 0; i < 6; i++) { float z = 270 + i * 45; Box(t, "Canyon wall", new Vector3(-58, -5, z), new Vector3(8, 40, 30), "Rock"); Box(t, "Canyon wall", new Vector3(58, -5, z), new Vector3(8, 40, 30), "Rock"); }
            Ring(t, new Vector3(0, 14, 330), 8); Ring(t, new Vector3(0, 10, 430), 8);
            Sign(t, new Vector3(-16, -2, 460), Mode.Boat, 12);
            // 4 lagoon (z 480..900, surface y -2, depth 12) with the landing corridor (foam markers)
            Water(t, "Lagoon", new Vector3(0, -8, 690), new Vector3(92, 12, 420));
            Box(t, "Lagoon floor", new Vector3(0, -15, 690), new Vector3(92, 2, 420), "Sand");
            Box(t, "Lagoon bank L", new Vector3(-48, -5, 690), new Vector3(4, 22, 420), "Rock"); Box(t, "Lagoon bank R", new Vector3(48, -5, 690), new Vector3(4, 22, 420), "Rock");
            Box(t, "Lagoon back wall", new Vector3(0, -8, 478), new Vector3(92, 14, 4), "Rock");
            Zone(t, "Hull down (landing corridor)", new Vector3(0, 3, 550), new Vector3(36, 12, 100), Mode.Boat, true);
            for (int i = 0; i <= 5; i++) { float z = 500 + i * 20; Cyl(t, "foam marker", new Vector3(-18, -1.6f, z), .5f, 1.4f, "Foam", 0, false); Cyl(t, "foam marker", new Vector3(18, -1.6f, z), .5f, 1.4f, "Foam", 0, false); }
            // 5 boat section with a side current (pushes +x) and buoys
            var cur = Vol<CurrentZone>(t, "Current", new Vector3(0, -3, 705), new Vector3(80, 8, 110)); cur.push = new Vector3(4.5f, 0, 0);
            for (int i = 0; i < 8; i++) Box(t, "current ribbon", new Vector3(-30 + i * 9, -1.96f, 660 + (i % 3) * 30), new Vector3(7, .03f, .5f), "Foam", 0, false);
            foreach (float z in new[] { 620f, 680f, 740f, 800f }) { Cyl(t, "buoy", new Vector3(-17, -1.5f, z), .8f, 2.2f, "Buoy", ShiftVehicle.ObstacleLayer); Cyl(t, "buoy", new Vector3(17, -1.5f, z), .8f, 2.2f, "Buoy", ShiftVehicle.ObstacleLayer); }
            Sign(t, new Vector3(-16, -2, 800), Mode.Dive, 6);
            // 6 dive portal: descending guide lights, a stone face over the water at z 910 (the cave roof starts there)
            Zone(t, "Canopy seals (dive portal)", new Vector3(0, -3, 868), new Vector3(30, 8, 12), Mode.Dive);
            for (int i = 0; i < 6; i++) Box(t, "guide light", new Vector3(-6, -2 - i * 1.3f, 866 + i * 8), new Vector3(.6f, .6f, .6f), "Light", 0, false);
            for (int i = 0; i < 6; i++) Box(t, "guide light", new Vector3(6, -2 - i * 1.3f, 866 + i * 8), new Vector3(.6f, .6f, .6f), "Light", 0, false);
            Box(t, "Portal pillar L", new Vector3(-14, 2, 908), new Vector3(4, 12, 4), "Stone"); Box(t, "Portal pillar R", new Vector3(14, 2, 908), new Vector3(4, 12, 4), "Stone");
            Box(t, "Portal lintel", new Vector3(0, 6.5f, 908), new Vector3(32, 3, 4), "Stone");
            // 7 underwater stretch under a rock roof (z 910..1080), coral pillars
            Water(t, "Cave water", new Vector3(0, -10, 995), new Vector3(92, 12, 190), false);
            Box(t, "Cave roof", new Vector3(0, 0, 995), new Vector3(92, 8, 170), "Rock");
            Box(t, "Cave floor", new Vector3(0, -17, 1030), new Vector3(92, 2, 340), "Sand");
            Box(t, "Cave wall L", new Vector3(-48, -6, 1030), new Vector3(4, 24, 340), "Rock"); Box(t, "Cave wall R", new Vector3(48, -6, 1030), new Vector3(4, 24, 340), "Rock");
            foreach (var (z, x) in new[] { (960f, 7f), (1000f, -8f), (1040f, 6f) }) Cyl(t, "coral pillar", new Vector3(x, -10, z), 1.5f, 12, "Coral", ShiftVehicle.ObstacleLayer);
            for (int i = 0; i < 10; i++) Box(t, "coral", new Vector3((i % 2 == 0 ? -1 : 1) * (12 + i % 3 * 5), -15.2f, 925 + i * 15), new Vector3(2.5f, 1.6f, 2.5f), "Coral", 0, false);
            // 8 surfacing pool (z 1080..1190, open top) -> water ramp -> road landing; the waterfall curtain
            Water(t, "Surfacing pool", new Vector3(0, -9, 1135), new Vector3(92, 14, 110));
            Zone(t, "Surface (pool)", new Vector3(0, -9, 1092), new Vector3(40, 14, 12), Mode.Boat);
            Sign(t, new Vector3(-16, -2, 1140), Mode.Drive, 5);
            var ramp = Slab(t, "Water ramp", new Vector3(0, -2.6f, 1160), new Vector3(0, 3, 1200), 18, "WetRamp"); ramp.AddComponent<WetRamp>();
            Box(t, "Water ramp support", new Vector3(0, -7.5f, 1196), new Vector3(16, 16, 8), "Rock");
            Box(t, "Waterfall curtain", new Vector3(0, 5, 1186), new Vector3(30, 16, .3f), "Waterfall", 0, false);
            Zone(t, "Wheels down (water ramp lip)", new Vector3(0, 6, 1206), new Vector3(30, 10, 10), Mode.Drive);
            Box(t, "Landing road", new Vector3(0, -.5f, 1370), new Vector3(20, 1, 320), "Road");
            Box(t, "Landing grass", new Vector3(0, -.56f, 1370), new Vector3(92, 1, 320), "Ground");
            Box(t, "Spillway", new Vector3(0, -10, 1205), new Vector3(92, 2, 12), "Rock");
            for (int i = 0; i < 5; i++) Box(t, "landing chevron", new Vector3(0, .02f, 1222 + i * 6), new Vector3(8, .05f, 1.2f), "SignCar", 0, false, new Vector3(0, i % 2 == 0 ? 20 : -20, 0));
            Box(t, "Finish line", new Vector3(0, .03f, 1450), new Vector3(20, .05f, 2), "Finish", 0, false);
            // ordered gates G0..G10 (Paradise Plunge naming), separate from the zones
            Gate(c, 0, "G0 start", new Vector3(0, 4, 26), new Vector2(10, 8)); Gate(c, 1, "G1 road", new Vector3(0, 4, 180), new Vector2(10, 8));
            Gate(c, 2, "G2 launch", new Vector3(0, 14, 239), new Vector2(15, 10)); Gate(c, 3, "G3 air", new Vector3(0, 12, 430), new Vector2(28, 22));
            Gate(c, 4, "G4 landing", new Vector3(0, 0, 560), new Vector2(30, 12)); Gate(c, 5, "G5 boat exit", new Vector3(0, -2, 840), new Vector2(44, 10));
            Gate(c, 6, "G6 dive portal", new Vector3(0, -6, 885), new Vector2(44, 10)); Gate(c, 7, "G7 cave exit", new Vector3(0, -10, 1070), new Vector2(44, 8));
            Gate(c, 8, "G8 surfacing", new Vector3(0, -6, 1102), new Vector2(44, 12)); Gate(c, 9, "G9 road landing", new Vector3(0, 3, 1240), new Vector2(12, 8));
            Gate(c, 10, "G10 finish", new Vector3(0, 3, 1450), new Vector2(12, 8));
            Anchor(c, new Vector3(0, .9f, 20), Mode.Drive); Anchor(c, new Vector3(0, .9f, 170), Mode.Drive); Anchor(c, new Vector3(0, 15, 300), Mode.Fly);
            Anchor(c, new Vector3(0, -1.4f, 620), Mode.Boat); Anchor(c, new Vector3(0, -9, 935), Mode.Dive); Anchor(c, new Vector3(0, -1.4f, 1118), Mode.Boat);
            Anchor(c, new Vector3(0, .9f, 1265), Mode.Drive);
            return c;
        }

        // ---------- one lane per mode (item 4) ----------
        static Course DriveLane(Vector3 o)
        {
            var c = NewCourse("Drive lane", o); var t = c.transform; c.startMode = Mode.Drive; c.halfWidth = 40;
            Line(c, new Vector3(0, .9f, -10), new Vector3(0, .9f, 700));
            Box(t, "Lane road", new Vector3(0, -.5f, 345), new Vector3(24, 1, 730), "Road"); Box(t, "Lane grass", new Vector3(0, -.56f, 345), new Vector3(90, 1, 730), "Ground");
            for (int i = 0; i < 8; i++) Cyl(t, "cone", new Vector3(i % 2 == 0 ? -4 : 4, .5f, 120 + i * 50), .35f, 1, "Buoy", ShiftVehicle.ObstacleLayer);
            Anchor(c, new Vector3(0, .9f, 10), Mode.Drive); Gate(c, 0, "lane end", new Vector3(0, 3, 680), new Vector2(40, 10));
            return c;
        }
        static Course BoatLane(Vector3 o)
        {
            var c = NewCourse("Boat lane", o); var t = c.transform; c.startMode = Mode.Boat; c.halfWidth = 40;
            Line(c, new Vector3(0, -1.4f, -10), new Vector3(0, -1.4f, 700));
            Water(t, "Lane water", new Vector3(0, -8, 345), new Vector3(80, 12, 730)); Box(t, "Lane bed", new Vector3(0, -15, 345), new Vector3(80, 2, 730), "Sand");
            var cur = Vol<CurrentZone>(t, "Current", new Vector3(0, -3, 330), new Vector3(70, 8, 150)); cur.push = new Vector3(-4.5f, 0, 0);
            for (int i = 0; i < 8; i++) Box(t, "current ribbon", new Vector3(-28 + i * 8, -1.96f, 270 + (i % 3) * 40), new Vector3(6, .03f, .5f), "Foam", 0, false);
            for (int i = 0; i < 8; i++) Cyl(t, "buoy", new Vector3(i % 2 == 0 ? -6 : 6, -1.5f, 100 + i * 70), .8f, 2.2f, "Buoy", ShiftVehicle.ObstacleLayer);
            Anchor(c, new Vector3(0, -1.4f, 10), Mode.Boat); Gate(c, 0, "lane end", new Vector3(0, 0, 680), new Vector2(40, 10));
            return c;
        }
        static Course FlyLane(Vector3 o)
        {
            var c = NewCourse("Fly lane", o); var t = c.transform; c.startMode = Mode.Fly; c.halfWidth = 45; c.minY = -20;
            Line(c, new Vector3(0, 15, -10), new Vector3(0, 15, 300), new Vector3(0, 25, 500), new Vector3(0, 10, 750), new Vector3(0, 15, 1000));
            Box(t, "Lane floor", new Vector3(0, -15, 495), new Vector3(100, 2, 1030), "Ground");
            foreach (var (z, y) in new[] { (200f, 15f), (400f, 22f), (600f, 17f), (800f, 11f) }) Ring(t, new Vector3(0, y, z), 9);
            Anchor(c, new Vector3(0, 15, 10), Mode.Fly); Gate(c, 0, "lane end", new Vector3(0, 15, 980), new Vector2(45, 30));
            return c;
        }
        static Course DiveLane(Vector3 o)
        {
            var c = NewCourse("Dive lane", o); var t = c.transform; c.startMode = Mode.Dive; c.halfWidth = 40; c.minY = -25;
            Line(c, new Vector3(0, -8, -10), new Vector3(0, -8, 200), new Vector3(0, -15, 350), new Vector3(0, -6, 500), new Vector3(0, -9, 700));
            Water(t, "Lane water", new Vector3(0, -11, 345), new Vector3(80, 18, 730)); Box(t, "Lane bed", new Vector3(0, -21, 345), new Vector3(80, 2, 730), "Sand");
            var cur = Vol<CurrentZone>(t, "Current", new Vector3(0, -10, 450), new Vector3(70, 16, 120)); cur.push = new Vector3(3.5f, 0, 0);
            for (int i = 0; i < 7; i++) Cyl(t, "coral pillar", new Vector3(i % 2 == 0 ? -7 : 7, -12, 120 + i * 80), 1.5f, 16, "Coral", ShiftVehicle.ObstacleLayer);
            Anchor(c, new Vector3(0, -8, 10), Mode.Dive); Gate(c, 0, "lane end", new Vector3(0, -10, 680), new Vector2(40, 12));
            return c;
        }
    }
}
