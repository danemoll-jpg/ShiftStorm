using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm
{
    // Paradise Plunge's look, first pass: Tee'd Off's Jurassic Par art style (TO Resources/Tuning/ArtStyle_Jurassic.asset and
    // ArtStyle.CreateJurassic, read-only: the values are copied here) on ShiftStorm's URP toon shaders (Assets/Shaders, ported from TO).
    // Warm, humid, hazy sky (gold low down, peach haze), a low warm sun, jungle greens, mossy greys, dark earthy water.
    // Apply(true) when Paradise Plunge starts; Apply(false) puts the Round 1 gray-box look back for the test courses.
    public static class JurassicLook
    {
        // ArtStyle_Jurassic (TO)
        public static readonly Color SunColor = new Color(1f, 0.9f, 0.74f), Ambient = new Color(0.66f, 0.62f, 0.52f);
        public const float SunIntensity = 1.15f; public static readonly Vector2 SunAngles = new Vector2(42f, 120f);
        public static readonly Color SkyTop = new Color(0.46f, 0.62f, 0.78f), SkyHorizon = new Color(1f, 0.84f, 0.62f), SkyBelow = new Color(0.95f, 0.8f, 0.62f);
        public static readonly Color OutlineColor = new Color(0.10f, 0.11f, 0.16f); public const float OutlineWidth = 2.4f;
        public const float LitThreshold = .55f, MidThreshold = .12f, MidBrightness = .84f, ShadowBrightness = .64f;
        public static readonly Color Water = new Color(0.26f, 0.5f, 0.52f), WaterLight = new Color(0.36f, 0.6f, 0.58f), WaterRim = new Color(0.8f, 0.9f, 0.85f);
        // JurassicBuilder (TO) palette
        public static readonly Color Moss = new Color(0.32f, 0.55f, 0.24f), Rock = new Color(0.52f, 0.47f, 0.42f), RockDark = new Color(0.38f, 0.34f, 0.31f);
        public static readonly Color Frond = new Color(0.26f, 0.6f, 0.22f), FrondLight = new Color(0.42f, 0.72f, 0.28f), FernTrunk = new Color(0.42f, 0.3f, 0.2f);
        public static readonly Color CycadA = new Color(0.55f, 0.42f, 0.24f), CycadB = new Color(0.62f, 0.48f, 0.28f);
        public static readonly Color Bone = new Color(0.95f, 0.9f, 0.78f), Amber = new Color(1f, 0.62f, 0.12f), Spring = new Color(0.35f, 0.85f, 0.88f);
        public static readonly Color SwampDeep = new Color(0.25f, 0.36f, 0.2f), SwampLight = new Color(0.36f, 0.47f, 0.26f), SwampCrest = new Color(0.62f, 0.7f, 0.5f);
        // ShiftStorm additions in the same palette
        public static readonly Color Jungle = new Color(0.34f, 0.58f, 0.25f), JungleDark = new Color(0.24f, 0.44f, 0.2f), Sand = new Color(0.84f, 0.76f, 0.56f);
        public static readonly Color Road = new Color(0.66f, 0.58f, 0.47f), RoadEdge = new Color(0.92f, 0.86f, 0.7f), Stone = new Color(0.7f, 0.66f, 0.56f);
        public static readonly Color Mud = new Color(0.4f, 0.32f, 0.22f), CaveRock = new Color(0.3f, 0.34f, 0.32f), Crystal = new Color(0.3f, 0.95f, 0.9f);
        public static readonly Color UnderwaterFog = new Color(0.07f, 0.27f, 0.25f); public const float UnderwaterDensity = .034f;
        public static readonly Color Haze = new Color(0.88f, 0.84f, 0.72f); public const float HazeDensity = .0021f;
        // Round 5 item 6: Tee'd Off's own fog is linear (ArtStyle_Jurassic fogStart 140, fogEnd 820): clear close by, haze far off. Here 1.35 x that
        // (the course is built bigger): from 190 m to 1100 m. The arch now reads from the ramp (~500 m: 34 % haze; exponential gave 65 %).
        public const float FogStart = 190f, FogEnd = 1100f;

        static Material toon, outline, water, fall, sky;
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Material Base(ref Material m, string name) { if (!m) m = Resources.Load<Material>("Materials/Jurassic/" + name); if (!m) Debug.LogWarning("[Look] missing material " + name); return m; }

        // A toon material in one colour (cached): outline for scenery and props, none for big surfaces.
        public static Material Mat(Color c, bool withOutline = false, Color emission = default, float outlineScale = 1f)
        {
            string key = $"{(withOutline ? "O" : "T")}{c}{emission}{outlineScale}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            var b = withOutline ? Base(ref outline, "ToonOutline") : Base(ref toon, "Toon");
            m = new Material(b) { name = key }; m.SetColor("_BaseColor", c); m.SetColor("_Emission", emission); m.SetFloat("_VertexColor", 0); m.SetFloat("_OutlineScale", outlineScale);
            cache[key] = m; return m;
        }
        // White, vertex-coloured (MeshKit.Batch scenery).
        public static Material VertexColored(bool withOutline = true, Color emission = default)
        {
            string key = $"VC{withOutline}{emission}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(withOutline ? Base(ref outline, "ToonOutline") : Base(ref toon, "Toon")) { name = key };
            m.SetColor("_BaseColor", Color.white); m.SetFloat("_VertexColor", 1); m.SetColor("_Emission", emission); m.SetFloat("_OutlineScale", 1);
            cache[key] = m; return m;
        }
        static readonly System.Collections.Generic.Dictionary<(Texture2D, bool), Material> texCache = new System.Collections.Generic.Dictionary<(Texture2D, bool), Material>();
        public static Material Textured(Texture2D tex, bool withOutline = true)
        {
            // Round 9: cached by the texture itself (every vehicle design's is named "Car_basecolor" etc.: by name, the first design loaded
            // lent its texture to all of them)
            var tk = (tex, withOutline);
            if (texCache.TryGetValue(tk, out var m) && m) return m;
            m = new Material(withOutline ? Base(ref outline, "ToonOutline") : Base(ref toon, "Toon")) { name = "TX" + (tex ? tex.name : "none") + withOutline };
            m.SetColor("_BaseColor", Color.white); if (tex) m.SetTexture("_BaseMap", tex); m.SetFloat("_VertexColor", 0); m.SetFloat("_OutlineScale", 1);
            texCache[tk] = m; return m;
        }
        public static Material WaterMat(Color deep, Color light, Color crest, float swell = .12f, float scale = 1f)
        {
            string key = $"W{deep}{light}{crest}{swell}{scale}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Base(ref water, "ToonWater")) { name = key };
            m.SetColor("_Deep", deep); m.SetColor("_Shallow", light); m.SetColor("_Crest", crest); m.SetFloat("_Swell", swell); m.SetFloat("_Scale", scale);
            cache[key] = m; return m;
        }
        public static Material FallMat(float alpha = .55f)
        {
            string key = "F" + alpha;
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = new Material(Base(ref fall, "Waterfall")) { name = key }; var col = m.GetColor("_Color"); col.a = alpha; m.SetColor("_Color", col);
            cache[key] = m; return m;
        }

        // ---------------- course switch ----------------
        static bool saved; static Material r1Sky; static Color r1Fog, r1Ambient, r1Sun; static float r1Density, r1SunIntensity; static bool r1FogOn; static Quaternion r1SunRot; static AmbientMode r1AmbientMode;
        public static bool On { get; private set; }

        public static void Apply(bool on)
        {
            var sun = Sun();
            if (!saved)
            {
                saved = true; r1Sky = RenderSettings.skybox; r1Fog = RenderSettings.fogColor; r1Density = RenderSettings.fogDensity; r1FogOn = RenderSettings.fog;
                r1Ambient = RenderSettings.ambientLight; r1AmbientMode = RenderSettings.ambientMode;
                if (sun) { r1Sun = sun.color; r1SunIntensity = sun.intensity; r1SunRot = sun.transform.rotation; }
            }
            On = on;
            Shader.SetGlobalFloat("_ToonLit", LitThreshold); Shader.SetGlobalFloat("_ToonMidT", MidThreshold); Shader.SetGlobalFloat("_ToonMid", MidBrightness);
            Shader.SetGlobalFloat("_ToonShadow", ShadowBrightness); Shader.SetGlobalColor("_ToonOutlineColor", OutlineColor); Shader.SetGlobalFloat("_ToonOutlineWidth", OutlineWidth);
            if (on)
            {
                if (!sky) { var b = Resources.Load<Material>("Materials/Jurassic/Sky"); if (b) { sky = new Material(b); sky.SetColor("_Top", SkyTop); sky.SetColor("_Horizon", SkyHorizon); sky.SetColor("_Below", SkyBelow); } }
                if (sky) RenderSettings.skybox = sky;
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = FogStart; RenderSettings.fogEndDistance = FogEnd; RenderSettings.fogColor = Haze; RenderSettings.fogDensity = HazeDensity;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Ambient;
                if (sun) { sun.color = SunColor; sun.intensity = SunIntensity; sun.transform.rotation = Quaternion.Euler(SunAngles.x, SunAngles.y, 0); sun.shadows = LightShadows.Hard; }
                ChaseCam.UnderwaterColor = UnderwaterFog; ChaseCam.UnderwaterDensity = UnderwaterDensity;
            }
            else
            {
                RenderSettings.skybox = r1Sky; RenderSettings.fog = r1FogOn; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogColor = r1Fog; RenderSettings.fogDensity = r1Density;
                RenderSettings.ambientMode = r1AmbientMode; RenderSettings.ambientLight = r1Ambient;
                if (sun) { sun.color = r1Sun; sun.intensity = r1SunIntensity; sun.transform.rotation = r1SunRot; }
                ChaseCam.UnderwaterColor = ChaseCam.DefaultUnderwater; ChaseCam.UnderwaterDensity = ChaseCam.DefaultUnderwaterDensity;
            }
            ChaseCam.ResetSkyFog();
        }
        static Light Sun()
        {
            if (RenderSettings.sun) return RenderSettings.sun;
            foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) return l;
            return null;
        }
    }
}
