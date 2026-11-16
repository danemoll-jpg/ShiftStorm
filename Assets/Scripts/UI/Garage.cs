using System.Collections.Generic;
using UnityEngine;

namespace ShiftStorm
{
    // Round 5 item 5: the Garage (Menu > Garage), so Dan can judge vehicle designs on the Deck: the chosen design's form turning slowly on
    // a turntable, far from the course (30 km out, past the camera's far plane from the track), lit by the course's sun. The menu lists:
    // Design (A: next), Form (A: next, played with the change effect), Race with this design, Back. The main camera is borrowed while the
    // page is open (ChaseCam stands aside), the game is paused, so everything here runs on unscaled time.
    public sealed class Garage : MonoBehaviour
    {
        public static Garage Instance { get; private set; }
        public static bool Active => Instance && Instance.open;
        public static readonly Vector3 Stage = new Vector3(0, 400, 30000);
        bool open; FormSet set; Transform turntable; Camera cam; float spin; bool fogWas;
        public string Design { get; private set; } = VehicleDesigns.GrayBox;
        public Mode Form { get; private set; } = Mode.Drive;
        public FormSet Shown => set;

        public static Garage Get()
        {
            if (Instance) return Instance;
            var g = new GameObject("Garage"); Instance = g.AddComponent<Garage>(); Instance.BuildStage(); return Instance;
        }

        void BuildStage()
        {
            transform.position = Stage;
            turntable = new GameObject("Turntable").transform; turntable.SetParent(transform, false);
            var floor = new MeshKit.Batch();
            floor.Add(MeshKit.LowCylinder, new Vector3(0, -1.05f, 0), Quaternion.identity, new Vector3(9, .3f, 9), new Color(.82f, .8f, .74f));
            floor.Add(MeshKit.LowCylinder, new Vector3(0, -1.2f, 0), Quaternion.identity, new Vector3(9.6f, .2f, 9.6f), new Color(1f, .62f, .15f));
            floor.Object(turntable, "Turntable disc", JurassicLook.VertexColored(true), false, false);
            var back = new MeshKit.Batch();
            back.Add(MeshKit.LowCylinder, new Vector3(0, -1.6f, 0), Quaternion.identity, new Vector3(80, .2f, 80), new Color(.55f, .72f, .5f));
            back.Object(transform, "Garage ground", JurassicLook.VertexColored(false), false, false);
            gameObject.SetActive(false);
        }

        public void Open(Camera c)
        {
            cam = c; open = true; gameObject.SetActive(true); fogWas = RenderSettings.fog;
            if (!set || set.DesignName != Design) Rebuild();
        }
        public void Close() { open = false; gameObject.SetActive(false); RenderSettings.fog = fogWas; }

        public void NextDesign() { var n = VehicleDesigns.Names; int i = n.IndexOf(Design); Design = n[(i + 1) % n.Count]; Rebuild(); }
        public void SetDesign(string d) { Design = d; Rebuild(); }
        public void NextForm() { int i = System.Array.IndexOf(FormSet.All, Form); Form = FormSet.All[(i + 1) % 4]; if (set) { set.Unscaled = true; set.Begin(Form, .6f); } }
        public void SetForm(Mode m, bool effect) { Form = m; if (!set) return; set.Unscaled = true; if (effect) set.Begin(m, .6f); else set.Snap(m); }

        void Rebuild()
        {
            if (set) Destroy(set.gameObject);
            set = FormSet.Make(turntable, Design, Paints.Color, Form, 1f, true);   // Round 13: the paint won in rival challenges set.Unscaled = true;
        }
        public string FormInfo(Mode m) => set && set.IsImported(m) ? "Tripo model" : "gray box";

        void LateUpdate()
        {
            if (!open || !cam) return;
            spin = (spin + Time.unscaledDeltaTime * 24f) % 360f;   // a slow turn: 15 s a round
            turntable.localRotation = Quaternion.Euler(0, spin, 0);
            // the vehicle in the upper part of the screen, the menu below it (a 7 m plane fits across)
            cam.transform.position = Stage + new Vector3(0, 3.0f, -9.6f);
            cam.transform.rotation = Quaternion.LookRotation(Stage + new Vector3(0, -1.5f, 0) - cam.transform.position, Vector3.up);
            cam.fieldOfView = 34;
            RenderSettings.fog = false;
        }
        // a picture from a set yaw (checks), the turntable stopped there
        public void Pose(float yaw) { spin = yaw; turntable.localRotation = Quaternion.Euler(0, yaw, 0); }
    }
}
