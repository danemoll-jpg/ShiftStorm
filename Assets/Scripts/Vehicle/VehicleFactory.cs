using UnityEngine;

namespace ShiftStorm
{
    // Makes the player's vehicle, the rivals' and the small vehicle models on the signs. Round 5: the looks are a FormSet: the chosen design's
    // four models (Tools/import_vehicles.py), and for any form it lacks the gray box: Tools/Blender/shiftstorm_vehicle.py's part model
    // (Assets/Resources/Models/ShiftVehicle.fbx; if missing, a primitive copy with the same named parts, the fallback), posed per form.
    public static class VehicleFactory
    {
        public static string ModelSource { get; private set; } = "";

        public static ShiftVehicle MakePlayer(ShiftTuning shared, DriveTuning drive, BoatTuning boat, FlyTuning fly, DiveTuning dive)
        {
            var root = new GameObject("ShiftVehicle (player)");
            var col = root.AddComponent<BoxCollider>(); col.size = new Vector3(1.9f, .95f, 4.0f); col.center = new Vector3(0, .1f, 0);
            var pm = new PhysicsMaterial("Hull") { dynamicFriction = 0, staticFriction = 0, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            col.material = pm;
            root.AddComponent<Rigidbody>();
            root.AddComponent<ShiftInput>();
            MakeVisual(root.transform, Mode.Drive, 1f, true, VehicleDesigns.PlayerTint, true);
            var v = root.AddComponent<ShiftVehicle>();
            v.shared = shared; v.drive = drive; v.boat = boat; v.fly = fly; v.dive = dive;
            root.AddComponent<VehicleSound>(); root.AddComponent<SpeedFx>();
            ShiftVehicle.MarkPlayer(v); v.RacerName = "Abi"; v.Tint = new Color(.95f, .45f, .1f);
            return v;
        }

        // Round 3: a rival: the same vehicle, hull, mass and tuning assets as the player's (cosmetic differences only: body colour and
        // driver), driven by RivalPilot through its own ShiftInput, with a 3D engine hum.
        public static ShiftVehicle MakeRival(Rival r, ShiftTuning shared, DriveTuning drive, BoatTuning boat, FlyTuning fly, DiveTuning dive)
        {
            var root = new GameObject($"ShiftVehicle (rival {r.name})");
            var col = root.AddComponent<BoxCollider>(); col.size = new Vector3(1.9f, .95f, 4.0f); col.center = new Vector3(0, .1f, 0);
            col.material = new PhysicsMaterial("Hull") { dynamicFriction = 0, staticFriction = 0, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum };
            root.AddComponent<Rigidbody>();
            root.AddComponent<ShiftInput>().Scripted = true;
            MakeVisual(root.transform, Mode.Drive, 1f, false, r.color, true);
            var v = root.AddComponent<ShiftVehicle>();
            v.shared = shared; v.drive = drive; v.boat = boat; v.fly = fly; v.dive = dive; v.RacerName = r.name; v.Tint = r.color;
            root.AddComponent<VehicleSound>().spatial = true; root.AddComponent<SpeedFx>();
            Driver.Make(v, r.character);
            var pilot = root.AddComponent<RivalPilot>(); pilot.rival = r;
            return v;
        }

        // Round 5: the vehicle's looks: a FormSet (four whole models, swapped with an effect) of the chosen design; forms the design does
        // not have are the gray box. Signs show the same design (no effect).
        public static FormSet MakeVisual(Transform parent, Mode mode, float scale, bool player = false, Color? tint = null, bool effects = false)
            => FormSet.Make(parent, Settings.Design, tint ?? VehicleDesigns.PlayerTint, mode, scale, effects);

        // One gray-box form: the Blender part model (or its primitive fallback) posed for one form by its own FormAnimator (which also spins
        // the wheels / propeller). A rival's body is its colour.
        public static FormAnimator MakeGrayForm(Transform parent, Mode mode, Color tint)
        {
            var holder = new GameObject("Gray " + mode); holder.transform.SetParent(parent, false);
            var anim = holder.AddComponent<FormAnimator>();
            var prefab = Resources.Load<GameObject>("Models/ShiftVehicle");
            Transform model;
            if (prefab) { model = Object.Instantiate(prefab, holder.transform, false).transform; model.name = "Model"; ModelSource = "Blender FBX (Resources/Models/ShiftVehicle)"; }
            else { model = BuildFallback(holder.transform); ModelSource = "primitive fallback (no FBX found)"; }
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            // facing check: the front wheels must be ahead (+z); an import that came in backward is turned round here
            var fl = Find(model, "Wheel_FL"); var rl = Find(model, "Wheel_RL");
            if (fl && rl && holder.transform.InverseTransformPoint(fl.position).z < holder.transform.InverseTransformPoint(rl.position).z) model.localRotation = Quaternion.Euler(0, 180, 0) * model.localRotation;
            Paint(model);
            if (tint != VehicleDesigns.PlayerTint)
            {
                var body = Resources.Load<Material>("Materials/VehicleBody");
                if (!tinted.TryGetValue(tint, out var m)) { m = new Material(body) { name = "VehicleBody " + tint }; m.SetColor("_BaseColor", tint); m.SetColor("_Color", tint); tinted[tint] = m; }
                foreach (var rd in model.GetComponentsInChildren<Renderer>(true)) { var ms = rd.sharedMaterials; for (int i = 0; i < ms.Length; i++) if (ms[i] == body) ms[i] = m; rd.sharedMaterials = ms; }
            }
            anim.Setup(model);
            anim.Snap(mode);
            return anim;
        }
        static readonly System.Collections.Generic.Dictionary<Color, Material> tinted = new System.Collections.Generic.Dictionary<Color, Material>();

        static Transform Find(Transform root, string name) { foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t; return null; }

        static void Paint(Transform model)
        {
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                string n = r.transform.name; var t = r.transform; while (t != null && System.Array.IndexOf(FormAnimator.PartNames, t.name) < 0) t = t.parent; if (t) n = t.name;
                string mat = n == "Body" ? "VehicleBody" : n == "Cockpit" ? "VehicleTrim" : n == "Canopy" ? "VehicleGlass" : n.StartsWith("Wheel") ? "VehicleTire"
                    : n.StartsWith("Wing") || n == "Tail" ? "VehicleWing" : n.StartsWith("Fin") || n == "Keel" ? "VehicleFin" : "VehicleMetal";
                var m = Resources.Load<Material>("Materials/" + mat); if (m) r.sharedMaterial = m;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        // Primitive placeholder with the same parts, origins at the hinges, in the rest pose the FormAnimator expects.
        public static Transform BuildFallback(Transform parent)
        {
            var model = new GameObject("FallbackModel").transform; model.SetParent(parent, false);
            Transform Part(string name, Vector3 at) { var t = new GameObject(name).transform; t.SetParent(model, false); t.localPosition = at; return t; }
            void Shape(Transform p, PrimitiveType type, Vector3 at, Vector3 size, Vector3 euler = default)
            { var g = GameObject.CreatePrimitive(type); g.transform.SetParent(p, false); g.transform.localPosition = at; g.transform.localScale = size; g.transform.localEulerAngles = euler; }
            var body = Part("Body", Vector3.zero); Shape(body, PrimitiveType.Cube, new Vector3(0, .05f, 0), new Vector3(1.8f, .6f, 3.9f)); Shape(body, PrimitiveType.Cube, new Vector3(0, .05f, 2.1f), new Vector3(1.3f, .4f, .6f));
            var cockpit = Part("Cockpit", new Vector3(0, .35f, .1f)); Shape(cockpit, PrimitiveType.Cube, Vector3.zero, new Vector3(1.2f, .3f, 1.6f));
            var canopy = Part("Canopy", new Vector3(0, .5f, -.6f)); Shape(canopy, PrimitiveType.Sphere, new Vector3(0, 0, .75f), new Vector3(1.0f, .55f, 1.6f));
            foreach (var (n, x, z) in new[] { ("Wheel_FL", -1f, 1.35f), ("Wheel_FR", 1f, 1.35f), ("Wheel_RL", -1f, -1.35f), ("Wheel_RR", 1f, -1.35f) })
            { var w = Part(n, new Vector3(x, -.45f, z)); Shape(w, PrimitiveType.Cylinder, Vector3.zero, new Vector3(.8f, .15f, .8f), new Vector3(0, 0, 90)); }
            var wl = Part("Wing_L", new Vector3(-.9f, .15f, -.2f)); Shape(wl, PrimitiveType.Cube, new Vector3(-1.5f, 0, 0), new Vector3(3f, .1f, 1.2f));
            var wr = Part("Wing_R", new Vector3(.9f, .15f, -.2f)); Shape(wr, PrimitiveType.Cube, new Vector3(1.5f, 0, 0), new Vector3(3f, .1f, 1.2f));
            var tail = Part("Tail", new Vector3(0, .35f, -1.75f)); Shape(tail, PrimitiveType.Cube, new Vector3(0, .5f, -.1f), new Vector3(.1f, 1f, .7f));
            var fl = Part("Fin_L", new Vector3(-.9f, -.25f, -.4f)); Shape(fl, PrimitiveType.Cube, new Vector3(0, -.3f, 0), new Vector3(.08f, .6f, 1.4f));
            var fr = Part("Fin_R", new Vector3(.9f, -.25f, -.4f)); Shape(fr, PrimitiveType.Cube, new Vector3(0, -.3f, 0), new Vector3(.08f, .6f, 1.4f));
            var keel = Part("Keel", new Vector3(0, -.3f, -.2f)); Shape(keel, PrimitiveType.Cube, new Vector3(0, -.25f, 0), new Vector3(.1f, .5f, 2.2f));
            var prop = Part("Prop", new Vector3(0, -.1f, -2.05f)); Shape(prop, PrimitiveType.Cube, Vector3.zero, new Vector3(1.0f, .12f, .05f)); Shape(prop, PrimitiveType.Cube, Vector3.zero, new Vector3(.12f, 1.0f, .05f));
            return model;
        }
    }
}
