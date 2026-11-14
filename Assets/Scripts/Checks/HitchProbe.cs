using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace ShiftStorm
{
    // Round 8 item 8: what a long frame was spent on. A set of the engine's profiler markers (main thread) and memory counters is read every
    // frame; a frame over the limit is kept with the previous frame's marker times (a frame's delta time is mostly the work of the frame
    // before it), the managed GC count change and the allocations. The marker times need a development build (Builds/ShiftStormDev:
    // BuildPlayer -development); in a release build only the counters and the GC count are there.
    public sealed class HitchProbe
    {
        static readonly string[] Markers =
        {
            "PlayerLoop", "GC.Collect", "GC.Alloc", "Shader.CreateGPUProgram", "Shader.Parse", "Loading.ReadObject", "Loading.AwakeFromLoad", "Loading.LoadFileHeaders",
            "AudioClip.LoadAudioData", "Audio.Update", "FixedUpdate.PhysicsFixedUpdate", "FixedUpdate.ScriptRunBehaviourFixedUpdate", "Physics.Simulate", "Physics.Processing",
            "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate", "PreLateUpdate.DirectorUpdateAnimationBegin", "PreLateUpdate.DirectorUpdateAnimationEnd",
            "Camera.Render", "RenderPipelineManager.DoRenderLoop_Internal()", "Inl_UniversalRenderPipeline.RenderSingleCameraInternal", "Gfx.WaitForPresentOnGfxThread", "Gfx.WaitForGfxCommandsFromMainThread",
            "Mesh.UploadMeshData", "Texture.AwakeFromLoad", "Gfx.UploadTexture", "Shadows.RenderShadowMap", "Culling", "SkinnedMeshRenderer.Update", "MeshSkinning.Update",
            "PostLateUpdate.UpdateAllSkinnedMeshes", "PostLateUpdate.UpdateAudio", "Initialization.AsyncUploadTimeSlicedUpdate", "PreUpdate.SendMouseEvents", "Update.ScriptRunDelayedDynamicFrameRate",
            "PostLateUpdate.PlayerUpdateCanvases", "GUI.Repaint", "GUIUtility.ProcessEvent", "ParticleSystem.Update", "PostLateUpdate.ParticleSystemEndUpdateAll",
        };
        readonly List<(string name, ProfilerRecorder r)> recs = new List<(string, ProfilerRecorder)>();
        ProfilerRecorder gcAlloc, gcUsed;
        float[] last = new float[0]; long lastAlloc; int lastGc;
        public readonly List<string> Kept = new List<string>();
        public readonly Dictionary<string, int> Blame = new Dictionary<string, int>();
        public bool HasMarkers { get; private set; }

        public void Start()
        {
            foreach (var m in Markers) { var r = new ProfilerRecorder(m, 2, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately); recs.Add((m, r)); }
            gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            gcUsed = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
            last = new float[recs.Count]; lastGc = System.GC.CollectionCount(0);
        }
        public void Stop() { foreach (var (_, r) in recs) r.Dispose(); recs.Clear(); gcAlloc.Dispose(); gcUsed.Dispose(); }

        // once per frame; dtMs: this frame's delta time. A long one is kept with what the previous frame spent.
        public void Frame(float dtMs, float limitMs, string where)
        {
            int gc = System.GC.CollectionCount(0);
            if (dtMs > limitMs)
            {
                var parts = new List<(string, float)>();
                for (int i = 0; i < recs.Count; i++) if (last[i] > 2f && recs[i].name != "PlayerLoop") parts.Add((recs[i].name, last[i]));
                parts.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                string top = parts.Count > 0 ? string.Join(", ", parts.Take(5).Select(p => $"{p.Item1} {p.Item2:0}")) : "no marker over 2 ms";
                if (parts.Count > 0) { var k = parts[0].Item1; Blame[k] = (Blame.TryGetValue(k, out var n) ? n : 0) + 1; }
                if (gc != lastGc) Blame["(GC collection)"] = (Blame.TryGetValue("(GC collection)", out var g) ? g : 0) + 1;
                Kept.Add($"{dtMs:0} ms {where}: {top}; GC collections {gc - lastGc}, allocated last frame {lastAlloc / 1024} KB, heap {(gcUsed.Valid ? gcUsed.LastValue / (1024 * 1024) : -1)} MB");
            }
            for (int i = 0; i < recs.Count; i++) { var r = recs[i].r; last[i] = r.Valid && r.Count > 0 ? r.LastValue / 1e6f : 0; if (r.Valid && r.Count > 0 && i == 0) HasMarkers = true; }
            lastAlloc = gcAlloc.Valid ? gcAlloc.LastValue : 0; lastGc = gc;
        }
    }
}
