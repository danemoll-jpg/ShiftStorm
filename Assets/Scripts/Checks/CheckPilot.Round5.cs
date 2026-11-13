using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiftStorm
{
    // Round 5 checks (built exe, -batchmode -mute, no window):
    //   garage   item 5: Menu > Garage: every design x every form, pictures from a set angle, a mid-swap picture, "Race with this design"
    //            (the player's and rivals' models rebuilt, Abi on the new seat); "-testDesign _Test" adds the import test's design
    //   lap      (item 1) also reports the speed feel per form: engine pitch, wind volume, field of view, live particles (SpeedFx)
    //   round5 = garage + lap + strip
    public sealed partial class CheckPilot
    {
        // ---- item 1: the speed feel, sampled every frame of the lap ----
        readonly Dictionary<Mode, (float pitch, float wind, float fov, int fx, float frac)> feel = new Dictionary<Mode, (float, float, float, int, float)>();
        void FeelSample()
        {
            if (!measure || !v || v.Holding) return;
            var snd = v.GetComponent<VehicleSound>(); var fx = v.GetComponent<SpeedFx>();
            feel.TryGetValue(v.Mode, out var f);
            feel[v.Mode] = (Mathf.Max(f.pitch, snd ? snd.Pitch : 0), Mathf.Max(f.wind, snd ? snd.WindVolume : 0), Mathf.Max(f.fov, cam.fieldOfView), Math.Max(f.fx, fx ? fx.Live : 0), Mathf.Max(f.frac, v.Speed / v.TopSpeedNow));
        }
        void FeelReport()
        {
            if (feel.Count == 0) return;
            Line("SPEED FEEL (item 1), the most on the lap per form: engine pitch (build 5 topped out at 1.8 x at 45 m/s), wind volume (0-0.6), field of view (deg), live speed particles, speed / top speed");
            foreach (var kv in feel) Line($"  {kv.Key,-5} pitch {kv.Value.pitch:0.00}  wind {kv.Value.wind:0.00}  FOV {kv.Value.fov:0.0}  particles {kv.Value.fx}  speed {kv.Value.frac * 100:0} % of top");
        }

        IEnumerator WaitReal(float s) { float end = Time.unscaledTime + s; while (Time.unscaledTime < end) yield return null; }

        // ---- item 5: the Garage ----
        IEnumerator GarageCheck()
        {
            scenario = "garage"; Clear(); coast = true; BeginAt(0, 0);
            var testName = ""; { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, "-testDesign"); if (i >= 0 && i + 1 < a.Length) testName = a[i + 1]; }
            if (testName != "") VehicleDesigns.AddForCheck(new VehicleDesigns.Design { name = testName, forms = new[] { "Car", "Boat", "Plane", "Sub" }, driver = true });
            yield return Wait(.5f);
            game.ShowGarage(); yield return WaitReal(.3f);
            var gar = Garage.Instance;
            Line($"GARAGE: menu page {game.MenuPage}, designs {string.Join(", ", VehicleDesigns.Names)}; items: {string.Join(" | ", game.MenuItems)}");
            foreach (var d in VehicleDesigns.Names)
            {
                gar.SetDesign(d); game.RefreshGarage(); yield return WaitReal(.2f);
                var forms = new List<string>();
                foreach (var m in FormSet.All)
                {
                    gar.SetForm(m, false); game.RefreshGarage(1); gar.Pose(35); yield return WaitReal(.15f);
                    var r = gar.Shown.ModelOf(m).GetComponentsInChildren<Renderer>(); var b = r.Length > 0 ? r[0].bounds : new Bounds(); foreach (var x in r) b.Encapsulate(x.bounds);
                    forms.Add($"{Game.FormName(m)} {gar.FormInfo(m)} {b.size.x:0.0} x {b.size.y:0.0} x {b.size.z:0.0} m");
                    Capture($"{d.Replace(' ', '-')}-{Game.FormName(m)}");
                }
                Line($"  {d}: {string.Join("; ", forms)}");
                // the change effect, mid-swap and just after
                gar.SetForm(Mode.Drive, false); gar.Pose(35); yield return WaitReal(.1f);
                gar.SetForm(Mode.Fly, true); yield return WaitReal(.2f); Capture($"{d.Replace(' ', '-')}-change-squash");
                yield return WaitReal(.15f); Capture($"{d.Replace(' ', '-')}-change-swap"); yield return WaitReal(.5f);
                Line($"  {d}: change Car->Plane in the Garage: done {gar.Shown.Done}, swapped {gar.Shown.Swapped}, bursts {gar.Shown.BurstsPlayed}");
            }
            // race with a design (the test design if given, else the last one)
            string pick = testName != "" ? testName : VehicleDesigns.Names.Last();
            gar.SetDesign(pick); game.UseDesign(pick); yield return WaitReal(.2f);
            var pf = v.Forms; int rivalsOk = game.Rivals.Count(rv => rv.GetComponentInChildren<FormSet>(true).DesignName == pf.DesignName);
            Line($"GARAGE race with '{pick}': player's design {pf.DesignName} (forms imported: {string.Join("/", FormSet.All.Where(pf.IsImported).Select(Game.FormName))}), rivals in it {rivalsOk}/{game.Rivals.Count}, saved setting {Settings.Design}");
            game.CloseMenu(); yield return Wait(.3f);
            // a race start in that design: the grid and a change mid-way
            game.StartCourse(0, true); yield return Until(() => !v.Holding, 6f); coast = false; smart = true;
            Shot(.4f, $"race-in-{pick}"); yield return Wait(1.5f);
            Line($"GARAGE race in '{pick}': shown form {Game.FormName(v.Forms.Current)}, Abi seat offset {v.Forms.SeatOffset}, Abi shown {v.Forms.ShowDriver}");
            game.UseDesign(VehicleDesigns.GrayBox); Clear(); coast = true; game.StartCourse(0, false); yield return Wait(.2f);   // back to a time trial for the checks after
            Save();
        }
    }
}
