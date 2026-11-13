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
        [System.Serializable] public sealed class Design { public string name; public string[] forms; public bool driver = true; public bool Has(Mode m) => forms != null && System.Array.IndexOf(forms, FileName(m)) >= 0; }
        [System.Serializable] sealed class Index { public Design[] designs; }
        static List<Design> list;

        public static string FileName(Mode m) => m == Mode.Drive ? "Car" : m == Mode.Boat ? "Boat" : m == Mode.Fly ? "Plane" : "Sub";
        public static List<Design> Imported
        {
            get
            {
                if (list != null) return list;
                list = new List<Design>();
                var ta = Resources.Load<TextAsset>("Vehicles/designs");
                if (ta) { try { var ix = JsonUtility.FromJson<Index>(ta.text); if (ix?.designs != null) foreach (var d in ix.designs) if (d != null && !string.IsNullOrEmpty(d.name) && !d.name.StartsWith("_")) list.Add(d); } catch (System.Exception e) { Debug.LogWarning("[Designs] designs.json unreadable: " + e.Message); } }
                Debug.Log($"[Designs] {list.Count} imported: {string.Join(", ", list.ConvertAll(d => $"{d.name} ({string.Join("/", d.forms ?? new string[0])})"))}");
                return list;
            }
        }
        // every name the Garage cycles through: the gray box first
        public static List<string> Names { get { var n = new List<string> { GrayBox }; foreach (var d in Imported) n.Add(d.name); return n; } }
        public static Design Find(string name) { if (string.IsNullOrEmpty(name) || name == GrayBox) return null; foreach (var d in Imported) if (d.name == name) return d; return null; }
        // checks can add a design that is not in the shipped list (the import test's "_Test")
        public static void AddForCheck(Design d) { Imported.RemoveAll(x => x.name == d.name); Imported.Add(d); }

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
            // Dan's prompts ask for open cockpits with no glass (Tripo bakes glass in opaque): the sub gets a separate clear dome over the
            // seat (closed while diving); the other forms stay open. Sized from the seat: 1.3 m across, 1.7 m long.
            if (m == Mode.Dive)
            {
                var glass = Resources.Load<Material>("Materials/VehicleGlass");
                var dome = new GameObject("Canopy (game)"); dome.transform.SetParent(go.transform, false);
                dome.transform.position = parent.TransformPoint(seat + new Vector3(0, .55f, .1f)); dome.transform.localScale = new Vector3(1.3f, 1.1f, 1.7f);
                dome.AddComponent<MeshFilter>().sharedMesh = MeshKit.LowSphere; var dr = dome.AddComponent<MeshRenderer>(); dr.sharedMaterial = glass;
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
