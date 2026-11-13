using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // The Round 1 HUD (uGUI on the main camera, so player-view pictures include it): form and speed, the shared boost meter, gates and time,
    // the invert setting, and the last thing that happened (a change, a reset, a gate). Controls hint at the bottom.
    public sealed class Hud : MonoBehaviour
    {
        public ShiftVehicle v; public Game game;
        Text mode, info, last, help; RectTransform boostFill; Image boostImage;

        public static Hud Make(Camera cam, ShiftVehicle v, Game game)
        {
            var go = new GameObject("HUD"); var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .5f;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            var h = go.AddComponent<Hud>(); h.v = v; h.game = game; h.Build(go.transform); return h;
        }
        Text Label(Transform p, Vector2 anchor, Vector2 pos, int size, TextAnchor align, Vector2 box)
        {
            var g = new GameObject("text"); g.transform.SetParent(p, false); var t = g.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = size; t.alignment = align; t.color = Color.white;
            var sh = g.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .8f); sh.effectDistance = new Vector2(2, -2);
            var r = t.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = box; return t;
        }
        void Build(Transform p)
        {
            mode = Label(p, new Vector2(0, 0), new Vector2(24, 70), 40, TextAnchor.LowerLeft, new Vector2(600, 60));
            info = Label(p, new Vector2(1, 1), new Vector2(-24, -18), 22, TextAnchor.UpperRight, new Vector2(700, 120));
            last = Label(p, new Vector2(.5f, 1), new Vector2(0, -18), 22, TextAnchor.UpperCenter, new Vector2(900, 40));
            help = Label(p, new Vector2(.5f, 0), new Vector2(0, 8), 16, TextAnchor.LowerCenter, new Vector2(1250, 26));
            help.color = new Color(1, 1, 1, .75f);
            help.text = "RT/W go  LT/S brake  A/Space boost  RB/Shift drift-carve  stick Y / I-K climb-dive  Y/R reset  View/F2 invert  D-pad/F3-F4 course";
            var bg = new GameObject("boost bg"); bg.transform.SetParent(p, false); var bgi = bg.AddComponent<Image>(); bgi.color = new Color(0, 0, 0, .55f);
            var br = bgi.rectTransform; br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0); br.anchoredPosition = new Vector2(24, 40); br.sizeDelta = new Vector2(320, 22);
            var fill = new GameObject("boost"); fill.transform.SetParent(bg.transform, false); boostImage = fill.AddComponent<Image>(); boostImage.color = new Color(1, .78f, .15f);
            boostFill = boostImage.rectTransform; boostFill.anchorMin = boostFill.pivot = new Vector2(0, 0); boostFill.anchorMax = new Vector2(0, 1); boostFill.anchoredPosition = new Vector2(3, 0); boostFill.sizeDelta = new Vector2(0, -6);
            boostFill.offsetMin = new Vector2(3, 3); boostFill.offsetMax = new Vector2(3, -3);
            var bl = Label(bg.transform, new Vector2(0, .5f), new Vector2(330, 0), 18, TextAnchor.MiddleLeft, new Vector2(120, 22)); bl.text = "BOOST";
        }
        void Update()
        {
            if (!v) return;
            string form = v.Mode == Mode.Drive ? "CAR" : v.Mode == Mode.Boat ? "BOAT" : v.Mode == Mode.Fly ? "WINGS" : "SUB";
            string extra = v.Drifting ? "  DRIFT" : v.Carving ? "  CARVE" : "";
            mode.text = $"{form}{(v.Changing ? " ..." : "")}  {v.Speed * 2.237f:0} mph{extra}";
            boostFill.offsetMax = new Vector2(3 + 314 * v.Boost, -3); boostImage.color = v.Boosting ? new Color(1, .45f, .1f) : new Color(1, .78f, .15f);
            var c = v.Course; float time = v.Finished ? v.FinishTime : Time.time - v.StartTime;
            info.text = $"{(c ? c.courseName : "")}\nGates {v.GatesPassed}/{(c ? c.gates.Count : 0)}   {time:0.0} s   resets {v.Resets}\nInvert {(ShiftInput.Invert ? "ON" : "off")}";
            last.text = v.Finished ? $"FINISHED {v.FinishTime:0.00} s  -  reset to go again" : v.LastEvent;
        }
    }
}
