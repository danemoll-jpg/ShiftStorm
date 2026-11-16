using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftStorm
{
    // Round 13 item 5: Graphics presets for the Steam Deck (measured on the PC; Dan checks the Deck). Menu > Settings "Graphics": Auto / Deck /
    // Low / Medium / High (settings.json "graphics", -1 = Auto: Deck on a Steam Deck, High elsewhere; "-graphics <name>" for a run only).
    // Each preset sets the render scale, shadows (distance, map size, cascades), the draw distance (far clip and the Detail layer), the
    // dinosaurs' and set-pieces' draw range, the crowd's density, texture quality (mip limit), the LOD bias and MSAA. Split screen picks the
    // next lighter preset by itself (High -> Medium -> Low; Deck -> Low), and its own Round 6 cuts still apply on top.
    public static class GraphicsPreset
    {
        public static readonly string[] Names = { "Deck", "Low", "Medium", "High" };
        public struct P { public float scale, shadow, far, detail, dino, crowd, lod; public int shadowRes, cascades, mip, msaa; }
        public static readonly P[] Table =
        {
            new P { scale = .8f, shadow = 80, shadowRes = 1024, cascades = 1, far = 1600, detail = 520, dino = .75f, crowd = .5f, mip = 1, lod = .75f, msaa = 1 },    // Deck: 1280 x 800 drawn at 1024 x 640
            new P { scale = .7f, shadow = 55, shadowRes = 512, cascades = 1, far = 1250, detail = 400, dino = .6f, crowd = .35f, mip = 1, lod = .6f, msaa = 1 },      // Low
            new P { scale = 1f, shadow = 95, shadowRes = 1024, cascades = 1, far = 2000, detail = 650, dino = .85f, crowd = .75f, mip = 0, lod = .85f, msaa = 1 },    // Medium
            new P { scale = 1f, shadow = 110, shadowRes = 2048, cascades = 1, far = 2600, detail = ParadisePlunge.DetailCullDistance, dino = 1f, crowd = 1f, mip = 0, lod = 1f, msaa = 1 },   // High: build 14's look
        };

        // Steam on the Deck sets SteamDeck=1 (also under Proton); its APU reports as "AMD Custom GPU 0405" (LCD) / "0932" (OLED)
        public static bool IsSteamDeck
        {
            get
            {
                if (Environment.GetEnvironmentVariable("SteamDeck") == "1") return true;
                string g = SystemInfo.graphicsDeviceName ?? "";
                return g.Contains("Custom GPU 0405") || g.Contains("Custom GPU 0932") || g.Contains("VanGogh") || g.Contains("Sephiroth");
            }
        }
        static int ArgLevel { get { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-graphics"); if (i >= 0 && i + 1 < a.Length) { int k = Array.FindIndex(Names, n => n.Equals(a[i + 1], StringComparison.OrdinalIgnoreCase)); if (k >= 0) return k; } return -2; } }
        public static int Override = -2;   // checks: a level for the run only
        public static int Chosen => Override >= -1 ? Override : ArgLevel >= 0 ? ArgLevel : Settings.Graphics;   // -1 = Auto
        public static int Base => Chosen >= 0 ? Chosen : IsSteamDeck ? 0 : 3;
        public static int Lighter(int l) => l == 3 ? 2 : l == 2 ? 1 : 1;
        public static int Effective => SplitScreen.Active ? Lighter(Base) : Base;
        public static string MenuRow => $"Graphics: {(Chosen < 0 ? $"Auto ({Names[Base]}{(IsSteamDeck ? ", Steam Deck" : "")})" : Names[Chosen])}   (Auto / Deck / Low / Medium / High; 2 players: one lighter)";
        public static void Cycle() { int c = Chosen; Settings.Graphics = c >= 3 ? -1 : c + 1; Override = -2; Apply(); }
        public static string Applied { get; private set; } = "";

        public static void Apply() => Apply(Effective);
        public static void Apply(int level)
        {
            var p = Table[Mathf.Clamp(level, 0, Table.Length - 1)];
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp)
            {
                urp.renderScale = p.scale; urp.shadowDistance = p.shadow; urp.mainLightShadowmapResolution = p.shadowRes; urp.shadowCascadeCount = p.cascades; urp.msaaSampleCount = p.msaa;
                urp.upscalingFilter = p.scale < .99f ? UpscalingFilterSelection.FSR : UpscalingFilterSelection.Auto;
            }
            QualitySettings.globalTextureMipmapLimit = p.mip; QualitySettings.lodBias = p.lod;
            var g = Game.Instance;
            if (g && g.Cam)
            {
                g.Cam.farClipPlane = p.far; var cull = new float[32]; cull[ParadisePlunge.DetailLayer] = p.detail; g.Cam.layerCullDistances = cull; g.Cam.layerCullSpherical = true;
            }
            DinoActor.RangeScale = p.dino; Crowd.Density = p.crowd;
            Applied = $"{Names[level]}: render scale {p.scale:0.00}, shadows {p.shadow:0} m / {p.shadowRes} / {p.cascades} cascade(s), far clip {p.far:0} m, detail {p.detail:0} m, dinos x{p.dino:0.00}, crowd {p.crowd * 100:0} %, texture mip limit {p.mip}, LOD bias {p.lod:0.00}, MSAA {p.msaa}x";
            Debug.Log($"[Graphics] preset {Applied}{(SplitScreen.Active ? " (2 players: one lighter)" : "")}{(Chosen < 0 ? $" (Auto{(IsSteamDeck ? ": Steam Deck" : "")})" : "")}; GPU {SystemInfo.graphicsDeviceName}");
        }
    }
}
