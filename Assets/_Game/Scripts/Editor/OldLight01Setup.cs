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
    /// <summary>Editor-API graybox recipe for the first Old Light chamber.</summary>
    public static class OldLight01Setup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/OldLight01.unity";
        private const string NeutralPath = "Assets/_Game/Art/Materials/OldLightStone.mat";
        private const string MemoryPath = "Assets/_Game/Art/Materials/OldLightMemory.mat";
        private const string GhostPath = "Assets/_Game/Art/Materials/OldLightMemoryGhost.mat";
        private const string HintPath = "Assets/_Game/Art/Materials/OldLightMemoryHint.mat";
        private const string DialoguePath = "Assets/_Game/Art/Dialogue/OldLightEcho.asset";
        private const string FragmentPath = "Assets/_Game/Art/MemoryFragments/memory_boathouse_01.asset";

        [MenuItem("Light Remembers/Prototype/Build Milestone 4 Old Light Chamber")]
        public static void BuildOldLight01()
        {
            EnsureFolder("Assets/_Game/Scenes/Prototype");
            EnsureFolder("Assets/_Game/Art/Materials");
            EnsureFolder("Assets/_Game/Art/Dialogue");
            EnsureFolder("Assets/_Game/Art/MemoryFragments");

            var stone = LoadOrCreateMaterial(NeutralPath, new Color(0.28f, 0.30f, 0.32f), Color.black, false);
            var memory = LoadOrCreateMaterial(MemoryPath, new Color(0.48f, 0.82f, 0.88f), new Color(0.2f, 0.72f, 1.1f), false);
            var ghost = LoadOrCreateMaterial(GhostPath, new Color(0.36f, 0.88f, 0.96f, 0.25f), new Color(0.2f, 0.95f, 1.4f), true);
            var hint = LoadOrCreateMaterial(HintPath, new Color(1f, 0.76f, 0.12f), new Color(0.28f, 0.16f, 0.015f), false);
            var dialogue = LoadOrCreateDialogue();
            var fragment = LoadOrCreateFragment();

            var scene = EditorAutomation.OpenOrCreateScene(ScenePath);
            ClearScene(scene);
            var root = EditorAutomation.CreateGameObject("OldLight01", null, Vector3.zero, Vector3.zero, Vector3.one);
            var environment = Child("Environment", root.transform);
            var gameplay = Child("Gameplay", root.transform);
            BuildLighting(root.transform);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null) throw new FileNotFoundException("PlayerPrototype prefab is required. Build Milestone 2 first.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, 1f), Quaternion.identity);
            var reader = player.GetComponent<PlayerInputReader>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay, player.transform.Find("CameraTarget"), reader);
            player.GetComponent<PlayerMovement>().Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            player.GetComponent<PlayerMemoryLight>().Configure(reader, player.transform.Find("MemoryLightOrigin"), camera.transform,
                player.transform.Find("MemoryLightOrigin").GetComponent<Light>());
            player.AddComponent<CheckpointRespawner>();

            BuildBrokenPath(environment, gameplay, stone, memory, ghost, hint);
            BuildCollapsedStair(environment, gameplay, stone, memory, ghost, hint);
            var focus = Child("MemoryChoiceFocusGroup", gameplay).gameObject.AddComponent<MemoryFocusGroup>();
            BuildMemoryChoice(environment, gameplay, stone, memory, ghost, hint, focus);
            var narrative = BuildFinalChamber(environment, gameplay, stone, memory, ghost, hint, dialogue, fragment);

            BuildSubtitleCanvas(root.transform, narrative.Echo);
            BuildReturnRoute(environment, gameplay, stone);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.24f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Light Remembers/Prototype/Reset Current Recall Puzzles")]
        public static void ResetCurrentRecallPuzzles()
        {
            foreach (var recallable in Object.FindObjectsByType<MemoryRecallable>(FindObjectsInactive.Include))
            {
                recallable.SetRevealed(false);
                recallable.RequestSafeRelease();
            }
            foreach (var group in Object.FindObjectsByType<MemoryFocusGroup>(FindObjectsInactive.Include))
                group.ResetGroup();
        }

        private static void BuildBrokenPath(Transform environment, Transform gameplay, Material stone, Material memory, Material ghost, Material hint)
        {
            var room = Child("Room01_BrokenPath", environment);
            CreateCube("StartSidePlatform", room, new Vector3(0f, 0.35f, 4f), new Vector3(5f, 0.7f, 7f), stone);
            CreateCube("DestinationSidePlatform", room, new Vector3(0f, 0.35f, 17.5f), new Vector3(5f, 0.7f, 7f), stone);
            var puzzleRoot = Child("BrokenBridgePuzzle", gameplay);
            var resetPoint = Child("Checkpoint01Spawn", gameplay);
            resetPoint.position = new Vector3(0f, 0.1f, 1f);
            MakeCheckpoint(gameplay, "Checkpoint01", new Vector3(0f, 0.15f, 2.4f), resetPoint);
            var recallable = CreateRecallable(puzzleRoot, "BrokenBridgeMemory", new Vector3(0f, 0f, 10.75f), stone, memory, ghost, hint, 8f);
            CreateCube("BrokenBridge_RuinedButTargetable", recallable.Present.transform,
                new Vector3(0f, 1.05f, -3.2f), new Vector3(0.65f, 2.1f, 0.55f), stone);
            CreateCrack(recallable.Present.transform, new Vector3(0f, 1.35f, -3.49f), hint);
            CreateCube("RuinedFragment_Left", recallable.Present.transform, new Vector3(-1.25f, 0.58f, -3.5f), new Vector3(1.2f, 0.18f, 0.8f), stone);
            CreateCube("RuinedFragment_Right", recallable.Present.transform, new Vector3(1.25f, 0.48f, 3.25f), new Vector3(1.1f, 0.18f, 0.7f), stone);
            ConfigureBridgeMemory(recallable, 0f, 10.75f, 0.7f, 7f, 3.2f, memory, ghost);
            var pit = CreateResetVolume(environment, "BrokenBridgePuzzle_RecoveryZone", new Vector3(0f, -3f, 10.5f), new Vector3(14f, 1f, 22f), false);
            var puzzle = pit.AddComponent<BrokenBridgePuzzle>();
            puzzle.Configure(recallable.Component,
                FindColliderNamed(room, "StartSidePlatform"), FindColliderNamed(room, "DestinationSidePlatform"), resetPoint);
        }

        private static void BuildCollapsedStair(Transform environment, Transform gameplay, Material stone, Material memory, Material ghost, Material hint)
        {
            var room = Child("Room02_CollapsedStair", environment);
            CreateCube("ApproachWalkway", room, new Vector3(0f, 0.3f, 23f), new Vector3(5f, 0.6f, 5f), stone);
            CreateCube("LowerLanding", room, new Vector3(0f, 0.3f, 27.1f), new Vector3(5f, 0.6f, 3.4f), stone);
            CreateCube("MiddleLanding", room, new Vector3(0f, 0.3f, 33.1f), new Vector3(5f, 0.6f, 4f), stone);
            for (var i = 0; i < 3; i++)
                CreateCube($"PermanentStair_{i + 1}", room, new Vector3(0f, 0.4f + i * 0.2f, 35.2f + i * 0.55f),
                    new Vector3(3.2f, 0.2f + i * 0.2f, 0.65f), stone);
            CreateCube("UpperLanding", room, new Vector3(0f, 0.9f, 41.25f), new Vector3(5f, 0.6f, 5f), stone);
            var checkpoint = Child("Checkpoint02Spawn", gameplay);
            checkpoint.position = new Vector3(0f, 0.1f, 22.2f);
            MakeCheckpoint(gameplay, "Checkpoint02", new Vector3(0f, 0.15f, 22.4f), checkpoint);
            var root = Child("CollapsedStairPuzzle", gameplay);
            var lower = CreateRecallable(root, "LowerStairMemory", new Vector3(0f, 0f, 29.8f), stone, memory, ghost, hint, 8f);
            var upper = CreateRecallable(root, "UpperWalkwayMemory", new Vector3(0f, 0.6f, 38.65f), stone, memory, ghost, hint, 8f);
            ConfigureBridgeMemory(lower, 0f, 29.8f, 0.6f, 3.6f, 3.1f, memory, ghost);
            ConfigureBridgeMemory(upper, 0f, 38.65f, 1.2f, 3.5f, 3.1f, memory, ghost);
            // Stair spans are independent recalls; a shared group would make the first vanish underfoot.
            CreateResetVolume(environment, "Room02_ResetVolume", new Vector3(0f, -3f, 34f), new Vector3(14f, 1f, 25f));
        }

        private static void BuildMemoryChoice(Transform environment, Transform gameplay, Material stone, Material memory, Material ghost, Material hint, MemoryFocusGroup focus)
        {
            var room = Child("Room03_MemoryChoice", environment);
            CreateCube("ChoiceApproach", room, new Vector3(0f, 0.3f, 46.25f), new Vector3(8f, 0.6f, 6f), stone);
            CreateCube("ChoiceMergePlatform", room, new Vector3(0f, 0.3f, 56f), new Vector3(8f, 0.6f, 5.5f), stone);
            MakeCheckpoint(gameplay, "Checkpoint03", new Vector3(0f, 0.15f, 43.5f), MakeSpawn(gameplay, "Checkpoint03Spawn", new Vector3(0f, 0.1f, 43f)));
            var left = CreateRecallable(gameplay, "MemoryDoorThreshold", new Vector3(-2.4f, 0f, 51.6f), stone, memory, ghost, hint, 8f);
            var right = CreateRecallable(gameplay, "MovableMemoryPlatform", new Vector3(2.4f, 0f, 51.6f), stone, memory, ghost, hint, 8f);
            ConfigureBridgeMemory(left, -2.4f, 51.6f, 0.6f, 6f, 2.1f, memory, ghost);
            ConfigureBridgeMemory(right, 2.4f, 51.6f, 0.6f, 6f, 2.1f, memory, ghost);
            BuildDoorwayMemory(left, stone, memory, ghost);
            left.Component.ConfigureFocusGroup(focus);
            right.Component.ConfigureFocusGroup(focus);
            CreateResetVolume(environment, "Room03_ResetVolume", new Vector3(0f, -3f, 51.5f), new Vector3(18f, 1f, 20f));
        }

        private static NarrativeParts BuildFinalChamber(Transform environment, Transform gameplay, Material stone, Material memory,
            Material ghost, Material hint, DialogueSequence dialogue, MemoryFragmentDefinition fragment)
        {
            var room = Child("FinalChamber", environment);
            CreateCube("FinalChamberFloor", room, new Vector3(0f, 0.25f, 63.5f), new Vector3(12f, 0.5f, 11f), stone);
            MakeCheckpoint(gameplay, "CheckpointFinal", new Vector3(0f, 0.15f, 59.5f), MakeSpawn(gameplay, "CheckpointFinalSpawn", new Vector3(0f, 0.1f, 59f)));
            CreateCube("OldLightStoneBase", room, new Vector3(0f, 0.85f, 62.5f), new Vector3(1.2f, 1.2f, 1.2f), stone);
            var lamp = new GameObject("OldLight");
            lamp.transform.SetParent(room, false);
            lamp.transform.localPosition = new Vector3(0f, 1.7f, 62.5f);
            var lampMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lampMesh.name = "OldLightCore";
            lampMesh.transform.SetParent(lamp.transform, false);
            lampMesh.transform.localScale = Vector3.one * 0.35f;
            lampMesh.GetComponent<Renderer>().sharedMaterial = memory;
            Object.DestroyImmediate(lampMesh.GetComponent<Collider>());
            var chamberLight = lamp.AddComponent<Light>();
            chamberLight.type = LightType.Point;
            chamberLight.range = 8f;
            chamberLight.intensity = 0.45f;
            chamberLight.color = new Color(0.65f, 0.77f, 1f);
            var lever = Child("AncientLever", room);
            lever.localPosition = new Vector3(-2.1f, 0.55f, 62.5f);
            var trigger = lever.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(1.5f, 1.8f, 1.5f);
            CreateCube("LeverPedestal", lever, Vector3.zero, new Vector3(0.45f, 1.1f, 0.45f), stone);
            var handle = CreateCube("LeverHandle", lever, new Vector3(0f, 0.82f, 0.24f), new Vector3(0.16f, 0.16f, 0.8f), hint).transform;
            var sequence = Child("MemoryEcho", gameplay).gameObject.AddComponent<MemoryEchoSequence>();
            var child = CreateEchoFigure(room, "ChildProtagonist_Echo", new Vector3(-0.6f, 0.25f, 64f), memory, 0.78f);
            var grandfather = CreateEchoFigure(room, "Grandfather_Echo", new Vector3(0.8f, 0.25f, 64.2f), memory, 1f);
            var silhouette = CreateEchoFigure(room, "UnknownChild_Silhouette", new Vector3(2.2f, 0.25f, 64.1f), ghost, 0.82f);
            var collector = Child("MemoryFragmentCollector", gameplay).gameObject.AddComponent<MemoryFragmentCollector>();
            var exit = BuildExit(room, stone);
            sequence.Configure(dialogue, null, child, grandfather, silhouette, chamberLight, fragment, collector, exit);
            lever.gameObject.AddComponent<AncientLever>().Configure(sequence, handle);
            CreateResetVolume(environment, "FinalChamber_ResetVolume", new Vector3(0f, -3f, 63f), new Vector3(20f, 1f, 24f));
            return new NarrativeParts(sequence, exit);
        }

        private static ExitUnlocker BuildExit(Transform parent, Material stone)
        {
            var exit = Child("ChamberExit", parent);
            exit.localPosition = new Vector3(0f, 0f, 68.1f);
            var barrier = CreateCube("ExitBarrier", exit, new Vector3(0f, 1.4f, 0f), new Vector3(3.5f, 2.8f, 0.35f), stone);
            var gate = exit.gameObject.AddComponent<BoxCollider>();
            gate.isTrigger = true;
            gate.center = new Vector3(0f, 1.2f, 1.6f);
            gate.size = new Vector3(5f, 3f, 3f);
            var unlocker = exit.gameObject.AddComponent<ExitUnlocker>();
            unlocker.Configure(barrier, "BoathousePrototype");
            return unlocker;
        }

        private static void BuildSubtitleCanvas(Transform root, MemoryEchoSequence sequence)
        {
            var canvasGameObject = new GameObject("SubtitleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGameObject.transform.SetParent(root, false);
            var canvas = canvasGameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGameObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var panelGameObject = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(SubtitlePresenter));
            panelGameObject.transform.SetParent(canvasGameObject.transform, false);
            var panelObject = panelGameObject.transform;
            var panelRect = panelGameObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.12f, 0.04f);
            panelRect.anchorMax = new Vector2(0.88f, 0.22f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = panelGameObject.GetComponent<Image>();
            panelImage.color = new Color(0.025f, 0.04f, 0.055f, 0.88f);
            panelImage.raycastTarget = false;
            var group = panelGameObject.GetComponent<CanvasGroup>();
            var textGameObject = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            textGameObject.transform.SetParent(panelObject, false);
            var textObject = textGameObject.transform;
            var rect = textGameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.04f, 0.1f);
            rect.anchorMax = new Vector2(0.96f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = textGameObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 29;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.94f, 0.88f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            panelGameObject.GetComponent<SubtitlePresenter>().Configure(group, text);
            var presenter = panelGameObject.GetComponent<SubtitlePresenter>();
            sequence.ConfigurePresenter(presenter);
        }

        private static void BuildReturnRoute(Transform environment, Transform gameplay, Material stone)
        {
            var walkway = Child("ReturnWalkway", environment);
            CreateCube("ReturnToExitWalkway", walkway, new Vector3(0f, 0.25f, 71f), new Vector3(6f, 0.5f, 9f), stone);
            CreateResetVolume(environment, "EndOfPathResetVolume", new Vector3(0f, -3f, 74f), new Vector3(12f, 1f, 12f));
        }

        private static RecallableParts CreateRecallable(Transform parent, string name, Vector3 position, Material stone, Material memory, Material ghost, Material hint, float duration)
        {
            var root = Child(name, parent);
            root.localPosition = position;
            var present = Child("PresentState_Ruins", root);
            var target = CreateCube("MemoryTarget_RuinedPillar", present, new Vector3(0f, 1f, -2f), new Vector3(0.7f, 2f, 0.6f), stone);
            CreateCrack(present, new Vector3(0f, 1.35f, -2.31f), hint);
            CreateCube("PresentDebris_A", present, new Vector3(-0.8f, 0.3f, -0.9f), new Vector3(0.9f, 0.35f, 0.8f), stone);
            CreateCube("PresentDebris_B", present, new Vector3(0.75f, 0.22f, 2.2f), new Vector3(0.7f, 0.22f, 0.55f), stone);
            var memoryRoot = Child("MemoryState", root);
            var preview = Child("GhostPreview", memoryRoot);
            var materialized = Child("MaterializedMemory", memoryRoot);
            var recallable = root.gameObject.AddComponent<MemoryRecallable>();
            recallable.Configure(present.gameObject, memoryRoot.gameObject, preview.gameObject, materialized.gameObject, duration);
            return new RecallableParts(recallable, present.gameObject, preview.gameObject, materialized.gameObject, target);
        }

        private static void ConfigureBridgeMemory(RecallableParts parts, float x, float z, float top, float length, float width, Material memory, Material ghost)
        {
            var localPosition = new Vector3(x - parts.RootOffsetX, top + 0.08f - parts.RootOffsetY, z - parts.RootOffsetZ);
            CreateCube("GhostRememberedWalkway", parts.Preview.transform, localPosition, new Vector3(width, 0.12f, length), ghost);
            CreateCube("RememberedWalkway", parts.Materialized.transform, localPosition, new Vector3(width, 0.16f, length), memory);
            parts.Component.RefreshStateObjects();
        }

        private static void BuildDoorwayMemory(RecallableParts parts, Material stone, Material memory, Material ghost)
        {
            CreateCube("BrokenDoorLeaf", parts.Present.transform, new Vector3(0f, 1.15f, -2.4f), new Vector3(2.2f, 2.2f, 0.24f), stone);
            AddDoorwayFrame(parts.Preview.transform, ghost, false);
            AddDoorwayFrame(parts.Materialized.transform, memory, true);
            parts.Component.RefreshStateObjects();
        }

        private static void AddDoorwayFrame(Transform parent, Material material, bool physical)
        {
            CreateDoorFramePart("RememberedDoorJamb_L", parent, new Vector3(-1.2f, 1.25f, -2.4f), new Vector3(0.22f, 2.5f, 0.28f), material, physical);
            CreateDoorFramePart("RememberedDoorJamb_R", parent, new Vector3(1.2f, 1.25f, -2.4f), new Vector3(0.22f, 2.5f, 0.28f), material, physical);
            CreateDoorFramePart("RememberedDoorLintel", parent, new Vector3(0f, 2.48f, -2.4f), new Vector3(2.55f, 0.24f, 0.32f), material, physical);
        }

        private static void CreateDoorFramePart(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool physical)
        {
            var piece = CreateCube(name, parent, position, scale, material);
            if (!physical) Object.DestroyImmediate(piece.GetComponent<Collider>());
        }

        private static MemoryRecallable FindRecallable(Transform root, string name) => root.Find(name).GetComponent<MemoryRecallable>();

        private static GameObject CreateResetVolume(Transform parent, string name, Vector3 position, Vector3 size, bool attachReset = true)
        {
            var go = Child(name, parent).gameObject;
            go.transform.localPosition = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            if (attachReset) go.AddComponent<ResetVolume>();
            return go;
        }

        private static void MakeCheckpoint(Transform parent, string name, Vector3 position, Transform respawn)
        {
            var checkpoint = Child(name, parent).gameObject;
            checkpoint.transform.localPosition = position;
            var trigger = checkpoint.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(5f, 2f, 1.2f);
            checkpoint.AddComponent<Checkpoint>().Configure(respawn);
        }

        private static Transform MakeSpawn(Transform parent, string name, Vector3 position)
        {
            var spawn = Child(name, parent);
            spawn.position = position;
            return spawn;
        }

        private static void CreateCrack(Transform parent, Vector3 origin, Material hint)
        {
            var points = new[] { origin, origin + new Vector3(0.2f, 0.2f, 0f), origin + new Vector3(-0.04f, 0.44f, 0f),
                origin + new Vector3(0.16f, 0.66f, 0f), origin + new Vector3(-0.06f, 0.9f, 0f) };
            for (var i = 0; i < points.Length - 1; i++) CreateCrackSegment(parent, $"YellowMemoryCrack_{i}", points[i], points[i + 1], hint);
        }

        private static void CreateCrackSegment(Transform parent, string name, Vector3 a, Vector3 b, Material material)
        {
            var direction = b - a;
            var crack = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent, (a + b) * 0.5f,
                Quaternion.LookRotation(direction, Vector3.up).eulerAngles, new Vector3(0.055f, 0.025f, direction.magnitude));
            crack.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(crack.GetComponent<Collider>());
        }

        private static GameObject CreateEchoFigure(Transform parent, string name, Vector3 position, Material material, float height)
        {
            var figure = Child(name, parent);
            figure.localPosition = position;
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "SilhouetteBody";
            body.transform.SetParent(figure, false);
            body.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            body.transform.localScale = new Vector3(0.48f, height * 0.5f, 0.38f);
            body.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            figure.gameObject.SetActive(false);
            return figure.gameObject;
        }

        private static Transform Child(string name, Transform parent) => EditorAutomation.CreateGameObject(name, parent, Vector3.zero, Vector3.zero, Vector3.one).transform;

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent, position, Vector3.zero, scale);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void BuildLighting(Transform parent)
        {
            var lightObject = Child("DirectionalLight", parent);
            lightObject.localEulerAngles = new Vector3(48f, -32f, 0f);
            var light = lightObject.gameObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(0.82f, 0.88f, 1f);
        }

        private static Collider FindColliderNamed(Transform parent, string name) => parent.Find(name).GetComponent<Collider>();

        private static void ClearScene(Scene scene)
        {
            foreach (var gameObject in scene.GetRootGameObjects()) Object.DestroyImmediate(gameObject);
        }

        private static DialogueSequence LoadOrCreateDialogue()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DialogueSequence>(DialoguePath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<DialogueSequence>();
            var serialized = new SerializedObject(asset);
            var lines = serialized.FindProperty("lines");
            lines.arraySize = 3;
            SetLine(lines.GetArrayElementAtIndex(0), "Child", "The lamp... it still knows us.", 3.2f);
            SetLine(lines.GetArrayElementAtIndex(1), "Grandfather", "Careful. Both of you stay where I can see you.", 4f);
            SetLine(lines.GetArrayElementAtIndex(2), "Child", "I thought there were only two of us.", 3f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(asset, DialoguePath);
            return asset;
        }

        private static void SetLine(SerializedProperty line, string speaker, string text, float duration)
        {
            line.FindPropertyRelative("speaker").stringValue = speaker;
            line.FindPropertyRelative("text").stringValue = text;
            line.FindPropertyRelative("duration").floatValue = duration;
        }

        private static MemoryFragmentDefinition LoadOrCreateFragment()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(FragmentPath);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            asset.Configure("memory_boathouse_01", "Two Shadows", "A grandfather, a child, and one quiet silhouette the light cannot explain.");
            AssetDatabase.CreateAsset(asset, FragmentPath);
            return asset;
        }

        private static Material LoadOrCreateMaterial(string path, Color baseColor, Color emission, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidDataException("URP/Lit is not available.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", baseColor);
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
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private readonly struct RecallableParts
        {
            public readonly MemoryRecallable Component;
            public readonly GameObject Present;
            public readonly GameObject Preview;
            public readonly GameObject Materialized;
            public readonly GameObject Target;
            public readonly float RootOffsetX;
            public readonly float RootOffsetY;
            public readonly float RootOffsetZ;
            public RecallableParts(MemoryRecallable component, GameObject present, GameObject preview, GameObject materialized, GameObject target)
            {
                Component = component; Present = present; Preview = preview; Materialized = materialized; Target = target;
                RootOffsetX = component.transform.position.x;
                RootOffsetY = component.transform.position.y;
                RootOffsetZ = component.transform.position.z;
            }
        }

        private readonly struct NarrativeParts
        {
            public readonly MemoryEchoSequence Echo;
            public readonly ExitUnlocker Exit;
            public NarrativeParts(MemoryEchoSequence echo, ExitUnlocker exit) { Echo = echo; Exit = exit; }
        }
    }
}
