using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Start-up: loads the tuning (Resources/Tuning), builds the world (Paradise Plunge and the Round 1 test courses), makes the player's
    // vehicle with Abi in the seat, the three AI rivals (Round 3), the camera, HUD, time trial / race and course sound, and starts on Paradise
    // Plunge ("-course <n>": 0 Paradise Plunge, 1 transition strip, 2 drive lane, 3 boat lane, 4 fly lane, 5 dive lane; "-trial" alone,
    // "-race" against the rivals: Paradise Plunge starts as a race unless a check asks otherwise).
    // Round 3, the menu (Menu / Esc): Resume, Restart, Race, Time Trial, the test courses, Settings (invert flight / dive controls, screen,
    // FPS readout) and Quit game (with a "Quit? No / Yes" page). The results screen has Race again / Menu / Quit game.
    // D-pad left / right (F3 / F4) still steps through the courses. A check run ("-check <name>") hands over to CheckPilot.
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
        public enum Page { Main, Settings, Quit }
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
            Cam.farClipPlane = 2600; Cam.nearClipPlane = .2f; Cam.fieldOfView = 65; Cam.clearFlags = CameraClearFlags.Skybox;
            if (!FindAnyObjectByType<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.gameObject.AddComponent<AudioMeter>();
            Cam.gameObject.AddComponent<ChaseCam>().target = Player;
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

        void Update()
        {
            var inp = Player.Input;
            if (inp.FullscreenPressed) ScreenSetting.Toggle();
            if (inp.StatsPressed) ToggleStats();
            if (MenuOpen)
            {
                if (inp.MenuMove != 0 && MenuItems.Count > 0) { do MenuIndex = (MenuIndex + inp.MenuMove + MenuItems.Count) % MenuItems.Count; while (menuActions[MenuIndex] == null); }
                if (inp.ConfirmPressed) Choose(MenuIndex);
                else if (inp.BackPressed) { if (MenuPage == Page.Main) CloseMenu(); else ShowPage(Page.Main); }
                else if (inp.MenuPressed) CloseMenu();
                inp.ConsumeReset(); inp.ConsumeStartStep();
                return;
            }
            if (inp.MenuPressed) { OpenMenu(); return; }
            // the results screen: D-pad / arrows choose, A / Enter takes it (Y / R still races again)
            if (Trial.HasResults && Player.Finished)
            {
                if (inp.MenuMove != 0) ResultsIndex = (ResultsIndex + inp.MenuMove + ResultsItems.Length) % ResultsItems.Length;
                if (inp.ConfirmPressed)
                {
                    if (ResultsIndex == 0) Restart();
                    else if (ResultsIndex == 1) OpenMenu();
                    else { OpenMenu(); ShowPage(Page.Quit); }
                }
            }
            int step = inp.ConsumeStartStep();
            if (step != 0 && !inp.Scripted) StartCourse(CourseIndex + step);
        }

        public void OpenMenu() { MenuOpen = true; Time.timeScale = 0; ShowPage(Page.Main); }
        public void CloseMenu() { MenuOpen = false; Time.timeScale = 1; }

        void ShowPage(Page page, int select = -1)
        {
            MenuPage = page; MenuItems.Clear(); menuActions.Clear();
            void Item(string label, Action act) { MenuItems.Add(label); menuActions.Add(act); }
            var pp = Courses[0];
            switch (page)
            {
                case Page.Main:
                    Item("Resume", CloseMenu);
                    Item("Restart " + Courses[CourseIndex].courseName + (RaceMode ? " (race)" : ""), () => { CloseMenu(); Restart(); });
                    Item($"Race: {pp.courseName}  (vs 3 rivals)", () => { CloseMenu(); StartCourse(0, true); });
                    Item($"Time Trial: {pp.courseName}  (alone)", () => { CloseMenu(); StartCourse(0, false); });
                    for (int k = 1; k < Courses.Count; k++) { int idx = k; Item(Courses[k].courseName + "  (test)", () => { CloseMenu(); StartCourse(idx, false); }); }
                    Item("Settings", () => ShowPage(Page.Settings));
                    Item("Quit game", () => ShowPage(Page.Quit));
                    break;
                case Page.Settings:
                    Item(InvertItem, () => { ShiftInput.Invert = !ShiftInput.Invert; ShowPage(Page.Settings, 0); });
                    Item(ScreenItem, () => { ScreenSetting.Toggle(); ShowPage(Page.Settings, 1); });
                    Item(StatsItem, () => { ToggleStats(); ShowPage(Page.Settings, 2); });
                    Item("Back", () => ShowPage(Page.Main));
                    break;
                case Page.Quit:
                    Item("No, keep playing", () => ShowPage(Page.Main));
                    Item("Yes, quit", Quit);
                    break;
            }
            MenuIndex = select >= 0 ? select : 0;
        }
        public string MenuTitle => MenuPage == Page.Main ? "MENU" : MenuPage == Page.Settings ? "SETTINGS" : "QUIT SHIFTSTORM?";
        static string InvertItem => "Invert flight / dive controls: " + (ShiftInput.Invert ? "On" : "Off") + "   (View / F2)";
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
