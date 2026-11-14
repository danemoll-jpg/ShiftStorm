using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftStorm.EditorTools
{
    // Windows player build, batch mode: "Unity -batchmode -quit -projectPath . -executeMethod ShiftStorm.EditorTools.BuildPlayer.Run
    // -buildVersion 0.1.0". Output: Builds/ShiftStorm/ShiftStorm.exe. Also logs where the imported vehicle's parts ended up (a check that the
    // Blender model came in facing +Z with its parts named).
    public static class BuildPlayer
    {
        public static void Run()
        {
            string version = Arg("-buildVersion") ?? "0.1.0";
            PlayerSettings.bundleVersion = version;
            PlayerSettings.enableFrameTimingStats = true;
            // Round 2b: borderless full screen at the native resolution by default (ScreenSetting applies it at start-up); the game handles
            // Alt+Enter itself (and saves the choice), so Unity's own switch is off
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.defaultIsNativeResolution = true; PlayerSettings.allowFullscreenSwitch = false;   // checks read GPU frame times (FrameTimingManager) where the device gives them
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Models/ShiftVehicle.fbx");
            if (model) foreach (var n in new[] { "Wheel_FL", "Wheel_RL", "Wing_L", "Wing_R", "Tail", "Canopy" }) { var t = model.transform.Find(n); Debug.Log($"[Build] model part {n}: {(t ? t.localPosition.ToString("F2") + " rot " + t.localEulerAngles.ToString("F0") : "MISSING")}"); }
            else Debug.LogWarning("[Build] no vehicle FBX: the game uses its primitive fallback");
            // Round 8 item 8: "-development" makes a profiling build (Builds/ShiftStormDev: ProfilerRecorder sees the engine's markers) for the
            // hitch hunt; the release build never has it
            bool dev = Array.IndexOf(Environment.GetCommandLineArgs(), "-development") >= 0; string outDir = dev ? "Builds/ShiftStormDev" : "Builds/ShiftStorm";
            Directory.CreateDirectory(outDir);
            var options = new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = outDir + "/ShiftStorm.exe", target = BuildTarget.StandaloneWindows64, options = dev ? BuildOptions.Development : BuildOptions.None };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Build] {report.summary.result}: {report.summary.totalSize / 1e6:0.0} MB, {report.summary.totalErrors} errors, {report.summary.totalTime.TotalSeconds:0} s");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
        static string Arg(string k) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, k); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }
    }
}
