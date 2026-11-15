using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShiftStorm.EditorTools
{
    // One-time / repeatable project setup, batch mode: "Unity -batchmode -quit -projectPath . -executeMethod ShiftStorm.EditorTools.ProjectSetup.Run".
    // URP (Assets/Settings/URP.asset, copied from chastain_rush), Input System only, layers (8 Vehicle, 9 VehicleGhost, 10 Obstacle), player
    // settings, the materials the gray boxes use (Resources/Materials), the tuning assets (Resources/Tuning, made only if missing so tuned
    // values are never overwritten), the scene (Assets/Scenes/Main.unity) and the build list. Safe to run again.
    public static class ProjectSetup
    {
        public static void Run()
        {
            // render pipeline
            var urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP.asset");
            GraphicsSettings.defaultRenderPipeline = urp;
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = urp; }
            // Input System only, player settings
            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            ps.FindProperty("activeInputHandler").intValue = 1; ps.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.companyName = "ShiftStorm"; PlayerSettings.productName = "ShiftStorm";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 800;
            PlayerSettings.resizableWindow = true; PlayerSettings.runInBackground = false; PlayerSettings.visibleInBackground = false;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.danemoll.shiftstorm");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/GameIcon.png"); if (icon) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            // layers
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); var layers = tm.FindProperty("layers");
            layers.GetArrayElementAtIndex(8).stringValue = "Vehicle"; layers.GetArrayElementAtIndex(9).stringValue = "VehicleGhost"; layers.GetArrayElementAtIndex(10).stringValue = "Obstacle";
            tm.ApplyModifiedPropertiesWithoutUndo();
            // materials and tuning
            Directory.CreateDirectory("Assets/Resources/Materials"); Directory.CreateDirectory("Assets/Resources/Tuning");
            Mat("Gate", new Color(.95f, .85f, .2f));   // Round 10: the Round 1 test courses' materials are gone
            Mat("SignCar", new Color(.95f, .5f, .1f)); Mat("SignWing", new Color(.35f, .7f, 1f)); Mat("SignBoat", new Color(.1f, .75f, .7f)); Mat("SignSub", new Color(.15f, .2f, .55f));   // Round 10: the Round 1 test courses' materials are gone
            Mat("VehicleBody", new Color(.95f, .45f, .1f)); Mat("VehicleTrim", new Color(.95f, .9f, .8f)); Mat("VehicleGlass", new Color(.35f, .65f, .9f, .7f), transparent: true);
            Mat("VehicleTire", new Color(.08f, .08f, .09f)); Mat("VehicleWing", new Color(.96f, .96f, .96f)); Mat("VehicleFin", new Color(.1f, .6f, .65f)); Mat("VehicleMetal", new Color(.55f, .57f, .6f));
            // Round 2: Paradise Plunge's toon look (ShiftStorm/* shaders, URP ports of Tee'd Off's): base materials the runtime copies and colours
            Directory.CreateDirectory("Assets/Resources/Materials/Jurassic");
            foreach (var (n, sh) in new[] { ("Toon", "ShiftStorm/Toon"), ("ToonOutline", "ShiftStorm/ToonOutline"), ("ToonWater", "ShiftStorm/ToonWater"), ("Waterfall", "ShiftStorm/Waterfall"), ("Sky", "ShiftStorm/Sky") })
            {
                string path = $"Assets/Resources/Materials/Jurassic/{n}.mat"; var shader = Shader.Find(sh);
                if (!shader) { Debug.LogError("[Setup] shader missing: " + sh); continue; }
                var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); } else m.shader = shader;
                EditorUtility.SetDirty(m);
            }
            Tuning<ShiftTuning>("Shift"); Tuning<DriveTuning>("Drive"); Tuning<BoatTuning>("Boat"); Tuning<FlyTuning>("Fly"); Tuning<DiveTuning>("Dive");
            // scene and build list
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists("Assets/Scenes/Main.unity"))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                var sun = Object.FindAnyObjectByType<Light>(); if (sun) { sun.shadows = LightShadows.Soft; sun.intensity = 1.6f; sun.transform.rotation = Quaternion.Euler(48, -35, 0); }
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = .0015f; RenderSettings.fogColor = new Color(.7f, .82f, .92f);
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] done: URP " + (urp ? "set" : "MISSING") + ", input handler 1 (Input System only), layers, materials, tuning, scene");
        }

        static void Tuning<T>(string name) where T : ScriptableObject
        {
            string path = $"Assets/Resources/Tuning/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path)) return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        }

        static void Mat(string name, Color color, bool transparent = false, bool emission = false)
        {
            string path = $"Assets/Resources/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", transparent ? .85f : .2f);
            if (transparent)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0); m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent; m.SetOverrideTag("RenderType", "Transparent");
                m.SetFloat("_Cull", 0);
            }
            if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 2f); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; }
            EditorUtility.SetDirty(m);
        }
    }

    // The Blender vehicle (Tools/Blender/shiftstorm_vehicle.py) imports without axis rotations, no materials (FormAnimator / VehicleFactory set them).
    // Round 2: Abi (Resources/Driver/Abigail.fbx, from Tee'd Off with its own Humanoid avatar settings) imports as Humanoid without
    // animation or materials (Driver paints her with the toon shader); her and the dinosaurs' textures are capped in size (Steam Deck memory);
    // music streams, ambience is compressed in memory, short sounds decompress on load.
    public sealed class VehicleImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            var ti = (TextureImporter)assetImporter;
            if (assetPath.Contains("Resources/Dinos/")) ti.maxTextureSize = 1024;
            else if (assetPath.Contains("Resources/Driver/")) ti.maxTextureSize = 2048;
            else if (assetPath.Contains("Resources/Vehicles/")) { ti.maxTextureSize = 1024; ti.isReadable = true; }   // Round 5: readable (rival recolour)
        }
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("Resources/Audio/")) return;
            var ai = (AudioImporter)assetImporter; var st = ai.defaultSampleSettings;
            if (assetPath.Contains("/Music/")) { st.loadType = AudioClipLoadType.Streaming; st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = .6f; }
            else if (assetPath.Contains("/amb_jungle") || assetPath.Contains("/amb_bubbles")) { st.loadType = AudioClipLoadType.CompressedInMemory; st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = .5f; }
            else { st.loadType = AudioClipLoadType.DecompressOnLoad; st.compressionFormat = AudioCompressionFormat.Vorbis; st.quality = .6f; }
            st.preloadAudioData = !assetPath.Contains("/Music/");   // Round 7: new effects and voice preloaded too (off caused first-play hitches, Round 2b)
            ai.defaultSampleSettings = st; ai.forceToMono = false; ai.loadInBackground = assetPath.Contains("/Music/");
        }
        // Round 2: the shared driver animations (Mixamo, "Without Skin", Resources/Drivers/Animations): Humanoid clips any driver can play.
        // Driving and Sitting Idle loop; all keep the root in place (the seat holds the driver).
        static bool IsDriverClip(string path) => path.Contains("Resources/Drivers/Animations/");
        void OnPreprocessAnimation()
        {
            if (!IsDriverClip(assetPath)) return;
            var mi = (ModelImporter)assetImporter; var clips = mi.defaultClipAnimations; bool loop = assetPath.EndsWith("Driving.fbx") || assetPath.EndsWith("Sitting Idle.fbx");
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = loop; clips[i].lockRootRotation = true; clips[i].lockRootHeightY = true; clips[i].lockRootPositionXZ = true;
                clips[i].keepOriginalOrientation = true; clips[i].keepOriginalPositionY = true; clips[i].keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
        }
        void OnPreprocessModel()
        {
            if (IsDriverClip(assetPath))
            {
                var am = (ModelImporter)assetImporter;
                am.animationType = ModelImporterAnimationType.Human; am.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; am.importAnimation = true;
                am.materialImportMode = ModelImporterMaterialImportMode.None; am.importCameras = false; am.importLights = false;
                return;
            }
            if (assetPath.EndsWith("Driver/Abigail.fbx"))
            {
                var hm = (ModelImporter)assetImporter;
                hm.animationType = ModelImporterAnimationType.Human; hm.importAnimation = false; hm.materialImportMode = ModelImporterMaterialImportMode.None;
                hm.importCameras = false; hm.importLights = false; hm.addCollider = false; hm.isReadable = true;
                return;
            }
            if (!assetPath.EndsWith("ShiftVehicle.fbx") && !assetPath.Contains("Resources/Vehicles/")) return;   // Round 5: Tools/import_vehicles.py output too
            var mi = (ModelImporter)assetImporter;
            mi.bakeAxisConversion = true; mi.globalScale = 1; mi.useFileScale = true; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.None; mi.importCameras = false; mi.importLights = false; mi.addCollider = false;
        }
    }
}
