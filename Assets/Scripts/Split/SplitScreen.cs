using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShiftStorm
{
    // Round 6 item 1 (Dan: "Is it possible to throw in split screen so I can try with a friend? Woodstock Rush and Chastain Rush has this"):
    // chastain_rush's SplitScreen / SplitSession (Split/SplitScreen.cs, itself Racer's) ported to ShiftStorm. Two players on one PC or Deck,
    // each bound to their own device (ShiftInput.Bind: a pad answers only itself, the keyboard only itself), their own vehicle, driver,
    // camera, camera view, HUD and results. Layout as Chastain Rush: top / bottom with player 1 on top (the default), or left / right
    // (Menu > 2 Players > Screen, saved in settings.json). The setup is the menu's 2 PLAYERS page (Game): player 1 is the device that opened
    // it, player 2 joins by pressing A on another controller (or Enter on the keyboard when player 1 is on a controller).
    //   Player 1 = Abi in Jetstream green; player 2 picks Rae, Chip or Vivi and gets that colour; 0-2 of the other AI racers fill the field.
    //   Race: against the AI (no best lap, as the 1-player race); Time Trial: the two players alone (best laps are saved, as Time Trial).
    //   Either player's Menu pauses both. A controller pulled out pauses the game until it (or another) comes back.
    //   Detail while split (Chastain Rush's per-view cuts, restored at the end): LOD bias x0.6, shadow distance x0.6, far clip 1150 m (the
    //   Jurassic fog is solid at 1100 m), the polished stretch's Detail layer culled at 500 m (800 alone), textures one mip level down.
    //   Sound: one listener (player 1's camera); both players' engines and wind are non-spatial at 0.7 each (VehicleSound.Mix).
    // Changes from Chastain Rush: no Free Roam / stunt modes, no AI player 2 (the AI racers are the rivals), no names / garages per player;
    // the menu is ShiftStorm's own (Game), the HUD is the game's own Hud once per half (not a separate split HUD).
    public static class SplitScreen
    {
        public static bool Active => Session != null;
        public static SplitSession Session { get; internal set; }
        public static InputDevice P1Device, P2Device;
        public static int P2Pick;            // index into Rival.Roster: player 2's driver and colour
        public static int AiCount = 2;       // 0-2 AI racers in a 2-player race
        public static bool LeftRight => Settings.SplitLeftRight;
        public static Rival P2Rival => Lineup.P2;   // Round 11: any driver player 1 did not pick (Settings.Driver2)
        public static string DeviceName(InputDevice d) => d == null ? "-" : d is Keyboard ? "Keyboard" : d is Gamepad g ? "Controller " + (Gamepad.all.ToList().IndexOf(g) + 1) + (string.IsNullOrEmpty(g.displayName) ? "" : " (" + g.displayName + ")") : d.displayName;
        public static readonly List<Camera> Views = new List<Camera>();
        public static int PlayerOf(ShiftVehicle v) => !Session || !v ? (v && v.IsPlayer ? 1 : 0) : v == Session.P1 ? 1 : v == Session.P2 ? 2 : 0;

        public static SplitSession Begin()
        {
            End();
            var go = new GameObject("Split screen (Round 6)"); Session = go.AddComponent<SplitSession>(); Session.Begin();
            return Session;
        }
        public static void End() { if (Session) { var s = Session; s.Teardown(); Session = null; Object.Destroy(s.gameObject); } Views.Clear(); }
    }

    public sealed class SplitSession : MonoBehaviour
    {
        public ShiftVehicle P1 { get; private set; }
        public ShiftVehicle P2 { get; private set; }
        public Camera Cam1 { get; private set; }
        public Camera Cam2 { get; private set; }
        public Hud Hud2 { get; private set; }
        public TimeTrial Trial2 { get; private set; }
        public CameraViews Views2 { get; private set; }
        public int MissingPlayer { get; private set; }
        public const float LodScale = .6f, ShadowScale = .6f, FarClip = 1150f, DetailCull = 500f;
        Rect rect1; float far1, lodBias, shadowDistance = -1; float[] cull1; int mip1;
        public string QualityNote { get; private set; } = "";

        public void Begin()
        {
            var g = Game.Instance;
            P1 = g.Player; P1.Input.player = 1; P1.Input.Bind(SplitScreen.P1Device);
            P2 = VehicleFactory.MakePlayer2(SplitScreen.P2Rival, g.Shared, g.Drive, g.Boat, g.Fly, g.Dive);
            Caller.Instance?.Watch(P2);   // Round 7: one caller for both players
            GameSounds.Instance?.Watch(P2);
            P2.Input.Bind(SplitScreen.P2Device);
            Trial2 = gameObject.AddComponent<TimeTrial>(); Trial2.v = P2;
            CourseAudio.Instance?.Follow(P2);
            InputSystem.onDeviceChange += DeviceChanged;
            VehicleSound.Mix = .7f;
            // per-view detail, lowered for the two views only (restored in Teardown)
            lodBias = QualitySettings.lodBias; QualitySettings.lodBias = lodBias * LodScale;
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp) { shadowDistance = urp.shadowDistance; urp.shadowDistance = shadowDistance * ShadowScale; }
            mip1 = QualitySettings.globalTextureMipmapLimit; QualitySettings.globalTextureMipmapLimit = mip1 + 1;
            Cam1 = g.Cam; rect1 = Cam1.rect; far1 = Cam1.farClipPlane; cull1 = (float[])Cam1.layerCullDistances.Clone();
            Cameras();
            Hud2 = Hud.Make(Cam2, P2, g, 2); Hud.Instance?.Relayout();
            QualityNote = $"LOD bias {lodBias:0.00} -> {QualitySettings.lodBias:0.00}, shadow distance {(urp ? $"{shadowDistance:0} -> {urp.shadowDistance:0} m" : "-")}, far clip {far1:0} -> {Cam1.farClipPlane:0} m, Detail layer cull {cull1[ParadisePlunge.DetailLayer]:0} -> {DetailCull:0} m, texture mip limit {mip1} -> {QualitySettings.globalTextureMipmapLimit}";
            Debug.Log($"[Split] started: player 1 {SplitScreen.DeviceName(SplitScreen.P1Device)}, player 2 {SplitScreen.DeviceName(SplitScreen.P2Device)} ({P2.RacerName}), {(SplitScreen.LeftRight ? "left / right" : "top / bottom")}; {QualityNote}");
        }

        void Cameras()
        {
            var go = new GameObject("Player 2 camera"); Cam2 = go.AddComponent<Camera>(); Cam2.CopyFrom(Cam1);
            var d1 = Cam1.GetUniversalAdditionalCameraData(); var d2 = Cam2.GetUniversalAdditionalCameraData();
            d2.renderPostProcessing = d1.renderPostProcessing; d2.antialiasing = d1.antialiasing; d2.renderShadows = d1.renderShadows;
            var chase2 = go.AddComponent<ChaseCam>(); chase2.target = P2; chase2.primary = false;
            Views2 = go.AddComponent<CameraViews>(); Views2.SetPlayer(2);
            var v1 = Cam1.GetComponent<CameraViews>(); if (v1) v1.SetPlayer(1);
            foreach (var c in new[] { Cam1, Cam2 })
            {
                c.farClipPlane = Mathf.Min(far1, FarClip);
                var cull = (float[])cull1.Clone(); cull[ParadisePlunge.DetailLayer] = DetailCull; c.layerCullDistances = cull; c.layerCullSpherical = true;
            }
            Relayout();
        }
        public void Relayout()
        {
            bool lr = SplitScreen.LeftRight;
            if (Cam1) Cam1.rect = lr ? new Rect(0, 0, .5f, 1) : new Rect(0, .5f, 1, .5f);
            if (Cam2) Cam2.rect = lr ? new Rect(.5f, 0, .5f, 1) : new Rect(0, 0, 1, .5f);
            Hud.Instance?.Relayout(); if (Hud2) Hud2.Relayout();
            SplitScreen.Views.Clear(); if (Cam1) SplitScreen.Views.Add(Cam1); if (Cam2) SplitScreen.Views.Add(Cam2);
        }

        // ---------- devices (Chastain Rush's SplitSession.DeviceChanged) ----------
        void DeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                int player = device == SplitScreen.P1Device ? 1 : device == SplitScreen.P2Device ? 2 : 0; if (player == 0) return;
                MissingPlayer = player; Debug.Log($"[Split] player {player}'s controller is gone: paused");
                if (Game.Instance && !Game.Instance.MenuOpen) Game.Instance.OpenMenu();
            }
            else if ((change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected) && MissingPlayer != 0 && device is Gamepad pad && pad != (MissingPlayer == 1 ? SplitScreen.P2Device : SplitScreen.P1Device))
            {
                if (MissingPlayer == 1) { SplitScreen.P1Device = pad; P1.Input.Bind(null); P1.Input.Bind(pad); } else { SplitScreen.P2Device = pad; P2.Input.Bind(null); P2.Input.Bind(pad); }
                Debug.Log($"[Split] player {MissingPlayer} is back on {SplitScreen.DeviceName(pad)}"); MissingPlayer = 0;
            }
        }
        public string MissingText => MissingPlayer == 0 ? null : $"PLAYER {MissingPlayer}'S CONTROLLER IS DISCONNECTED - reconnect it (or any controller) to go on, or end 2 players";

        public void Teardown()
        {
            InputSystem.onDeviceChange -= DeviceChanged;
            VehicleSound.Mix = 1;
            if (Hud2) Destroy(Hud2.gameObject);
            if (P2) { P2.gameObject.SetActive(false); Destroy(P2.gameObject); P2 = null; }
            if (Cam2) Destroy(Cam2.gameObject);
            if (Cam1) { Cam1.rect = rect1; Cam1.farClipPlane = far1; Cam1.layerCullDistances = cull1; var v1 = Cam1.GetComponent<CameraViews>(); if (v1) v1.SetPlayer(1); }
            if (P1) { P1.Input.Bind(null); P1.Input.player = 0; }
            QualitySettings.lodBias = lodBias; QualitySettings.globalTextureMipmapLimit = mip1;
            { var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; if (urp && shadowDistance >= 0) urp.shadowDistance = shadowDistance; shadowDistance = -1; }
            SplitScreen.Views.Clear();
            Hud.Instance?.Relayout();
            Debug.Log("[Split] ended");
        }
        void OnDestroy() { InputSystem.onDeviceChange -= DeviceChanged; }
    }
}
