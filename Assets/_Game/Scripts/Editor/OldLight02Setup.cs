using System.IO;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using LightRemembers.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LightRemembers.Editor
{
    /// <summary>Editor-API graybox recipe for the Recall + Echo workshop milestone.</summary>
    public static class OldLight02Setup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/OldLight02.unity";
        private const string StonePath = "Assets/_Game/Art/Materials/OldLight02Stone.mat";
        private const string MemoryPath = "Assets/_Game/Art/Materials/OldLight02Memory.mat";
        private const string GhostPath = "Assets/_Game/Art/Materials/OldLight02Ghost.mat";
        private const string HintPath = "Assets/_Game/Art/Materials/OldLight02Hint.mat";
        private const string DialoguePath = "Assets/_Game/Art/Dialogue/OldLight02Echo.asset";
        private const string FragmentPath = "Assets/_Game/Art/MemoryFragments/memory_workshop_02.asset";

        [MenuItem("Light Remembers/Prototype/Build Milestone 6 Old Light Workshop")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Scenes/Prototype");
            EnsureFolder("Assets/_Game/Art/Materials");
            EnsureFolder("Assets/_Game/Art/Dialogue");
            EnsureFolder("Assets/_Game/Art/MemoryFragments");
            var stone = MaterialAsset(StonePath, new Color(0.2f, 0.22f, 0.24f), Color.black, false);
            var memory = MaterialAsset(MemoryPath, new Color(0.54f, 0.82f, 0.88f), new Color(0.24f, 0.78f, 1.2f), false);
            var ghost = MaterialAsset(GhostPath, new Color(0.3f, 0.86f, 0.94f, 0.22f), new Color(0.2f, 0.9f, 1.3f), true);
            var hint = MaterialAsset(HintPath, new Color(1f, 0.72f, 0.12f), new Color(0.35f, 0.18f, 0.015f), false);
            var dialogue = DialogueAsset();
            var fragment = FragmentAsset();

            var scene = EditorAutomation.OpenOrCreateScene(ScenePath);
            ClearScene(scene);
            var root = EditorAutomation.CreateGameObject("OldLight02", null, Vector3.zero, Vector3.zero, Vector3.one);
            var environment = Child("Environment", root.transform);
            var entrance = Child("Entrance", environment);
            var teaching = Child("TeachingRoom", environment);
            var craneRoom = Child("CraneRoom", environment);
            var mechanismRoom = Child("MechanismRoom", environment);
            var finalRoom = Child("FinalChamber", environment);
            var puzzles = Child("Puzzles", root.transform);
            var memoryRoot = Child("Memory", root.transform);
            var lighting = Child("Lighting", root.transform);
            var gameplay = Child("Gameplay", root.transform);
            var checkpoints = Child("Checkpoints", gameplay);
            var resets = Child("ResetVolumes", gameplay);
            Child("Exit", root.transform);
            BuildLighting(lighting);
            BuildRoomShell(entrance, -1f, 8f, 10f, stone);
            BuildRoomShell(teaching, 12.5f, 30f, 9f, stone);
            BuildRoomShell(craneRoom, 39f, 23f, 15f, stone);
            BuildRoomShell(mechanismRoom, 69f, 38f, 17f, stone);
            BuildRoomShell(finalRoom, 112f, 22f, 17f, stone);
            Cube("EntranceFloor", entrance, new Vector3(0f, -0.35f, -1f), new Vector3(10f, 0.7f, 8f), stone);
            Cube("TeachingToCraneConnector", environment, new Vector3(0f, -0.1f, 27f), new Vector3(24f, 0.4f, 4f), stone);
            Cube("CraneToMechanismConnector", environment, new Vector3(0f, -0.1f, 50.75f), new Vector3(24f, 0.4f, 3.5f), stone);
            Cube("MechanismToFinalStair_01", environment, new Vector3(0f, 2.55f, 93f), new Vector3(5f, 0.5f, 2.2f), stone);
            Cube("MechanismToFinalStair_02", environment, new Vector3(0f, 1.8f, 94.7f), new Vector3(5f, 0.5f, 2.2f), stone);
            Cube("MechanismToFinalStair_03", environment, new Vector3(0f, 1.05f, 96.4f), new Vector3(5f, 0.5f, 2.2f), stone);

            var spawn = Child("PlayerSpawn", gameplay);
            spawn.position = new Vector3(0f, 0.1f, -3f);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null)
                throw new FileNotFoundException("Build the existing Milestone 2 player prefab before OldLight02.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var reader = player.GetComponent<PlayerInputReader>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay, player.transform.Find("CameraTarget"), reader);
            player.GetComponent<PlayerMovement>().Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            var lightOrigin = player.transform.Find("MemoryLightOrigin");
            player.GetComponent<PlayerMemoryLight>().Configure(reader, lightOrigin, camera.transform, lightOrigin.GetComponent<Light>());
            player.AddComponent<CheckpointRespawner>();
            if (gameplay.GetComponent<MemoryAbilityBootstrap>() == null)
                gameplay.gameObject.AddComponent<MemoryAbilityBootstrap>();
            var focus = Child("RecallCapacityFocusGroup", memoryRoot).gameObject.AddComponent<MemoryFocusGroup>();
            var subtitles = BuildSubtitleCanvas(root.transform);
            var collector = Child("MemoryFragmentCollector", gameplay).gameObject.AddComponent<MemoryFragmentCollector>();

            MakeCheckpoint(checkpoints, "Checkpoint_Teaching", new Vector3(0f, 0.1f, 4f));
            BuildTeachingPuzzle(teaching, puzzles, stone, memory, ghost, hint, focus, resets);
            MakeCheckpoint(checkpoints, "Checkpoint_Crane", new Vector3(0f, 0.1f, 29f));
            BuildCranePuzzle(craneRoom, puzzles, stone, memory, ghost, hint, focus, resets);
            BuildWorkshopLatch(craneRoom, stone, hint, subtitles);
            MakeCheckpoint(checkpoints, "Checkpoint_Mechanisms", new Vector3(0f, 0.1f, 53f));
            BuildMechanismPuzzle(mechanismRoom, puzzles, stone, memory, ghost, hint, focus, resets);
            var narrative = BuildFinalMemory(finalRoom, gameplay, puzzles, stone, memory, ghost, hint, dialogue, fragment, collector, subtitles);
            BuildDualRecallDemo(finalRoom, puzzles, stone, memory, ghost, hint, focus);
            BuildExit(finalRoom, stone, narrative.Sequence, subtitles);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.075f, 0.09f, 0.12f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild(scene.path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildTeachingPuzzle(Transform room, Transform puzzles, Material stone, Material memory,
            Material ghost, Material hint, MemoryFocusGroup focus, Transform resets)
        {
            Cube("Approach", room, new Vector3(0f, 0f, 4f), new Vector3(7f, 0.6f, 8f), stone);
            Cube("FarSide", room, new Vector3(0f, 0f, 21f), new Vector3(7f, 0.6f, 9f), stone);
            var root = Child("BrokenMemoryPlatform", puzzles);
            var recall = BuildRecall(root, "TeachingPlatform", new Vector3(0f, 0f, 12.5f),
                new Vector3(3.4f, 0.2f, 3.4f), stone, memory, ghost, hint, focus, 11f);
            BuildMotion(recall.Root, "TeachingPlatformMotion", recall.Materialized.transform, new[]
            {
                new Pose(new Vector3(0f, 0.5f, 8.2f), Quaternion.identity),
                new Pose(new Vector3(0f, 0.5f, 12.5f), Quaternion.identity),
                new Pose(new Vector3(0f, 0.5f, 17.2f), Quaternion.identity)
            }, 6.5f, ghost, recall.Component, true);
            Hazard(resets, "TeachingFallReset", new Vector3(0f, -2f, 12.5f), new Vector3(14f, 1f, 21f));
        }

        private static void BuildCranePuzzle(Transform room, Transform puzzles, Material stone, Material memory,
            Material ghost, Material hint, MemoryFocusGroup focus, Transform resets)
        {
            // Keep both banks aligned with the crane room. These used to be authored at z=0,
            // leaving the crane at z=39 with no reachable ledges despite the visible geometry.
            Cube("CraneApproach", room, new Vector3(-6f, 0f, 39f), new Vector3(8f, 0.6f, 20f), stone);
            Cube("CraneLanding", room, new Vector3(6f, 0f, 39f), new Vector3(8f, 0.6f, 20f), stone);
            var crane = BuildRecall(puzzles, "BrokenWorkshopCrane", new Vector3(0f, 0f, 39f),
                new Vector3(0.8f, 2.3f, 0.8f), stone, memory, ghost, hint, focus, 16f);
            Cube("CraneBrokenMast", crane.Present.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.55f, 2.5f, 0.55f), stone);
            CreateCrack(crane.Present.transform, new Vector3(0.3f, 1.2f, -0.31f), hint);
            var boom = Child("RememberedCraneBoom", crane.Materialized.transform);
            Cube("CraneMast", boom, new Vector3(0f, 1.3f, 0f), new Vector3(0.5f, 2.6f, 0.5f), memory);
            Cube("CraneArm", boom, new Vector3(2.8f, 2.5f, 0f), new Vector3(6f, 0.38f, 0.42f), memory);
            var deck = Cube("SuspendedMemoryWalkway", boom, new Vector3(5.7f, 0.1f, 0f), new Vector3(3.8f, 0.22f, 3.2f), memory);
            var ride = AddRideSurface(deck, new Vector3(4f, 2.1f, 3.4f));
            var path = BuildPath(puzzles, "BrokenWorkshopCrane_EchoPath", new[]
            {
                new Pose(new Vector3(-9.6f, 0f, 39f), Quaternion.Euler(0f, -12f, 0f)),
                new Pose(new Vector3(-4.8f, 0f, 39f), Quaternion.identity),
                new Pose(new Vector3(-0.1f, 0f, 39f), Quaternion.Euler(0f, 12f, 0f))
            }, 7f, ghost);
            var echo = crane.Root.gameObject.AddComponent<MemoryEchoable>();
            boom.SetPositionAndRotation(path.Path.Waypoints[0].position, path.Path.Waypoints[0].rotation);
            echo.Configure(path.Path, boom, path.Preview, ride, boom.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureRecallRequirement(crane.Component);
            var composite = crane.Root.gameObject.AddComponent<MemoryComposite>();
            composite.Configure(crane.Component, echo);
            crane.Component.ConfigureFocusGroup(focus);
            crane.Component.RefreshStateObjects();
            Hazard(resets, "CraneFallReset", new Vector3(0f, -2f, 39f), new Vector3(24f, 1f, 24f));
        }

        private static void BuildMechanismPuzzle(Transform room, Transform puzzles, Material stone, Material memory,
            Material ghost, Material hint, MemoryFocusGroup focus, Transform resets)
        {
            Cube("MechanismApproach", room, new Vector3(0f, 0f, 56f), new Vector3(9f, 0.6f, 7f), stone);
            Cube("MechanismControlSide", room, new Vector3(0f, 0f, 68f), new Vector3(9f, 0.6f, 10f), stone);
            Cube("UpperMechanismLanding", room, new Vector3(0f, 3f, 80f), new Vector3(9f, 0.6f, 8f), stone);
            Cube("DriveWheelLanding", room, new Vector3(0f, 3f, 91f), new Vector3(9f, 0.6f, 9f), stone);

            var bridge = BuildRecall(puzzles, "BrokenGearBridge", new Vector3(0f, 0f, 62f),
                new Vector3(3.4f, 0.2f, 5.2f), stone, memory, ghost, hint, focus, 15f);
            var liftRoot = Child("CounterweightLift", puzzles);
            liftRoot.position = new Vector3(0f, 0.4f, 73.8f);
            Cube("CounterweightLift_RuinedMark", liftRoot, new Vector3(0f, -0.45f, 0f), new Vector3(0.5f, 1.3f, 0.5f), stone);
            var lift = Cube("CounterweightLiftPlatform", liftRoot, Vector3.zero, new Vector3(3.4f, 0.28f, 3.4f), memory);
            var path = BuildPath(puzzles, "CounterweightLift_EchoPath", new[]
            {
                new Pose(new Vector3(0f, 0.4f, 73.8f), Quaternion.identity),
                new Pose(new Vector3(0f, 3.25f, 73.8f), Quaternion.identity)
            }, 4.5f, ghost);
            var echo = liftRoot.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path.Path, liftRoot, path.Preview, AddRideSurface(lift, new Vector3(3.6f, 2f, 3.6f)), liftRoot.GetComponentsInChildren<Renderer>(true));
            var composite = liftRoot.gameObject.AddComponent<MemoryComposite>();
            composite.Configure(null, echo);

            var wheel = BuildRecall(puzzles, "LighthouseDriveWheel", new Vector3(0f, 3f, 86f),
                new Vector3(5.7f, 0.22f, 1.4f), stone, memory, ghost, hint, focus, 14f);
            var wheelPath = BuildPath(puzzles, "LighthouseDriveWheel_EchoPath", new[]
            {
                new Pose(new Vector3(0f, 3.5f, 86f), Quaternion.Euler(0f, 90f, 0f)),
                new Pose(new Vector3(0f, 3.5f, 86f), Quaternion.identity)
            }, 3.8f, ghost);
            var wheelEcho = wheel.Root.gameObject.AddComponent<MemoryEchoable>();
            var wheelSurface = AddRideSurface(wheel.Materialized, new Vector3(6f, 1.8f, 1.7f));
            wheel.Materialized.transform.SetPositionAndRotation(wheelPath.Path.Waypoints[0].position, wheelPath.Path.Waypoints[0].rotation);
            wheelEcho.Configure(wheelPath.Path, wheel.Materialized.transform, wheelPath.Preview, wheelSurface,
                wheel.Materialized.GetComponentsInChildren<Renderer>(true));
            wheelEcho.ConfigureRecallRequirement(wheel.Component);
            wheel.Root.gameObject.AddComponent<MemoryComposite>().Configure(wheel.Component, wheelEcho);
            wheel.Component.RefreshStateObjects();
            Hazard(resets, "MechanismFallReset", new Vector3(0f, -2f, 74f), new Vector3(16f, 1f, 38f));
            _ = bridge;
        }

        private static NarrativeParts BuildFinalMemory(Transform room, Transform gameplay, Transform puzzles, Material stone,
            Material memory, Material ghost, Material hint, DialogueSequence dialogue, MemoryFragmentDefinition fragment,
            MemoryFragmentCollector collector, SubtitlePresenter subtitles)
        {
            Cube("FinalApproach", room, new Vector3(0f, 0f, 99f), new Vector3(10f, 0.6f, 9f), stone);
            Cube("FinalChamberFloor", room, new Vector3(0f, 0f, 111f), new Vector3(12f, 0.6f, 17f), stone);
            Cube("PostMemoryPath", room, new Vector3(0f, 0f, 127f), new Vector3(9f, 0.6f, 11f), stone);
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "SmallWorkshopMechanism";
            lamp.transform.SetParent(room, false);
            lamp.transform.localPosition = new Vector3(0f, 1.2f, 112f);
            lamp.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            lamp.GetComponent<Renderer>().sharedMaterial = hint;
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            var child = Figure(room, "ChildProtagonist_Echo", new Vector3(-1.3f, 0.3f, 110.3f), memory, 0.8f);
            var grandfather = Figure(room, "Grandfather_Echo", new Vector3(0f, 0.3f, 110.7f), memory, 1f);
            var unknown = Figure(room, "UnknownChild_Silhouette", new Vector3(1.3f, 0.3f, 110.4f), ghost, 0.82f);
            var sequence = Child("MemoryEchoSequence", gameplay).gameObject.AddComponent<MemoryEchoSequence>();
            var collectorObject = collector;
            var rewardObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rewardObject.name = "MemoryAnchorReward";
            rewardObject.transform.SetParent(puzzles, false);
            rewardObject.transform.position = new Vector3(0f, 1f, 114f);
            rewardObject.transform.localScale = Vector3.one * 0.55f;
            rewardObject.GetComponent<Renderer>().sharedMaterial = memory;
            rewardObject.GetComponent<Collider>().isTrigger = true;
            var reward = rewardObject.AddComponent<MemoryAnchorReward>();
            reward.Configure(fragment, collectorObject, subtitles);
            sequence.Configure(dialogue, subtitles, child, grandfather, unknown, null, fragment, collectorObject, null);
            sequence.ConfigurePostMemoryMechanism(lamp.transform, new Vector3(0f, 0f, 34f), 0.8f);
            sequence.ConfigureCompletionReward(reward);
            var trigger = Child("FinalMemoryLever", room);
            trigger.localPosition = new Vector3(-3f, 0.8f, 111f);
            var interaction = trigger.gameObject.AddComponent<BoxCollider>();
            interaction.isTrigger = true;
            interaction.size = new Vector3(1.7f, 2f, 1.7f);
            var handle = Cube("LeverHandle", trigger, new Vector3(0f, 0.55f, 0.2f), new Vector3(0.16f, 0.18f, 0.85f), hint).transform;
            trigger.gameObject.AddComponent<AncientLever>().Configure(sequence, handle);
            MakeCheckpoint(gameplay, "Checkpoint_FinalChamber", new Vector3(0f, 0.1f, 101f));
            Hazard(gameplay, "FinalFallReset", new Vector3(0f, -2f, 112f), new Vector3(18f, 1f, 35f));
            return new NarrativeParts(sequence);
        }

        private static void BuildDualRecallDemo(Transform room, Transform puzzles, Material stone, Material memory,
            Material ghost, Material hint, MemoryFocusGroup focus)
        {
            Cube("DualRecallApproach", room, new Vector3(0f, 0f, 129f), new Vector3(9f, 0.6f, 6f), stone);
            Cube("DualRecallFarSide", room, new Vector3(0f, 0f, 147f), new Vector3(9f, 0.6f, 8f), stone);
            var first = BuildRecall(puzzles, "DualBridge_LeftHalf", new Vector3(-1.6f, 0f, 137f),
                new Vector3(3.2f, 0.22f, 11.5f), stone, memory, ghost, hint, focus, 20f);
            var second = BuildRecall(puzzles, "DualBridge_RightHalf", new Vector3(1.6f, 0f, 137f),
                new Vector3(3.2f, 0.22f, 11.5f), stone, memory, ghost, hint, focus, 20f);
            var barrier = Cube("MemoryAnchorGate", room, new Vector3(0f, 1.4f, 142.5f), new Vector3(5f, 2.8f, 0.4f), stone);
            var gate = Child("DualRecallGate", puzzles).gameObject.AddComponent<DualRecallGate>();
            gate.Configure(first.Component, second.Component, barrier);
            var signA = Cube("LeftBridgeTarget", first.Present.transform, new Vector3(0f, 1f, 0f), new Vector3(0.55f, 1.8f, 0.5f), stone);
            CreateCrack(first.Present.transform, new Vector3(0.29f, 0.8f, -0.15f), hint);
            var signB = Cube("RightBridgeTarget", second.Present.transform, new Vector3(0f, 1f, 0f), new Vector3(0.55f, 1.8f, 0.5f), stone);
            CreateCrack(second.Present.transform, new Vector3(-0.29f, 0.8f, -0.15f), hint);
            _ = signA;
            _ = signB;
        }

        private static void BuildWorkshopLatch(Transform room, Material stone, Material hint, SubtitlePresenter subtitles)
        {
            var door = Cube("WorkshopShortcutBarrier", room, new Vector3(0f, 1.4f, 49.5f), new Vector3(24f, 2.8f, 0.35f), stone);
            var latch = Child("WorkshopLatch", room);
            latch.localPosition = new Vector3(3f, 0.4f, 45.5f);
            latch.gameObject.AddComponent<BoxCollider>().isTrigger = true;
            Cube("LatchHandle", latch, Vector3.up * 0.7f, new Vector3(0.16f, 0.16f, 0.75f), hint);
            latch.gameObject.AddComponent<WorkshopLatch>().Configure(door, subtitles);
        }

        private static void BuildExit(Transform room, Material stone, MemoryEchoSequence sequence, SubtitlePresenter subtitles)
        {
            var barrier = Cube("OldLight02ExitBarrier", room, new Vector3(0f, 1.35f, 151f), new Vector3(4f, 2.7f, 0.4f), stone);
            var exit = Child("OldLight02Exit", room);
            exit.localPosition = new Vector3(0f, 0f, 150f);
            var trigger = exit.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1f, 1.3f);
            trigger.size = new Vector3(5f, 2.5f, 3f);
            var unlocker = exit.gameObject.AddComponent<ExitUnlocker>();
            unlocker.Configure(barrier, LighthouseApproachSetup.SceneName, subtitles);
            if (sequence != null)
            {
                sequence.ConfigureExitUnlocker(unlocker);
                sequence.ConfigurePresenter(subtitles);
            }
        }

        private static RecallParts BuildRecall(Transform parent, string name, Vector3 position, Vector3 platformSize,
            Material stone, Material memory, Material ghost, Material hint, MemoryFocusGroup focus, float duration)
        {
            var root = Child(name, parent);
            root.position = position;
            var present = Child("PresentState_Ruins", root);
            var target = Cube("MemoryTarget_YellowCrack", present, new Vector3(0f, 0.8f, -1.7f), new Vector3(0.58f, 1.7f, 0.5f), stone);
            CreateCrack(present, new Vector3(0.3f, 0.35f, -1.96f), hint);
            Cube("PresentDebris_Left", present, new Vector3(-1f, 0.15f, 0.8f), new Vector3(1.4f, 0.3f, 1f), stone);
            Cube("PresentDebris_Right", present, new Vector3(1f, 0.1f, 1.6f), new Vector3(1.1f, 0.2f, 0.8f), stone);
            var memoryRoot = Child("MemoryState", root);
            var preview = Child("GhostPreview", memoryRoot);
            var ghostForm = Cube("GhostRememberedForm", preview, Vector3.zero, platformSize, ghost);
            Object.DestroyImmediate(ghostForm.GetComponent<Collider>());
            var materialized = Child("MaterializedMemory", memoryRoot);
            var platform = Cube("RememberedForm", materialized, Vector3.zero, platformSize, memory);
            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryRoot.gameObject, preview.gameObject, materialized.gameObject, duration);
            recall.ConfigureFocusGroup(focus);
            var parts = new RecallParts(root, present.gameObject, preview.gameObject, materialized.gameObject, recall, target);
            return parts;
        }

        private static void BuildMotion(Transform root, string pathName, Transform mover, Pose[] poses, float duration,
            Material ghost, MemoryRecallable recall, bool requiresRecall)
        {
            var path = BuildPath(root.parent, pathName, poses, duration, ghost);
            var rememberedCollider = mover.GetComponentInChildren<BoxCollider>(true);
            var rideSize = rememberedCollider != null
                ? rememberedCollider.size + new Vector3(0.3f, 1.8f, 0.3f)
                : new Vector3(3.5f, 2f, 3.5f);
            var ride = AddRideSurface(mover.gameObject, rideSize);
            mover.SetPositionAndRotation(path.Path.Waypoints[0].position, path.Path.Waypoints[0].rotation);
            var echo = root.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path.Path, mover, path.Preview, ride, mover.GetComponentsInChildren<Renderer>(true));
            if (requiresRecall)
                echo.ConfigureRecallRequirement(recall);
            root.gameObject.AddComponent<MemoryComposite>().Configure(recall, echo);
            recall.RefreshStateObjects();
        }

        private static (EchoPath Path, GameObject Preview) BuildPath(Transform parent, string name, Pose[] poses,
            float duration, Material ghost)
        {
            var pathRoot = Child(name, parent);
            var waypoints = new Transform[poses.Length];
            for (var i = 0; i < poses.Length; i++)
            {
                var point = Child($"Waypoint_{i + 1:00}", pathRoot);
                point.SetPositionAndRotation(poses[i].Position, poses[i].Rotation);
                waypoints[i] = point;
            }
            var path = pathRoot.gameObject.AddComponent<EchoPath>();
            path.Configure(waypoints, duration, 1.5f, true);
            var preview = Child($"{name}_MotionPreview", parent);
            preview.gameObject.SetActive(false);
            var line = Child("RememberedMotionLine", preview).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = poses.Length;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.sharedMaterial = ghost;
            line.numCapVertices = 3;
            for (var i = 0; i < poses.Length; i++)
            {
                line.SetPosition(i, poses[i].Position + Vector3.up * 0.55f);
                var marker = EditorAutomation.CreatePrimitive(PrimitiveType.Sphere, $"MotionGlow_{i + 1:00}", preview,
                    poses[i].Position + Vector3.up * 0.55f, Vector3.zero, Vector3.one * 0.25f);
                marker.GetComponent<Renderer>().sharedMaterial = ghost;
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
            return (path, preview.gameObject);
        }

        private static EchoRideSurface AddRideSurface(GameObject target, Vector3 size)
        {
            var rideObject = Child("EchoRideSurface", target.transform).gameObject;
            var trigger = rideObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = size;
            trigger.center = Vector3.up * Mathf.Max(0.5f, size.y * 0.42f);
            var ride = rideObject.AddComponent<EchoRideSurface>();
            ride.Configure(trigger);
            return ride;
        }

        private static void BuildRoomShell(Transform room, float centerZ, float length, float width, Material stone)
        {
            var sideOffset = Mathf.Max(width * 0.5f, 12f);
            Cube($"{room.name}_WestWall", room, new Vector3(-sideOffset, 3.2f, centerZ), new Vector3(0.35f, 6f, length), stone);
            Cube($"{room.name}_EastWall", room, new Vector3(sideOffset, 3.2f, centerZ), new Vector3(0.35f, 6f, length), stone);
            for (var i = -1; i <= 1; i += 2)
                Cube($"{room.name}_TimberBeam_{i}", room, new Vector3(i * (width * 0.5f - 0.5f), 5.2f, centerZ), new Vector3(0.45f, 0.45f, length), stone);
        }

        private static void MakeCheckpoint(Transform parent, string name, Vector3 position)
        {
            var point = Child($"{name}_Spawn", parent);
            point.position = position;
            var checkpoint = Child(name, parent);
            checkpoint.position = position + Vector3.forward;
            var trigger = checkpoint.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(6f, 2.5f, 1.5f);
            checkpoint.gameObject.AddComponent<Checkpoint>().Configure(point);
        }

        private static void Hazard(Transform parent, string name, Vector3 position, Vector3 size)
        {
            var zone = Child(name, parent);
            zone.position = position;
            var collider = zone.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            zone.gameObject.AddComponent<ResetVolume>();
        }

        private static SubtitlePresenter BuildSubtitleCanvas(Transform root)
        {
            var canvasObject = new GameObject("SubtitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var panel = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SubtitlePresenter));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.14f, 0.035f);
            panelRect.anchorMax = new Vector2(0.86f, 0.21f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.035f, 0.05f, 0.9f);
            panel.GetComponent<Image>().raycastTarget = false;
            var textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.08f);
            rect.anchorMax = new Vector2(0.96f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 27;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.97f, 0.95f, 0.87f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var presenter = panel.GetComponent<SubtitlePresenter>();
            presenter.Configure(panel.GetComponent<CanvasGroup>(), text);
            return presenter;
        }

        private static GameObject Figure(Transform parent, string name, Vector3 position, Material material, float height)
        {
            var figure = Child(name, parent);
            figure.position = position;
            var body = EditorAutomation.CreatePrimitive(PrimitiveType.Capsule, "SilhouetteBody", figure,
                new Vector3(0f, height * 0.5f, 0f), Vector3.zero, new Vector3(0.42f, height * 0.5f, 0.34f));
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            figure.gameObject.SetActive(false);
            return figure.gameObject;
        }

        private static void CreateCrack(Transform parent, Vector3 origin, Material hint)
        {
            var offsets = new[] { Vector3.zero, new Vector3(0.16f, 0.2f, 0f), new Vector3(-0.04f, 0.42f, 0f),
                new Vector3(0.14f, 0.63f, 0f), new Vector3(-0.06f, 0.84f, 0f) };
            for (var i = 0; i < offsets.Length - 1; i++)
            {
                var a = origin + offsets[i];
                var b = origin + offsets[i + 1];
                var delta = b - a;
                var segment = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, $"YellowMemoryCrack_{i}", parent,
                    (a + b) * 0.5f, Quaternion.LookRotation(delta, Vector3.up).eulerAngles,
                    new Vector3(0.055f, 0.025f, delta.magnitude));
                segment.GetComponent<Renderer>().sharedMaterial = hint;
                Object.DestroyImmediate(segment.GetComponent<Collider>());
            }
        }

        private static void BuildLighting(Transform parent)
        {
            var lightObject = Child("WorkshopKeyLight", parent);
            lightObject.localEulerAngles = new Vector3(46f, -25f, 0f);
            var light = lightObject.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.65f;
            light.color = new Color(0.62f, 0.71f, 0.88f);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent, position, Vector3.zero, scale);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static Transform Child(string name, Transform parent) =>
            EditorAutomation.CreateGameObject(name, parent, Vector3.zero, Vector3.zero, Vector3.one).transform;

        private static void ClearScene(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
                Object.DestroyImmediate(gameObject);
        }

        private static DialogueSequence DialogueAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialoguePath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<DialogueSequence>();
            var serialized = new SerializedObject(asset);
            var lines = serialized.FindProperty("lines");
            var data = new[]
            {
                new DialogueLine("Grandfather", "One holds it steady.", 2.2f),
                new DialogueLine("Grandfather", "The other makes it move.", 2.4f),
                new DialogueLine("UnknownChild", "That's not fair. He always gets to move it.", 3.2f),
                new DialogueLine("Child", "Because you always let go.", 2.8f),
                new DialogueLine("UnknownChild", "I do not.", 2.1f),
                new DialogueLine("Grandfather", "(laughs)", 1.4f),
                new DialogueLine("UnknownChild", "You forgot my turn.", 3.5f)
            };
            lines.arraySize = data.Length;
            for (var i = 0; i < data.Length; i++)
            {
                var item = lines.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("speaker").stringValue = data[i].speaker;
                item.FindPropertyRelative("text").stringValue = data[i].text;
                item.FindPropertyRelative("duration").floatValue = data[i].duration;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, DialoguePath);
            return asset;
        }

        private static MemoryFragmentDefinition FragmentAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(FragmentPath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            asset.Configure("memory_workshop_02", "Her Turn", "She was always there. Why can't I remember her face?");
            AssetDatabase.CreateAsset(asset, FragmentPath);
            return asset;
        }

        private static Material MaterialAsset(string path, Color color, Color emission, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidDataException("URP/Lit is unavailable.");
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetColor("_EmissionColor", emission);
            material.EnableKeyword("_EMISSION");
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetShaderPassEnabled("ShadowCaster", false);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddToBuild(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(item => item.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private readonly struct Pose
        {
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public Pose(Vector3 position, Quaternion rotation) { Position = position; Rotation = rotation; }
        }

        private readonly struct RecallParts
        {
            public readonly Transform Root;
            public readonly GameObject Present;
            public readonly GameObject Preview;
            public readonly GameObject Materialized;
            public readonly MemoryRecallable Component;
            public readonly GameObject Target;
            public RecallParts(Transform root, GameObject present, GameObject preview, GameObject materialized,
                MemoryRecallable component, GameObject target)
            { Root = root; Present = present; Preview = preview; Materialized = materialized; Component = component; Target = target; }
        }

        private readonly struct NarrativeParts
        {
            public readonly MemoryEchoSequence Sequence;
            public NarrativeParts(MemoryEchoSequence sequence) { Sequence = sequence; }
        }
    }
}
