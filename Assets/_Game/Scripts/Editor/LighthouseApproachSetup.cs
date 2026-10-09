using System.Collections.Generic;
using System.IO;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Editor
{
    /// <summary>Editor-API graybox recipe for the Recall + Echo lighthouse approach.</summary>
    public static class LighthouseApproachSetup
    {
        public const string SceneName = "LighthouseApproach01";
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/LighthouseApproach01.unity";

        private const string OldLight02ScenePath = "Assets/_Game/Scenes/Prototype/OldLight02.unity";
        private const string StonePath = "Assets/_Game/Art/Materials/OldLight02Stone.mat";
        private const string MemoryPath = "Assets/_Game/Art/Materials/OldLight02Memory.mat";
        private const string GhostPath = "Assets/_Game/Art/Materials/OldLight02Ghost.mat";
        private const string HintPath = "Assets/_Game/Art/Materials/OldLight02Hint.mat";

        [MenuItem("Light Remembers/Prototype/Build Milestone 9 Lighthouse Approach")]
        public static void Build()
        {
            var stone = LoadMaterial(StonePath);
            var memory = LoadMaterial(MemoryPath);
            var ghost = LoadMaterial(GhostPath);
            var hint = LoadMaterial(HintPath);
            var scene = OpenOrCreateScene();
            EditorSceneManager.SetActiveScene(scene);
            ClearScene(scene);
            BuildScene(scene, stone, memory, ghost, hint);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            ConnectOldLightExit();
            EditorSceneManager.SetActiveScene(scene);
        }

        private static void BuildScene(Scene scene, Material stone, Material memory, Material ghost, Material hint)
        {
            var root = EditorAutomation.CreateGameObject(SceneName, null, Vector3.zero, Vector3.zero, Vector3.one);
            var environment = Child("Environment", root.transform);
            var gameplay = Child("Gameplay", root.transform);
            var memoryRoot = Child("Memory", root.transform);
            var lighting = Child("Lighting", root.transform);
            var checkpoints = Child("Checkpoints", gameplay);
            var resetVolumes = Child("ResetVolumes", gameplay);
            var focus = Child("LighthouseApproachFocusGroup", memoryRoot).gameObject.AddComponent<MemoryFocusGroup>();

            Cube("LighthouseApproach_NearBank", environment, new Vector3(0f, -0.3f, 2f),
                new Vector3(8f, 0.6f, 9f), stone);
            Cube("LighthouseApproach_FarBank", environment, new Vector3(0f, -0.3f, 21f),
                new Vector3(8f, 0.6f, 10f), stone);
            Cube("NearAbutment_West", environment, new Vector3(-3.5f, 1f, 6.15f),
                new Vector3(0.45f, 2f, 0.55f), stone);
            Cube("NearAbutment_East", environment, new Vector3(3.5f, 1f, 6.15f),
                new Vector3(0.45f, 2f, 0.55f), stone);
            Cube("FarAbutment_West", environment, new Vector3(-3.5f, 1f, 16.15f),
                new Vector3(0.45f, 2f, 0.55f), stone);
            Cube("FarAbutment_East", environment, new Vector3(3.5f, 1f, 16.15f),
                new Vector3(0.45f, 2f, 0.55f), stone);
            BuildLighthouseLandmark(environment, stone, hint);
            BuildPuzzle(memoryRoot, stone, memory, ghost, hint, focus);
            BuildLighting(lighting);

            var playerSpawn = Child("PlayerSpawn", gameplay);
            playerSpawn.position = new Vector3(0f, 0.1f, 0.8f);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null)
                throw new FileNotFoundException("Build the existing Milestone 2 player prefab before LighthouseApproach01.");

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            var reader = player.GetComponent<PlayerInputReader>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay, player.transform.Find("CameraTarget"), reader);
            player.GetComponent<PlayerMovement>().Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            var lightOrigin = player.transform.Find("MemoryLightOrigin");
            player.GetComponent<PlayerMemoryLight>().Configure(reader, lightOrigin, camera.transform,
                lightOrigin.GetComponent<Light>());
            player.AddComponent<CheckpointRespawner>();

            gameplay.gameObject.AddComponent<MemoryAbilityBootstrap>();
            MakeCheckpoint(checkpoints, "Checkpoint_ApproachStart", new Vector3(0f, 0.1f, 1f));
            MakeCheckpoint(checkpoints, "Checkpoint_ApproachFarBank", new Vector3(0f, 0.1f, 18.5f));
            Hazard(resetVolumes, "ApproachFallReset", new Vector3(0f, -2f, 11f), new Vector3(18f, 1f, 18f));
        }

        private static void BuildPuzzle(Transform memoryRoot, Material stone, Material memory, Material ghost,
            Material hint, MemoryFocusGroup focus)
        {
            const float startZ = 6.4f;
            const float destinationZ = 14.2f;
            var root = Child("LighthousePontoonMemory", memoryRoot);
            root.position = new Vector3(0f, 0f, startZ);

            var present = Child("PresentState_Ruins", root);
            Cube("MemoryTarget_YellowCrack", present, new Vector3(0f, 0.95f, -1.85f),
                new Vector3(0.62f, 1.9f, 0.5f), stone);
            CreateCrack(present, new Vector3(0.32f, 0.45f, -2.11f), hint);
            Cube("BrokenPontoonPost_West", present, new Vector3(-1.35f, 0.35f, 0.2f),
                new Vector3(0.5f, 0.7f, 0.5f), stone);
            Cube("BrokenPontoonPost_East", present, new Vector3(1.35f, 0.35f, 0.2f),
                new Vector3(0.5f, 0.7f, 0.5f), stone);
            Cube("BrokenPlank_Debris", present, new Vector3(0.65f, 0.12f, 1.4f),
                new Vector3(1.5f, 0.24f, 0.55f), stone);

            var memoryState = Child("MemoryState", root);
            var preview = Child("GhostPreview", memoryState);
            var ghostForm = Cube("RememberedPontoon_Ghost", preview, Vector3.zero,
                new Vector3(3.6f, 0.26f, 4.5f), ghost);
            Object.DestroyImmediate(ghostForm.GetComponent<Collider>());

            var materialized = Child("MaterializedMemory", memoryState);
            var pontoon = Cube("RememberedPontoon", materialized, Vector3.zero,
                new Vector3(3.6f, 0.26f, 4.5f), memory);
            var rideObject = Child("EchoRideSurface", pontoon.transform).gameObject;
            var rideTrigger = rideObject.AddComponent<BoxCollider>();
            rideTrigger.isTrigger = true;
            rideTrigger.center = Vector3.up * 1.05f;
            rideTrigger.size = new Vector3(3.9f, 2.4f, 4.8f);
            rideObject.AddComponent<EchoRideSurface>().Configure(rideTrigger);

            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryState.gameObject, preview.gameObject,
                materialized.gameObject, 26f);
            recall.ConfigureFocusGroup(focus);

            var path = BuildEchoPath(memoryRoot, ghost, startZ, destinationZ);
            var echo = root.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path.Path, materialized, path.Preview, rideObject.GetComponent<EchoRideSurface>(),
                materialized.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureRecallRequirement(recall);
            root.gameObject.AddComponent<MemoryComposite>().Configure(recall, echo);
            recall.RefreshStateObjects();
            pontoon.transform.SetPositionAndRotation(path.Path.Waypoints[0].position, path.Path.Waypoints[0].rotation);
        }

        private static (EchoPath Path, GameObject Preview) BuildEchoPath(Transform parent, Material ghost,
            float startZ, float destinationZ)
        {
            var pathRoot = Child("LighthousePontoon_EchoPath", parent);
            var start = Child("Waypoint_Start", pathRoot);
            start.position = new Vector3(0f, 0f, startZ);
            var destination = Child("Waypoint_FarBank", pathRoot);
            destination.position = new Vector3(0f, 0f, destinationZ);
            var path = pathRoot.gameObject.AddComponent<EchoPath>();
            path.Configure(new[] { start, destination }, 5f, 8f, true);

            var preview = Child("LighthousePontoon_MotionPreview", parent);
            preview.gameObject.SetActive(false);
            var line = Child("RememberedMotionLine", preview).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.11f;
            line.endWidth = 0.11f;
            line.sharedMaterial = ghost;
            line.numCapVertices = 3;
            line.SetPosition(0, start.position + Vector3.up * 0.48f);
            line.SetPosition(1, destination.position + Vector3.up * 0.48f);
            for (var i = 0; i < 2; i++)
            {
                var marker = EditorAutomation.CreatePrimitive(PrimitiveType.Sphere, $"MotionGlow_{i + 1:00}",
                    preview, (i == 0 ? start.position : destination.position) + Vector3.up * 0.48f,
                    Vector3.zero, Vector3.one * 0.3f);
                marker.GetComponent<Renderer>().sharedMaterial = ghost;
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            return (path, preview.gameObject);
        }

        private static void BuildLighthouseLandmark(Transform environment, Material stone, Material hint)
        {
            var tower = Cube("LighthouseApproachBeacon", environment, new Vector3(0f, 2.2f, 25f),
                new Vector3(1.2f, 4.4f, 1.2f), stone);
            var light = Cube("BeaconLamp", tower.transform, new Vector3(0f, 0.31f, -0.51f),
                new Vector3(0.72f, 0.52f, 0.08f), hint);
            Object.DestroyImmediate(light.GetComponent<Collider>());
            Cube("BeaconCap", environment, new Vector3(0f, 4.55f, 25f),
                new Vector3(1.65f, 0.3f, 1.65f), hint);
        }

        private static void BuildLighting(Transform parent)
        {
            var key = Child("LighthouseApproachKeyLight", parent);
            key.localEulerAngles = new Vector3(48f, -28f, 0f);
            var light = key.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.85f;
            light.color = new Color(0.72f, 0.8f, 0.92f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.12f, 0.15f);
        }

        private static void MakeCheckpoint(Transform parent, string name, Vector3 position)
        {
            var respawn = Child(name + "_Spawn", parent);
            respawn.position = position;
            var checkpoint = Child(name, parent);
            checkpoint.position = position + Vector3.forward;
            var trigger = checkpoint.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(7f, 2.5f, 2f);
            checkpoint.gameObject.AddComponent<Checkpoint>().Configure(respawn);
        }

        private static void Hazard(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var reset = Child(name, parent);
            reset.position = position;
            var trigger = reset.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            reset.gameObject.AddComponent<ResetVolume>();
        }

        private static void CreateCrack(Transform parent, Vector3 origin, Material material)
        {
            var offsets = new[]
            {
                Vector3.zero, new Vector3(0.16f, 0.2f, 0f), new Vector3(-0.04f, 0.42f, 0f),
                new Vector3(0.14f, 0.63f, 0f), new Vector3(-0.06f, 0.84f, 0f)
            };
            for (var i = 0; i < offsets.Length - 1; i++)
            {
                var a = origin + offsets[i];
                var b = origin + offsets[i + 1];
                var delta = b - a;
                var segment = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, $"YellowMemoryCrack_{i}",
                    parent, (a + b) * 0.5f, Quaternion.LookRotation(delta, Vector3.up).eulerAngles,
                    new Vector3(0.055f, 0.025f, delta.magnitude));
                segment.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(segment.GetComponent<Collider>());
            }
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent, position, Vector3.zero, size);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Transform Child(string name, Transform parent) =>
            EditorAutomation.CreateGameObject(name, parent, Vector3.zero, Vector3.zero, Vector3.one).transform;

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                throw new FileNotFoundException($"Required prototype material not found: {path}");
            return material;
        }

        private static Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
            {
                var loaded = SceneManager.GetSceneByPath(ScenePath);
                return loaded.IsValid() && loaded.isLoaded
                    ? loaded
                    : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                Object.DestroyImmediate(root);
        }

        private static void ConnectOldLightExit()
        {
            var scene = SceneManager.GetSceneByPath(OldLight02ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(OldLight02ScenePath, OpenSceneMode.Additive);

            var exitObject = EditorAutomation.FindGameObject(scene, "OldLight02Exit");
            var barrier = EditorAutomation.FindGameObject(scene, "OldLight02ExitBarrier");
            var exit = exitObject != null ? exitObject.GetComponent<ExitUnlocker>() : null;
            if (exit == null || barrier == null)
                throw new System.InvalidOperationException("OldLight02 exit or barrier was not found; cannot link the lighthouse approach.");

            var subtitlePanel = EditorAutomation.FindGameObject(scene, "SubtitlePanel");
            var subtitles = subtitlePanel != null ? subtitlePanel.GetComponent<LightRemembers.UI.SubtitlePresenter>() : null;
            exit.Configure(barrier, SceneName, subtitles);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
