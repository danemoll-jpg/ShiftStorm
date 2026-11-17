using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftStorm
{
    // Start-up: loads the tuning (Resources/Tuning), builds the world (Round 10: Paradise Plunge and Coaster Coast; the Round 1 test courses
    // are gone), makes the player's vehicle with Abi in the seat, the three AI rivals (Round 3), the camera, HUD, time trial / race and course
    // sound. A check run ("-check <name>") starts a course straight away ("-course <n>": 0 Paradise Plunge, 1 Coaster Coast; "-trial" alone,
    // "-race" against the rivals) and hands over to CheckPilot; so do "-race", "-trial" and "-course" on an ordinary start.
    // Round 14 item 1 (Dan: "I don't like the menu in general. Needs clean up to be more readable", "Left and right should increase /
    // decrease values", and the game must start on a title screen, not in a race): the menus, drawn by Hud.Menu:
    //   Title       the logo, the theme song, "Press A / Start" (any button); the course flies by behind it (MenuCam)
    //   Main        The Tour, Race, Time Trial, 2 Players, Garage, Settings, Quit game (B: back to the title)
    //   Course      a card per track (name, picture, the forms it uses, best time); left / right picks, A goes on to:
    //   Pick        choose your racer: driver, machine, form (left / right), then Go
    //   Settings    Gameplay / Audio / Video / Controls, one setting per row; left / right changes the value (sliders for the volumes,
    //               cycling for the options), A also changes it, B goes back; the new value shows at once
    //   Pause       in a race (Menu / Esc): Resume, Restart, Settings, Quit to menu (+ the 2 Players rows while split)
    //   results     Race again, Next (the next course, or the next Tour stop), Menu
    // One highlighted row, a description line for it, the same layout on every page; D-pad, the left stick and the keyboard everywhere;
    // either player works the menus in split screen. The race behind the front menus is frozen (Time.timeScale 0) and hidden.
    // Round 6: Menu > 2 Players (split screen, SplitScreen): player 1 is the device that opened the page, player 2 joins with A on another
    // controller (Enter on the keyboard); Race (with 0-2 AI) or Time Trial (the two alone).
    public sealed class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public List<Course> Courses { get; private set; }
        public ShiftVehicle Player { get; private set; }
        public Camera Cam { get; private set; }
        public TimeTrial Trial { get; private set; }
        public Race Race { get; private set; }
        public int CourseIndex { get; private set; }
        public bool RaceMode { get; private set; }   // the current course is raced against the rivals
        public int MenuCourse { get; private set; }  // the course the menus' Race / Time Trial / 2 Players start (the one last started)
        public ShiftTuning Shared; public DriveTuning Drive; public BoatTuning Boat; public FlyTuning Fly; public DiveTuning Dive;
        public readonly List<ShiftVehicle> Rivals = new List<ShiftVehicle>();

        // ---- the menu ----
        public enum Page { Main, Settings, Quit, Garage, Split, Tour, Pick, Title, Pause, Course, SetGameplay, SetAudio, SetVideo, SetControls }
        public enum PickFor { Race, Trial, Tour }
        PickFor pickFor; int pickStop; public TourRunner TourRun { get; private set; }
        public ChallengeRunner ChallengeRun { get; private set; }   // Round 13 item 4
        public string MenuNote { get; private set; } = "";
        // Round 6: the second player (split screen) and both players
        public ShiftVehicle Player2 => SplitScreen.Session ? SplitScreen.Session.P2 : null;
        public List<ShiftVehicle> Humans { get { var l = new List<ShiftVehicle> { Player }; if (Player2) l.Add(Player2); return l; } }
        public TimeTrial TrialOf(ShiftVehicle v) => v && v == Player2 && SplitScreen.Session ? SplitScreen.Session.Trial2 : Trial;
        public bool AllHumansFinished { get { foreach (var h in Humans) if (!h.Finished || !TrialOf(h).HasResults) return false; return true; } }
        // the AI racers in this race: all three alone; in split screen the ones player 2 did not pick, up to SplitScreen.AiCount
        public List<ShiftVehicle> ActiveRivals()
        {
            var l = new List<ShiftVehicle>();
            for (int k = 0; k < Rivals.Count; k++) { if (SplitScreen.Active && Rivals[k].RacerName == Lineup.P2.name) continue; if (SplitScreen.Active && l.Count >= SplitScreen.AiCount) break; l.Add(Rivals[k]); }   // Round 11: the AI = the drivers no player picked
            return l;
        }
        // split screen: the two players side by side on the back row (11 m apart, the same distance to the line); the AI ahead
        public static readonly Vector3 SplitGridP1 = new Vector3(-5.5f, 0, -25f), SplitGridP2 = new Vector3(5.5f, 0, -25f);
        public bool MenuOpen { get; private set; }
        public bool InFront { get; private set; }   // Round 14: the front menus (title, main menu and what opens from it): no race to go back to
        public Page MenuPage { get; private set; }
        public int MenuIndex { get; private set; }
        // Round 14: one row of a menu page: its label, its value (shown "< value >" when left / right change it), what A does, what left /
        // right do, a slider (0..1) for a volume, a description line; a row with neither action is information only (never selected)
        public sealed class MenuRow
        {
            public string label, value, desc = ""; public Action act; public Action<int> side; public float slider = -1; public bool card;
            public bool Selectable => act != null || side != null;
            public string Text => value == null ? label : $"{label}: {value}";
        }
        public List<MenuRow> Rows { get; } = new List<MenuRow>();
        public List<string> MenuItems => Rows.ConvertAll(r => r.Text);   // checks: each row's text
        public string MenuDescription => MenuIndex >= 0 && MenuIndex < Rows.Count ? Rows[MenuIndex].desc : "";
        public int ResultsIndex { get; private set; }
        public string[] ResultsItems => new[] { "Race again", NextLabel, "Menu" };
        public bool QuitChosen { get; private set; }
        Page settingsFrom = Page.Main, quitFrom = Page.Main;
        public static bool StartOnTitle { get { if (QuietRun.Check) return false; var a = Environment.GetCommandLineArgs(); return Array.IndexOf(a, "-race") < 0 && Array.IndexOf(a, "-trial") < 0 && Array.IndexOf(a, "-course") < 0; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!Instance) new GameObject("Game").AddComponent<Game>(); }

        static T Load<T>(string n) where T : ScriptableObject { var t = Resources.Load<T>("Tuning/" + n); if (!t) { Debug.LogWarning("[Game] tuning asset missing, defaults used: " + n); t = ScriptableObject.CreateInstance<T>(); } return t; }

        void Awake()
        {
            Instance = this;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);   // a log line must not cost a frame spike
            Application.targetFrameRate = QuietRun.Check ? -1 : 60; QualitySettings.vSyncCount = QuietRun.Check ? 0 : 1;
            // Round 2b: movement never slows with the frame rate: physics steps at a fixed 60 Hz and up to 15 steps catch up per frame
            // (maximumDeltaTime 0.25 s), so the game runs at full speed down to 4 fps; the readout (R3 / F1) shows steps per frame.
            Time.fixedDeltaTime = 1f / 60f; Time.maximumDeltaTime = .25f;
            Hud.StatsOn = !QuietRun.Check && Settings.ShowStats;
            Physics.IgnoreLayerCollision(ShiftVehicle.GhostLayer, ShiftVehicle.ObstacleLayer, true);
            Physics.IgnoreLayerCollision(ShiftVehicle.GhostLayer, ShiftVehicle.VehicleLayer, true);   // a racer just reset never lands on another
            Physics.IgnoreLayerCollision(ShiftVehicle.GhostLayer, ShiftVehicle.GhostLayer, true);
            Shared = Load<ShiftTuning>("Shift"); Drive = Load<DriveTuning>("Drive"); Boat = Load<BoatTuning>("Boat"); Fly = Load<FlyTuning>("Fly"); Dive = Load<DiveTuning>("Dive");
            Courses = WorldBuilder.Build();
            Player = VehicleFactory.MakePlayer(Shared, Drive, Boat, Fly, Dive);
            Driver.Make(Player);
            foreach (var r in Rival.Roster) { var rv = VehicleFactory.MakeRival(r, Shared, Drive, Boat, Fly, Dive); Rivals.Add(rv); rv.gameObject.SetActive(false); }
            Cam = Camera.main; if (!Cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; Cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
            Cam.farClipPlane = 2600; Cam.nearClipPlane = .2f;
            { var cull = new float[32]; cull[ParadisePlunge.DetailLayer] = ParadisePlunge.DetailCullDistance; Cam.layerCullDistances = cull; Cam.layerCullSpherical = true; }   // Round 5: the polished stretch's detail
            Cam.fieldOfView = 65; Cam.clearFlags = CameraClearFlags.Skybox;
            GraphicsPreset.Apply();   // Round 13 item 5: Auto = Deck on a Steam Deck, High elsewhere
            if (!FindAnyObjectByType<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.gameObject.AddComponent<AudioMeter>();
            Cam.gameObject.AddComponent<ChaseCam>().target = Player;
            Cam.gameObject.AddComponent<CameraViews>();   // Round 6: X / V cycles Chase, Far chase, First person, Front
            Cam.gameObject.AddComponent<MenuCam>();       // Round 14: the course flying by behind the front menus
            CourseAudio.Make(Cam.gameObject, Player);
            Caller.Make(this).Watch(Player);   // Round 7: Barnaby calls the race
            Buck.Make(this).Watch(Player);   // Round 11: Buck briefs before a race and remarks after it
            TourRun = TourRunner.Make(this);   // Round 11: The Tour
            ChallengeRun = ChallengeRunner.Make(this);   // Round 13: rival challenges
            GameSounds.Make(Cam.gameObject, this).Watch(Player);   // Round 7: the sound pass
            PlaytestLog.Make(this);   // Round 8 item 10: playtest logs
            Trial = gameObject.AddComponent<TimeTrial>(); Trial.v = Player;
            Race = gameObject.AddComponent<Race>();
            Hud.Make(Cam, Player, this);
            gameObject.AddComponent<RouteGuide>().v = Player;
            if (!FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -30, 0); l.shadows = LightShadows.Soft; }
            var a = Environment.GetCommandLineArgs();
            Lineup.Apply(this);   // Round 11: the saved driver / machine picks (Abi + Jetstream by default); the rest are the AI
            int start = 0; int i = Array.IndexOf(a, "-course"); if (i >= 0 && i + 1 < a.Length) int.TryParse(a[i + 1], out start);
            bool race = Array.IndexOf(a, "-race") >= 0 || (!QuietRun.Check && Array.IndexOf(a, "-trial") < 0);
            bool title = StartOnTitle;
            StartCourse(Mathf.Clamp(start, 0, Courses.Count - 1), race, !title);   // Round 14: no briefing under the title screen
            // Round 8 item 8: every pipeline, mesh and texture made now (start-up), not at first sight mid-race
            if (Array.IndexOf(a, "-noWarmup") < 0)
            {
                var was = new List<bool>(); foreach (var rv in Rivals) { was.Add(rv.gameObject.activeSelf); rv.gameObject.SetActive(true); }
                var sets = new List<FormSet> { Player.Forms }; foreach (var rv in Rivals) sets.Add(rv.Forms);
                foreach (var wc in Courses) { CourseLook.Apply(wc); Warmup.Run(wc, Cam, sets); }   // Round 10: every course
                CourseLook.Apply(Courses[CourseIndex]);
                for (int k = 0; k < Rivals.Count; k++) Rivals[k].gameObject.SetActive(was[k]);
            }
            CourseCards.Make(this);   // Round 14: each course's picture for the course cards
            Debug.Log($"[Screen] at the first frame: {Screen.fullScreenMode} {Screen.width} x {Screen.height}, desktop {Display.main.systemWidth} x {Display.main.systemHeight}, current resolution {Screen.currentResolution.width} x {Screen.currentResolution.height}");
            Debug.Log($"[Time] fixed step {Time.fixedDeltaTime * 1000:0.00} ms, maximum frame catch-up {Time.maximumDeltaTime:0.00} s, target frame rate {Application.targetFrameRate}, vSync {QualitySettings.vSyncCount}");
            Debug.Log($"[Game] ready: {Courses.Count} courses, model {VehicleFactory.ModelSource}, driver {Driver.Instance?.Source}, {Rivals.Count} rivals, muted {QuietRun.Muted}, listener volume {AudioListener.volume}, starts on {(title ? "the title screen" : "a course")}");
            if (title) ShowTitle();
            if (QuietRun.Check) gameObject.AddComponent<CheckPilot>();
        }

        // Start (or restart) a course. race: against the rivals on the grid (only a time-trial course, Paradise Plunge); otherwise alone.
        public void StartCourse(int index) => StartCourse(index, RaceMode);
        public void StartCourse(int index, bool race) => StartCourse(index, race, true);
        // Round 11: a stop of The Tour (its scene instead of the briefing)
        public void StartTourCourse(int index) => StartCourse(index, true, false);
        void StartCourse(int index, bool race, bool brief)
        {
            if (MenuOpen && InFront) CloseMenuForced();
            if (ChallengeRun) ChallengeRun.Abort();   // Round 13: any other start ends a rival challenge
            InFront = false;
            CourseIndex = (index % Courses.Count + Courses.Count) % Courses.Count; MenuCourse = CourseIndex;
            var c = Courses[CourseIndex];
            // Round 11: Buck's briefing before a course's first race / time trial of the session (Barnaby's start line waits for it)
            brief = brief && Buck.Instance && Buck.Instance.WantsBriefing(CourseIndex) && c.timeTrial;
            if (brief) Buck.Instance.Hold();
            try { StartCourseInner(c, race); } finally { if (brief) Buck.Instance.Briefing(CourseIndex); }
        }
        void StartCourseInner(Course c, bool race)
        {
            RaceMode = race && c.timeTrial;
            CourseLook.Apply(c);   // Round 10: only this course is shown (its look and sky); the other one is hidden
            CourseAudio.Instance?.SetCourse(c);
            ResultsIndex = 0;
            var p2 = Player2;
            if (p2)
            {
                // Round 6: two players (race: with the AI racers ahead of them; time trial: the two alone)
                Player.Begin(c, null, SplitGridP1); p2.Begin(c, null, SplitGridP2);
                var ai = RaceMode ? ActiveRivals() : new List<ShiftVehicle>();
                for (int k = 0; k < Rivals.Count; k++) { var rv = Rivals[k]; int at = ai.IndexOf(rv); rv.gameObject.SetActive(at >= 0); if (at >= 0) rv.Begin(c, null, Race.GridSlot(Race.RivalSlot(at))); }
                var field = new List<ShiftVehicle> { p2 }; field.AddRange(ai);
                Race.Setup(true, Player, field);
                return;
            }
            if (RaceMode)
            {
                Player.Begin(c, null, Race.GridSlot(Race.PlayerSlot));
                for (int k = 0; k < Rivals.Count; k++) { var rv = Rivals[k]; rv.gameObject.SetActive(true); rv.Begin(c, null, Race.GridSlot(Race.RivalSlot(k))); }
            }
            else { Player.Begin(c); foreach (var rv in Rivals) rv.gameObject.SetActive(false); }
            Race.Setup(RaceMode, Player, RaceMode ? Rivals : null);
        }
        // Reset after the finish (Y / R), or Race again on the results: the same course again, the same way
        public void Restart()
        {
            if (Challenge.Last >= 0 && (Challenge.Active != null || ChallengeRun.Frozen)) { ChallengeRun.Play(Challenge.Last); return; }   // Round 13: replay the rival challenge
            if (Tour.ActiveStop >= 0 && !Tour.Running) { TourRun.Play(Tour.ActiveStop, false); return; }   // Round 11: retry the Tour stop (no scene)
            StartCourse(CourseIndex, RaceMode);
        }
        // Round 14: the results' Next: the next Tour stop once it is open, else the next course the same way (race / time trial)
        bool NextIsStop => Tour.ActiveStop >= 0 && Tour.ActiveStop + 1 < Tour.Stops.Length && Tour.ActiveStop + 1 < Tour.Unlocked;
        string NextLabel => NextIsStop ? $"Next stop: {Tour.Stops[Tour.ActiveStop + 1].title}" : $"Next: {Courses[(CourseIndex + 1) % Courses.Count].courseName}";
        public void Next()
        {
            if (NextIsStop) { TourRun.Play(Tour.ActiveStop + 1, true); return; }
            Tour.ActiveStop = -1; StartCourse((CourseIndex + 1) % Courses.Count, RaceMode);
        }
        // Round 13 item 4: a rival challenge: the player and one rival side by side at an anchor part-way round the lap (the others parked)
        public void StartChallenge(int index, RecoveryAnchor at, ShiftVehicle rival)
        {
            if (SplitScreen.Active) EndSplit(false);
            InFront = false;
            CourseIndex = (index % Courses.Count + Courses.Count) % Courses.Count; MenuCourse = CourseIndex; var c = Courses[CourseIndex];
            RaceMode = true; CourseLook.Apply(c); CourseAudio.Instance?.SetCourse(c); ResultsIndex = 0;
            Player.Begin(c, at, new Vector3(-5.5f, 0, 0));
            foreach (var rv in Rivals) { bool on = rv == rival; rv.gameObject.SetActive(on); if (on) rv.Begin(c, at, new Vector3(5.5f, 0, 0)); }
            Race.Setup(true, Player, new List<ShiftVehicle> { rival });
        }
        public void ShowTourPage() { OpenFrontMenu(); ShowPage(Page.Tour, FirstTourRow()); }
        int FirstTourRow() => 0;
        // Reset (Y / R) after the finish: the same course again; in split screen only once both players are done (the results are up)
        public void ResetAfterFinish(ShiftVehicle who) { if (!SplitScreen.Active || AllHumansFinished) Restart(); }

        // Round 6: start / end two players
        public void StartSplit(bool race)
        {
            if (SplitScreen.P2Device == null) { MenuNote = "Player 2: press A on another controller (or Enter on the keyboard) first"; ShowPage(Page.Split, MenuIndex); return; }
            if (Garage.Active) Garage.Instance.Close();
            if (!SplitScreen.Active) SplitScreen.Begin();
            else if (SplitScreen.Session.P2 && SplitScreen.Session.P2.RacerName != Lineup.P2.name) { SplitScreen.Begin(); }   // another driver picked: a new player 2
            Tour.ActiveStop = -1; Lineup.Apply(this);   // Round 11: player 1's pick and the AI fill
            if (Player2) { Player2.Forms.SetOwnDesign(Lineup.P2Machine); Player2.Tint = Lineup.P2.color; }
            MenuNote = ""; CloseMenuForced(); StartCourse(MenuCourse, race);
        }
        public void EndSplit(bool restart = true)
        {
            if (!SplitScreen.Active) return;
            SplitScreen.End(); SplitScreen.P2Device = null;
            if (restart) StartCourse(MenuCourse, RaceMode);
        }
        // the inputs that work the menu: player 1's; in split screen both players'
        IEnumerable<ShiftInput> MenuInputs() { yield return Player.Input; if (Player2) yield return Player2.Input; }

        void Update()
        {
            bool full = false, stats = false, menuP = false, confirm = false, backP = false, any = false; int move = 0, side = 0;
            foreach (var i in MenuInputs()) { full |= i.FullscreenPressed; stats |= i.StatsPressed; menuP |= i.MenuPressed; confirm |= i.ConfirmPressed; backP |= i.BackPressed; any |= i.AnyPressed; if (i.MenuMove != 0) move = i.MenuMove; if (i.MenuSide != 0) side = i.MenuSide; }
            if (full) ScreenSetting.Toggle();
            if (stats) ToggleStats();
            // Round 11: Buck's card (a briefing or a Tour scene): A / Enter (or Menu / B) skips him
            if (Buck.Instance && Buck.Instance.CardOn && !MenuOpen) { if (confirm || menuP || backP) Buck.Instance.Skip(); return; }
            if (MenuOpen)
            {
                foreach (var i in MenuInputs()) { i.ConsumeReset(); i.ConsumeView(); i.ConsumeWings(); }
                if (MenuPage == Page.Title)
                {
                    // any device: the title is the first thing a player sees, before 2 Players binds anything
                    bool pad = false; foreach (var g in Gamepad.all) pad |= g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame;
                    var kb = Keyboard.current; bool key = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
                    if (any || confirm || menuP || pad || key) ShowPage(Page.Main);
                    return;
                }
                if (MenuPage == Page.Split) SplitJoin();
                if (move != 0 && Rows.Count > 0) MoveTo(move);
                if (side != 0 && MenuIndex < Rows.Count && Rows[MenuIndex].side != null) { int keep = MenuIndex; Rows[MenuIndex].side(side); SideMoved++; if (MenuOpen && MenuIndex == keep) RefreshPage(); }
                if (confirm) Choose(MenuIndex);
                else if (backP) Back();
                else if (menuP) { if (!InFront) CloseMenu(); }
                return;
            }
            if (menuP) { OpenMenu(); return; }
            // the results screen: up / down chooses, A / Enter takes it (Y / R still races again); split screen: once both are done
            if (Trial.HasResults && Player.Finished && (!SplitScreen.Active || AllHumansFinished))
            {
                int n = ResultsItems.Length;
                if (move != 0) ResultsIndex = (ResultsIndex + move + n) % n;
                if (side != 0) ResultsIndex = (ResultsIndex + side + n) % n;
                if (confirm)
                {
                    if (ResultsIndex == 0) Restart();
                    else if (ResultsIndex == 1) Next();
                    else QuitToMenu();
                }
            }
        }
        public int SideMoved { get; private set; }   // checks
        void MoveTo(int dir)
        {
            int n = Rows.Count, i = MenuIndex;
            for (int k = 0; k < n; k++) { i = (i + dir + n) % n; if (Rows[i].Selectable) { MenuIndex = i; return; } }
        }
        // B / Backspace: one page back (the title from the main menu; a pause menu resumes)
        public void Back()
        {
            switch (MenuPage)
            {
                case Page.Main: ShowTitle(); break;
                case Page.Pause: CloseMenu(); break;
                case Page.Settings: ShowPage(settingsFrom, settingsFrom == Page.Main ? MainRow("Settings") : PauseRow("Settings")); break;
                case Page.SetGameplay: case Page.SetAudio: case Page.SetVideo: case Page.SetControls: ShowPage(Page.Settings, (int)MenuPage - (int)Page.SetGameplay); break;
                case Page.Course: ShowPage(Page.Main, MainRow(pickFor == PickFor.Trial ? "Time Trial" : "Race")); break;
                case Page.Pick: if (pickFor == PickFor.Tour) ShowPage(Page.Tour, pickStop); else ShowPage(Page.Course); break;
                case Page.Quit: ShowPage(quitFrom, quitFrom == Page.Main ? MainRow("Quit game") : PauseRow("Quit to menu")); break;
                case Page.Split: if (SplitScreen.Active && !InFront) ShowPage(Page.Pause); else ShowPage(Page.Main, MainRow("2 Players")); break;
                default: ShowPage(InFront ? Page.Main : Page.Pause); break;
            }
        }
        int MainRow(string label) { ShowPage(Page.Main); int i = Rows.FindIndex(r => r.label == label); return Mathf.Max(0, i); }
        int PauseRow(string label) { ShowPage(Page.Pause); int i = Rows.FindIndex(r => r.label == label); return Mathf.Max(0, i); }

        // ---- the 2 Players page: who is player 1, joining player 2 (Chastain Rush's UpdateSplitJoin) ----
        bool Assigned(InputDevice d) => d != null && (d == SplitScreen.P1Device || d == SplitScreen.P2Device);
        void OpenSplitPage()
        {
            var opener = Player.Input.LastDevice ?? (InputDevice)Gamepad.current ?? Keyboard.current;
            if (!SplitScreen.Active)
            {
                if (SplitScreen.P1Device == null || !SplitScreen.P1Device.added) SplitScreen.P1Device = opener;
                if (SplitScreen.P2Device != null && (!SplitScreen.P2Device.added || SplitScreen.P2Device == SplitScreen.P1Device)) SplitScreen.P2Device = null;
                Player.Input.Bind(SplitScreen.P1Device);   // while the page is up only player 1's device works the menu (A elsewhere joins)
            }
            MenuNote = ""; ShowPage(Page.Split);
        }
        void SplitJoin()
        {
            if (SplitScreen.Active) return;
            foreach (var pad in Gamepad.all) if (!Assigned(pad) && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) { Join(pad); return; }
            var k = Keyboard.current; if (k != null && !Assigned(k) && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) Join(k);
        }
        public void Join(InputDevice d)
        {
            SplitScreen.P2Device = d; MenuNote = "Player 2 joined: " + SplitScreen.DeviceName(d);
            Debug.Log("[Split] " + MenuNote); ShowPage(Page.Split, MenuIndex);
        }
        void LeaveSplitPage() { if (!SplitScreen.Active) Player.Input.Bind(null); }

        // the pause menu in a race (Menu / Esc); with no race running (the front menus) the main menu
        public void OpenMenu() { if (InFront) { OpenFrontMenu(); return; } MenuOpen = true; Time.timeScale = 0; ShowPage(Page.Pause); }
        // the front menus: the main menu, the race behind frozen and hidden
        public void OpenFrontMenu() { MenuOpen = true; InFront = true; Time.timeScale = 0; ShowPage(Page.Main); }
        public void ShowTitle() { MenuOpen = true; InFront = true; Time.timeScale = 0; Hush(); ShowPage(Page.Title); }
        // Round 14: Quit to menu (the pause menu, the results): the race stops, Barnaby, Buck and the drivers stop, the main menu
        public void QuitToMenu()
        {
            if (SplitScreen.Active) { CloseMenuForced(); EndSplit(false); }
            if (ChallengeRun) ChallengeRun.Abort();
            Tour.ActiveStop = -1; Hush(); OpenFrontMenu();
        }
        void Hush() { if (Caller.Instance) Caller.Instance.Quiet(); Driver.Hush(); if (Buck.Instance) Buck.Instance.Skip(); }
        public void CloseMenu()
        {
            if (SplitScreen.Session && SplitScreen.Session.MissingPlayer != 0) return;
            if (InFront) { CloseMenuForced(); CourseLook.Apply(Courses[CourseIndex]); return; }   // the front menus: only checks close them without starting something (a player has no row for it)
            if (MenuPage == Page.Split) LeaveSplitPage(); MenuOpen = false; Time.timeScale = 1; if (Garage.Active) Garage.Instance.Close();
            if (ChallengeRun && ChallengeRun.Frozen) { ChallengeRun.Abort(); StartCourse(CourseIndex, true); }   // Round 13: Resume after a challenge's result: a normal race there
        }
        public static string FormName(Mode m) => m == Mode.Drive ? "Car" : m == Mode.Boat ? "Boat" : m == Mode.Fly ? "Plane" : "Sub";
        // Round 5: race with a design: saved, and every vehicle's (and sign's) models rebuilt in it, keeping each one's form
        public void UseDesign(string design)
        {
            Settings.Design = design;
            foreach (var fs in FindObjectsByType<FormSet>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!Garage.Instance || fs != Garage.Instance.Shown) fs.Restyle();   // Round 9: a rival's own design stays
        }
        public void ShowGarage() { OpenFrontMenu(); ShowPage(Page.Garage); }
        public void RefreshGarage(int select = 0) { if (MenuOpen && MenuPage == Page.Garage) ShowPage(Page.Garage, select); }
        void RefreshPage() => ShowPage(MenuPage, MenuIndex);

        static int Wrap(int i, int n) => (i % n + n) % n;
        static string Machine(int dir, string now) { var l = Lineup.Machines; int i = l.IndexOf(now); return l[Wrap(i + dir, l.Count)]; }
        static string Pct(float x) => $"{x * 100:0} %";

        public void ShowPage(Page page, int select = -1)
        {
            if (page != Page.Garage && page != Page.Pick && Garage.Active) Garage.Instance.Close();
            if (MenuPage == Page.Split && page != Page.Split) LeaveSplitPage();
            if (page != Page.Split && page != MenuPage) MenuNote = "";
            if (page == Page.Settings && MenuPage != Page.Settings && (MenuPage == Page.Main || MenuPage == Page.Pause)) settingsFrom = MenuPage;
            if (page == Page.Quit && MenuPage != Page.Quit) quitFrom = InFront ? Page.Main : Page.Pause;
            MenuPage = page; Rows.Clear();
            MenuRow Item(string label, Action act, string desc = "", string value = null, Action<int> side = null, float slider = -1)
            { var r = new MenuRow { label = label, act = act, desc = desc, value = value, side = side, slider = slider }; Rows.Add(r); return r; }
            void Info(string text, string desc = "") => Item(text, null, desc);
            var pp = Courses[Mathf.Clamp(MenuCourse, 0, Courses.Count - 1)];
            switch (page)
            {
                case Page.Title: break;
                case Page.Main:
                    Item("The Tour", () => ShowPage(Page.Tour), "The story: race the tour's stops, win points, unlock the next stop and the rival challenges.");
                    Item("Race", () => ShowCourses(PickFor.Race), "Race three rivals on a course of your choice.");
                    Item("Time Trial", () => ShowCourses(PickFor.Trial), "Race the clock alone. Your best lap is saved.");
                    Item("2 Players", OpenSplitPage, "Split screen: a second controller joins with A.");
                    Item("Garage", () => ShowPage(Page.Garage), "Look at the machines in every form; paints won in rival challenges.");
                    Item("Settings", () => ShowPage(Page.Settings), "Gameplay, audio, video and controls.");
                    Item("Quit game", () => ShowPage(Page.Quit), "Close ShiftStorm.");
                    break;
                case Page.Pause:
                    Item("Resume", CloseMenu, "Back to the race.");
                    Item("Restart", () => { CloseMenu(); Restart(); }, $"Start {Courses[CourseIndex].courseName} again{(SplitScreen.Active ? " (2 players)" : RaceMode ? " (race)" : " (time trial)")}.");
                    if (SplitScreen.Active)
                    {
                        Item("2 Players setup", OpenSplitPage, "Drivers, machines, AI racers, the screen layout.");
                        Item("End 2 players", () => { CloseMenuForced(); EndSplit(); }, "Back to one player on this course.");
                    }
                    Item("Settings", () => ShowPage(Page.Settings), "Gameplay, audio, video and controls.");
                    Item("Quit to menu", () => QuitToMenu(), "Leave this race for the main menu.");
                    break;
                case Page.Course:
                    {
                        string mode = pickFor == PickFor.Race ? "Race against three rivals" : "Time trial: alone, best lap saved";
                        var r = Item("Course", () => ShowPick(pickFor), mode + ". Left / right: another course, A: choose your racer.", pp.courseName, d => { MenuCourse = Wrap(MenuCourse + d, Courses.Count); });
                        r.card = true;
                        Item("Back", Back, "Back to the main menu.");
                    }
                    break;
                case Page.Pick:
                    {
                        // Round 11 item 4: choose your racer: a driver and a machine (the Garage's turntable shows it); the others race as the AI
                        var gar = Garage.Get(); if (!Garage.Active) gar.Open(Cam); if (gar.Design != Settings.Design) gar.SetDesign(Settings.Design);
                        string go = pickFor == PickFor.Race ? $"Race {pp.courseName}" : pickFor == PickFor.Trial ? $"Time Trial {pp.courseName}" : $"Tour stop {pickStop + 1}: {Tour.Stops[pickStop].title}";
                        Item("Driver", () => StepDriver(1), "Who drives. The other three race as the AI, each in their own machine.", Lineup.P1.name, StepDriver);
                        Item("Machine", () => StepMachine(1), "The machine you race (every one has a car, boat, plane and sub form).", VehicleDesigns.Title(Settings.Design) + OwnerOf(Settings.Design), StepMachine);
                        Item("Form", () => gar.NextForm(), "Look at the machine's four forms (the change plays).", FormName(gar.Form), d => { if (d > 0) gar.NextForm(); else gar.SetForm(FormSet.All[Wrap(Array.IndexOf(FormSet.All, gar.Form) - 1, 4)], true); });
                        Item("Go: " + go, () => GoPick(), "Start.");
                        Item("Back", Back, pickFor == PickFor.Tour ? "Back to The Tour." : "Back to the courses.");
                    }
                    break;
                case Page.Settings:
                    Item("Gameplay", () => ShowPage(Page.SetGameplay), "AI difficulty, Buck's briefings, The Tour's stops, playtest logs.");
                    Item("Audio", () => ShowPage(Page.SetAudio), "Music, voices, engines, effects and crowd volumes.");
                    Item("Video", () => ShowPage(Page.SetVideo), "Graphics quality, full screen, the FPS readout, the 2-player layout.");
                    Item("Controls", () => ShowPage(Page.SetControls), "Invert flight / dive, camera view, the controls.");
                    Item("Back", Back, "");
                    break;
                case Page.SetGameplay:
                    Item("AI difficulty", () => StepDifficulty(1), "How well the three rivals drive (never faster machines).", Settings.Difficulty.ToString(), StepDifficulty);
                    Item("Buck's briefings", () => StepBuck(1), "Buck talks before a course's first race of the session: in full, short, or not at all.", Settings.BuckNames[Settings.BuckBriefings], StepBuck);
                    Item("Unlock all Tour stops", () => Settings.UnlockAllTour = !Settings.UnlockAllTour, "On: every Tour stop is open. Off: the stops open in order (top 3 opens the next). Race and Time Trial are always open.", Settings.UnlockAllTour ? "On" : "Off", d => Settings.UnlockAllTour = !Settings.UnlockAllTour);
                    Item("Save playtest logs", () => Settings.PlaytestLogs = !Settings.PlaytestLogs, "A small log of each race for playtesting.", Settings.PlaytestLogs ? "On" : "Off", d => Settings.PlaytestLogs = !Settings.PlaytestLogs);
                    if (!Application.isMobilePlatform) Item("Open log folder", () => PlaytestLog.OpenFolder(), PlaytestLog.Folder);
                    Item("Back", Back, "");
                    break;
                case Page.SetAudio:
                    void Vol(string label, string key, string desc) { int max = Settings.MaxVolume(key); Item(label, () => Settings.SetVolume(key, Settings.Volume(key) >= max ? 0 : Settings.Volume(key) + 1), desc, Pct(Settings.Volume(key) / 10f), d => Settings.SetVolume(key, Settings.Volume(key) + d), Settings.Volume(key) / (float)max); }
                    Vol("Music", "music", "Dan's songs: the races, the menus, the results.");
                    Vol("Voices", "voice", "Barnaby, Buck and the drivers.");
                    Vol("Engines", "engine", "Every machine's engine and the wind.");
                    Vol("Effects", "sfx", "Changes, gates, boosts, splashes, the menus.");
                    Vol("Crowd", "crowd", "The grandstands: murmur, cheers, the roar at GO, the finish (up to 200 %).");
                    Item("Back", Back, "");
                    break;
                case Page.SetVideo:
                    Item("Graphics", () => GraphicsPreset.Step(1), "Auto picks Deck on a Steam Deck, High elsewhere. 2 players: one step lighter.", GraphicsPreset.Value, GraphicsPreset.Step);
                    Item("Screen", () => ScreenSetting.Toggle(), "Full screen or a window (Alt+Enter).", ScreenSetting.Fullscreen ? "Full screen" : "Windowed", d => ScreenSetting.Toggle());
                    Item("FPS + speed readout", ToggleStats, "Frame rate and speed in the corner (R3 / F1).", Hud.StatsOn ? "On" : "Off", d => ToggleStats());
                    Item("2-player screen", () => SplitLayout(), "How the screen is split for 2 players.", SplitScreen.LeftRight ? "Left / right" : "Top / bottom", d => SplitLayout());
                    Item("Back", Back, "");
                    break;
                case Page.SetControls:
                    {
                        var views = Cam ? Cam.GetComponent<CameraViews>() : null;
                        Item(SplitScreen.Active ? "Player 1 invert flight / dive" : "Invert flight / dive", () => ShiftInput.Invert = !ShiftInput.Invert, "Off: pull back to climb. On: push forward to climb (View / F2 in the race).", ShiftInput.Invert ? "On" : "Off", d => ShiftInput.Invert = !ShiftInput.Invert);
                        if (SplitScreen.Active) Item("Player 2 invert flight / dive", () => Settings.InvertVertical2 = !Settings.InvertVertical2, "Player 2's own setting (player 2's View button).", Settings.InvertVertical2 ? "On" : "Off", d => Settings.InvertVertical2 = !Settings.InvertVertical2);
                        if (views) Item("Camera view", () => views.NextView(), "Chase, far chase, first person or front (X / V in the race).", views.CurrentName, d => views.SetView((CameraViews.View)Wrap((int)views.Current + d, CameraViews.Names.Length)));
                        Info("RT / W: go    LT / S: brake    left stick / A-D: steer");
                        Info("A / Space: boost    RB / Shift: drift, carve    LB / E: wings on a choice jump");
                        Info("Stick back / I-K: climb, dive    Y / R: reset    Start / Esc: pause");
                        Item("Back", Back, "");
                    }
                    break;
                case Page.Split:
                    {
                        var p2 = SplitScreen.P2Device; bool ready = p2 != null; var pick = Lineup.P2;
                        Info($"Player 1: {SplitScreen.DeviceName(SplitScreen.P1Device)}");
                        Item("Player 1 driver", () => StepDriver(1), "", Lineup.P1.name, StepDriver);
                        Item("Player 1 machine", () => StepMachine(1), "", VehicleDesigns.Title(Settings.Design), StepMachine);
                        Item(ready ? $"Player 2: {SplitScreen.DeviceName(p2)}" : "Player 2: press A on another controller", ready ? () => { if (SplitScreen.Active) { MenuNote = "End 2 players first to change player 2's controller"; RefreshPage(); return; } SplitScreen.P2Device = null; RefreshPage(); } : (Action)null, ready ? "A: let this controller go." : "Or Enter on the keyboard if player 1 is on a controller.");
                        Item("Player 2 driver", () => Lineup.NextP2(), "", pick.name, d => { if (d > 0) Lineup.NextP2(); else for (int k = 0; k < 2; k++) Lineup.NextP2(); });
                        Item("Player 2 machine", () => Settings.Design2 = Machine(1, Lineup.P2Machine), "", VehicleDesigns.Title(Lineup.P2Machine), d => Settings.Design2 = Machine(d, Lineup.P2Machine));
                        Item("AI racers", () => SplitScreen.AiCount = (SplitScreen.AiCount + 1) % 3, "0, 1 or 2 AI rivals in a 2-player race.", SplitScreen.AiCount.ToString(), d => SplitScreen.AiCount = Mathf.Clamp(SplitScreen.AiCount + d, 0, 2));
                        Item("Screen", () => SplitLayout(), "", SplitScreen.LeftRight ? "Left / right" : "Top / bottom (player 1 on top)", d => SplitLayout());
                        Item("Course", () => MenuCourse = Wrap(MenuCourse + 1, Courses.Count), "", pp.courseName, d => MenuCourse = Wrap(MenuCourse + d, Courses.Count));
                        Item($"Race: 2 players + {SplitScreen.AiCount} AI", () => StartSplit(true), ready ? "" : "Waiting for player 2.");
                        Item("Time Trial: 2 players", () => StartSplit(false), ready ? "" : "Waiting for player 2.");
                        Item("Back", Back, "");
                    }
                    break;
                case Page.Garage:
                    {
                        var gar = Garage.Get(); if (!Garage.Active) { gar.SetDesign(Settings.Design); gar.Open(Cam); }
                        var names = VehicleDesigns.Names;
                        Item("Design", () => gar.NextDesign(), "Each racer's machine; 'Race with this design' makes it yours.", VehicleDesigns.Title(gar.Design) + (VehicleDesigns.Find(gar.Design)?.rival is string rv && rv != "" ? $" ({rv}'s)" : ""), d => { if (d > 0) gar.NextDesign(); else gar.SetDesign(names[Wrap(names.IndexOf(gar.Design) - 1, names.Count)]); });
                        Item("Form", () => gar.NextForm(), "The change plays.", FormName(gar.Form), d => { if (d > 0) gar.NextForm(); else gar.SetForm(FormSet.All[Wrap(Array.IndexOf(FormSet.All, gar.Form) - 1, 4)], true); });
                        Item(gar.Design == Settings.Design ? "Racing with this design" : "Race with this design", () => UseDesign(gar.Design), "");
                        Item("Paint", () => PaintStep(1), "Paints won in The Tour's rival challenges.", Paints.All[Paints.Current].name, PaintStep);
                        Item("Back", Back, "");
                    }
                    break;
                case Page.Tour:
                    // Round 11 item 3: the stops in order, best place and points; a locked stop says what unlocks it
                    for (int k = 0; k < Tour.Stops.Length; k++) { int stop = k; Item(Tour.Row(stop), stop + 1 <= Tour.Unlocked ? () => ShowPick(PickFor.Tour, stop) : (Action)null, Tour.Describe(stop)); }
                    // Round 13 item 4: a one-on-one rival challenge per cleared stop
                    for (int k = 0; k < Challenge.All.Length; k++) { int ci = k; Item(Challenge.Row(ci), Challenge.Open(ci) ? () => { CloseMenuForced(); ChallengeRun.Play(ci); } : (Action)null, Challenge.Describe(ci)); }
                    if (Challenge.Note != "") Info(Challenge.Note);
                    Item("Back", Back, "");
                    break;
                case Page.Quit:
                    Item("No, keep playing", Back, "");
                    Item("Yes, quit", Quit, "ShiftStorm closes (and the launcher with it).");
                    break;
            }
            MenuIndex = Mathf.Clamp(select >= 0 ? select : 0, 0, Mathf.Max(0, Rows.Count - 1));
            if (Rows.Count > 0 && !Rows[MenuIndex].Selectable) MoveTo(1);   // an information row is never selected
        }
        public string MenuTitle => MenuPage switch
        {
            Page.Title => "", Page.Main => SplitScreen.Active ? "MAIN MENU (2 PLAYERS)" : "MAIN MENU", Page.Pause => "PAUSED", Page.Settings => "SETTINGS",
            Page.SetGameplay => "SETTINGS: GAMEPLAY", Page.SetAudio => "SETTINGS: AUDIO", Page.SetVideo => "SETTINGS: VIDEO", Page.SetControls => "SETTINGS: CONTROLS",
            Page.Garage => "GARAGE", Page.Split => "2 PLAYERS", Page.Tour => "THE TOUR", Page.Pick => "CHOOSE YOUR RACER",
            Page.Course => pickFor == PickFor.Race ? "RACE: CHOOSE A COURSE" : "TIME TRIAL: CHOOSE A COURSE", _ => "QUIT SHIFTSTORM?"
        };
        public PickFor PickingFor => pickFor;
        void ShowCourses(PickFor what) { pickFor = what; ShowPage(Page.Course); }
        // Round 11: choose your racer, then the race / time trial / Tour stop
        void ShowPick(PickFor what, int stop = 0) { pickFor = what; pickStop = stop; ShowPage(Page.Pick, 3); }
        public void PickAndGo(PickFor what, int stop = 0) { pickFor = what; pickStop = stop; GoPick(); }   // checks
        public void PickCourse(int course) => MenuCourse = Wrap(course, Courses.Count);   // checks
        static string OwnerOf(string design) { foreach (var r in Lineup.All) if (Lineup.MachineOf(r) == design) return $" ({r.name}'s)"; return ""; }
        void StepDriver(int d) { Settings.Driver = Wrap(Settings.Driver + d, 4); }
        void StepMachine(int d) { Settings.Design = Machine(d, Settings.Design); if (Garage.Active) Garage.Instance.SetDesign(Settings.Design); if (MenuPage == Page.Split) UseDesign(Settings.Design); }
        static void StepDifficulty(int d) => Settings.Difficulty = (Difficulty)Wrap((int)Settings.Difficulty + d, 3);
        static void StepBuck(int d) => Settings.BuckBriefings = Wrap(Settings.BuckBriefings + d, 3);
        void PaintStep(int d) { Settings.Paint = d > 0 ? Paints.Next() : Paints.Prev(); if (Garage.Active) Garage.Instance.SetDesign(Garage.Instance.Design); Lineup.Apply(this); }
        static void SplitLayout() { Settings.SplitLeftRight = !Settings.SplitLeftRight; SplitScreen.Session?.Relayout(); }
        void GoPick()
        {
            if (SplitScreen.Active) { CloseMenuForced(); EndSplit(false); }
            UseDesign(Settings.Design); Lineup.Apply(this);
            CloseMenuForced();
            if (pickFor == PickFor.Tour) { TourRun.Play(pickStop, true); return; }
            Tour.ActiveStop = -1; StartCourse(MenuCourse, pickFor == PickFor.Race);
        }
        void CloseMenuForced() { if (MenuPage == Page.Split) LeaveSplitPage(); if (Garage.Active) Garage.Instance.Close(); MenuOpen = false; InFront = false; Time.timeScale = 1; }
        void ToggleStats() { Hud.StatsOn = !Hud.StatsOn; if (!QuietRun.Check) Settings.ShowStats = Hud.StatsOn; }
        public void Choose(int item) { if (item < 0 || item >= Rows.Count) return; int keep = MenuIndex; var page = MenuPage; Rows[item].act?.Invoke(); if (MenuOpen && MenuPage == page && Rows.Count > 0 && Rows[Mathf.Min(item, Rows.Count - 1)].side != null) ShowPage(page, item); }

        // Quit: the process ends with exit code 0, so the launcher (which waits for its game) closes too, and Steam sees the game end.
        // A watchdog ends the process if the engine's own shutdown ever hangs (seen with some Unity games under Proton).
        public void Quit()
        {
            if (QuitChosen) return; QuitChosen = true;
            Debug.Log("[Game] quit chosen from the menu: Application.Quit(0)");
            Time.timeScale = 1;
            new System.Threading.Thread(() => { System.Threading.Thread.Sleep(6000); Environment.Exit(0); }) { IsBackground = true }.Start();
            Application.Quit(0);
        }
    }
}
