using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 5 item 5: the vehicle designs. "Gray box" is the Blender part model of Rounds 1-4 (always there). Imported designs come from
    // Tools/import_vehicles.py: Resources/Vehicles/designs.json lists them and the forms they have; each form is
    // Resources/Vehicles/<Design>/<Form>.fbx (LOD0, LOD1, Seat) + <Form>_basecolor.png. Forms a design does not have yet stay gray box.
    // The design raced with is saved in settings.json (Garage > "Race with this design"); rivals use the same design, recoloured: the
    // green parts of the texture (the body colour Dan's prompts ask for) are turned to the rival's colour.
    public static class VehicleDesigns
    {
        public const string GrayBox = "Gray box";
        // Round 9 item 3c: per design (designs.json, from the design's import.json): "rival" the rival who races it (Rae / Chip / Vivi, when all
        // four forms are in); "canopy" per form (Car, Boat, Plane, Sub order): "bubble" (the default: the clear bubble over the cockpit rim),
        // "sphere" (a clear glass sphere round the rider, the Scrambler's sub, mounted on the model's chrome ring), "none" (a motorcycle);
        // "astride" the forms ridden astride (the rider's legs either side, leaning forward); "lean" the forms that lean into turns (BikeLean);
        // "sphere" the sphere's radius (m) and "sphereY" its centre over the seat (m); "riderScale" the driver's size on it (the Scrambler's
        // bikes are imported 2.6 m long, import.json "length", and ridden by a 1.4x Abi: real bike-and-rider proportions, her hands on the bars).
        [System.Serializable] public sealed class Design
        {
            public string name; public string[] forms; public bool driver = true; public string[] steering;
            public string title = ""; public bool @default; public string rival = ""; public string[] canopy; public string[] astride; public string[] lean; public float sphere = 1.05f, sphereY = .42f, riderScale = 1f;
            public bool Has(Mode m) => forms != null && System.Array.IndexOf(forms, FileName(m)) >= 0;
            public bool Complete => forms != null && forms.Length >= 4;
            public string CanopyOf(Mode m) { int i = System.Array.IndexOf(FormSet.All, m); return canopy != null && i < canopy.Length && !string.IsNullOrEmpty(canopy[i]) ? canopy[i] : "bubble"; }
            public bool Astride(Mode m) => astride != null && System.Array.IndexOf(astride, FileName(m)) >= 0;
            public bool Leans(Mode m) => lean != null && System.Array.IndexOf(lean, FileName(m)) >= 0;
        }
        [System.Serializable] sealed class Index { public Design[] designs; }
        static List<Design> list, hidden;
        // the design a rival races (designs.json "rival", all four forms in), else null (the player's design, recoloured)
        public static Design ForRival(string character)
        {
            if (string.IsNullOrEmpty(character)) return null;
            foreach (var d in Imported) if (d.Complete && string.Equals(d.rival, character, System.StringComparison.OrdinalIgnoreCase)) return d;
            return null;
        }

        public static string FileName(Mode m) => m == Mode.Drive ? "Car" : m == Mode.Boat ? "Boat" : m == Mode.Fly ? "Plane" : "Sub";
        public static List<Design> Imported
        {
            get
            {
                if (list != null) return list;
                list = new List<Design>();
                var ta = Resources.Load<TextAsset>("Vehicles/designs");
                hidden = new List<Design>();
                if (ta) { try { var ix = JsonUtility.FromJson<Index>(ta.text); if (ix?.designs != null) foreach (var d in ix.designs) if (d != null && !string.IsNullOrEmpty(d.name)) (d.name.StartsWith("_") ? hidden : list).Add(d); } catch (System.Exception e) { Debug.LogWarning("[Designs] designs.json unreadable: " + e.Message); } }
                Debug.Log($"[Designs] {list.Count} imported: {string.Join(", ", list.ConvertAll(d => $"{d.name} ({string.Join("/", d.forms ?? new string[0])})"))}");
                return list;
            }
        }
        // every name the Garage cycles through: the gray box first
        public static List<string> Names { get { var n = new List<string> { GrayBox }; foreach (var d in Imported) n.Add(d.name); return n; } }
        // the design raced with when none was chosen: Dan's pick (the first imported design), else the gray box
        public static string Default { get { foreach (var d in Imported) if (d.@default) return d.name; return Imported.Count > 0 ? Imported[0].name : GrayBox; } }   // Round 9: designs.json "default" (Jetstream, Dan's pick)
        public static string Title(string name) { var d = Find(name); return d != null && !string.IsNullOrEmpty(d.title) ? d.title : name; }   // Round 9: "Grand Prix", "Hot Rod"
        public static Design Find(string name) { if (string.IsNullOrEmpty(name) || name == GrayBox) return null; foreach (var d in Imported) if (d.name == name) return d; return null; }
        // checks can add a design that is not in the shipped list (the import test's "_Test")
        public static void AddForCheck(Design d) { Imported.RemoveAll(x => x.name == d.name); Imported.Add(d); }
        // Round 9: a hidden design (a placeholder, "_Scrambler") with its designs.json entry as it is (canopy, astride, lean...)
        public static bool AddHiddenForCheck(string name) { var _ = Imported; var d = hidden.Find(x => x.name == name); if (d == null) return false; AddForCheck(d); return true; }

        public static readonly bool OldBubble = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldBubble") >= 0;   // checks: build 15's ball over the seat
        static Vector3 Inv(Vector3 v) => new Vector3(1 / Mathf.Max(1e-4f, v.x), 1 / Mathf.Max(1e-4f, v.y), 1 / Mathf.Max(1e-4f, v.z));
        static Mesh smooth; static Material glass;
        static Mesh SmoothSphere { get { if (!smooth) { var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); smooth = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); } return smooth; } }
        // clearer than the gray box's glass (alpha 0.7): Abi must show through it
        static Material Glass { get { if (!glass) { var b = Resources.Load<Material>("Materials/VehicleGlass"); glass = new Material(b) { name = "Canopy glass" }; var c = new Color(.6f, .82f, 1f, .32f); glass.SetColor("_BaseColor", c); glass.SetFloat("_Smoothness", .95f); } return glass; } }
        static Material sphereGlass;   // the sphere: clearer still (the whole rider is seen through it), a faint blue edge
        static Material SphereGlass { get { if (!sphereGlass) { var b = Resources.Load<Material>("Materials/VehicleGlass"); sphereGlass = new Material(b) { name = "Sphere glass" }; sphereGlass.SetColor("_BaseColor", new Color(.7f, .88f, 1f, .2f)); sphereGlass.SetFloat("_Smoothness", .97f); } return sphereGlass; } }
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        public static GameObject Instantiate(Design d, Mode m, Transform parent, Color tint, out Vector3 seat)
        {
            seat = FormSet.GraySeat;
            string path = $"Vehicles/{d.name}/{FileName(m)}";
            var prefab = Resources.Load<GameObject>(path);
            if (!prefab) { Debug.LogWarning("[Designs] missing " + path); return null; }
            var go = Object.Instantiate(prefab, parent, false);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            Transform lod0 = null, lod1 = null;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Seat") seat = parent.InverseTransformPoint(t.position);
                else if (t.name.StartsWith("LOD0")) lod0 = t; else if (t.name.StartsWith("LOD1")) lod1 = t;
            }
            var mat = Mat(d.name, FileName(m), tint);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;   // (the game's own canopy is added after this)
            // Dan's models have open cockpits and no glass (Tripo bakes glass in opaque): the game adds its own clear bubble over the cockpit
            // rim on every form (Dan, 2026-11-13: "add the separate clear canopies"), sized from the seat: the rim is 0.42 m over Abi's hips
            // (Tools/Blender/vehicle_import.py), the bubble 0.72 m wide, 1.1 m long, ~0.3 m high over the rim (Abi's head clears it by ~0.1 m); its lower half is inside the hull.
            // Round 9: per form (designs.json "canopy"): "none" (the Scrambler's bike, jet ski and flying bike), "sphere" (its sub: a clear
            // glass sphere round the rider and the bars, on the model's chrome ring: Dan's design)
            string kind = d.CanopyOf(m);
            if (kind == "sphere" && !OldBubble) BubbleSphere.Make(go, parent, seat, d.riderScale);   // Round 14 item 3: a whole ball round the bike and rider
            else if (kind == "sphere")
            {
                var ball = new GameObject("Glass sphere (game)"); ball.transform.SetParent(go.transform, false);
                ball.transform.position = parent.TransformPoint(seat + new Vector3(0, d.sphereY, .08f));
                ball.transform.rotation = parent.rotation; ball.transform.localScale = Vector3.Scale(Vector3.one * (2 * d.sphere), Inv(go.transform.localScale));
                ball.AddComponent<MeshFilter>().sharedMesh = SmoothSphere; var br = ball.AddComponent<MeshRenderer>(); br.sharedMaterial = SphereGlass;
                br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else if (kind != "none")
            {
                var dome = new GameObject("Canopy (game)"); dome.transform.SetParent(go.transform, false);
                dome.transform.position = parent.TransformPoint(seat + new Vector3(0, .36f, -.05f));
                dome.transform.rotation = parent.rotation; dome.transform.localScale = Vector3.Scale(new Vector3(.72f, .74f, 1.1f), Inv(go.transform.localScale));
                dome.AddComponent<MeshFilter>().sharedMesh = SmoothSphere; var dr = dome.AddComponent<MeshRenderer>(); dr.sharedMaterial = Glass;
                dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (lod0 && lod1)
            {
                var g = go.AddComponent<LODGroup>();
                g.SetLODs(new[] { new LOD(.12f, lod0.GetComponentsInChildren<Renderer>()), new LOD(.01f, lod1.GetComponentsInChildren<Renderer>()) }); g.RecalculateBounds();
            }
            return go;
        }

        // the toon material with the design's texture; for a rival (tint not Abi's), the green parts recoloured to the tint
        static Material Mat(string design, string form, Color tint)
        {
            bool player = tint == PlayerTint;
            string key = $"{design}/{form}/{(player ? "player" : ColorUtility.ToHtmlStringRGB(tint))}";
            if (mats.TryGetValue(key, out var m) && m) return m;
            var tex = Resources.Load<Texture2D>($"Vehicles/{design}/{form}_basecolor");
            if (tex && !player) tex = Recolor(tex, tint);
            m = tex ? JurassicLook.Textured(tex, true) : JurassicLook.Mat(player ? new Color(.12f, .5f, .28f) : tint, true);
            m.name = "Vehicle " + key; mats[key] = m; return m;
        }
        public static readonly Color PlayerTint = new Color(.95f, .45f, .1f);

        // hue shift of the green body parts (hue 80-170 deg, some saturation) to the rival's hue, keeping their shading
        public static Texture2D Recolor(Texture2D src, Color to)
        {
            Color32[] px;
            try { px = src.GetPixels32(); } catch { Debug.LogWarning("[Designs] texture not readable: " + src.name); return src; }
            Color.RGBToHSV(to, out float th, out float ts, out _);
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i]; Color.RGBToHSV(c, out float h, out float s, out float v);
                if (h < 80f / 360 || h > 170f / 360 || s < .25f) continue;
                var n = Color.HSVToRGB(th, Mathf.Clamp01(Mathf.Max(s, ts * .9f)), v); n.a = c.a; px[i] = n;
            }
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true) { name = src.name + " recoloured", wrapMode = src.wrapMode, filterMode = src.filterMode };
            t.SetPixels32(px); t.Apply(true, true); return t;
        }
    }
}
