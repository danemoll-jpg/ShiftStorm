using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Start-up: loads the tuning (Resources/Tuning), builds the gray boxes, makes the player's vehicle, camera and HUD, and starts on the
    // transition strip (or "-course <n>": 0 strip, 1 drive lane, 2 boat lane, 3 fly lane, 4 dive lane). D-pad left / right (F3 / F4) moves to
    // the previous / next course. A check run ("-check <name>") hands over to CheckPilot.
    public sealed class Game : MonoBehaviour
    {
        public static Game Instance { get; private set; }
        public List<Course> Courses { get; private set; }
        public ShiftVehicle Player { get; private set; }
        public Camera Cam { get; private set; }
        public int CourseIndex { get; private set; }
        public ShiftTuning Shared; public DriveTuning Drive; public BoatTuning Boat; public FlyTuning Fly; public DiveTuning Dive;

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
            Courses = WorldBuilder.Build();
            Player = VehicleFactory.MakePlayer(Shared, Drive, Boat, Fly, Dive);
            Cam = Camera.main; if (!Cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; Cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
            Cam.farClipPlane = 1500; Cam.nearClipPlane = .2f; Cam.fieldOfView = 65;
            if (!FindAnyObjectByType<AudioListener>()) Cam.gameObject.AddComponent<AudioListener>();
            Cam.gameObject.AddComponent<AudioMeter>();
            Cam.gameObject.AddComponent<ChaseCam>().target = Player;
            Hud.Make(Cam, Player, this);
            if (!FindAnyObjectByType<Light>()) { var l = new GameObject("Sun").AddComponent<Light>(); l.type = LightType.Directional; l.transform.rotation = Quaternion.Euler(50, -30, 0); l.shadows = LightShadows.Soft; }
            int start = 0; var a = System.Environment.GetCommandLineArgs(); int i = System.Array.IndexOf(a, "-course"); if (i >= 0 && i + 1 < a.Length) int.TryParse(a[i + 1], out start);
            StartCourse(Mathf.Clamp(start, 0, Courses.Count - 1));
            Debug.Log($"[Game] ready: {Courses.Count} courses, model {VehicleFactory.ModelSource}, muted {QuietRun.Muted}, listener volume {AudioListener.volume}");
            if (QuietRun.Check) gameObject.AddComponent<CheckPilot>();
        }

        public void StartCourse(int index)
        {
            CourseIndex = (index % Courses.Count + Courses.Count) % Courses.Count;
            Player.Begin(Courses[CourseIndex]);
        }

        void Update()
        {
            int step = Player.Input.ConsumeStartStep();
            if (step != 0 && !Player.Input.Scripted) StartCourse(CourseIndex + step);
        }
    }
}
