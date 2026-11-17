using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftStorm
{
    // The HUD (uGUI on the main camera, so player-view pictures include it). Round 1: form and speed, the shared boost meter, the invert
    // setting, the last thing that happened, the controls. Round 2: the lap clock and the current segment with its target, the last split,
    // lap progress (gates + progress to the next gate), the countdown (3, 2, 1, GO), return cues (missed gate, wrong way), the results
    // (lap, splits against their targets and the best lap's, new best) and the course menu (Menu / Esc).
    // Round 2b: where to go (an arrow toward the route ahead when it is off to the side or behind, CLIMB / DIVE in the air and underwater, a
    // marker on the next gate with its distance, the route strip along the top: the lap by form, the gates, you), direction cues in colour
    // (red WRONG WAY / TURN BACK, yellow THIS WAY), a speedometer, speed lines at speed, and the FPS + speed readout (F1 / R3).
    public sealed partial class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }
        public ShiftVehicle v; public Game game;
        Text mode, info, last, help, timer, segment, center, results, place, note, sub; RectTransform boostFill; Image boostImage; GameObject resultsPanel, menuPanel;
        float lastSplitShown = -1; string splitLine = "";

        // Round 6 (split screen): one HUD per half, each on its player's camera (a Screen Space - Camera canvas fills that camera's half).
        // player: 0 the one player, 1 / 2 in split screen. Player 2's HUD has no menu of its own (the menu is on player 1's half; either
        // player works it) and shows PAUSED while it is open. Hud.Instance is player 1's.
        public int player; CanvasScaler scaler; Canvas canvasRef;
        // Round 15b (Dan on the Deck: "the second I switch to either the 2nd or 3rd race the screen shakes"): the cause. Build 17's canvas
        // (Screen Space - Camera) sat 0.22 m in front of the race camera, so it was a tiny thing (1 pixel ~ 0.3 mm) with world positions next
        // to the camera, and Unity builds and draws the UI's vertices through those world positions. Coaster Coast is 8 km east and Big Top
        // 9 km west of the origin, where a float holds a position to ~1 mm, ~4 pixels on the Deck's 800 lines at 0.22 m: as the camera
        // moved, the whole menu / HUD snapped back and forth by pixels (Paradise Plunge, near the origin, by a fraction of one). Now the
        // canvas stands 100 m ahead (1 mm there is ~0.01 pixel), sorted last, and ShiftStorm/HudUI draws each vertex from the canvas's own
        // coordinates straight into the camera's view at the old 0.22 m (the same depth against the cockpit and the scenery), never through
        // a world position. Unity's canvas scaler sizes it as before. "-oldHud": build 17's canvas (before numbers).
        public static readonly bool OldHud = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-oldHud") >= 0;
        public const float CanvasDistance = 100f;
        static readonly int IdLocalToView = Shader.PropertyToID("_HudLocalToView");
        Material uiMat; float planeDistance = .5f;
        public Material UiMaterial => uiMat;
        public static Hud Make(Camera cam, ShiftVehicle v, Game game, int player = 0)
        {
            var go = new GameObject("HUD"); var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = .5f;
            var scaler = go.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            var h = go.AddComponent<Hud>(); h.v = v; h.game = game; h.cam = cam; h.player = player; h.scaler = scaler; h.canvasRef = canvas; if (player != 2) Instance = h;
            var sh = OldHud ? null : Resources.Load<Shader>("Shaders/HudUI");
            if (sh) { h.uiMat = new Material(sh) { name = $"HUD {(player == 2 ? 2 : 1)}" }; canvas.planeDistance = CanvasDistance; canvas.sortingOrder = 32000; }
            else if (!OldHud) Debug.LogWarning("[HUD] Shaders/HudUI missing: build 17's canvas");
            h.Build(go.transform); h.BuildGuide(go.transform); h.Relayout(); h.UseMaterial(); return h;
        }
        // every graphic on the HUD's shader (rows, cards and markers made later too: checked every half second)
        void UseMaterial() { if (!uiMat) return; foreach (var g in GetComponentsInChildren<Graphic>(true)) if (g.material != uiMat) g.material = uiMat; }
        void OnEnable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforeRender;
        void OnDisable() => UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforeRender;
        void BeforeRender(UnityEngine.Rendering.ScriptableRenderContext ctx, Camera c)
        {
            if (!uiMat || c != cam || !canvasRef) return;
            // canvas units -> the camera's view: the canvas faces the camera, centred on its view; drawn at planeDistance (not where it
            // stands), scaled to match (Unity's view space looks down -z)
            float d = planeDistance, k = transform.lossyScale.x * d / Mathf.Max(.01f, canvasRef.planeDistance);
            var m = Matrix4x4.identity; m.m00 = k; m.m11 = k; m.m22 = -k; m.m23 = -d;
            uiMat.SetMatrix(IdLocalToView, m);
        }
        void OnDestroy() { if (uiMat) Destroy(uiMat); }
        // the canvas for the camera's share of the screen: whole (1280 x 720 reference, as before); top / bottom halves (1280 x 600: a
        // little smaller); left / right halves (960 wide, by width)
        public void Relayout()
        {
            if (!scaler) return;
            bool split = SplitScreen.Active, lr = split && SplitScreen.LeftRight;
            scaler.referenceResolution = !split ? new Vector2(1280, 720) : lr ? new Vector2(960, 720) : new Vector2(1280, 600);
            scaler.matchWidthOrHeight = lr ? 0 : 1;
            if (resultsPanel) resultsPanel.transform.localScale = Vector3.one * (split && !lr ? .82f : 1f);
            if (menuPanel) menuPanel.transform.localScale = Vector3.one;
            if (help) help.enabled = !split;
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
            lapText = Label(p, new Vector2(0, 1), new Vector2(24, -186), 36, TextAnchor.UpperLeft, new Vector2(400, 50)); lapText.supportRichText = true; lapText.color = new Color(.65f, .93f, 1f);   // Round 16: LAP 2 / 3
            center = Label(p, new Vector2(.5f, .5f), new Vector2(0, 120), 90, TextAnchor.MiddleCenter, new Vector2(1200, 120));
            help = Label(p, new Vector2(.5f, 0), new Vector2(0, 8), 16, TextAnchor.LowerCenter, new Vector2(1250, 26));
            help.color = new Color(1, 1, 1, .75f);
            help.text = "RT/W go  LT/S brake  A/Space boost  RB/Shift drift-carve  X/V view  stick Y / I-K climb-dive  Y/R reset  Start/Esc pause  R3/F1 FPS";
            // Round 3: race position (big, top left under the stats) and the one-time note on the first flight
            place = Label(p, new Vector2(0, 1), new Vector2(24, -70), 54, TextAnchor.UpperLeft, new Vector2(400, 120)); place.supportRichText = true;
            note = Label(p, new Vector2(.5f, .5f), new Vector2(0, -150), 30, TextAnchor.MiddleCenter, new Vector2(900, 40)); note.color = new Color(.75f, .95f, 1f);
            sub = Label(p, new Vector2(.5f, 0), new Vector2(0, 118), 24, TextAnchor.LowerCenter, new Vector2(1100, 34)); sub.supportRichText = true;   // Round 7: the caller's subtitle
            var bg = new GameObject("boost bg"); bg.transform.SetParent(p, false); var bgi = bg.AddComponent<Image>(); bgi.color = new Color(0, 0, 0, .55f);
            var br = bgi.rectTransform; br.anchorMin = br.anchorMax = br.pivot = new Vector2(0, 0); br.anchoredPosition = new Vector2(24, 40); br.sizeDelta = new Vector2(320, 22);
            var fill = new GameObject("boost"); fill.transform.SetParent(bg.transform, false); boostImage = fill.AddComponent<Image>(); boostImage.color = new Color(1, .78f, .15f);
            boostFill = boostImage.rectTransform; boostFill.anchorMin = boostFill.pivot = new Vector2(0, 0); boostFill.anchorMax = new Vector2(0, 1); boostFill.anchoredPosition = new Vector2(3, 0); boostFill.sizeDelta = new Vector2(0, -6);
            boostFill.offsetMin = new Vector2(3, 3); boostFill.offsetMax = new Vector2(3, -3);
            var bl = Label(bg.transform, new Vector2(0, .5f), new Vector2(330, 0), 18, TextAnchor.MiddleLeft, new Vector2(120, 22)); bl.text = "BOOST";
            resultsPanel = Panel(p, new Vector2(680, 640), new Color(.05f, .07f, .06f, .82f));
            results = Label(resultsPanel.transform, new Vector2(.5f, .5f), Vector2.zero, 21, TextAnchor.MiddleCenter, new Vector2(660, 620)); results.supportRichText = true;
            resultsPanel.SetActive(false);
            BuildMenu(p); menuPanel = menuRoot;   // Round 14 item 1: the menus (Hud.Menu)
            // Round 11 item 2: Buck's card (a briefing, a Tour scene): portrait placeholder, his name, the subtitle, the skip hint
            buckCard = Panel(p, new Vector2(860, 170), new Color(.08f, .05f, .03f, .88f));
            var bkr = buckCard.GetComponent<RectTransform>(); bkr.anchorMin = bkr.anchorMax = bkr.pivot = new Vector2(.5f, 0); bkr.anchoredPosition = new Vector2(0, 150);
            var pg = new GameObject("Buck portrait"); pg.transform.SetParent(buckCard.transform, false); var pi = pg.AddComponent<RawImage>(); pi.texture = Buck.Portrait;
            var pr = pi.rectTransform; pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0, .5f); pr.anchoredPosition = new Vector2(14, 0); pr.sizeDelta = new Vector2(144, 144);
            var bt = Label(buckCard.transform, new Vector2(0, 1), new Vector2(176, -12), 22, TextAnchor.UpperLeft, new Vector2(660, 30)); bt.text = Buck.Title; buckTitle = bt; bt.color = new Color(1f, .78f, .3f);
            buckSub = Label(buckCard.transform, new Vector2(0, 1), new Vector2(176, -44), 22, TextAnchor.UpperLeft, new Vector2(660, 100)); buckSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            buckHint = Label(buckCard.transform, new Vector2(1, 0), new Vector2(-14, 10), 16, TextAnchor.LowerRight, new Vector2(300, 22)); buckHint.color = new Color(.85f, .85f, .75f);
            buckCard.SetActive(false);
        }
        GameObject buckCard; Text buckSub, buckHint, buckTitle;
        public bool BuckCardShown => buckCard && buckCard.activeSelf;

        Camera cam;
        static string Form(Mode m) => m == Mode.Drive ? "CAR" : m == Mode.Boat ? "BOAT" : m == Mode.Fly ? "WINGS" : "SUB";
        static string Delta(float d) => (d >= 0 ? "+" : "-") + Mathf.Abs(d).ToString("0.00");

        bool garageHidden;
        void Update()
        {
            if (!v) return;
            if (player == 2 && menuPanel.activeSelf) menuPanel.SetActive(false);
            // Round 6: the HUD's plane just past the camera's near clip, so the cockpit in first person (near clip 5 cm) never covers it
            if (canvasRef && cam) { planeDistance = Mathf.Min(.5f, cam.nearClipPlane + .02f); if (!uiMat) canvasRef.planeDistance = planeDistance; }
            if (uiMat && Time.frameCount % 30 == 0) UseMaterial();   // Round 15b: rows / cards made later on the HUD's shader too
            string extra = v.Drifting ? "  DRIFT" : v.Carving ? "  CARVE" : "";
            mode.text = $"{Form(v.Mode)}{(v.Changing ? " ..." : "")}{extra}";
            UpdateGuide();
            var bk = Buck.Instance; bool card = bk && (bk.CardOn || (bk.Voice && bk.Voice.isPlaying && bk.Subtitle != ""));
            if (buckCard && card != buckCard.activeSelf) buckCard.SetActive(card);
            if (card) { buckSub.text = bk.Subtitle; buckHint.text = bk.CardOn ? bk.Hint : ""; if (buckTitle) buckTitle.text = bk.Speaker; }
            boostFill.offsetMax = new Vector2(3 + 314 * v.Boost, -3); boostImage.color = v.Boosting ? new Color(1, .45f, .1f) : new Color(1, .78f, .15f);
            var c = v.Course; bool trial = c && c.timeTrial;
            float time = v.Finished ? v.FinishTime : v.Holding ? 0 : Time.time - v.StartTime + v.Penalty;   // Round 4: near-miss penalties count at once
            timer.text = TimeTrial.Clock(time);
            int gates = c ? c.gates.Count : 0;
            info.text = $"{(c ? c.courseName : "")}\nGates {v.GatesPassed}/{gates}   lap {100f * v.RaceDistance / Mathf.Max(1, gates):0}%   resets {v.Resets}\nInvert {(v.Input.MyInvert ? "ON" : "off")}{(SplitScreen.Active ? $"   view {ViewName}" : "")}";
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
            if (Buck.Instance && Buck.Instance.CardOn) center.text = "";   // Round 11: no frozen countdown number under Buck's card
            else if (game && game.ChallengeRun && game.ChallengeRun.Frozen && player != 2) { center.fontSize = 34; center.color = Challenge.Note.StartsWith("CHALLENGE WON") ? new Color(.5f, 1, .5f) : new Color(1, .6f, .25f); center.text = Challenge.Note.Replace(": ", "\n").Replace("  -  ", "\n"); }   // Round 13: a rival challenge's result
            else if (v.Holding) { float left = v.HoldUntil - Time.time; center.fontSize = 110; center.color = new Color(1, .85f, .3f); center.text = left > 5 ? "" : left > 0 ? Mathf.CeilToInt(left).ToString() : "GO!"; }   // Round 13: a long hold (a challenge's lines) shows no number
            else if (trial && v.HoldUntil > 0 && Time.time - v.HoldUntil < .9f && !v.Finished) { center.fontSize = 110; center.color = new Color(.5f, 1, .5f); center.text = "GO!"; }
            else if (v.WingsOpen && (v.GroundedWheels == 0 || !ShiftVehicle.OldChoice))   // Round 7: the choice jump's wings window; Round 14: from the ramp on
            { center.fontSize = 64; center.color = v.WingsArmed ? new Color(.5f, 1f, .55f) : new Color(.55f, .95f, 1f); center.text = v.WingsArmed ? "WINGS READY" : v.Input.Device is UnityEngine.InputSystem.Keyboard ? "E: FLY" : "LB: FLY"; }
            else if (Time.time < lapFlashUntil && !v.Finished) { center.fontSize = 76; center.color = lapFlashText.StartsWith("FINAL") ? new Color(1f, .55f, .25f) : new Color(.65f, .93f, 1f); center.text = lapFlashText; }   // Round 16
            else if (Time.time < v.CueUntil && !v.Finished)
            {
                center.fontSize = v.CueType == ShiftVehicle.CueKind.Guide ? 44 : 52;
                center.color = v.CueType == ShiftVehicle.CueKind.Wrong ? new Color(1, .3f, .22f) : v.CueType == ShiftVehicle.CueKind.Warn ? new Color(1, .6f, .25f) : new Color(1, .9f, .35f);
                center.text = v.Cue;
            }
            else center.text = "";
            last.text = v.Finished ? $"FINISHED {TimeTrial.Clock(v.FinishTime)}  -  Y / R to race again" : v.LastEvent;
            // Round 7: the race caller's words
            var cl = Caller.Instance; if (sub) sub.text = cl && Time.time < cl.SubtitleUntil && cl.Subtitle != "" && player != 2 ? $"<color=#ffd34d><b>{Caller.Name}:</b></color> {cl.Subtitle}" : "";
            // Round 16: the lap on a multi-lap course (and a flash as each lap starts)
            if (c && c.laps > 1 && !v.Holding)
            {
                int lap = c.LapAt(v.Progress); if (v.Finished) lap = c.laps;
                lapText.text = $"LAP <b>{lap}</b><size=24> / {c.laps}</size>";
                if (lap != lapShown) { if (lapShown > 0 && lap > lapShown && !v.Finished) { lapFlashText = lap == c.laps ? "FINAL LAP!" : $"LAP {lap} / {c.laps}"; lapFlashUntil = Time.time + 2.2f; } lapShown = lap; }
            }
            else { lapText.text = c && c.laps > 1 ? $"LAP <b>1</b><size=24> / {c.laps}</size>" : ""; lapShown = c && c.laps > 1 && v.Holding ? 1 : lapShown; }
            // race position
            var race = game ? game.Race : null;
            if (race && race.Active && c) { int pl = race.Place(v); place.text = $"{Race.Ordinal(pl)}<size=28> / {race.Racers.Count}</size>"; place.color = pl == 1 ? new Color(1, .85f, .3f) : Color.white; }
            else place.text = "";
            // the first flight of this game: where the invert setting is (until it has been set once)
            if (v.Mode == Mode.Fly && !v.Holding && !firstFlightSeen && !Settings.InvertChosen) { firstFlightSeen = true; noteUntil = Time.time + 5f; }
            note.text = Time.time < noteUntil ? "Invert controls: Menu > Settings" : "";
            if (player == 2 && game && game.MenuOpen) { center.fontSize = 70; center.color = new Color(1, .85f, .3f); center.text = "PAUSED"; }
            // results
            var tt = game ? game.TrialOf(v) : null;
            bool showResults = tt && tt.HasResults && v.Finished && !menuPanel.activeSelf;
            if (showResults != resultsPanel.activeSelf) resultsPanel.SetActive(showResults);
            if (showResults) results.text = ResultsText(tt, c);
            sub.rectTransform.anchoredPosition = new Vector2(0, showResults ? 6 : 118); if (help) help.enabled = !SplitScreen.Active && !showResults;   // Round 14: Barnaby's words under the results panel, not over its buttons
            // menu (Round 14: Hud.Menu)
            bool open = game && game.MenuOpen && player != 2;
            // Round 5: the Garage hides the race HUD (everything but the menu) while it is open; Round 14: so do the front menus (title, main menu)
            bool gar = Garage.Active || (game && game.MenuOpen && game.InFront);
            if (gar != garageHidden) { garageHidden = gar; foreach (Transform ch in transform) if (ch.gameObject != menuPanel) ch.gameObject.SetActive(!gar); }
            UpdateMenu(open);
        }

        string ViewName { get { var cv = cam ? cam.GetComponent<CameraViews>() : null; return cv ? cv.CurrentName : "-"; } }
        // checks (Round 6): what this half shows
        public string SpeedText => speedo ? speedo.text : ""; public string PlaceText => place ? place.text : ""; public string TimerText => timer ? timer.text : "";
        public string ModeText => mode ? mode.text : ""; public string CenterText => center ? center.text : ""; public string InfoText => info ? info.text : "";
        public bool ResultsShown => resultsPanel && resultsPanel.activeSelf; public string ResultsBody => results ? results.text : "";
        public Camera Cam => cam;
        bool firstFlightSeen; float noteUntil = -1;
        Text lapText; int lapShown; float lapFlashUntil = -1; string lapFlashText = "";
        public string LapText => lapText ? lapText.text : ""; public string LapFlash => Time.time < lapFlashUntil ? lapFlashText : "";
        public string NoteText => note ? note.text : "";
        string ResultsText(TimeTrial tt, Course c)
        {
            var sb = new StringBuilder(); var race = game.Race;
            sb.AppendLine($"<size=30><b>{c.courseName}</b></size>{(race && race.Active ? $"   <size=24>RACE: <b>{Race.Ordinal(race.Place(v))}</b> of {race.Racers.Count}</size>" : "   <size=22>TIME TRIAL</size>")}");
            // Round 11: The Tour's result at the top (Buck's card covers the bottom while he talks)
            if (Tour.ActiveStop >= 0 && Tour.Note != "" && player != 2) sb.AppendLine($"<size=19><color=#ffd34d>{Tour.Note}</color>   (total {Tour.Total} points)</size>");
            if (race && race.Active)
            {
                var order = race.Order();
                for (int i = 0; i < order.Count; i++)
                {
                    var r = order[i]; int fi = race.Finishers.FindIndex(f => f.v == r);
                    string col = ColorUtility.ToHtmlStringRGB(r.Tint), t = fi >= 0 ? TimeTrial.Clock(race.Finishers[fi].time) : $"racing (gate {r.GatesPassed}/{c.gates.Count})";
                    string who = SplitScreen.Active ? (r == game.Player ? "  <b>[P1]</b>" : r == game.Player2 ? "  <b>[P2]</b>" : "") : "";
                    sb.AppendLine($"{(r == v ? "<b>" : "")}{i + 1}. <color=#{col}>{r.RacerName}</color>{who}   {t}{(r.Penalty > 0 ? $"  <color=#ffb070>(+{r.Penalty:0} s: {Missed(r)})</color>" : "")}{(r == v ? "</b>" : "")}");
                }
                sb.AppendLine();
            }
            sb.AppendLine($"<size=40><b>{TimeTrial.Clock(tt.Lap)}</b></size>{(tt.NewBest ? "   <color=#ffd34d><b>NEW BEST!</b></color>" : "")}");
            if (c.laps > 1 && tt.SegmentTimes.Count > 0) { float bl = Mathf.Min(tt.SegmentTimes.ToArray()); int bi = tt.SegmentTimes.IndexOf(bl); sb.AppendLine($"<size=24>best lap this race <b>{TimeTrial.Clock(bl)}</b> (lap {bi + 1}){(tt.NewBestLap ? "   <color=#ffd34d><b>NEW BEST LAP!</b></color>" : "")}   best lap ever {TimeTrial.Clock(tt.Best != null && tt.Best.single > 0 ? tt.Best.single : -1)}</size>"); }   // Round 16
            sb.AppendLine($"best {TimeTrial.Clock(tt.Best != null ? tt.Best.lap : -1)}   resets {v.Resets}{(v.Penalty > 0 ? $"   <color=#ffb070>penalty +{v.Penalty:0} s (missed {Missed(v)})</color>" : "")}");
            sb.AppendLine();
            for (int i = 0; i < c.segments.Count; i++)
            {
                var s = c.segments[i]; float t = i < tt.SegmentTimes.Count ? tt.SegmentTimes[i] : -1;
                float vsBest = tt.BestBefore != null && tt.BestBefore.splits != null && i < tt.BestBefore.splits.Length && t >= 0 ? t - tt.BestBefore.splits[i] : float.NaN;
                string col = t < 0 ? "#cccccc" : t <= s.target ? "#8cff8c" : "#ffb070";
                sb.AppendLine($"{i + 1} {s.name,-17}  <color={col}>{(t >= 0 ? t.ToString("0.00") : "--")}</color>  target {s.target:0}{(float.IsNaN(vsBest) ? "" : $"  best {Delta(vsBest)}")}");
            }
            sb.AppendLine($"<size=17><color=#c8c8b4>Race called by {Caller.Name}</color></size>");   // Round 7
            if (SplitScreen.Active && !game.AllHumansFinished) { sb.Append("<color=#ffd34d>Waiting for the other player to finish...</color>"); return sb.ToString(); }
            var items = game.ResultsItems;
            for (int i = 0; i < items.Length; i++) { var it = items[i]; sb.Append(i == game.ResultsIndex ? $"<size=26><color=#ffd34d><b>► {it}</b></color></size>     " : $"<size=24>{it}</size>     "); }
            sb.AppendLine(); sb.Append("<size=18>D-pad / stick: choose    A / Enter: go    Y / R: race again</size>");
            return sb.ToString();
        }
        static string Missed(ShiftVehicle r) => string.Join(", ", r.NearMisses.ConvertAll(x => x.Split(' ')[0]));
    }
}
