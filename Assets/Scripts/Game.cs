using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Start-up: loads the tuning (Resources/Tuning), builds the world (Paradise Plunge and the Round 1 test courses), makes the player's
    // vehicle with Abi in the seat, the camera, HUD, time trial and course sound, and starts on Paradise Plunge ("-course <n>": 0 Paradise
    // Plunge, 1 transition strip, 2 drive lane, 3 boat lane, 4 fly lane, 5 dive lane). The course menu (Menu / Esc) restarts or picks a
    // course; D-pad left / right (F3 / F4) still steps through them. A check run ("-check <name>") hands over to CheckPilot.
    public sealed class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public List<Course> Courses { get; private set; }
        public ShiftVehicle Player { get; private set; }
        public Camera Cam { get; private set; }
        public TimeTrial Trial { get; private set; }
        public int CourseIndex { get; private set; }
        public ShiftTuning Shared; public DriveTuning Drive; public BoatTuning Boat; public FlyTuning Fly; public DiveTuning Dive;
        public bool MenuOpen { get; private set; }
        public int MenuIndex { get; private set; }
        public List<string> MenuItems { get; } = new List<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() { if (!Instance) new GameObject("Game").AddComponent<Game>(); }

        static T Load<T>(string n) where T : ScriptableObject { var t = Resources.Load<T>("Tuning/" + n); if (!t) { Debug.LogWarning("[Game] tuning asset missing, defaults used: " + n); t = ScriptableObject.CreateInstance<T>(); } return t; }

        void Awake()
        {
            Instance = this;
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);   // a log line must not cost a frame spike
            Application.targetFrameRate = QuietRun.Check ? -1 : 60; QualitySettings.vSyncCount = QuietRun.Check ? 0 : 1;
            Time.fixedDeltaTime = 1f / 60f;
            Physics.IgnoreLayerCollision(ShiftVehicle.GhostLayer, ShiftVehicle.ObstacleLayer, true);
            Shared = Load<ShiftTuning>("Shift"); Drive = Load<DriveTuning>("Drive"); Boat = Load<BoatTuning>("Boat"); Fly = Load<FlyTuning>("Fly"); Dive = Load<DiveTuning>("Dive");
            JurassicLook.Apply(false);   // remember the Round 1 look before anything changes it
            Courses = WorldBuilder.Build();
            Player = VehicleFactory.MakePlayer(Shared, Drive, Boat, Fly, Dive);
            Driver.Make(Player);
            Cam = Camera.main; if (!Cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; Cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
            Cam.farClipPlane = 1800; Cam.nearClipPlane = .2f; Cam.fieldOfView = 65; Cam.clearFlags = CameraClearFlags.Skybox;
            if (!FindAnyObjectByType<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.gameObject.AddComponent<AudioMeter>();
            Cam.gameObject.AddComponent<ChaseCam>().target = Player;
            CourseAudio.Make(Cam.gameObject, Player);
            Trial = gameObject.AddComponent<TimeTrial>(); Trial.v = Player;
            Hud.Make(Cam, Player, this);
            if (!FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -30, 0); l.shadows = LightShadows.Soft; }
            int start = 0; var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-course"); if (i >= 0 && i + 1 < a.Length) int.TryParse(a[i + 1], out start);
            StartCourse(Mathf.Clamp(start, 0, Courses.Count - 1));
            Debug.Log($"[Game] ready: {Courses.Count} courses, model {VehicleFactory.ModelSource}, driver {Driver.Instance?.Source}, muted {QuietRun.Muted}, listener volume {AudioListener.volume}");
            if (QuietRun.Check) gameObject.AddComponent<CheckPilot>();
        }

        public void StartCourse(int index)
        {
            CourseIndex = (index % Courses.Count + Courses.Count) % Courses.Count;
            var c = Courses[CourseIndex];
            JurassicLook.Apply(c.look == "jurassic");
            CourseAudio.Instance?.SetCourse(c);
            Player.Begin(c);
        }

        void Update()
        {
            var inp = Player.Input;
            if (MenuOpen)
            {
                if (inp.MenuMove != 0) MenuIndex = (MenuIndex + inp.MenuMove + MenuItems.Count) % MenuItems.Count;
                if (inp.ConfirmPressed) Choose(MenuIndex);
                else if (inp.BackPressed || inp.MenuPressed) CloseMenu();
                inp.ConsumeReset(); inp.ConsumeStartStep();
                return;
            }
            if (inp.MenuPressed) { OpenMenu(); return; }
            int step = inp.ConsumeStartStep();
            if (step != 0 && !inp.Scripted) StartCourse(CourseIndex + step);
        }

        public void OpenMenu()
        {
            MenuItems.Clear(); MenuItems.Add("Resume"); MenuItems.Add("Restart " + Courses[CourseIndex].courseName);
            foreach (var c in Courses) MenuItems.Add(c.courseName + (c.timeTrial ? "  (time trial)" : "  (test)"));
            MenuIndex = 0; MenuOpen = true; Time.timeScale = 0;
        }
        public void CloseMenu() { MenuOpen = false; Time.timeScale = 1; }
        public void Choose(int item)
        {
            CloseMenu();
            if (item == 1) StartCourse(CourseIndex);
            else if (item >= 2) StartCourse(item - 2);
        }
    }
}
