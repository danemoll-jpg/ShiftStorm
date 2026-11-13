using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // The HUD (uGUI on the main camera, so player-view pictures include it). Round 1: form and speed, the shared boost meter, the invert
    // setting, the last thing that happened, the controls. Round 2: the lap clock and the current segment with its target, the last split,
    // lap progress (gates + progress to the next gate), the countdown (3, 2, 1, GO), return cues (missed gate, wrong way), the results
    // (lap, splits against their targets and the best lap's, new best) and the course menu (Menu / Esc).
    public sealed class Hud : MonoBehaviour
    {
        public ShiftVehicle v; public Game game;
        Text mode, info, last, help, timer, segment, center, results, menu; RectTransform boostFill; Image boostImage; GameObject resultsPanel, menuPanel;
        float lastSplitShown = -1; string splitLine = "";

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
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = g.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, .8f); sh.effectDistance = new Vector2(2, -2);
            var r = t.rectTransform; r.anchorMin = r.anchorMax = anchor; r.pivot = anchor; r.anchoredPosition = pos; r.sizeDelta = box; return t;
        }
        GameObject Panel(Transform p, Vector2 size, Color color)
        {
            var g = new GameObject("panel"); g.transform.SetParent(p, false); var i = g.AddComponent<Image>(); i.color = color;
            var r = i.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.sizeDelta = size; r.anchoredPosition = Vector2.zero; return g;
        }
        void Build(Transform p)
        {
            mode = Label(p, new Vector2(0, 0), new Vector2(24, 70), 40, TextAnchor.LowerLeft, new Vector2(600, 60));
            info = Label(p, new Vector2(1, 1), new Vector2(-24, -124), 20, TextAnchor.UpperRight, new Vector2(700, 120));
            timer = Label(p, new Vector2(1, 1), new Vector2(-24, -14), 46, TextAnchor.UpperRight, new Vector2(400, 56));
            segment = Label(p, new Vector2(1, 1), new Vector2(-24, -66), 22, TextAnchor.UpperRight, new Vector2(700, 50));
            last = Label(p, new Vector2(.5f, 1), new Vector2(0, -18), 22, TextAnchor.UpperCenter, new Vector2(900, 40));
            center = Label(p, new Vector2(.5f, .5f), new Vector2(0, 120), 90, TextAnchor.MiddleCenter, new Vector2(1200, 120));
            help = Label(p, new Vector2(.5f, 0), new Vector2(0, 8), 16, TextAnchor.LowerCenter, new Vector2(1250, 26));
            help.color = new Color(1, 1, 1, .75f);
            help.text = "RT/W go  LT/S brake  A/Space boost  RB/Shift drift-carve  stick Y / I-K climb-dive  Y/R reset  Menu/Esc courses  View/F2 invert";
            var bg = new GameObject("boost bg"); bg.transform.SetParent(p, false); var bgi = bg.AddComponent<Image>(); bgi.color = new Color(0, 0, 0, .55f);
            var br = bgi.rectTransform; br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0); br.anchoredPosition = new Vector2(24, 40); br.sizeDelta = new Vector2(320, 22);
            var fill = new GameObject("boost"); fill.transform.SetParent(bg.transform, false); boostImage = fill.AddComponent<Image>(); boostImage.color = new Color(1, .78f, .15f);
            boostFill = boostImage.rectTransform; boostFill.anchorMin = boostFill.pivot = new Vector2(0, 0); boostFill.anchorMax = new Vector2(0, 1); boostFill.anchoredPosition = new Vector2(3, 0); boostFill.sizeDelta = new Vector2(0, -6);
            boostFill.offsetMin = new Vector2(3, 3); boostFill.offsetMax = new Vector2(3, -3);
            var bl = Label(bg.transform, new Vector2(0, .5f), new Vector2(330, 0), 18, TextAnchor.MiddleLeft, new Vector2(120, 22)); bl.text = "BOOST";
            resultsPanel = Panel(p, new Vector2(620, 470), new Color(.05f, .07f, .06f, .82f));
            results = Label(resultsPanel.transform, new Vector2(.5f, .5f), Vector2.zero, 22, TextAnchor.MiddleCenter, new Vector2(600, 450)); results.supportRichText = true;
            resultsPanel.SetActive(false);
            menuPanel = Panel(p, new Vector2(520, 420), new Color(.05f, .07f, .06f, .88f));
            menu = Label(menuPanel.transform, new Vector2(.5f, .5f), Vector2.zero, 26, TextAnchor.MiddleCenter, new Vector2(500, 400)); menu.supportRichText = true;
            menuPanel.SetActive(false);
        }

        static string Form(Mode m) => m == Mode.Drive ? "CAR" : m == Mode.Boat ? "BOAT" : m == Mode.Fly ? "WINGS" : "SUB";
        static string Delta(float d) => (d >= 0 ? "+" : "-") + Mathf.Abs(d).ToString("0.00");

        void Update()
        {
            if (!v) return;
            string extra = v.Drifting ? "  DRIFT" : v.Carving ? "  CARVE" : "";
            mode.text = $"{Form(v.Mode)}{(v.Changing ? " ..." : "")}  {v.Speed * 2.237f:0} mph{extra}";
            boostFill.offsetMax = new Vector2(3 + 314 * v.Boost, -3); boostImage.color = v.Boosting ? new Color(1, .45f, .1f) : new Color(1, .78f, .15f);
            var c = v.Course; bool trial = c && c.timeTrial;
            float time = v.Finished ? v.FinishTime : v.Holding ? 0 : Time.time - v.StartTime;
            timer.text = TimeTrial.Clock(time);
            int gates = c ? c.gates.Count : 0;
            info.text = $"{(c ? c.courseName : "")}\nGates {v.GatesPassed}/{gates}   lap {100f * v.RaceDistance / Mathf.Max(1, gates):0}%   resets {v.Resets}\nInvert {(ShiftInput.Invert ? "ON" : "off")}";
            // the segment now, and the last split against its target
            if (trial && c.segments.Count > 0)
            {
                int i = Mathf.Min(v.Splits.Count, c.segments.Count - 1); var s = c.segments[i];
                if (v.Splits.Count > 0 && v.Splits[v.Splits.Count - 1] != lastSplitShown)
                {
                    int k = v.Splits.Count - 1; float t = v.Splits[k] - (k > 0 ? v.Splits[k - 1] : 0); lastSplitShown = v.Splits[k];
                    splitLine = $"{c.segments[k].name} {t:0.0} s ({Delta(t - c.segments[k].target)})";
                }
                if (v.Splits.Count == 0) splitLine = "";
                segment.text = v.Finished ? splitLine : $"{i + 1}/{c.segments.Count} {s.name} ({s.target:0} s)\n<size=18>{splitLine}</size>";
                segment.supportRichText = true;
            }
            else segment.text = "";
            // center: countdown, then cues
            if (v.Holding) { float left = v.HoldUntil - Time.time; center.fontSize = 110; center.color = new Color(1, .85f, .3f); center.text = left > 0 ? Mathf.CeilToInt(left).ToString() : "GO!"; }
            else if (trial && Time.time - v.StartTime < .9f && !v.Finished) { center.fontSize = 110; center.color = new Color(.5f, 1, .5f); center.text = "GO!"; }
            else if (Time.time < v.CueUntil && !v.Finished) { center.fontSize = 40; center.color = new Color(1, .45f, .3f); center.text = v.Cue; }
            else center.text = "";
            last.text = v.Finished ? $"FINISHED {TimeTrial.Clock(v.FinishTime)}  -  Y / R to race again" : v.LastEvent;
            // results
            var tt = game ? game.Trial : null;
            bool showResults = tt && tt.HasResults && v.Finished && !menuPanel.activeSelf;
            if (showResults != resultsPanel.activeSelf) resultsPanel.SetActive(showResults);
            if (showResults) results.text = ResultsText(tt, c);
            // menu
            bool open = game && game.MenuOpen;
            if (open != menuPanel.activeSelf) menuPanel.SetActive(open);
            if (open) menu.text = MenuText();
        }

        string ResultsText(TimeTrial tt, Course c)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size=30><b>{c.courseName}</b></size>");
            sb.AppendLine($"<size=40><b>{TimeTrial.Clock(tt.Lap)}</b></size>{(tt.NewBest ? "   <color=#ffd34d><b>NEW BEST!</b></color>" : "")}");
            sb.AppendLine($"best {TimeTrial.Clock(tt.Best != null ? tt.Best.lap : -1)}   resets {v.Resets}");
            sb.AppendLine();
            for (int i = 0; i < c.segments.Count; i++)
            {
                var s = c.segments[i]; float t = i < tt.SegmentTimes.Count ? tt.SegmentTimes[i] : -1;
                float vsBest = tt.BestBefore != null && tt.BestBefore.splits != null && i < tt.BestBefore.splits.Length && t >= 0 ? t - tt.BestBefore.splits[i] : float.NaN;
                string col = t < 0 ? "#cccccc" : t <= s.target ? "#8cff8c" : "#ffb070";
                sb.AppendLine($"{i + 1} {s.name,-17}  <color={col}>{(t >= 0 ? t.ToString("0.00") : "--")}</color>  target {s.target:0}{(float.IsNaN(vsBest) ? "" : $"  best {Delta(vsBest)}")}");
            }
            sb.AppendLine(); sb.Append("<size=20>Y / R: race again     Menu / Esc: courses</size>");
            return sb.ToString();
        }
        string MenuText()
        {
            var sb = new StringBuilder(); sb.AppendLine("<b>COURSES</b>\n");
            var items = game.MenuItems;
            for (int i = 0; i < items.Count; i++) sb.AppendLine(i == game.MenuIndex ? $"<color=#ffd34d><b>> {items[i]} <</b></color>" : items[i]);
            sb.AppendLine("\n<size=18>D-pad / arrows: choose   A / Enter: go   B / Esc: back</size>");
            return sb.ToString();
        }
    }
}
