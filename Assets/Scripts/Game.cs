using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ShiftStorm
{
    // Start-up: loads the tuning (Resources/Tuning), builds the world (Paradise Plunge and the Round 1 test courses), makes the player's
    // vehicle with Abi in the seat, the three AI rivals (Round 3), the camera, HUD, time trial / race and course sound, and starts on Paradise
    // Plunge ("-course <n>": 0 Paradise Plunge, 1 transition strip, 2 drive lane, 3 boat lane, 4 fly lane, 5 dive lane; "-trial" alone,
    // "-race" against the rivals: Paradise Plunge starts as a race unless a check asks otherwise).
    // Round 3, the menu (Menu / Esc): Resume, Restart, Race, Time Trial, the test courses, Settings (invert flight / dive controls, screen,
    // FPS readout) and Quit game (with a "Quit? No / Yes" page). The results screen has Race again / Menu / Quit game.
    // D-pad left / right (F3 / F4) still steps through the courses. A check run ("-check <name>") hands over to CheckPilot.
    // Round 6: Menu > 2 Players (split screen, SplitScreen): player 1 is the device that opened the page, player 2 joins with A on another
    // controller (Enter on the keyboard); Race (with 0-2 AI) or Time Trial (the two alone). While split, either player's Menu opens the one
    // menu (on player 1's half) and pauses both; either player works it. Settings has each player's invert and the split layout.
    public sealed class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public List<Course> Courses { get; private set; }
        public ShiftVehicle Player { get; private set; }
        public Camera Cam { get; private set; }
        public TimeTrial Trial { get; private set; }
        public Race Race { get; private set; }
        public int CourseIndex { get; private set; }
        public bool RaceMode { get; private set; }   // the current course is raced against the rivals (Paradise Plunge only)
        public ShiftTuning Shared; public DriveTuning Drive; public BoatTuning Boat; public FlyTuning Fly; public DiveTuning Dive;
        public readonly List<ShiftVehicle> Rivals = new List<ShiftVehicle>();

        // ---- the menu ----
        public enum Page { Main, Settings, Quit, Garage, Split }
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
            for (int k = 0; k < Rivals.Count; k++) { if (SplitScreen.Active && k == SplitScreen.P2Pick) continue; if (SplitScreen.Active && l.Count >= SplitScreen.AiCount) break; l.Add(Rivals[k]); }
            return l;
        }
        // split screen: the two players side by side on the back row (11 m apart, the same distance to the line); the AI ahead
        public static readonly Vector3 SplitGridP1 = new Vector3(-5.5f, 0, -25f), SplitGridP2 = new Vector3(5.5f, 0, -25f);
        public bool MenuOpen { get; private set; }
        public Page MenuPage { get; private set; }
        public int MenuIndex { get; private set; }
        public List<string> MenuItems { get; } = new List<string>();
        readonly List<Action> menuActions = new List<Action>();
        public int ResultsIndex { get; private set; }
        public static readonly string[] ResultsItems = { "Race again", "Menu", "Quit game" };
        public bool QuitChosen { get; private set; }

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
            JurassicLook.Apply(false);   // remember the Round 1 look before anything changes it
            Courses = WorldBuilder.Build();
            Player = VehicleFactory.MakePlayer(Shared, Drive, Boat, Fly, Dive);
            Driver.Make(Player);
            foreach (var r in Rival.Roster) { var rv = VehicleFactory.MakeRival(r, Shared, Drive, Boat, Fly, Dive); Rivals.Add(rv); rv.gameObject.SetActive(false); }
            Cam = Camera.main; if (!Cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; Cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
            Cam.farClipPlane = 2600; Cam.nearClipPlane = .2f;
            { var cull = new float[32]; cull[ParadisePlunge.DetailLayer] = ParadisePlunge.DetailCullDistance; Cam.layerCullDistances = cull; Cam.layerCullSpherical = true; }   // Round 5: the polished stretch's detail Cam.fieldOfView = 65; Cam.clearFlags = CameraClearFlags.Skybox;
            if (!FindAnyObjectByType<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.gameObject.AddComponent<AudioMeter>();
            Cam.gameObject.AddComponent<ChaseCam>().target = Player;
            Cam.gameObject.AddComponent<CameraViews>();   // Round 6: X / V cycles Chase, Far chase, First person, Front
            CourseAudio.Make(Cam.gameObject, Player);
            Trial = gameObject.AddComponent<TimeTrial>(); Trial.v = Player;
            Race = gameObject.AddComponent<Race>();
            Hud.Make(Cam, Player, this);
            gameObject.AddComponent<RouteGuide>().v = Player;
            if (!FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -30, 0); l.shadows = LightShadows.Soft; }
            var a = Environment.GetCommandLineArgs();
            int start = 0; int i = Array.IndexOf(a, "-course"); if (i >= 0 && i + 1 < a.Length) int.TryParse(a[i + 1], out start);
            bool race = Array.IndexOf(a, "-race") >= 0 || (!QuietRun.Check && Array.IndexOf(a, "-trial") < 0);
            StartCourse(Mathf.Clamp(start, 0, Courses.Count - 1), race);
            Debug.Log($"[Screen] at the first frame: {Screen.fullScreenMode} {Screen.width} x {Screen.height}, desktop {Display.main.systemWidth} x {Display.main.systemHeight}, current resolution {Screen.currentResolution.width} x {Screen.currentResolution.height}");
            Debug.Log($"[Time] fixed step {Time.fixedDeltaTime * 1000:0.00} ms, maximum frame catch-up {Time.maximumDeltaTime:0.00} s, target frame rate {Application.targetFrameRate}, vSync {QualitySettings.vSyncCount}");
            Debug.Log($"[Game] ready: {Courses.Count} courses, model {VehicleFactory.ModelSource}, driver {Driver.Instance?.Source}, {Rivals.Count} rivals, muted {QuietRun.Muted}, listener volume {AudioListener.volume}");
            if (QuietRun.Check) gameObject.AddComponent<CheckPilot>();
        }

        // Start (or restart) a course. race: against the rivals on the grid (only a time-trial course, Paradise Plunge); otherwise alone.
        public void StartCourse(int index) => StartCourse(index, RaceMode);
        public void StartCourse(int index, bool race)
        {
            CourseIndex = (index % Courses.Count + Courses.Count) % Courses.Count;
            var c = Courses[CourseIndex];
            RaceMode = race && c.timeTrial;
            JurassicLook.Apply(c.look == "jurassic");
            CourseAudio.Instance?.SetCourse(c);
            ResultsIndex = 0;
            if (SplitScreen.Active && !c.timeTrial) { EndSplit(false); }   // the test courses are one player's
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
        public void Restart() => StartCourse(CourseIndex, RaceMode);
        // Reset (Y / R) after the finish: the same course again; in split screen only once both players are done (the results are up)
        public void ResetAfterFinish(ShiftVehicle who) { if (!SplitScreen.Active || AllHumansFinished) Restart(); }

        // Round 6: start / end two players
        public void StartSplit(bool race)
        {
            if (SplitScreen.P2Device == null) { MenuNote = "Player 2: press A on another controller (or Enter on the keyboard) first"; ShowPage(Page.Split, MenuIndex); return; }
            if (Garage.Active) Garage.Instance.Close();
            if (!SplitScreen.Active) SplitScreen.Begin();
            else if (SplitScreen.Session.P2 && SplitScreen.Session.P2.RacerName != SplitScreen.P2Rival.name) { SplitScreen.Begin(); }   // another driver picked: a new player 2
            MenuNote = ""; CloseMenu(); StartCourse(0, race);
        }
        public void EndSplit(bool restart = true)
        {
            if (!SplitScreen.Active) return;
            SplitScreen.End(); SplitScreen.P2Device = null;
            if (restart) StartCourse(0, RaceMode);
        }
        // the inputs that work the menu: player 1's; in split screen both players'
        IEnumerable<ShiftInput> MenuInputs() { yield return Player.Input; if (Player2) yield return Player2.Input; }

        void Update()
        {
            bool full = false, stats = false, menuP = false, confirm = false, backP = false; int move = 0;
            foreach (var i in MenuInputs()) { full |= i.FullscreenPressed; stats |= i.StatsPressed; menuP |= i.MenuPressed; confirm |= i.ConfirmPressed; backP |= i.BackPressed; if (i.MenuMove != 0) move = i.MenuMove; }
            var inp = Player.Input;
            if (full) ScreenSetting.Toggle();
            if (stats) ToggleStats();
            if (MenuOpen)
            {
                if (MenuPage == Page.Split) SplitJoin();
                if (move != 0 && MenuItems.Count > 0) { do MenuIndex = (MenuIndex + move + MenuItems.Count) % MenuItems.Count; while (menuActions[MenuIndex] == null); }
                if (confirm) Choose(MenuIndex);
                else if (backP) { if (MenuPage == Page.Main) CloseMenu(); else ShowPage(Page.Main); }
                else if (menuP) CloseMenu();
                foreach (var i in MenuInputs()) { i.ConsumeReset(); i.ConsumeStartStep(); i.ConsumeView(); }
                return;
            }
            if (menuP) { OpenMenu(); return; }
            // the results screen: D-pad / arrows choose, A / Enter takes it (Y / R still races again); split screen: once both are done
            if (Trial.HasResults && Player.Finished && (!SplitScreen.Active || AllHumansFinished))
            {
                if (move != 0) ResultsIndex = (ResultsIndex + move + ResultsItems.Length) % ResultsItems.Length;
                if (confirm)
                {
                    if (ResultsIndex == 0) Restart();
                    else if (ResultsIndex == 1) OpenMenu();
                    else { OpenMenu(); ShowPage(Page.Quit); }
                }
            }
            int step = inp.ConsumeStartStep(); if (Player2) Player2.Input.ConsumeStartStep();
            if (step != 0 && !inp.Scripted && !SplitScreen.Active) StartCourse(CourseIndex + step);
        }

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

        public void OpenMenu() { MenuOpen = true; Time.timeScale = 0; ShowPage(Page.Main); }
        public void CloseMenu() { if (SplitScreen.Session && SplitScreen.Session.MissingPlayer != 0) return; if (MenuPage == Page.Split) LeaveSplitPage(); MenuOpen = false; Time.timeScale = 1; if (Garage.Active) Garage.Instance.Close(); }
        public static string FormName(Mode m) => m == Mode.Drive ? "Car" : m == Mode.Boat ? "Boat" : m == Mode.Fly ? "Plane" : "Sub";
        // Round 5: race with a design: saved, and every vehicle's (and sign's) models rebuilt in it, keeping each one's form
        public void UseDesign(string design)
        {
            Settings.Design = design;
            foreach (var fs in FindObjectsByType<FormSet>(FindObjectsInactive.Include, FindObjectsSortMode.None)) if (!Garage.Instance || fs != Garage.Instance.Shown) fs.Build(design);
        }
        public void ShowGarage() { OpenMenu(); ShowPage(Page.Garage); }
        public void RefreshGarage(int select = 0) { if (MenuOpen && MenuPage == Page.Garage) ShowPage(Page.Garage, select); }

        void ShowPage(Page page, int select = -1)
        {
            if (page != Page.Garage && Garage.Active) Garage.Instance.Close();
            if (MenuPage == Page.Split && page != Page.Split) LeaveSplitPage();
            if (page != Page.Split) MenuNote = "";
            MenuPage = page; MenuItems.Clear(); menuActions.Clear();
            void Item(string label, Action act) { MenuItems.Add(label); menuActions.Add(act); }
            var pp = Courses[0];
            switch (page)
            {
                case Page.Main when SplitScreen.Active:
                    Item("Resume", CloseMenu);
                    Item($"Restart (2 players, {(RaceMode ? $"race, {ActiveRivals().Count} AI" : "time trial")})", () => { CloseMenu(); Restart(); });
                    Item("2 Players setup (driver, AI racers, screen)", OpenSplitPage);
                    Item("End 2 players (back to one player)", () => { CloseMenuForced(); EndSplit(); });
                    Item("Settings", () => ShowPage(Page.Settings));
                    Item("Quit game", () => ShowPage(Page.Quit));
                    break;
                case Page.Split:
                    {
                        var p2 = SplitScreen.P2Device; bool ready = p2 != null; var pick = SplitScreen.P2Rival;
                        Item($"Player 1:   {SplitScreen.DeviceName(SplitScreen.P1Device)}  -  Abi (Jetstream green)", null);
                        Item(ready ? $"Player 2:   {SplitScreen.DeviceName(p2)}   (A: let go)" : "Player 2:   press A on another controller (or Enter on the keyboard)",
                            () => { if (SplitScreen.Active) { MenuNote = "End 2 players first to change player 2's controller"; ShowPage(Page.Split, 1); return; } SplitScreen.P2Device = null; ShowPage(Page.Split, 1); });
                        Item($"Player 2 driver:   {pick.name}   (A: next)", () => { SplitScreen.P2Pick = (SplitScreen.P2Pick + 1) % Rival.Roster.Length; ShowPage(Page.Split, 2); });
                        Item($"AI racers:   {SplitScreen.AiCount}   (A: 0 / 1 / 2)", () => { SplitScreen.AiCount = (SplitScreen.AiCount + 1) % 3; ShowPage(Page.Split, 3); });
                        Item("Screen:   " + (SplitScreen.LeftRight ? "Left / right" : "Top / bottom (player 1 on top)"), () => { Settings.SplitLeftRight = !Settings.SplitLeftRight; SplitScreen.Session?.Relayout(); ShowPage(Page.Split, 4); });
                        Item($"Race {pp.courseName}: 2 players + {SplitScreen.AiCount} AI" + (ready ? "" : "   (waiting for player 2)"), () => StartSplit(true));
                        Item($"Time Trial {pp.courseName}: 2 players" + (ready ? "" : "   (waiting for player 2)"), () => StartSplit(false));
                        Item("Back", () => ShowPage(Page.Main));
                    }
                    break;
                case Page.Main:
                    Item("Resume", CloseMenu);
                    Item("Restart " + Courses[CourseIndex].courseName + (RaceMode ? " (race)" : ""), () => { CloseMenu(); Restart(); });
                    Item($"Race: {pp.courseName}  (vs 3 rivals)", () => { CloseMenu(); StartCourse(0, true); });
                    Item($"Time Trial: {pp.courseName}  (alone)", () => { CloseMenu(); StartCourse(0, false); });
                    for (int k = 1; k < Courses.Count; k++) { int idx = k; Item(Courses[k].courseName + "  (test)", () => { CloseMenu(); StartCourse(idx, false); }); }
                    Item("2 Players (split screen)", OpenSplitPage);
                    Item("Garage (vehicle designs)", () => ShowPage(Page.Garage));
                    Item("Settings", () => ShowPage(Page.Settings));
                    Item("Quit game", () => ShowPage(Page.Quit));
                    break;
                case Page.Settings:
                    Item(DifficultyItem, () => { Settings.Difficulty = (Difficulty)(((int)Settings.Difficulty + 1) % 3); ShowPage(Page.Settings, 0); });
                    Item(InvertItem, () => { ShiftInput.Invert = !ShiftInput.Invert; ShowPage(Page.Settings, 1); });
                    Item(ScreenItem, () => { ScreenSetting.Toggle(); ShowPage(Page.Settings, 2); });
                    Item(StatsItem, () => { ToggleStats(); ShowPage(Page.Settings, 3); });
                    if (SplitScreen.Active)
                    {
                        Item(Invert2Item, () => { Settings.InvertVertical2 = !Settings.InvertVertical2; ShowPage(Page.Settings, 4); });
                        Item("Split screen:   " + (SplitScreen.LeftRight ? "Left / right" : "Top / bottom (player 1 on top)"), () => { Settings.SplitLeftRight = !Settings.SplitLeftRight; SplitScreen.Session?.Relayout(); ShowPage(Page.Settings, 5); });
                    }
                    Item("Back", () => ShowPage(Page.Main));
                    break;
                case Page.Garage:
                    {
                        var gar = Garage.Get(); if (!Garage.Active) { gar.SetDesign(Settings.Design); gar.Open(Cam); }
                        var names = VehicleDesigns.Names;
                        Item($"Design: {gar.Design}   ({names.IndexOf(gar.Design) + 1} of {names.Count}, A: next)", () => { gar.NextDesign(); ShowPage(Page.Garage, 0); });
                        Item($"Form: {FormName(gar.Form)} - {gar.FormInfo(gar.Form)}   (A: next, with the change)", () => { gar.NextForm(); ShowPage(Page.Garage, 1); });
                        Item(gar.Design == Settings.Design ? "Racing with this design" : "Race with this design", () => { UseDesign(gar.Design); ShowPage(Page.Garage, 2); });
                        Item("Back", () => ShowPage(Page.Main));
                    }
                    break;
                case Page.Quit:
                    Item("No, keep playing", () => ShowPage(Page.Main));
                    Item("Yes, quit", Quit);
                    break;
            }
            MenuIndex = Mathf.Clamp(select >= 0 ? select : 0, 0, Mathf.Max(0, MenuItems.Count - 1));
            for (int k = 0; k < menuActions.Count && menuActions[MenuIndex] == null; k++) MenuIndex = (MenuIndex + 1) % menuActions.Count;   // an information row is never selected
        }
        public string MenuTitle => MenuPage == Page.Main ? (SplitScreen.Active ? "MENU (2 PLAYERS)" : "MENU") : MenuPage == Page.Settings ? "SETTINGS" : MenuPage == Page.Garage ? "GARAGE" : MenuPage == Page.Split ? "2 PLAYERS" : "QUIT SHIFTSTORM?";
        static string Invert2Item => "Player 2 invert flight / dive controls: " + (Settings.InvertVertical2 ? "On" : "Off") + "   (player 2's View)";
        void CloseMenuForced() { MenuOpen = false; Time.timeScale = 1; }
        static string DifficultyItem => "AI difficulty: " + Settings.Difficulty + "   (Easy / Normal / Hard)";
        static string InvertItem => (SplitScreen.Active ? "Player 1 invert" : "Invert") + " flight / dive controls: " + (ShiftInput.Invert ? "On" : "Off") + "   (View / F2)";
        static string ScreenItem => "Screen: " + (ScreenSetting.Fullscreen ? "Full screen" : "Windowed") + "   (Alt+Enter)";
        static string StatsItem => "FPS + speed readout: " + (Hud.StatsOn ? "On" : "Off") + "   (R3 / F1)";
        void ToggleStats() { Hud.StatsOn = !Hud.StatsOn; if (!QuietRun.Check) Settings.ShowStats = Hud.StatsOn; }
        public void Choose(int item) { if (item >= 0 && item < menuActions.Count) menuActions[item]?.Invoke(); }

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
