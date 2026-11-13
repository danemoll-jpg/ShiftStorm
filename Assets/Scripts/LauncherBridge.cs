using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ShiftStorm
{
    // Copied from chastain_rush (Assets/Scripts/LauncherBridge.cs, itself Woodstock Rush's). The launcher (Launcher/, ShiftStormLauncher.exe) starts
    // the game with -racerManagedMusic <soundtrack> -racerPersonalMusic <kept songs> -racerLauncherReady <file> -racerLauncherNonce <text> (argument
    // names kept as in Chastain Rush so the launcher code stays the same). Once the game is up (the player's ShiftVehicle running a few frames) this
    // writes the nonce to the ready file, so the launcher knows the start worked. Changed for ShiftStorm: waits for ShiftVehicle instead of GameShell,
    // no WorldBoot deferral, and Chastain Rush's ReplaceOldLauncher (its launcher-1 migration) is dropped: ShiftStorm has no old launcher.
    public sealed class LauncherBridge : MonoBehaviour
    {
        public static string Argument(string key) { var args = Environment.GetCommandLineArgs(); for (int i = 1; i + 1 < args.Length; i++) if (args[i] == key) return args[i + 1]; return null; }
        public static string ManagedMusic => Argument("-racerManagedMusic");
        public static string PersonalMusic => Argument("-racerPersonalMusic");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartBridge()
        {
            if (Argument("-racerLauncherReady") == null) return;
            var go = new GameObject("Launcher startup handshake"); DontDestroyOnLoad(go); go.AddComponent<LauncherBridge>();
        }

        IEnumerator Start()
        {
            var path = Argument("-racerLauncherReady"); var nonce = Argument("-racerLauncherNonce");
            if (string.IsNullOrEmpty(nonce) || nonce.Length > 128 || !Path.IsPathRooted(path)) { Destroy(gameObject); yield break; }
            float end = Time.realtimeSinceStartup + 90f; ShiftVehicle player = null;
            while (Time.realtimeSinceStartup < end && !(player = FindAnyObjectByType<ShiftVehicle>())) yield return null;
            for (int i = 0; i < 3; i++) yield return null;
            if (player)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path + ".tmp", nonce); File.Move(path + ".tmp", path); }
                catch (Exception e) { Debug.LogWarning("Launcher ready signal unavailable: " + e.Message); }
                Debug.Log("[Launcher] ready signal written");
            }
            Destroy(gameObject);
        }
    }
}
