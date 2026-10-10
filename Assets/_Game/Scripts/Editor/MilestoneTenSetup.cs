using System.IO;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using LightRemembers.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LightRemembers.Editor
{
    /// <summary>Builds the Forget prototype scene with Unity Editor APIs.</summary>
    public static class MilestoneTenSetup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/ForgetPrototype.unity";
        private const string FragmentPath = "Assets/_Game/Art/MemoryFragments/memory_archive_04.asset";
        private const string StonePath = "Assets/_Game/Art/Materials/OldLight02Stone.mat";
        private const string MemoryPath = "Assets/_Game/Art/Materials/OldLight02Memory.mat";
        private const string GhostPath = "Assets/_Game/Art/Materials/OldLight02Ghost.mat";
        private const string HintPath = "Assets/_Game/Art/Materials/OldLight02Hint.mat";
        private const string VoidPath = "Assets/_Game/Art/Materials/HollowEncounterVoid.mat";

        [MenuItem("Light Remembers/Prototype/Build Milestone 10 Forget Archive")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Art/MemoryFragments");
            var stone = LoadMaterial(StonePath);
            var memory = LoadMaterial(MemoryPath);
            var ghost = LoadMaterial(GhostPath);
            var hint = LoadMaterial(HintPath);
            var voidMaterial = LoadMaterial(VoidPath);
            var fragment = GetOrCreateFragment();

            var scene = OpenOrCreateScene();
            EditorSceneManager.SetActiveScene(scene);
            foreach (var existing in scene.GetRootGameObjects())
                Object.DestroyImmediate(existing);

            var root = Child("ForgetPrototype", null);
            var environment = Child("Environment", root.transform);
            var entrance = Child("Entrance", environment.transform);
            var archiveHall = Child("ArchiveHall", environment.transform);
            var lockedPassage = Child("LockedPassage", environment.transform);
            var memoryRoom = Child("MemoryRoom", environment.transform);
            var exit = Child("Exit", environment.transform);
            var forgetObjects = Child("ForgetObjects", root.transform);
            var memoryObjects = Child("MemoryObjects", root.transform);
            var hollowRoot = Child("Hollow", root.transform);
            var lighting = Child("Lighting", root.transform);
            var gameplay = Child("Gameplay", root.transform);
            var narrative = Child("Narrative", root.transform);
            var spawn = Child("PlayerSpawn", gameplay.transform);
            spawn.transform.position = new Vector3(0f, 0.12f, 1f);
            var checkpoints = Child("Checkpoints", gameplay.transform);
            var resets = Child("ResetVolumes", gameplay.transform);
            var collectorObject = Child("MemoryFragmentCollector", gameplay.transform);
            var collector = collectorObject.AddComponent<MemoryFragmentCollector>();

            BuildEnvironment(entrance.transform, archiveHall.transform, lockedPassage.transform,
                memoryRoom.transform, exit.transform, stone, hint);
            BuildLighting(lighting.transform);

            var subtitles = BuildSubtitleCanvas(root.transform);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null)
                throw new FileNotFoundException("Build the existing Milestone 2 player prefab before ForgetPrototype.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            var reader = player.GetComponent<PlayerInputReader>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay.transform,
                player.transform.Find("CameraTarget"), reader);
            player.GetComponent<PlayerMovement>().Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            var lightOrigin = player.transform.Find("MemoryLightOrigin");
            var memoryLight = player.GetComponent<PlayerMemoryLight>();
            memoryLight.Configure(reader, lightOrigin, camera.transform, lightOrigin.GetComponent<Light>());
            player.GetComponent<PlayerInteractor>().Configure(reader, player.transform);
            player.AddComponent<CheckpointRespawner>();
            player.AddComponent<ForgetFailureFeedback>().Configure(memoryLight, subtitles);

            var bootstrap = gameplay.AddComponent<MemoryAbilityBootstrap>();
            bootstrap.ConfigureDevelopmentUnlocks(true, true);
            Child("ArchiveRecallFocusGroup", memoryObjects.transform).AddComponent<MemoryFocusGroup>();

            var archiveSeal = Cube("ForgetUnlock_ArchiveSeal", entrance.transform,
                new Vector3(0f, 0.55f, 3f), new Vector3(0.9f, 1.1f, 0.9f), hint);
            var unlockPromptObject = Child("UnlockPrompt", archiveSeal.transform);
            unlockPromptObject.transform.localPosition = new Vector3(0f, 1f, -0.52f);
            var unlockPrompt = unlockPromptObject.AddComponent<TextMesh>();
            unlockPrompt.text = "E - TOUCH THE ARCHIVE SEAL";
            unlockPrompt.anchor = TextAnchor.MiddleCenter;
            unlockPrompt.alignment = TextAlignment.Center;
            unlockPrompt.characterSize = 0.1f;
            unlockPrompt.fontSize = 48;
            var unlock = archiveSeal.AddComponent<MemoryAbilityUnlock>();
            unlock.Configure(MemoryAbility.Forget, subtitles, unlockPrompt);

            var teachingWall = BuildForgettable(forgetObjects.transform, "ForgottenWall",
                new Vector3(0f, 1.45f, 6f), new Vector3(9f, 2.9f, 0.8f), stone, hint,
                ForgetCategory.Obstacle, "F / RS CLICK - FORGET");
            _ = teachingWall;

            var wallSection = BuildForgettable(forgetObjects.transform, "WallSection",
                new Vector3(-3f, 1.35f, 17f), new Vector3(4f, 2.7f, 0.65f), stone, hint,
                ForgetCategory.Barrier, "F / RS CLICK - FORGET");
            var support = BuildForgettable(forgetObjects.transform, "SupportBlock",
                new Vector3(3f, 1f, 17f), new Vector3(1.1f, 2f, 1.1f), stone, hint,
                ForgetCategory.Mechanism, "F / RS CLICK - FORGET");
            var droppedBeam = Cube("Consequence_DroppedBeam", archiveHall.transform,
                new Vector3(2.5f, 1.35f, 20.2f), new Vector3(4.6f, 2.7f, 0.65f), stone);
            var consequence = archiveHall.AddComponent<ForgetConsequence>();
            consequence.Configure(support, new[] { droppedBeam.GetComponent<Renderer>() },
                new[] { droppedBeam.GetComponent<Collider>() });
            _ = wallSection;

            var stopper = BuildForgettable(forgetObjects.transform, "EchoPathStopper",
                new Vector3(0f, 1f, 34f), new Vector3(1f, 2f, 0.6f), stone, hint,
                ForgetCategory.Mechanism, "F / RS CLICK - FORGET");
            var echoMechanism = BuildEchoBridge(memoryObjects.transform, memory, ghost, stopper);

            var rubble = BuildForgettable(forgetObjects.transform, "DoorwayRubble",
                new Vector3(0f, 1.25f, 49f), new Vector3(4.6f, 2.5f, 0.9f), stone, hint,
                ForgetCategory.Obstacle, "F / RS CLICK - FORGET");
            var recallDoor = BuildRecallDoor(memoryObjects.transform, memory, ghost, hint);
            _ = rubble;
            _ = echoMechanism;

            BuildHollow(hollowRoot.transform, player.transform, camera, memoryLight, voidMaterial,
                new Vector3(0f, 0.1f, 68f), subtitles);
            BuildCover(memoryRoom.transform, stone);

            var childEcho = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            childEcho.name = "UnknownChildEcho";
            childEcho.transform.SetParent(narrative.transform, false);
            childEcho.transform.SetPositionAndRotation(new Vector3(-1.5f, 0.9f, 79f), Quaternion.identity);
            childEcho.transform.localScale = new Vector3(0.48f, 0.85f, 0.48f);
            childEcho.GetComponent<Renderer>().sharedMaterial = ghost;
            Object.DestroyImmediate(childEcho.GetComponent<Collider>());
            childEcho.SetActive(false);
            var echoTrigger = Child("ArchiveMemoryEchoTrigger", narrative.transform);
            echoTrigger.transform.position = new Vector3(0f, 0.8f, 77f);
            var echoTriggerCollider = echoTrigger.AddComponent<BoxCollider>();
            echoTriggerCollider.isTrigger = true;
            echoTriggerCollider.size = new Vector3(10f, 2f, 3f);
            echoTrigger.AddComponent<ForgottenArchiveEcho>().Configure(subtitles, childEcho, player.transform,
                fragment, collector);

            var fragmentDisplay = Cube("MemoryFragment04Display", exit.transform,
                new Vector3(0f, 1.2f, 83f), new Vector3(0.45f, 0.45f, 0.45f), memory);
            Object.DestroyImmediate(fragmentDisplay.GetComponent<Collider>());
            var fragmentLight = fragmentDisplay.AddComponent<Light>();
            fragmentLight.color = new Color(0.36f, 0.82f, 1f);
            fragmentLight.range = 4f;
            fragmentLight.intensity = 1.15f;

            MakeCheckpoint(checkpoints.transform, "Checkpoint_Entrance", new Vector3(0f, 0.12f, 1f));
            MakeCheckpoint(checkpoints.transform, "Checkpoint_Archive", new Vector3(0f, 0.12f, 26f));
            MakeCheckpoint(checkpoints.transform, "Checkpoint_MemoryRoom", new Vector3(0f, 0.12f, 46f));
            MakeCheckpoint(checkpoints.transform, "Checkpoint_HollowCorridor", new Vector3(0f, 0.12f, 57f));
            MakeCheckpoint(checkpoints.transform, "Checkpoint_Exit", new Vector3(0f, 0.12f, 76f));
            var reset = Child("ArchiveFallReset", resets.transform);
            reset.transform.position = new Vector3(0f, -3f, 42f);
            var resetCollider = reset.AddComponent<BoxCollider>();
            resetCollider.isTrigger = true;
            resetCollider.size = new Vector3(28f, 1f, 100f);
            reset.AddComponent<ResetVolume>();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.11f, 0.12f, 0.15f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssetIfDirty(fragment);
            EditorSceneManager.SetActiveScene(scene);
        }

        private static void BuildEnvironment(Transform entrance, Transform hall, Transform passage,
            Transform memoryRoom, Transform exit, Material stone, Material hint)
        {
            Cube("EntranceFloor", entrance, new Vector3(0f, -0.35f, 1f), new Vector3(14f, 0.7f, 12f), stone);
            Cube("ArchiveHallFloor", hall, new Vector3(0f, -0.35f, 18f), new Vector3(14f, 0.7f, 20f), stone);
            Cube("EchoPassageNearBank", passage, new Vector3(0f, -0.35f, 29f), new Vector3(14f, 0.7f, 6f), stone);
            Cube("EchoPassageFarBank", passage, new Vector3(0f, -0.35f, 41f), new Vector3(14f, 0.7f, 6f), stone);
            Cube("MemoryRoomFloor", memoryRoom, new Vector3(0f, -0.35f, 51f), new Vector3(14f, 0.7f, 14f), stone);
            Cube("HollowCorridorFloor", memoryRoom, new Vector3(0f, -0.35f, 66f), new Vector3(14f, 0.7f, 18f), stone);
            Cube("ArchiveExitFloor", exit, new Vector3(0f, -0.35f, 81f), new Vector3(14f, 0.7f, 12f), stone);

            Cube("EntranceCeiling", entrance, new Vector3(0f, 5f, 1f), new Vector3(14f, 0.45f, 12f), stone);
            Cube("HallCeiling", hall, new Vector3(0f, 5f, 18f), new Vector3(14f, 0.45f, 20f), stone);
            Cube("PassageCeiling", passage, new Vector3(0f, 5f, 34f), new Vector3(14f, 0.45f, 18f), stone);
            Cube("MemoryRoomCeiling", memoryRoom, new Vector3(0f, 5f, 62f), new Vector3(14f, 0.45f, 40f), stone);
            Cube("ExitCeiling", exit, new Vector3(0f, 5f, 81f), new Vector3(14f, 0.45f, 12f), stone);

            var sideRanges = new[] { (entrance, 1f, 12f), (hall, 18f, 20f), (passage, 34f, 18f),
                (memoryRoom, 62f, 40f), (exit, 81f, 12f) };
            foreach (var segment in sideRanges)
            {
                Cube("ArchiveWall_West", segment.Item1, new Vector3(-7.2f, 2.5f, segment.Item2),
                    new Vector3(0.4f, 5f, segment.Item3), stone);
                Cube("ArchiveWall_East", segment.Item1, new Vector3(7.2f, 2.5f, segment.Item2),
                    new Vector3(0.4f, 5f, segment.Item3), stone);
            }

            Cube("WallTeachingArchHeader", entrance, new Vector3(0f, 3.4f, 6f), new Vector3(9f, 0.3f, 0.85f), hint);
            Cube("HallSupportPier_West", hall, new Vector3(-5.4f, 1.45f, 17f), new Vector3(1f, 2.9f, 1f), stone);
            Cube("HallSupportPier_East", hall, new Vector3(5.4f, 1.45f, 17f), new Vector3(1f, 2.9f, 1f), stone);
            Cube("ArchiveRacks_West", exit, new Vector3(-5.1f, 1.6f, 81f), new Vector3(1.6f, 3.2f, 5.5f), stone);
            Cube("ArchiveRacks_East", exit, new Vector3(5.1f, 1.6f, 81f), new Vector3(1.6f, 3.2f, 5.5f), stone);
        }

        private static void BuildLighting(Transform parent)
        {
            var key = Child("ArchiveKeyLight", parent);
            key.transform.eulerAngles = new Vector3(42f, -32f, 0f);
            var light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.75f;
            light.color = new Color(0.65f, 0.73f, 0.9f);
            var warm = Child("ArchiveWarmPool", parent);
            warm.transform.position = new Vector3(0f, 4f, 2f);
            var spot = warm.AddComponent<Light>();
            spot.type = LightType.Point;
            spot.range = 12f;
            spot.intensity = 4.5f;
            spot.color = new Color(1f, 0.68f, 0.33f);
        }

        private static MemoryForgettable BuildForgettable(Transform parent, string name, Vector3 position,
            Vector3 size, Material stone, Material signature, ForgetCategory category, string prompt)
        {
            var target = Cube(name, parent, position, size, stone);
            var collider = target.GetComponent<Collider>();
            var renderer = target.GetComponent<Renderer>();
            var forgettable = target.AddComponent<MemoryForgettable>();
            forgettable.Configure(category, new[] { renderer }, new[] { collider });

            var glyphObject = Child("ForgetSignature", target.transform);
            glyphObject.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.35f, -size.z * 0.5f - 0.035f);
            var glyph = glyphObject.AddComponent<TextMesh>();
            glyph.text = prompt;
            glyph.anchor = TextAnchor.MiddleCenter;
            glyph.alignment = TextAlignment.Center;
            glyph.characterSize = 0.12f;
            glyph.fontSize = 48;
            glyph.color = new Color(1f, 0.78f, 0.35f);
            var glyphRenderer = glyphObject.GetComponent<MeshRenderer>();
            glyphRenderer.sharedMaterial = signature;
            forgettable.ConfigureHint(glyph);
            return forgettable;
        }

        private static MemoryComposite BuildEchoBridge(Transform parent, Material memory, Material ghost,
            MemoryForgettable stopper)
        {
            var root = Child("EchoBridgeMechanism", parent);
            var start = Child("EchoBridgeWaypoint_Start", root.transform);
            start.transform.position = new Vector3(0f, 0.22f, 31.5f);
            var destination = Child("EchoBridgeWaypoint_Destination", root.transform);
            destination.transform.position = new Vector3(0f, 0.22f, 40f);
            var path = root.AddComponent<EchoPath>();
            path.Configure(new[] { start.transform, destination.transform }, 3.8f, 0f, false);

            var platform = Cube("EchoBridgePlatform", root.transform, start.transform.position,
                new Vector3(4.8f, 0.3f, 5.5f), memory);
            var motionPreview = Child("EchoBridgeMotionPreview", root.transform);
            motionPreview.SetActive(false);
            var line = motionPreview.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start.transform.position + Vector3.up * 0.5f);
            line.SetPosition(1, destination.transform.position + Vector3.up * 0.5f);
            line.startWidth = line.endWidth = 0.1f;
            line.sharedMaterial = ghost;

            var echo = root.AddComponent<MemoryEchoable>();
            echo.Configure(path, platform.transform, motionPreview, null, platform.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureForgottenBlockers(stopper);
            var composite = root.AddComponent<MemoryComposite>();
            composite.Configure(null, echo);
            return composite;
        }

        private static MemoryComposite BuildRecallDoor(Transform parent, Material memory, Material ghost, Material hint)
        {
            var root = Child("RecallDoorAfterRubble", parent);
            var present = Child("PresentState_BrokenDoorway", root.transform);
            var broken = Cube("BrokenDoorSlab", present.transform, new Vector3(0f, 1.35f, 50f),
                new Vector3(4.4f, 2.7f, 0.55f), hint);
            var state = Child("MemoryState", root.transform);
            var preview = Child("GhostDoorwayPreview", state.transform);
            BuildDoorArch(preview.transform, new Color(0.35f, 0.9f, 1f, 0.3f), ghost, false);
            var solid = Child("MaterializedDoorway", state.transform);
            BuildDoorArch(solid.transform, Color.white, memory, true);
            var recall = root.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, state.gameObject, preview.gameObject, solid.gameObject, 16f);
            var composite = root.AddComponent<MemoryComposite>();
            composite.Configure(recall, null);
            _ = broken;
            return composite;
        }

        private static void BuildDoorArch(Transform parent, Color color, Material material, bool collidable)
        {
            var left = Cube("DoorPost_Left", parent, new Vector3(-2f, 1.4f, 50f),
                new Vector3(0.48f, 2.8f, 0.5f), material);
            var right = Cube("DoorPost_Right", parent, new Vector3(2f, 1.4f, 50f),
                new Vector3(0.48f, 2.8f, 0.5f), material);
            var header = Cube("DoorHeader", parent, new Vector3(0f, 2.75f, 50f),
                new Vector3(4.45f, 0.5f, 0.55f), material);
            foreach (var piece in new[] { left, right, header })
            {
                if (!collidable)
                    Object.DestroyImmediate(piece.GetComponent<Collider>());
                var tint = piece.GetComponent<Renderer>().sharedMaterial;
                _ = tint;
            }
            _ = color;
        }

        private static void BuildHollow(Transform parent, Transform player, Camera camera,
            PlayerMemoryLight memoryLight, Material material, Vector3 position, SubtitlePresenter subtitles)
        {
            var hollow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            hollow.name = "TheHollow";
            hollow.transform.SetParent(parent, false);
            hollow.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            var primitiveCollider = hollow.GetComponent<CapsuleCollider>();
            Object.DestroyImmediate(primitiveCollider);
            var controller = hollow.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.38f;
            controller.center = Vector3.up;
            hollow.GetComponent<Renderer>().sharedMaterial = material;
            var threat = hollow.AddComponent<HollowController>();
            threat.Configure(player, camera, memoryLight, null, null, null, null,
                hollow.GetComponentsInChildren<Renderer>(true), null);
            var _ = subtitles;
        }

        private static void BuildCover(Transform parent, Material stone)
        {
            Cube("SafeCover_West", parent, new Vector3(-3.5f, 1.25f, 62f), new Vector3(1.2f, 2.5f, 1.1f), stone);
            Cube("SafeCover_East", parent, new Vector3(3.5f, 1.25f, 70f), new Vector3(1.2f, 2.5f, 1.1f), stone);
            Cube("ArchiveShelving_01", parent, new Vector3(-5.3f, 1.65f, 64f), new Vector3(1.2f, 3.3f, 4f), stone);
            Cube("ArchiveShelving_02", parent, new Vector3(5.3f, 1.65f, 69f), new Vector3(1.2f, 3.3f, 4f), stone);
        }

        private static SubtitlePresenter BuildSubtitleCanvas(Transform parent)
        {
            var canvasObject = new GameObject("ArchiveSubtitles", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvasObject.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.04f);
            rect.anchorMax = new Vector2(0.9f, 0.22f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 0.94f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            var presenter = canvasObject.AddComponent<SubtitlePresenter>();
            presenter.Configure(group, text);
            return presenter;
        }

        private static void MakeCheckpoint(Transform parent, string name, Vector3 position)
        {
            var point = Child(name + "_Spawn", parent);
            point.transform.position = position;
            var checkpoint = Child(name, parent);
            checkpoint.transform.position = position + Vector3.forward;
            var trigger = checkpoint.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(12f, 3f, 2.5f);
            checkpoint.AddComponent<Checkpoint>().Configure(point.transform);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent,
                position, Vector3.zero, size);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var child = new GameObject(name);
            if (parent != null)
                child.transform.SetParent(parent, false);
            return child;
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                throw new FileNotFoundException($"Required prototype material not found: {path}");
            return material;
        }

        private static MemoryFragmentDefinition GetOrCreateFragment()
        {
            var fragment = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(FragmentPath);
            if (fragment == null)
            {
                fragment = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
                AssetDatabase.CreateAsset(fragment, FragmentPath);
            }
            fragment.Configure("memory_archive_04", "Not Here", "If I couldn't see her, did I decide she wasn't there?");
            EditorUtility.SetDirty(fragment);
            return fragment;
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

        private static void AddToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
