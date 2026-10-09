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
    /// <summary>Editor-API graybox recipe for the first Hollow encounter.</summary>
    public static class HollowEncounterSetup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/HollowEncounter01.unity";
        private const string StonePath = "Assets/_Game/Art/Materials/HollowEncounterStone.mat";
        private const string MemoryPath = "Assets/_Game/Art/Materials/HollowEncounterMemory.mat";
        private const string GhostPath = "Assets/_Game/Art/Materials/HollowEncounterGhost.mat";
        private const string HintPath = "Assets/_Game/Art/Materials/HollowEncounterHint.mat";
        private const string VoidPath = "Assets/_Game/Art/Materials/HollowEncounterVoid.mat";
        private const string SanctuaryPath = "Assets/_Game/Art/Materials/HollowEncounterSanctuary.mat";
        private const string DialoguePath = "Assets/_Game/Art/Dialogue/HollowEncounter01.asset";
        private const string FragmentPath = "Assets/_Game/Art/MemoryFragments/memory_fear_03.asset";

        [MenuItem("Light Remembers/Prototype/Build Milestone 7 Hollow Encounter")]
        public static void Build()
        {
            EnsureFolder("Assets/_Game/Scenes/Prototype");
            EnsureFolder("Assets/_Game/Art/Materials");
            EnsureFolder("Assets/_Game/Art/Dialogue");
            EnsureFolder("Assets/_Game/Art/MemoryFragments");

            var stone = CreateMaterial(StonePath, new Color(0.22f, 0.24f, 0.27f), Color.black);
            var memory = CreateMaterial(MemoryPath, new Color(0.5f, 0.78f, 0.84f), new Color(0.15f, 0.55f, 0.72f));
            var ghost = CreateMaterial(GhostPath, new Color(0.25f, 0.72f, 0.85f, 0.24f), new Color(0.1f, 0.62f, 0.9f), true);
            var hint = CreateMaterial(HintPath, new Color(1f, 0.7f, 0.14f), new Color(0.32f, 0.13f, 0.01f));
            var voidMaterial = CreateMaterial(VoidPath, new Color(0.018f, 0.014f, 0.028f), new Color(0.08f, 0.025f, 0.14f));
            var sanctuaryMaterial = CreateMaterial(SanctuaryPath, new Color(0.8f, 0.58f, 0.29f), new Color(0.62f, 0.28f, 0.08f));
            var dialogue = DialogueAsset();
            var fragment = FragmentAsset();

            var scene = EditorAutomation.OpenOrCreateScene(ScenePath);
            ClearScene(scene);
            var root = Child("HollowEncounter01", null);
            var environment = Child("Environment", root.transform);
            var forestPath = Child("ForestPath", environment);
            var ruins = Child("Ruins", environment);
            var safeArea = Child("SafeArea", environment);
            var finalGate = Child("FinalGate", environment);
            var memoryObjects = Child("MemoryObjects", root.transform);
            var hollowRoot = Child("Hollow", root.transform);
            var lighting = Child("Lighting", root.transform);
            var gameplay = Child("Gameplay", root.transform);
            var spawn = Child("PlayerSpawn", gameplay);
            spawn.position = new Vector3(0f, 0.12f, 2f);
            var checkpoints = Child("Checkpoints", gameplay);
            var resetVolumes = Child("ResetVolumes", gameplay);
            Child("Exit", root.transform);

            BuildPathEnvironment(forestPath, ruins, safeArea, finalGate, stone, hint, sanctuaryMaterial);
            BuildLighting(lighting);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null)
                throw new FileNotFoundException("Build the existing Milestone 2 player prefab before HollowEncounter01.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var reader = player.GetComponent<PlayerInputReader>();
            var movement = player.GetComponent<PlayerMovement>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay, player.transform.Find("CameraTarget"), reader);
            movement.Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            var lightOrigin = player.transform.Find("MemoryLightOrigin");
            var memoryLight = player.GetComponent<PlayerMemoryLight>();
            memoryLight.Configure(reader, lightOrigin, camera.transform, lightOrigin.GetComponent<Light>());
            var interactor = player.GetComponent<PlayerInteractor>();
            interactor.Configure(reader, player.transform);
            var checkpointRespawner = player.AddComponent<CheckpointRespawner>();
            var integrity = player.AddComponent<MemoryIntegrity>();
            integrity.Configure(3);

            var hud = BuildHud(root.transform, integrity);
            var subtitles = BuildSubtitleCanvas(root.transform);
            var focus = Child("EncounterRecallFocusGroup", memoryObjects).gameObject.AddComponent<MemoryFocusGroup>();
            var collector = Child("MemoryFragmentCollector", gameplay).gameObject.AddComponent<MemoryFragmentCollector>();

            MakeCheckpoint(checkpoints, "Checkpoint_Forest", new Vector3(0f, 0.12f, 3f));
            MakeCheckpoint(checkpoints, "Checkpoint_Ruin", new Vector3(0f, 0.12f, 30f));
            MakeCheckpoint(checkpoints, "Checkpoint_Traversal", new Vector3(-5.5f, 0.12f, 78f));
            MakeCheckpoint(checkpoints, "Checkpoint_FinalGate", new Vector3(0f, 0.12f, 105f));

            var recallBridge = BuildRecall(memoryObjects, "RecallBridge_AfterAttack", new Vector3(0f, 0f, 57f),
                new Vector3(4.4f, 0.24f, 7.2f), stone, memory, ghost, hint, focus, 22f);
            var recallStep = BuildRecall(memoryObjects, "RecallStep_OpenRoom", new Vector3(-1f, 0f, 76f),
                new Vector3(3.4f, 0.26f, 3.8f), stone, memory, ghost, hint, focus, 22f);
            var echoPlatform = BuildEchoPlatform(memoryObjects, "EchoTraversalPlatform", memory, ghost,
                new[]
                {
                    new Pose(new Vector3(-5.4f, 0.24f, 84f), Quaternion.identity),
                    new Pose(new Vector3(-1f, 0.24f, 84f), Quaternion.identity),
                    new Pose(new Vector3(4.6f, 0.24f, 84f), Quaternion.identity)
                }, 6.2f, 2.4f, true);

            var gateParts = BuildFinalGateMechanism(memoryObjects, finalGate.transform, stone, memory, ghost, hint, focus);
            var safe = BuildSanctuary(safeArea, sanctuaryMaterial);
            var remnantRecall = BuildRemnant(memoryObjects, "RecallMemoryRemnant", new Vector3(3.6f, 1.15f, 61f), memory, sanctuaryMaterial);
            var remnantEcho = BuildRemnant(memoryObjects, "EchoMemoryRemnant", new Vector3(-3.4f, 1.15f, 90f), memory, sanctuaryMaterial);

            var resetter = Child("EncounterMemoryResetter", gameplay).gameObject.AddComponent<MemoryEncounterResetter>();
            var recallables = new[] { recallBridge.Component, recallStep.Component };
            var echoables = new[] { echoPlatform.Echo };
            var remnants = new[] { remnantRecall.Remnant, remnantEcho.Remnant };
            resetter.Configure(recallables, echoables, new[] { focus }, remnants);

            var corruption = player.AddComponent<MemoryCorruptionController>();
            var collapse = player.AddComponent<MemoryCollapseHandler>();
            var fade = BuildCollapseOverlay(root.transform);
            corruption.Configure(integrity, remnantRecall.Remnant, remnantEcho.Remnant, collapse, resetter, safe, subtitles);
            collapse.Configure(reader, movement, interactor, memoryLight, checkpointRespawner, integrity,
                corruption, resetter, fade, 0.38f, 0.5f, 0.6f);
            safe.Configure(5.7f, safeArea.Find("SanctuaryWarmPointLight").GetComponent<Light>(), corruption);

            BuildRecallFragmentHints(recallBridge, ruins, hint);
            BuildHollow(hollowRoot, player.transform, camera, memoryLight, corruption, safe,
                ghost, voidMaterial, new Vector3(8f, 0.1f, 82f));
            var wakeZone = Child("HollowWakeTrigger", ruins);
            wakeZone.position = new Vector3(0f, 0.8f, 20f);
            wakeZone.gameObject.AddComponent<BoxCollider>().isTrigger = true;
            wakeZone.gameObject.AddComponent<HollowWakeTrigger>().Configure(hollowRoot.GetComponent<HollowController>());

            BuildNarrative(root.transform, gameplay, safeArea, subtitles, collector, fragment,
                hollowRoot.GetComponent<HollowController>(), reader, movement, memoryLight, memory, ghost);
            BuildResetVolumes(resetVolumes);
            var bootstrap = gameplay.gameObject.AddComponent<MemoryAbilityBootstrap>();
            _ = bootstrap;
            Child("HollowDebugTools", gameplay).gameObject.AddComponent<HollowDebugTools>()
                .Configure(corruption, hollowRoot.GetComponent<HollowController>());

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.075f, 0.075f, 0.1f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild(scene.path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildPathEnvironment(Transform path, Transform ruins, Transform safeArea, Transform finalGate,
            Material stone, Material hint, Material sanctuary)
        {
            Cube("QuietForestTrail", path, new Vector3(0f, -0.32f, 9f), new Vector3(9f, 0.64f, 26f), stone);
            Cube("ObservationTrail", path, new Vector3(0f, -0.32f, 25f), new Vector3(9f, 0.64f, 12f), stone);
            Cube("RuinFloor", ruins, new Vector3(0f, -0.32f, 40f), new Vector3(11f, 0.64f, 18f), stone);
            Cube("BridgeNearBank", ruins, new Vector3(0f, -0.32f, 51f), new Vector3(9f, 0.64f, 6f), stone);
            Cube("BridgeFarBank", ruins, new Vector3(0f, -0.32f, 64f), new Vector3(9f, 0.64f, 8f), stone);
            Cube("OpenApproach", path, new Vector3(0f, -0.32f, 73f), new Vector3(12f, 0.64f, 11f), stone);
            Cube("EchoNearBank", path, new Vector3(-5.5f, -0.32f, 84f), new Vector3(7f, 0.64f, 12f), stone);
            Cube("EchoFarBank", path, new Vector3(5.5f, -0.32f, 84f), new Vector3(7f, 0.64f, 12f), stone);
            Cube("FinalApproach", finalGate, new Vector3(0f, -0.32f, 100f), new Vector3(11f, 0.64f, 20f), stone);
            Cube("FinalGateRoomFloor", finalGate, new Vector3(0f, -0.32f, 115f), new Vector3(11f, 0.64f, 14f), stone);
            Cube("AfterGatePath", path, new Vector3(0f, -0.32f, 126f), new Vector3(11f, 0.64f, 12f), stone);
            Cube("SanctuaryFloor", safeArea, new Vector3(0f, -0.32f, 137f), new Vector3(14f, 0.64f, 20f), stone);
            Cube("ExitTrail", path, new Vector3(0f, -0.32f, 153f), new Vector3(10f, 0.64f, 16f), stone);

            for (var side = -1; side <= 1; side += 2)
            {
                Cube($"ForestBoundary_{side}", path, new Vector3(side * 5.1f, 1.1f, 16f), new Vector3(0.3f, 2.8f, 22f), stone);
                Cube($"RuinPillar_{side}_01", ruins, new Vector3(side * 4.4f, 1.2f, 35f), new Vector3(0.8f, 2.4f, 0.8f), stone);
                Cube($"RuinPillar_{side}_02", ruins, new Vector3(side * 4.4f, 1.2f, 44f), new Vector3(0.8f, 2.4f, 0.8f), stone);
                Cube($"SanctuaryWall_{side}", safeArea, new Vector3(side * 7.1f, 1.3f, 138f), new Vector3(0.35f, 2.6f, 15f), stone);
            }
            Cube("RuinedLintel", ruins, new Vector3(0f, 3.3f, 35f), new Vector3(9f, 0.35f, 0.5f), stone);
            Cube("BrokenSignMemory", ruins, new Vector3(0f, 1.1f, 48f), new Vector3(0.65f, 2.2f, 0.32f), stone);
            CreateCrack(ruins, new Vector3(0.34f, 0.45f, 47.82f), hint);

            var barrier = Cube("FinalGateBarrier", finalGate, new Vector3(0f, 1.55f, 118.7f), new Vector3(6f, 3.1f, 0.55f), stone);
            var archLeft = Cube("FinalGatePost_Left", finalGate, new Vector3(-3.4f, 2.2f, 118.7f), new Vector3(0.7f, 4.4f, 1f), stone);
            var archRight = Cube("FinalGatePost_Right", finalGate, new Vector3(3.4f, 2.2f, 118.7f), new Vector3(0.7f, 4.4f, 1f), stone);
            Cube("FinalGateLintel", finalGate, new Vector3(0f, 4.35f, 118.7f), new Vector3(7.4f, 0.65f, 1f), stone);
            _ = barrier;
            _ = archLeft;
            _ = archRight;
            Cube("SanctuaryPedestal", safeArea, new Vector3(0f, 0.45f, 140f), new Vector3(1.7f, 0.9f, 1.7f), sanctuary);
            var warm = Child("SanctuaryWarmPointLight", safeArea);
            warm.position = new Vector3(0f, 2.2f, 140f);
            var point = warm.gameObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.67f, 0.34f);
            point.intensity = 2.4f;
            point.range = 13f;

            var sanctuaryLightRoot = Child("SanctuaryLight", safeArea);
            sanctuaryLightRoot.position = new Vector3(0f, 0.7f, 140f);
            sanctuaryLightRoot.gameObject.AddComponent<SphereCollider>().isTrigger = true;
            sanctuaryLightRoot.gameObject.AddComponent<SanctuaryLight>();
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "SanctuaryCoreGlow";
            glow.transform.SetParent(sanctuaryLightRoot, false);
            glow.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            glow.transform.localScale = Vector3.one * 0.55f;
            glow.GetComponent<Renderer>().sharedMaterial = sanctuary;
            Object.DestroyImmediate(glow.GetComponent<Collider>());
        }

        private static RecallParts BuildRecall(Transform parent, string name, Vector3 position, Vector3 size,
            Material stone, Material memory, Material ghost, Material hint, MemoryFocusGroup focus, float duration)
        {
            var root = Child(name, parent);
            root.position = position;
            var present = Child("PresentState_Ruins", root);
            Cube("MemoryTarget_YellowCrack", present, new Vector3(0f, 0.82f, -1.55f), new Vector3(0.58f, 1.7f, 0.5f), stone);
            CreateCrack(present, new Vector3(0.3f, 0.3f, -1.81f), hint);
            Cube("PresentDebris_Left", present, new Vector3(-1.1f, 0.14f, 0.8f), new Vector3(1.4f, 0.28f, 0.9f), stone);
            Cube("PresentDebris_Right", present, new Vector3(1.1f, 0.12f, 1.35f), new Vector3(1.2f, 0.24f, 0.75f), stone);
            var memoryState = Child("MemoryState", root);
            var preview = Child("GhostPreview", memoryState);
            var ghostForm = Cube("GhostRememberedForm", preview, Vector3.zero, size, ghost);
            Object.DestroyImmediate(ghostForm.GetComponent<Collider>());
            var materialized = Child("MaterializedMemory", memoryState);
            Cube("RememberedForm", materialized, Vector3.zero, size, memory);
            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryState.gameObject, preview.gameObject, materialized.gameObject, duration);
            recall.ConfigureFocusGroup(focus);
            return new RecallParts(root, present.gameObject, memoryState.gameObject, preview.gameObject,
                materialized.gameObject, recall);
        }

        private static EchoParts BuildEchoPlatform(Transform parent, string name, Material memory, Material ghost,
            Pose[] poses, float duration, float pause, bool returnAfter)
        {
            var pathRoot = Child(name + "_Path", parent);
            var points = BuildWaypoints(pathRoot, poses);
            var path = pathRoot.gameObject.AddComponent<EchoPath>();
            path.Configure(points, duration, pause, returnAfter);
            var preview = BuildMotionPreview(parent, name + "_MotionPreview", poses, ghost);
            var mover = Cube(name, parent, poses[0].Position, new Vector3(3.7f, 0.24f, 3.4f), memory);
            mover.transform.rotation = poses[0].Rotation;
            var rideObject = Child("EchoRideSurface", mover.transform).gameObject;
            var rideCollider = rideObject.AddComponent<BoxCollider>();
            rideCollider.isTrigger = true;
            rideCollider.size = new Vector3(3.9f, 2.2f, 3.6f);
            rideCollider.center = Vector3.up * 0.85f;
            var ride = rideObject.AddComponent<EchoRideSurface>();
            ride.Configure(rideCollider);
            var echo = mover.AddComponent<MemoryEchoable>();
            echo.Configure(path, mover.transform, preview, ride, mover.GetComponentsInChildren<Renderer>(true));
            return new EchoParts(echo, path, mover);
        }

        private static RecallParts BuildFinalGateMechanism(Transform memoryParent, Transform gateParent, Material stone,
            Material memory, Material ghost, Material hint, MemoryFocusGroup focus)
        {
            var recall = BuildRecall(memoryParent, "FinalGateMemoryMechanism", new Vector3(0f, 0f, 112.4f),
                new Vector3(3.6f, 0.26f, 3.2f), stone, memory, ghost, hint, focus, 24f);
            var doorPanel = Cube("RememberedGatePanel", recall.Materialized.transform,
                new Vector3(0f, 1.65f, 6.3f), new Vector3(5f, 3.3f, 0.4f), memory);
            recall.Component.RefreshStateObjects();

            var pathRoot = Child("FinalGate_EchoPath", memoryParent);
            var poses = new[]
            {
                new Pose(new Vector3(0f, 1.65f, 118.7f), Quaternion.identity),
                new Pose(new Vector3(0f, 5.1f, 118.7f), Quaternion.identity)
            };
            var points = BuildWaypoints(pathRoot, poses);
            var path = pathRoot.gameObject.AddComponent<EchoPath>();
            path.Configure(points, 3.8f, 0f, false);
            var preview = BuildMotionPreview(memoryParent, "FinalGate_EchoMotionPreview", poses, ghost);
            var echo = recall.Root.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path, doorPanel.transform, preview, null, doorPanel.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureRecallRequirement(recall.Component);
            recall.Root.gameObject.AddComponent<MemoryComposite>().Configure(recall.Component, echo);
            recall.Root.gameObject.AddComponent<MemoryStateVisualReactor>().Configure(
                MemoryAbility.Recall, recall.Materialized.GetComponentsInChildren<Renderer>(true));
            var finalBarrier = gateParent.Find("FinalGateBarrier").gameObject;
            var gateController = Child("FinalGateRecallEchoController", gateParent).gameObject.AddComponent<RecallEchoGate>();
            gateController.Configure(recall.Component, echo, finalBarrier);
            return recall;
        }

        private static SanctuaryLight BuildSanctuary(Transform parent, Material unused)
        {
            var sanctuary = parent.Find("SanctuaryLight").GetComponent<SanctuaryLight>();
            var collider = sanctuary.GetComponent<SphereCollider>();
            collider.radius = 5.7f;
            _ = unused;
            return sanctuary;
        }

        private static RemnantParts BuildRemnant(Transform parent, string name, Vector3 position, Material memory, Material hint)
        {
            var remnant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            remnant.name = name;
            remnant.transform.SetParent(parent, false);
            remnant.transform.position = position;
            remnant.transform.localScale = Vector3.one * 0.65f;
            remnant.GetComponent<Renderer>().sharedMaterial = memory;
            var light = remnant.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.47f, 0.88f, 1f);
            light.range = 5f;
            light.intensity = 0f;
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "MemoryRemnantHalo";
            halo.transform.SetParent(remnant.transform, false);
            halo.transform.localScale = Vector3.one * 1.6f;
            halo.GetComponent<Renderer>().sharedMaterial = hint;
            Object.DestroyImmediate(halo.GetComponent<Collider>());
            var behaviour = remnant.AddComponent<MemoryRemnant>();
            behaviour.Configure(MemoryAbility.Recall, null, remnant.GetComponentsInChildren<Renderer>(true), light);
            return new RemnantParts(remnant, behaviour);
        }

        private static void BuildHollow(Transform root, Transform player, Camera camera, PlayerMemoryLight memoryLight,
            MemoryCorruptionController corruption, SanctuaryLight sanctuary, Material ghost, Material voidMaterial,
            Vector3 returnPoint)
        {
            root.position = new Vector3(0f, 0.08f, 48f);
            var controller = root.gameObject.AddComponent<CharacterController>();
            controller.height = 3.1f;
            controller.radius = 0.58f;
            controller.center = new Vector3(0f, 1.52f, 0f);
            controller.stepOffset = 0.25f;
            controller.skinWidth = 0.08f;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "FacelessElongatedSilhouette";
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 1.58f, 0f);
            body.transform.localScale = new Vector3(0.76f, 1.68f, 0.62f);
            body.GetComponent<Renderer>().sharedMaterial = voidMaterial;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var shoulder = Cube("UnnaturalShoulderShape", root, new Vector3(0f, 2.08f, 0f), new Vector3(1.05f, 0.38f, 0.5f), voidMaterial);
            shoulder.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            Object.DestroyImmediate(shoulder.GetComponent<Collider>());
            var seamA = Cube("VoidSeam_Left", root, new Vector3(-0.27f, 1.55f, 0.3f), new Vector3(0.055f, 1.1f, 0.04f), ghost);
            var seamB = Cube("VoidSeam_Right", root, new Vector3(0.26f, 1.2f, 0.29f), new Vector3(0.045f, 0.65f, 0.04f), ghost);
            Object.DestroyImmediate(seamA.GetComponent<Collider>());
            Object.DestroyImmediate(seamB.GetComponent<Collider>());
            var fragments = new GameObject[3];
            for (var i = 0; i < fragments.Length; i++)
            {
                var fragment = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fragment.name = $"RevealedChildMemory_{i + 1:00}";
                fragment.transform.SetParent(root, false);
                fragment.transform.localPosition = new Vector3((i - 1) * 0.2f, 0.65f + i * 0.64f, 0.32f);
                fragment.transform.localScale = new Vector3(0.15f, 0.28f, 0.1f);
                fragment.GetComponent<Renderer>().sharedMaterial = ghost;
                Object.DestroyImmediate(fragment.GetComponent<Collider>());
                fragment.SetActive(false);
                fragments[i] = fragment;
            }
            var returnAnchor = Child("HollowReturnToDarkness", root.parent);
            returnAnchor.position = returnPoint;
            var markerA = Child("ObservationMarker_01", root.parent);
            markerA.position = new Vector3(6.2f, 0.08f, 29f);
            var markerB = Child("ObservationMarker_02", root.parent);
            markerB.position = new Vector3(-6.2f, 0.08f, 38f);
            var hollow = root.gameObject.AddComponent<HollowController>();
            hollow.Configure(player, camera, memoryLight, corruption, sanctuary, returnAnchor,
                new[] { markerA, markerB }, root.GetComponentsInChildren<Renderer>(true), fragments);
        }

        private static void BuildRecallFragmentHints(RecallParts recall, Transform parent, Material hint)
        {
            var fragmentBits = Child("RecallCorruptionFragments", parent);
            for (var i = 0; i < 4; i++)
            {
                var shard = Cube($"RecallShard_{i + 1:00}", fragmentBits,
                    new Vector3(-1.4f + i * 0.9f, 0.8f + (i % 2) * 0.4f, 58.2f + (i % 2) * 0.4f),
                    new Vector3(0.32f, 0.07f, 0.28f), hint);
                shard.transform.localRotation = Quaternion.Euler(0f, i * 19f, 14f * (i % 2 == 0 ? 1f : -1f));
                Object.DestroyImmediate(shard.GetComponent<Collider>());
            }
            fragmentBits.gameObject.SetActive(false);
            var reactor = recall.Root.gameObject.AddComponent<MemoryStateVisualReactor>();
            reactor.Configure(MemoryAbility.Recall, recall.Materialized.GetComponentsInChildren<Renderer>(true), fragmentBits.gameObject);
        }

        private static void BuildNarrative(Transform root, Transform gameplay, Transform safeArea,
            SubtitlePresenter subtitles, MemoryFragmentCollector collector, MemoryFragmentDefinition fragment,
            HollowController hollow, PlayerInputReader reader, PlayerMovement movement, PlayerMemoryLight memoryLight,
            Material memory, Material ghost)
        {
            var lines = DialogueAsset();
            var child = Figure(safeArea, "ChildProtagonist_Echo", new Vector3(-1.15f, 0.12f, 143.4f), memory, 0.92f);
            var unknown = Figure(safeArea, "UnknownChild_Echo", new Vector3(0f, 0.12f, 143.8f), ghost, 0.86f);
            var face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            face.name = "RememberedChildFace_Ephemeral";
            face.transform.SetParent(child.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.78f, 0.18f);
            face.transform.localScale = new Vector3(0.22f, 0.24f, 0.1f);
            face.GetComponent<Renderer>().sharedMaterial = memory;
            Object.DestroyImmediate(face.GetComponent<Collider>());
            var distortion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            distortion.name = "HollowFaceErasure";
            distortion.transform.SetParent(root, false);
            distortion.transform.position = new Vector3(-1.15f, 1.15f, 143.58f);
            distortion.transform.localScale = new Vector3(0.48f, 0.68f, 0.16f);
            distortion.GetComponent<Renderer>().sharedMaterial = ghost;
            Object.DestroyImmediate(distortion.GetComponent<Collider>());
            child.SetActive(false);
            unknown.SetActive(false);
            face.SetActive(false);
            distortion.SetActive(false);

            var echoObject = Child("HollowMemoryEcho", gameplay);
            var echo = echoObject.gameObject.AddComponent<HollowMemoryEcho>();
            echo.Configure(lines, subtitles, child, unknown, face, distortion, fragment, collector, hollow,
                reader, movement, memoryLight, 1.2f);
            var trigger = Child("HollowMemoryEchoTrigger", safeArea);
            trigger.position = new Vector3(0f, 0.8f, 141.8f);
            trigger.gameObject.AddComponent<BoxCollider>().isTrigger = true;
            trigger.gameObject.AddComponent<HollowMemoryEchoTrigger>().Configure(echo);
        }

        private static GameObject BuildHud(Transform parent, MemoryIntegrity integrity)
        {
            var canvasObject = new GameObject("MemoryIntegrityCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var labelObject = new GameObject("MemoryIntegritySegments", typeof(RectTransform), typeof(Text), typeof(MemoryIntegrityDisplay));
            labelObject.transform.SetParent(canvasObject.transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.025f, 0.91f);
            rect.anchorMax = new Vector2(0.22f, 0.99f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            labelObject.GetComponent<MemoryIntegrityDisplay>().Configure(integrity, text);
            return canvasObject;
        }

        private static CanvasGroup BuildCollapseOverlay(Transform parent)
        {
            var canvasObject = new GameObject("MemoryCollapseCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var imageObject = new GameObject("Darkness", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<Image>().color = new Color(0.008f, 0.01f, 0.018f, 1f);
            imageObject.GetComponent<Image>().raycastTarget = false;
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static SubtitlePresenter BuildSubtitleCanvas(Transform root)
        {
            var canvasObject = new GameObject("SubtitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var panel = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SubtitlePresenter));
            panel.transform.SetParent(canvasObject.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.14f, 0.035f);
            panelRect.anchorMax = new Vector2(0.86f, 0.2f);
            panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.02f, 0.025f, 0.04f, 0.9f);
            panel.GetComponent<Image>().raycastTarget = false;
            var textObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.08f);
            rect.anchorMax = new Vector2(0.96f, 0.92f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.96f, 0.94f, 0.88f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var presenter = panel.GetComponent<SubtitlePresenter>();
            presenter.Configure(panel.GetComponent<CanvasGroup>(), text);
            return presenter;
        }

        private static void BuildResetVolumes(Transform parent)
        {
            var reset = Child("ForestFallReset", parent);
            reset.position = new Vector3(0f, -2f, 70f);
            reset.gameObject.AddComponent<BoxCollider>().size = new Vector3(30f, 1f, 150f);
            reset.gameObject.AddComponent<ResetVolume>();
        }

        private static void MakeCheckpoint(Transform parent, string name, Vector3 position)
        {
            var spawn = Child(name + "_Spawn", parent);
            spawn.position = position;
            var trigger = Child(name, parent);
            trigger.position = position + Vector3.forward;
            trigger.gameObject.AddComponent<BoxCollider>().size = new Vector3(8f, 2.5f, 1.8f);
            trigger.gameObject.AddComponent<Checkpoint>().Configure(spawn);
        }

        private static void BuildLighting(Transform parent)
        {
            var keyLight = Child("MoonlitForestKeyLight", parent);
            keyLight.localEulerAngles = new Vector3(48f, -32f, 0f);
            var directional = keyLight.gameObject.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 0.52f;
            directional.color = new Color(0.55f, 0.62f, 0.8f);
            var beacon = Child("DistantWarmBeacon", parent);
            beacon.position = new Vector3(0f, 2f, 140f);
            var point = beacon.gameObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.59f, 0.31f);
            point.intensity = 1.6f;
            point.range = 11f;
        }

        private static GameObject Figure(Transform parent, string name, Vector3 position, Material material, float height)
        {
            var figure = Child(name, parent);
            figure.position = position;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "ChildSilhouette";
            body.transform.SetParent(figure, false);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(0.34f, height * 0.5f, 0.28f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            figure.gameObject.SetActive(false);
            return figure.gameObject;
        }

        private static Transform[] BuildWaypoints(Transform parent, Pose[] poses)
        {
            var points = new Transform[poses.Length];
            for (var i = 0; i < poses.Length; i++)
            {
                var point = Child($"Waypoint_{i + 1:00}", parent);
                point.SetPositionAndRotation(poses[i].Position, poses[i].Rotation);
                points[i] = point;
            }
            return points;
        }

        private static GameObject BuildMotionPreview(Transform parent, string name, Pose[] poses, Material ghost)
        {
            var preview = Child(name, parent);
            preview.gameObject.SetActive(false);
            var lineObject = Child("RememberedMotionLine", preview);
            var line = lineObject.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = poses.Length;
            line.startWidth = 0.075f;
            line.endWidth = 0.075f;
            line.sharedMaterial = ghost;
            for (var i = 0; i < poses.Length; i++)
                line.SetPosition(i, poses[i].Position + Vector3.up * 0.45f);
            return preview.gameObject;
        }

        private static void CreateCrack(Transform parent, Vector3 origin, Material material)
        {
            var offsets = new[] { Vector3.zero, new Vector3(0.12f, 0.18f, 0f), new Vector3(-0.05f, 0.38f, 0f),
                new Vector3(0.12f, 0.6f, 0f), new Vector3(-0.03f, 0.83f, 0f) };
            for (var i = 0; i < offsets.Length - 1; i++)
            {
                var a = origin + offsets[i];
                var b = origin + offsets[i + 1];
                var delta = b - a;
                var segment = Cube($"YellowMemoryCrack_{i}", parent, (a + b) * 0.5f,
                    new Vector3(0.045f, 0.028f, delta.magnitude), material);
                segment.transform.localRotation = Quaternion.LookRotation(delta, Vector3.up);
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

        private static Material CreateMaterial(string path, Color color, Color emission, bool transparent = false)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidDataException("URP/Lit shader is unavailable.");
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

        private static DialogueSequence DialogueAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialoguePath);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<DialogueSequence>();
            var serialized = new SerializedObject(asset);
            var lines = serialized.FindProperty("lines");
            var dialogueLines = new[]
            {
                new DialogueLine("UnknownChild", "You're scared again.", 2.1f),
                new DialogueLine("ChildProtagonist", "I'm not.", 1.8f),
                new DialogueLine("UnknownChild", "Then look at me.", 2.3f)
            };
            lines.arraySize = dialogueLines.Length;
            for (var i = 0; i < dialogueLines.Length; i++)
            {
                var item = lines.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("speaker").stringValue = dialogueLines[i].speaker;
                item.FindPropertyRelative("text").stringValue = dialogueLines[i].text;
                item.FindPropertyRelative("duration").floatValue = dialogueLines[i].duration;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, DialoguePath);
            return asset;
        }

        private static MemoryFragmentDefinition FragmentAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(FragmentPath);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            asset.Configure("memory_fear_03", "Look at Me", "I knew her voice. I still can't remember her face.");
            AssetDatabase.CreateAsset(asset, FragmentPath);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddToBuild(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects())
                Object.DestroyImmediate(gameObject);
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
            public readonly GameObject MemoryState;
            public readonly GameObject Preview;
            public readonly GameObject Materialized;
            public readonly MemoryRecallable Component;
            public RecallParts(Transform root, GameObject present, GameObject memoryState, GameObject preview,
                GameObject materialized, MemoryRecallable component)
            { Root = root; Present = present; MemoryState = memoryState; Preview = preview; Materialized = materialized; Component = component; }
        }

        private readonly struct EchoParts
        {
            public readonly MemoryEchoable Echo;
            public readonly EchoPath Path;
            public readonly GameObject Mover;
            public EchoParts(MemoryEchoable echo, EchoPath path, GameObject mover) { Echo = echo; Path = path; Mover = mover; }
        }

        private readonly struct RemnantParts
        {
            public readonly GameObject Object;
            public readonly MemoryRemnant Remnant;
            public RemnantParts(GameObject gameObject, MemoryRemnant remnant) { Object = gameObject; Remnant = remnant; }
        }
    }
}
