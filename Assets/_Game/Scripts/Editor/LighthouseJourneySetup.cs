using System;
using System.Collections.Generic;
using System.IO;
using LightRemembers.Interaction;
using LightRemembers.Environment;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using LightRemembers.UI;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LightRemembers.Editor
{
    /// <summary>Builds the standalone, playable Lighthouse Journey presentation level through Editor APIs.</summary>
    public static class LighthouseJourneySetup
    {
        public const string ScenePath = "Assets/_Game/Scenes/Prototype/LighthouseJourney.unity";
        private const string Nature = "Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/Models/";
        private const string Village = "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit/Models/";
        private const string KayKit = "Assets/ThirdParty/KayKit/ForestNaturePack/Models/";
        private const string FragmentPath = "Assets/_Game/Art/MemoryFragments/memory_archive_04.asset";
        private const string CharacterPath = "Assets/ThirdParty/Quaternius/UniversalBaseCharacters/Superhero_Male_FullBody.fbx";
        private const string AnimationPath = "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx";
        private const string CharacterControllerPath = "Assets/_Game/Art/Animation/LighthousePlayer.controller";
        private const string HouseGableMeshPath = "Assets/_Game/Art/Showcase/LR_HouseGable.asset";
        private const string MemoryParticleMaterialPath = "Assets/_Game/Art/Materials/LR_MemoryLightParticles.mat";
        private const string MemoryMoteTexturePath = "Assets/_Game/Art/Textures/LR_MemoryMote.asset";
        private const string MeadowMaterialFolder = "Assets/_Game/Art/Materials/Meadow";

        [MenuItem("Light Remembers/Showcase/Build Lighthouse Journey")]
        public static void Build()
        {
            ConfigureAnimationClipLoops();
            var neutral = CreateSceneMaterial("LR_Showcase_Stone", new Color(0.36f, 0.4f, 0.44f),
                new Color(0.025f, 0.035f, 0.045f));
            var ruins = LoadMaterial("Assets/_Game/Art/Materials/PrototypeBridgeRuins.mat");
            var memory = LoadMaterial("Assets/_Game/Art/Materials/PrototypeMemorySolid.mat");
            var ghost = LoadMaterial("Assets/_Game/Art/Materials/PrototypeMemoryGhost.mat");
            var gold = LoadMaterial("Assets/_Game/Art/Materials/PrototypeMemoryHintYellow.mat");
            var lantern = LoadMaterial("Assets/_Game/Art/Materials/PrototypeLantern.mat");
            var hollowMaterial = LoadMaterial("Assets/_Game/Art/Materials/HollowEncounterVoid.mat");
            var grass = CreateSceneMaterial("LR_Showcase_Grass", new Color(0.27f, 0.38f, 0.23f),
                new Color(0.035f, 0.055f, 0.018f));
            var earth = CreateSceneMaterial("LR_Showcase_Earth", new Color(0.29f, 0.25f, 0.18f),
                new Color(0.018f, 0.012f, 0.006f));
            SetShowcaseMaterial(grass, new Color(0.3f, 0.43f, 0.2f), new Color(0.045f, 0.065f, 0.018f));
            SetShowcaseMaterial(earth, new Color(0.39f, 0.27f, 0.15f), new Color(0.02f, 0.012f, 0.006f));
            var soilNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit/Textures/T_Noise_Terrain.png");
            if (soilNoise != null)
            {
                if (earth.HasProperty("_BaseMap")) earth.SetTexture("_BaseMap", soilNoise);
                else earth.mainTexture = soilNoise;
                earth.mainTextureScale = new Vector2(14f, 14f);
                EditorUtility.SetDirty(earth);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SetActiveScene(scene);

            var root = Group("LighthouseJourney", null);
            var environment = Group("Environment", root);
            var memoryRoot = Group("MemoryPuzzles", root);
            var gameplay = Group("Gameplay", root);
            var lighting = Group("Lighting", root);
            var story = Group("Story", root);
            var checkpoints = Group("Checkpoints", gameplay);
            var resets = Group("ResetVolumes", gameplay);

            ConfigureWorldLighting(lighting);
            BuildLandscape(environment, neutral, ruins, gold, lantern, grass, earth);

            var spawn = Group("PlayerSpawn", gameplay);
            spawn.position = new Vector3(0f, 0.1f, -4.5f);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MilestoneTwoSetup.PlayerPrefabPath);
            if (playerPrefab == null)
                throw new FileNotFoundException("PlayerPrototype prefab is required to build LighthouseJourney.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            var reader = player.GetComponent<PlayerInputReader>();
            var movement = player.GetComponent<PlayerMovement>();
            var camera = MilestoneTwoSetup.CreateCameraRig(gameplay, player.transform.Find("CameraTarget"), reader);
            var orbit = camera.transform.parent.Find("ThirdPersonCamera").GetComponent<CinemachineOrbitalFollow>();
            var verticalAxis = orbit.VerticalAxis;
            verticalAxis.Value = 17.5f;
            orbit.VerticalAxis = verticalAxis;
            orbit.Radius = 4.5f;
            var deoccluder = camera.transform.parent.Find("ThirdPersonCamera").GetComponent<CinemachineDeoccluder>();
            deoccluder.enabled = true;
            deoccluder.AvoidObstacles.Enabled = true;
            deoccluder.AvoidObstacles.CameraRadius = 0.34f;
            deoccluder.AvoidObstacles.Damping = 0.12f;
            deoccluder.AvoidObstacles.DampingWhenOccluded = 0.04f;
            deoccluder.CollideAgainst = ~0;
            deoccluder.TransparentLayers = 0;
            deoccluder.IgnoreTag = "Player";
            player.tag = "Player";
            movement.Configure(player.GetComponent<CharacterController>(), reader, camera.transform);
            ConfigurePlayerCharacter(player, reader);
            BindMeadowToPlayer(player);
            var lightOrigin = player.transform.Find("MemoryLightOrigin");
            PrepareMemoryLightHand(player, reader, lightOrigin);
            var memoryLight = player.GetComponent<PlayerMemoryLight>();
            ConfigureMemoryLightBeam(lightOrigin);
            memoryLight.Configure(reader, lightOrigin, camera.transform, lightOrigin.GetComponent<Light>());
            var memoryMotes = lightOrigin.Find("MemoryLight_Motes");
            if (memoryMotes != null)
                ConfigureMemoryMotes(memoryMotes.GetComponent<ParticleSystem>(),
                    player.GetComponent<MemoryLightBeamVisual>());
            memoryLight.MaximumRange = 19f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(memoryLight);
            EditorUtility.SetDirty(memoryLight);
            var interactor = player.GetComponent<PlayerInteractor>();
            interactor.Configure(reader, player.transform);
            var respawner = player.AddComponent<CheckpointRespawner>();
            var integrity = player.AddComponent<MemoryIntegrity>();
            integrity.Configure(3);

            var focusRoot = Group("JourneyRecallFocus", memoryRoot);
            var focus = focusRoot.gameObject.AddComponent<MemoryFocusGroup>();
            var subtitles = BuildSubtitleCanvas(root);
            var hud = BuildHud(root, player, reader, movement, interactor, memoryLight);
            var flow = gameplay.gameObject.AddComponent<PresentationLevelFlow>();
            flow.Configure(reader, movement, interactor, memoryLight, hud.Gameplay, hud.Title, hud.StartButton,
                hud.ObjectiveCard, hud.ObjectiveTitle, hud.ObjectiveDescription, hud.Tracker,
                hud.Recall, hud.Echo, hud.Forget, hud.Ending, hud.EndingTitle);
            EditorUtility.SetDirty(flow);
            gameplay.gameObject.AddComponent<MemoryAbilityBootstrap>().ConfigureDevelopmentUnlocks(true, false);
            var collector = Group("MemoryFragmentCollector", gameplay).gameObject.AddComponent<MemoryFragmentCollector>();
            var fragment = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(FragmentPath);

            var bridge = BuildRecallBridge(memoryRoot, neutral, ruins, memory, ghost, gold, focus);
            var combined = BuildRecallEchoFerry(memoryRoot, neutral, ruins, memory, ghost, gold, focus);
            var checkpointsData = BuildCheckpoints();
            BuildResetVolumes(resets);

            var sanctuary = BuildSanctuary(environment, lantern);
            var hollowRemnantsRoot = Group("HollowMemoryRemnants", memoryRoot);
            var remnantRecall = BuildRemnant(hollowRemnantsRoot, "RecallMemoryRemnant", MemoryAbility.Recall,
                new Vector3(4.4f, 1.05f, 102.5f), memory, gold);
            var remnantEcho = BuildRemnant(hollowRemnantsRoot, "EchoMemoryRemnant", MemoryAbility.Echo,
                new Vector3(4.4f, 1.05f, 105.5f), memory, gold);
            var resetter = Group("JourneyMemoryResetter", gameplay).gameObject.AddComponent<MemoryEncounterResetter>();
            resetter.Configure(new[] { bridge.Recall, combined.Recall }, new[] { combined.Echo }, new[] { focus },
                new[] { remnantRecall, remnantEcho });

            var corruption = player.AddComponent<MemoryCorruptionController>();
            var collapse = player.AddComponent<MemoryCollapseHandler>();
            var fade = BuildCollapseOverlay(root);
            corruption.Configure(integrity, remnantRecall, remnantEcho, collapse, resetter, sanctuary, subtitles);
            collapse.Configure(reader, movement, interactor, memoryLight, respawner, integrity, corruption,
                resetter, fade, 0.38f, 0.45f, 0.55f);
            remnantRecall.Configure(MemoryAbility.Recall, corruption, remnantRecall.GetComponentsInChildren<Renderer>(true),
                remnantRecall.GetComponent<Light>(), remnantRecall.transform.Find("AbilityLabel").GetComponent<TextMesh>());
            remnantEcho.Configure(MemoryAbility.Echo, corruption, remnantEcho.GetComponentsInChildren<Renderer>(true),
                remnantEcho.GetComponent<Light>(), remnantEcho.transform.Find("AbilityLabel").GetComponent<TextMesh>());
            sanctuary.Configure(4.8f, sanctuary.GetComponentInChildren<Light>(), corruption);

            var hollow = BuildHollow(environment, player.transform, camera, memoryLight, corruption, sanctuary,
                hollowMaterial, memory, gold);
            var wake = Trigger("HollowWakeTrigger", environment, new Vector3(0f, 1f, 96f), new Vector3(10f, 2.5f, 3f));
            wake.AddComponent<HollowWakeTrigger>().Configure(hollow);

            BuildForgetArchive(environment, memoryRoot, player, subtitles, memory, ghost, gold, neutral, flow,
                collector, fragment);
            BuildObjectiveTriggers(gameplay, flow);

            CreateCheckpoint(checkpoints, "Checkpoint_Start", checkpointsData[0]);
            CreateCheckpoint(checkpoints, "Checkpoint_AfterBridge", checkpointsData[1]);
            CreateCheckpoint(checkpoints, "Checkpoint_AfterFerry", checkpointsData[2]);
            CreateCheckpoint(checkpoints, "Checkpoint_Sanctuary", checkpointsData[3]);
            CreateCheckpoint(checkpoints, "Checkpoint_Archive", checkpointsData[4]);
            CreateCheckpoint(checkpoints, "Checkpoint_Lighthouse", checkpointsData[5]);

            BuildDecorativeVillage(environment, gold);
            CreateForestDressing(environment);
            BuildLighthouseLandmark(environment, neutral, gold, lantern);
            CreateGuideFigure(story, memory, gold);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.38f, 0.43f, 0.5f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.13f, 0.2f, 0.29f);
            RenderSettings.fogDensity = 0.0011f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettingsFirst(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.SetActiveScene(scene);
            Debug.Log("Lighthouse Journey built as the standalone opening level. Only this scene is open to prevent duplicate players.");
        }

        [MenuItem("Light Remembers/Showcase/Refresh Memory Light Visuals")]
        public static void RefreshMemoryLightVisuals()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = GameObject.Find("PlayerPrototype");
            var origin = player != null ? player.transform.Find("MemoryLightOrigin") : null;
            if (player == null || origin == null)
                throw new InvalidOperationException("PlayerPrototype and its MemoryLightOrigin must exist in LighthouseJourney.");

            player.transform.rotation = Quaternion.identity;
            var visualRoot = player.transform.Find("PlayerCharacterVisual");
            if (visualRoot != null)
                visualRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var reader = player.GetComponent<PlayerInputReader>();
            PrepareMemoryLightHand(player, reader, origin);

            var visual = player.GetComponent<MemoryLightBeamVisual>() ?? player.AddComponent<MemoryLightBeamVisual>();
            visual.Configure(origin, 19f, 28f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);

            var motes = origin.Find("MemoryLight_Motes");
            if (motes != null)
            {
                var system = motes.GetComponent<ParticleSystem>();
                ConfigureMemoryMotes(system, visual);
                var renderer = motes.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = LoadOrCreateMemoryParticleMaterial();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Light Remembers/Showcase/Refresh Lighthouse Vegetation")]
        public static void RefreshLandscapeVegetation()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var ground = GameObject.Find("IslandGround");
            if (ground == null || scene.path != ScenePath)
                throw new InvalidOperationException("Open LighthouseJourney before refreshing its vegetation.");

            var previous = GameObject.Find("MeadowDetail_ImportedNature");
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous);

            var land = new[]
            {
                new GroundPatch("OpeningShore", new Vector3(0f, 0f, 5f), new Vector3(23f, 0f, 26f)),
                new GroundPatch("BridgeFarBank", new Vector3(0f, 0f, 37f), new Vector3(18f, 0f, 22f)),
                new GroundPatch("FerryNearBank", new Vector3(0f, 0f, 52f), new Vector3(15f, 0f, 12f)),
                new GroundPatch("FerryFarBank", new Vector3(0f, 0f, 77f), new Vector3(17f, 0f, 22f)),
                new GroundPatch("HollowCauseway", new Vector3(0f, 0f, 99f), new Vector3(12f, 0f, 24f)),
                new GroundPatch("ArchiveCourtyard", new Vector3(0f, 0f, 122f), new Vector3(20f, 0f, 27f)),
                new GroundPatch("LighthouseApproach", new Vector3(0f, 0f, 147f), new Vector3(20f, 0f, 23f))
            };
            ScatterGroundPlants(ground.transform, land);
            BuildGroundCover(ground.transform, land);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Light Remembers/Showcase/Refresh Lighthouse Presentation Polish")]
        public static void RefreshPresentationPolish()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException($"Open {ScenePath} before refreshing its presentation settings.");

            var roots = scene.GetRootGameObjects();
            var player = FindInRoots(roots, "PlayerPrototype");
            var camera = FindInRoots(roots, "ThirdPersonCamera");
            var ground = FindInRoots(roots, "IslandGround");
            var cover = FindInRoots(roots, "MeadowGroundCover")?.GetComponent<MeadowGroundCover>();
            if (player == null || camera == null || ground == null || cover == null)
                throw new InvalidOperationException("LighthouseJourney is missing its player, camera, ground, or meadow cover.");

            player.tag = "Player";
            var deoccluder = camera.GetComponent<CinemachineDeoccluder>();
            deoccluder.enabled = true;
            deoccluder.CollideAgainst = ~0;
            deoccluder.TransparentLayers = 0;
            deoccluder.IgnoreTag = "Player";
            var avoidance = deoccluder.AvoidObstacles;
            avoidance.Enabled = true;
            avoidance.CameraRadius = 0.34f;
            avoidance.Damping = 0.12f;
            avoidance.DampingWhenOccluded = 0.04f;
            deoccluder.AvoidObstacles = avoidance;

            var reader = player.GetComponent<PlayerInputReader>();
            var origin = player.transform.Find("MemoryLightOrigin");
            var model = player.transform.Find("PlayerCharacterVisual");
            player.GetComponent<MemoryLightArmPose>()?.Configure(reader, model, origin);
            cover.SetDensityAndRebuild(22f);

            var earth = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Materials/LR_Showcase_Earth.mat");
            var soilNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/ThirdParty/Quaternius/MedievalVillageMegaKit/Textures/T_Noise_Terrain.png");
            if (earth != null && soilNoise != null)
            {
                if (earth.HasProperty("_BaseMap")) earth.SetTexture("_BaseMap", soilNoise);
                else earth.mainTexture = soilNoise;
                earth.mainTextureScale = new Vector2(14f, 14f);
                foreach (var renderer in ground.GetComponentsInChildren<Renderer>(true))
                    if (renderer.name.EndsWith("_GrassTop", StringComparison.Ordinal))
                        renderer.sharedMaterial = earth;
                EditorUtility.SetDirty(earth);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Lighthouse Journey presentation settings refreshed without rebuilding the scene.");
        }

        private static void PrepareMemoryLightHand(GameObject player, PlayerInputReader reader, Transform origin)
        {
            if (player == null || origin == null) return;
            var modelRoot = player.transform.Find("PlayerCharacterVisual") ?? player.transform;
            var hand = FindDescendant(modelRoot, "hand_r");
            if (hand == null)
                throw new InvalidOperationException("The showcase character is missing its right hand bone.");
            origin.position = hand.position;

            var armPose = player.GetComponent<MemoryLightArmPose>() ?? player.AddComponent<MemoryLightArmPose>();
            armPose.Configure(reader, modelRoot, origin);
            var palmGlow = origin.Find("MemoryLight_PalmGlow");
            if (palmGlow == null)
            {
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.name = "MemoryLight_PalmGlow";
                glow.transform.SetParent(origin, false);
                glow.transform.localPosition = Vector3.zero;
                glow.transform.localScale = Vector3.one * 0.055f;
                var collider = glow.GetComponent<Collider>();
                if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                glow.GetComponent<Renderer>().sharedMaterial = LoadOrCreatePalmMaterial();
                palmGlow = glow.transform;
            }

            var palmRenderer = palmGlow.GetComponent<Renderer>();
            var palmLightObject = palmGlow.Find("PalmLight");
            if (palmLightObject == null)
            {
                var lightObject = new GameObject("PalmLight", typeof(Light));
                lightObject.transform.SetParent(palmGlow, false);
                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.42f, 0.8f, 1f);
                light.intensity = 0.45f;
                light.range = 1.2f;
                light.shadows = LightShadows.None;
            }
            var feedback = player.GetComponent<MemoryLightPalmFeedback>() ?? player.AddComponent<MemoryLightPalmFeedback>();
            feedback.Configure(reader, palmRenderer, palmGlow.GetComponentInChildren<Light>(true));
            palmRenderer.enabled = false;
            var palmLight = palmGlow.GetComponentInChildren<Light>(true);
            if (palmLight != null) palmLight.enabled = false;
        }

        private static void BindMeadowToPlayer(GameObject player)
        {
            var cover = GameObject.Find("MeadowGroundCover")?.GetComponent<MeadowGroundCover>();
            if (cover != null && player != null)
                cover.BindPlayer(player.transform, player.GetComponent<PlayerInputReader>(),
                    player.GetComponent<CharacterController>());
        }

        private static void ConfigurePlayerCharacter(GameObject player, PlayerInputReader reader)
        {
            var characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            if (characterModel == null)
            {
                Debug.LogError($"Showcase character asset is missing: {CharacterPath}");
                return;
            }

            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            var visualRoot = new GameObject("PlayerCharacterVisual").transform;
            visualRoot.SetParent(player.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(characterModel, SceneManager.GetActiveScene());
            model.name = "Superhero_Male_FullBody";
            model.transform.SetParent(visualRoot, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            visualRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var bounds = new Bounds(player.transform.position, Vector3.zero);
            var hasBounds = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            if (hasBounds && bounds.size.y > 0.01f)
            {
                visualRoot.localScale = Vector3.one * (1.78f / bounds.size.y);
                visualRoot.position += Vector3.down * (bounds.min.y - player.transform.position.y) * visualRoot.localScale.y;
            }

            var animator = model.GetComponentInChildren<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            foreach (var imported in AssetDatabase.LoadAllAssetsAtPath(CharacterPath))
                if (imported is Avatar avatar) { animator.avatar = avatar; break; }
            animator.runtimeAnimatorController = CreateCharacterAnimatorController();
            ApplyCharacterMaterials(model);
            var animationDriver = player.GetComponent<PlayerAnimationDriver>() ?? player.AddComponent<PlayerAnimationDriver>();
            animationDriver.Configure(reader, player.GetComponent<CharacterController>(), animator);
        }

        private static void ApplyCharacterMaterials(GameObject model)
        {
            var body = CreateCharacterMaterial("LR_Character_Body", "Textures/T_Superhero_Male_Ligh.png",
                new Color(0.92f, 0.95f, 1f));
            var hair = CreateCharacterMaterial("LR_Character_Hair", "Textures/T_Hair_1_BaseColor.png",
                new Color(0.18f, 0.2f, 0.23f));
            var eyes = CreateCharacterMaterial("LR_Character_Eyes", "Textures/T_Eye_Brown.png", Color.white);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var original = renderer.sharedMaterials;
                var replacements = new Material[original.Length];
                for (var i = 0; i < original.Length; i++)
                {
                    var name = original[i] != null ? original[i].name : string.Empty;
                    replacements[i] = name.IndexOf("Hair", StringComparison.OrdinalIgnoreCase) >= 0 ? hair
                        : name.IndexOf("Eye", StringComparison.OrdinalIgnoreCase) >= 0 ? eyes : body;
                }
                renderer.sharedMaterials = replacements;
            }
        }

        private static Material CreateCharacterMaterial(string name, string textureRelativePath, Color tint)
        {
            var path = "Assets/_Game/Art/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = tint };
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/ThirdParty/Quaternius/UniversalBaseCharacters/" + textureRelativePath);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                else material.mainTexture = texture;
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static RuntimeAnimatorController CreateCharacterAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(CharacterControllerPath);
            const string folder = "Assets/_Game/Art/Animation";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Game/Art", "Animation");
            var controller = existing ?? AnimatorController.CreateAnimatorControllerAtPath(CharacterControllerPath);
            EnsureAnimatorParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            EnsureAnimatorParameter(controller, "Grounded", AnimatorControllerParameterType.Bool);
            EnsureAnimatorParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);

            var importedClips = AssetDatabase.LoadAllAssetsAtPath(AnimationPath);
            AnimationClip FindClip(string suffix)
            {
                var fullClipName = "|" + suffix;
                foreach (var asset in importedClips)
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal) &&
                        clip.name.EndsWith(fullClipName, StringComparison.Ordinal)) return clip;
                return null;
            }
            var idle = FindClip("Idle_Loop");
            var walk = FindClip("Walk_Loop");
            var jog = FindClip("Jog_Fwd_Loop");
            var sprint = FindClip("Sprint_Loop");
            var jumpStart = FindClip("Jump_Start");
            var jumpLoop = FindClip("Jump_Loop");
            var jumpLand = FindClip("Jump_Land");
            if (idle == null || walk == null || jog == null || sprint == null || jumpStart == null || jumpLoop == null)
            {
                Debug.LogWarning("Universal Animation Library is missing expected idle, walk, jog, sprint or jump clips.");
                return controller;
            }

            var stateMachine = controller.layers[0].stateMachine;
            foreach (var transition in stateMachine.anyStateTransitions)
                stateMachine.RemoveAnyStateTransition(transition);
            foreach (var state in stateMachine.states)
                stateMachine.RemoveState(state.state);
            foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(CharacterControllerPath))
                if (subAsset is BlendTree oldTree)
                    AssetDatabase.RemoveObjectFromAsset(oldTree);

            var blendTree = new BlendTree
            {
                name = "ShowcaseLocomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "MoveSpeed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            blendTree.AddChild(idle, 0f);
            blendTree.AddChild(walk, 0.38f);
            blendTree.AddChild(jog, 0.7f);
            blendTree.AddChild(sprint, 1f);
            var locomotion = stateMachine.AddState("Locomotion", new Vector3(280f, 100f));
            locomotion.motion = blendTree;
            stateMachine.defaultState = locomotion;
            var takeoff = stateMachine.AddState("JumpStart", new Vector3(560f, 40f));
            takeoff.motion = jumpStart;
            var airborne = stateMachine.AddState("JumpLoop", new Vector3(820f, 40f));
            airborne.motion = jumpLoop;
            var landing = jumpLand != null ? stateMachine.AddState("JumpLand", new Vector3(1080f, 40f)) : locomotion;
            if (jumpLand != null)
                landing.motion = jumpLand;
            var enterJump = stateMachine.AddAnyStateTransition(takeoff);
            enterJump.hasExitTime = false;
            enterJump.duration = 0.08f;
            enterJump.canTransitionToSelf = false;
            enterJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
            var enterLoop = takeoff.AddTransition(airborne);
            enterLoop.hasExitTime = true;
            enterLoop.exitTime = 0.82f;
            enterLoop.duration = 0.08f;
            enterLoop.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
            var earlyLand = takeoff.AddTransition(landing);
            earlyLand.hasExitTime = false;
            earlyLand.duration = 0.1f;
            earlyLand.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            var land = airborne.AddTransition(landing);
            land.hasExitTime = false;
            land.duration = 0.12f;
            land.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
            if (jumpLand != null)
            {
                var finishLanding = landing.AddTransition(locomotion);
                finishLanding.hasExitTime = true;
                finishLanding.exitTime = 0.88f;
                finishLanding.duration = 0.12f;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureAnimatorParameter(AnimatorController controller, string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            foreach (var parameter in controller.parameters)
                if (parameter.name == parameterName)
                    return;
            controller.AddParameter(parameterName, parameterType);
        }

        private static void ConfigureAnimationClipLoops()
        {
            var importer = AssetImporter.GetAtPath(AnimationPath) as ModelImporter;
            if (importer == null)
                return;

            var clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
                clips = importer.defaultClipAnimations;

            var changed = false;
            foreach (var clip in clips)
            {
                var isLoop = clip.name.EndsWith("Idle_Loop", StringComparison.Ordinal) ||
                             clip.name.EndsWith("Walk_Loop", StringComparison.Ordinal) ||
                             clip.name.EndsWith("Jog_Fwd_Loop", StringComparison.Ordinal) ||
                             clip.name.EndsWith("Sprint_Loop", StringComparison.Ordinal) ||
                             clip.name.EndsWith("Jump_Loop", StringComparison.Ordinal);
                if (clip.loopTime == isLoop && (!isLoop || clip.loopPose))
                    continue;
                clip.loopTime = isLoop;
                if (isLoop)
                    clip.loopPose = true;
                changed = true;
            }

            if (!changed)
                return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static void ConfigureMemoryLightBeam(Transform origin)
        {
            var beam = origin.GetComponent<Light>();
            beam.type = LightType.Spot;
            beam.range = 19f;
            beam.spotAngle = 28f;
            beam.innerSpotAngle = 12f;
            beam.intensity = 2.2f;
            beam.shadows = LightShadows.None;
            beam.enabled = false;
            var particles = new GameObject("MemoryLight_Motes", typeof(ParticleSystem), typeof(ParticleSystemRenderer));
            particles.transform.SetParent(origin, false);
            var system = particles.GetComponent<ParticleSystem>();
            ConfigureMemoryMotes(system, null);
            var particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = LoadOrCreateMemoryParticleMaterial();
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Material LoadOrCreateMemoryParticleMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MemoryParticleMaterialPath);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP particle unlit shader is unavailable for the Memory Light motes.");

            var texture = LoadOrCreateMemoryMoteTexture();
            if (existing != null)
            {
                if (existing.HasProperty("_BaseMap")) existing.SetTexture("_BaseMap", texture);
                else existing.mainTexture = texture;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var material = new Material(shader) { name = "LR_MemoryLightParticles" };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            else material.mainTexture = texture;
            AssetDatabase.CreateAsset(material, MemoryParticleMaterialPath);
            return material;
        }

        private static Texture2D LoadOrCreateMemoryMoteTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(MemoryMoteTexturePath);
            if (existing != null)
                return existing;

            const string textureFolder = "Assets/_Game/Art/Textures";
            if (!AssetDatabase.IsValidFolder(textureFolder))
                AssetDatabase.CreateFolder("Assets/_Game/Art", "Textures");

            const int size = 32;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                var dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                var radius = Mathf.Sqrt(dx * dx + dy * dy);
                var alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - radius) / 0.48f)) * 0.72f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "LR_MemoryMote",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            AssetDatabase.CreateAsset(texture, MemoryMoteTexturePath);
            return texture;
        }

        private static void BuildLandscape(Transform parent, Material neutral, Material ruins, Material gold,
            Material lantern, Material grass, Material earth)
        {
            var ground = Group("IslandGround", parent);
            var land = new[]
            {
                new GroundPatch("OpeningShore", new Vector3(0f, -0.35f, 5f), new Vector3(23f, 0.7f, 26f)),
                new GroundPatch("BridgeFarBank", new Vector3(0f, -0.35f, 37f), new Vector3(18f, 0.7f, 22f)),
                new GroundPatch("FerryNearBank", new Vector3(0f, -0.35f, 52f), new Vector3(15f, 0.7f, 12f)),
                new GroundPatch("FerryFarBank", new Vector3(0f, -0.35f, 77f), new Vector3(17f, 0.7f, 22f)),
                new GroundPatch("HollowCauseway", new Vector3(0f, -0.35f, 99f), new Vector3(12f, 0.7f, 24f)),
                new GroundPatch("ArchiveCourtyard", new Vector3(0f, -0.35f, 122f), new Vector3(20f, 0.7f, 27f)),
                new GroundPatch("LighthouseApproach", new Vector3(0f, -0.35f, 147f), new Vector3(20f, 0.7f, 23f))
            };
            foreach (var patch in land)
            {
                Block(patch.Name, ground, patch.Center, patch.Size, neutral);
                var turf = Block(patch.Name + "_GrassTop", ground,
                    new Vector3(patch.Center.x, 0.012f, patch.Center.z),
                    new Vector3(patch.Size.x - 0.08f, 0.025f, patch.Size.z - 0.08f), earth);
                UnityEngine.Object.DestroyImmediate(turf.GetComponent<Collider>());
                CreatePavedTrail(ground, patch);
            }

            Block("OuterSea", ground, new Vector3(0f, -8.4f, 70f), new Vector3(400f, 1f, 400f),
                CreateSceneMaterial("LR_Showcase_Sea", new Color(0.025f, 0.12f, 0.19f), new Color(0.025f, 0.17f, 0.24f)));
            Block("CliffRidge_West", ground, new Vector3(-14f, 2.4f, 70f), new Vector3(7f, 7f, 165f), ruins);
            Block("CliffRidge_East", ground, new Vector3(14f, 2.4f, 70f), new Vector3(7f, 7f, 165f), ruins);

            BuildRuinMarker(ground, "RecallBridgeTarget", new Vector3(0f, 1.05f, 17.1f), ruins, gold,
                new Vector3(0.75f, 2.1f, 0.55f));
            BuildRuinMarker(ground, "EchoFerryTarget", new Vector3(0f, 1.2f, 58f), ruins, gold,
                new Vector3(0.82f, 2.4f, 0.6f));

            for (var z = 6f; z < 154f; z += 14f)
            {
                CreateLantern(ground, new Vector3(-2.8f, 0.7f, z), lantern);
                CreateLantern(ground, new Vector3(2.8f, 0.7f, z), lantern);
            }

            CreatePathSign(ground, "SHORE OF OLD LIGHT", new Vector3(-8f, 1.3f, 9.5f), gold);
            CreatePathSign(ground, "THE QUIET WOOD", new Vector3(0f, 1.3f, 82.5f), gold);
            CreatePathSign(ground, "THE FORGOTTEN ARCHIVE", new Vector3(0f, 1.3f, 111.5f), gold);
            ScatterGroundPlants(ground, land);
            BuildGroundCover(ground, land);
            SetLayerRecursively(ground.gameObject, LayerMask.NameToLayer("Ignore Raycast"));
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0) return;
            root.layer = layer;
            foreach (Transform child in root.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private static void CreatePavedTrail(Transform parent, GroundPatch patch)
        {
            var start = patch.Center.z - patch.Size.z * 0.5f + 1f;
            var end = patch.Center.z + patch.Size.z * 0.5f - 1f;
            for (var z = start; z <= end; z += 1.92f)
            for (var lane = 0; lane < 2; lane++)
            {
                SpawnModel(parent, Village + "Floor_UnevenBrick.fbx",
                    new Vector3(lane == 0 ? -0.98f : 0.98f, 0.025f, z), Quaternion.identity, Vector3.one);
            }
        }

        private static void ScatterGroundPlants(Transform parent, GroundPatch[] land)
        {
            var forest = Group("MeadowDetail_ImportedNature", parent);
            var smallPlants = new[]
            {
                Nature + "Grass_Common_Short.fbx", Nature + "Grass_Wispy_Short.fbx",
                Nature + "Grass_Common_Tall.fbx", Nature + "Flower_3_Group.fbx",
                Nature + "Flower_4_Group.fbx", Nature + "Fern_1.fbx",
                Nature + "Bush_Common.fbx", Nature + "Bush_Common_Flowers.fbx",
                KayKit + "Grass_1_A_Color1.fbx", KayKit + "Grass_2_D_Color1.fbx",
                KayKit + "Bush_1_A_Color1.fbx", KayKit + "Bush_2_A_Color1.fbx"
            };
            var random = new System.Random(241008);
            var count = 0;
            var swayingPlants = new List<Transform>();
            foreach (var patch in land)
            {
                var halfX = patch.Size.x * 0.5f - 0.7f;
                var halfZ = patch.Size.z * 0.5f - 0.8f;
                var plantsOnPatch = Mathf.RoundToInt(patch.Size.z * 1.6f);
                for (var i = 0; i < plantsOnPatch; i++)
                {
                    var x = RandomRange(random, -halfX, halfX);
                    var z = RandomRange(random, patch.Center.z - halfZ, patch.Center.z + halfZ);
                    if (Mathf.Abs(x) < 3.1f || IsNearHouse(x, z))
                        continue;

                    var path = smallPlants[random.Next(smallPlants.Length)];
                    var size = path.Contains("Fern_") ? RandomRange(random, 0.1f, 0.16f)
                        : path.Contains("Bush_") ? RandomRange(random, 0.24f, 0.4f)
                        : RandomRange(random, 0.13f, 0.24f);
                    var plant = SpawnModel(forest, path, new Vector3(x, 0.025f, z),
                        Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Vector3.one * size);
                    if (plant != null && (path.Contains("Grass_") || path.Contains("Flower_") || path.Contains("Fern_")))
                        swayingPlants.Add(plant.transform);
                    count++;
                }
            }
            forest.gameObject.AddComponent<GrassWindSway>().Configure(swayingPlants.ToArray(), 3.2f, 0.9f);
            Debug.Log($"Lighthouse Journey meadow dressing: {count} imported grass, flower, fern and bush instances.");
        }

        private static void BuildGroundCover(Transform parent, GroundPatch[] land)
        {
            var existing = parent.Find("MeadowGroundCover");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);

            var sourcePaths = new[]
            {
                Nature + "Grass_Common_Short.fbx",
                Nature + "Grass_Wispy_Short.fbx"
            };
            var meshes = new Mesh[sourcePaths.Length];
            var materials = new Material[sourcePaths.Length];
            for (var i = 0; i < sourcePaths.Length; i++)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePaths[i]);
                var filter = source != null ? source.GetComponentInChildren<MeshFilter>(true) : null;
                var renderer = source != null ? source.GetComponentInChildren<MeshRenderer>(true) : null;
                if (filter == null || renderer == null || filter.sharedMesh == null || renderer.sharedMaterial == null)
                    throw new InvalidOperationException($"Meadow grass mesh/material is missing: {sourcePaths[i]}");
                meshes[i] = filter.sharedMesh;
                materials[i] = LoadOrCreateInstancedMeadowMaterial(renderer.sharedMaterial, i);
            }

            var patches = new MeadowGroundCover.Patch[land.Length];
            for (var i = 0; i < land.Length; i++)
                patches[i] = new MeadowGroundCover.Patch(land[i].Center,
                    new Vector2(land[i].Size.x, land[i].Size.z));

            var cover = new GameObject("MeadowGroundCover");
            cover.transform.SetParent(parent, false);
            var player = GameObject.Find("PlayerPrototype");
            var component = cover.AddComponent<MeadowGroundCover>();
            component.Configure(meshes, materials, patches, player != null ? player.transform : null,
                player != null ? player.GetComponent<PlayerInputReader>() : null,
                player != null ? player.GetComponent<CharacterController>() : null, 22f);
            Debug.Log($"Instanced meadow ground cover configured with {component.BladeCount} short grass clumps; clear path and house approaches are preserved.");
        }

        private static Material LoadOrCreateInstancedMeadowMaterial(Material source, int index)
        {
            const string folder = "Assets/_Game/Art/Materials/Meadow";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Game/Art/Materials", "Meadow");
            var path = folder + $"/LR_MeadowGrass_{index}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source) { name = $"LR_MeadowGrass_{index}" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreatePalmMaterial()
        {
            const string path = "Assets/_Game/Art/Materials/LR_MemoryLightPalm.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "LR_MemoryLightPalm" };
                AssetDatabase.CreateAsset(material, path);
            }
            var tint = new Color(0.42f, 0.86f, 1f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else material.color = tint;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", tint * 1.8f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureMemoryMotes(ParticleSystem system, MemoryLightBeamVisual visual)
        {
            var main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.00045f, 0.0024f);
            main.startColor = new Color(0.68f, 0.94f, 1f, 0.55f);
            main.maxParticles = 240;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = false;

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.018f, 0.018f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.014f, 0.014f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.018f, 0.018f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.025f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 0.08f;
            noise.octaveCount = 1;
            noise.damping = true;

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.65f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.3f, 0.65f, 1f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.58f, 0.2f), new GradientAlphaKey(0.5f, 0.8f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var drift = system.GetComponent<MemoryLightMoteDrift>() ?? system.gameObject.AddComponent<MemoryLightMoteDrift>();
            drift.Configure(system, visual, 19f, 28f);
        }

        private static Transform FindDescendant(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName)
                    return child;
            return null;
        }

        private static bool IsNearHouse(float x, float z)
        {
            return (Mathf.Abs(x + 5.8f) < 3.1f && Mathf.Abs(z - 5.4f) < 3.1f) ||
                   (Mathf.Abs(x - 5.8f) < 3.1f && Mathf.Abs(z - 9f) < 3.1f) ||
                   (Mathf.Abs(x + 5.8f) < 3.1f && Mathf.Abs(z - 121.5f) < 3.1f) ||
                   (Mathf.Abs(x - 5.8f) < 3.1f && Mathf.Abs(z - 126.5f) < 3.1f);
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static RecallPuzzle BuildRecallBridge(Transform parent, Material neutral, Material ruins,
            Material memory, Material ghost, Material gold, MemoryFocusGroup focus)
        {
            var root = Group("BrokenBridge_RecallPuzzle", parent);
            root.position = new Vector3(0f, 0f, 23f);
            var present = Group("PresentState_Ruins", root);
            var target = Block("BridgeMemoryTarget", present, root.position + new Vector3(0f, 1.25f, -5.2f),
                new Vector3(0.8f, 2.5f, 0.6f), ruins);
            CreateCrack(target.transform, gold);
            Block("BrokenPost_West", present, root.position + new Vector3(-2.2f, 0.7f, -3.1f), new Vector3(0.5f, 1.4f, 0.6f), neutral);
            Block("BrokenPost_East", present, root.position + new Vector3(2.2f, 0.45f, 3.5f), new Vector3(0.5f, 0.9f, 0.6f), neutral);
            Block("ScatteredBridgeDebris", present, root.position + new Vector3(1.5f, 0.18f, -0.2f), new Vector3(1.8f, 0.3f, 1.1f), ruins);

            var memoryState = Group("MemoryState", root);
            var preview = Group("GhostPreview", memoryState);
            var previewBridge = Block("RememberedBridge_Ghost", preview, root.position,
                new Vector3(4.4f, 0.25f, 9.3f), ghost);
            UnityEngine.Object.DestroyImmediate(previewBridge.GetComponent<Collider>());
            var materialized = Group("MaterializedMemory", memoryState);
            Block("RememberedBridge", materialized, root.position,
                new Vector3(4.4f, 0.32f, 9.3f), memory);
            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryState.gameObject, preview.gameObject, materialized.gameObject, 34f);
            recall.ConfigureFocusGroup(focus);
            recall.RefreshStateObjects();
            return new RecallPuzzle(root, recall);
        }

        private static FerryPuzzle BuildRecallEchoFerry(Transform parent, Material neutral, Material ruins,
            Material memory, Material ghost, Material gold, MemoryFocusGroup focus)
        {
            var root = Group("RestoredFerry_RecallThenEcho", parent);
            root.position = new Vector3(0f, 0f, 58f);
            var present = Group("PresentState_Ruins", root);
            var target = Block("FerryMemoryTarget", present, root.position + new Vector3(0f, 1.15f, 0f),
                new Vector3(0.9f, 2.3f, 0.75f), ruins);
            CreateCrack(target.transform, gold);
            Block("FerryBrokenBeam", present, root.position + new Vector3(-1.4f, 0.25f, 0f), new Vector3(2.6f, 0.3f, 0.5f), neutral);

            var memoryState = Group("MemoryState", root);
            var recallPreview = Group("GhostPreview", memoryState);
            var previewDeck = Block("FerryDeck_Ghost", recallPreview, root.position,
                new Vector3(3.8f, 0.25f, 4f), ghost);
            UnityEngine.Object.DestroyImmediate(previewDeck.GetComponent<Collider>());
            var materialized = Group("MaterializedMemory", memoryState);
            var moving = Block("FerryDeck", materialized, new Vector3(0f, 0.1f, 56.3f),
                new Vector3(3.8f, 0.32f, 4f), memory);
            moving.transform.SetParent(materialized, true);
            var ride = Group("EchoRideSurface", moving.transform).gameObject;
            var rideCollider = ride.AddComponent<BoxCollider>();
            rideCollider.isTrigger = true;
            rideCollider.center = Vector3.up * 1f;
            rideCollider.size = new Vector3(4f, 2.5f, 4.2f);
            var rideSurface = ride.AddComponent<EchoRideSurface>();
            rideSurface.Configure(rideCollider);

            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryState.gameObject, recallPreview.gameObject,
                materialized.gameObject, 36f);
            recall.ConfigureFocusGroup(focus);
            recall.RefreshStateObjects();

            var pathParent = Group("Ferry_EchoPath", parent);
            var start = Group("Waypoint_Start", pathParent);
            start.position = new Vector3(0f, 0.1f, 56.3f);
            var end = Group("Waypoint_FarBank", pathParent);
            end.position = new Vector3(0f, 0.1f, 70.7f);
            var path = pathParent.gameObject.AddComponent<EchoPath>();
            path.Configure(new[] { start, end }, 6f, 1f, false);

            var motionPreview = Group("Ferry_MotionPreview", parent);
            motionPreview.gameObject.SetActive(false);
            var line = motionPreview.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPositions(new[] { start.position + Vector3.up * 0.7f, end.position + Vector3.up * 0.7f });
            line.startWidth = line.endWidth = 0.12f;
            line.sharedMaterial = ghost;
            CreatePathMarker(motionPreview, "FerryMotion_Start", start.position + Vector3.up * 0.7f, ghost);
            CreatePathMarker(motionPreview, "FerryMotion_End", end.position + Vector3.up * 0.7f, ghost);

            var echo = root.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path, moving.transform, motionPreview.gameObject, rideSurface,
                moving.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureRecallRequirement(recall);
            root.gameObject.AddComponent<MemoryComposite>().Configure(recall, echo);
            return new FerryPuzzle(root, recall, echo);
        }

        private static HollowController BuildHollow(Transform parent, Transform player, Camera camera,
            PlayerMemoryLight memoryLight, MemoryCorruptionController corruption, SanctuaryLight sanctuary,
            Material hollowMaterial, Material memory, Material gold)
        {
            var hollowRoot = Group("HollowEncounter", parent);
            var observation = Group("HollowLookout", hollowRoot);
            observation.position = new Vector3(-4.8f, 0f, 103f);
            var returnPoint = Group("HollowReturnToDarkness", hollowRoot);
            returnPoint.position = new Vector3(0f, 0.1f, 110f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "TheHollow";
            body.transform.SetParent(hollowRoot, false);
            body.transform.SetPositionAndRotation(new Vector3(0f, 0.9f, 106f), Quaternion.identity);
            body.transform.localScale = new Vector3(0.78f, 1.2f, 0.78f);
            UnityEngine.Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            var controller = body.AddComponent<CharacterController>();
            controller.height = 2.25f;
            controller.radius = 0.38f;
            controller.center = Vector3.up * 1.1f;
            body.GetComponent<Renderer>().sharedMaterial = hollowMaterial;
            var threat = body.AddComponent<HollowController>();
            threat.Configure(player, camera, memoryLight, corruption, sanctuary, returnPoint,
                new[] { observation }, body.GetComponentsInChildren<Renderer>(true), Array.Empty<GameObject>());
            threat.ConfigurePerceptionForget(1.1f, 4f);

            BuildBrokenArch(hollowRoot, memory, gold);
            return threat;
        }

        private static void BuildForgetArchive(Transform environment, Transform memoryRoot, GameObject player,
            SubtitlePresenter subtitles, Material memory, Material ghost, Material gold, Material stone,
            PresentationLevelFlow flow, MemoryFragmentCollector collector, MemoryFragmentDefinition fragment)
        {
            var archive = Group("ForgottenArchive", environment);
            BuildArchiveRoom(archive, stone, gold);
            var seal = Block("ArchiveSeal_UnlockForget", archive, new Vector3(0f, 1f, 113f),
                new Vector3(0.75f, 2f, 0.7f), gold);
            var sealLabel = WorldLabel("SealPrompt", seal.transform, "E  ·  TOUCH THE ARCHIVE SEAL", 0.08f);
            sealLabel.transform.localPosition = new Vector3(0f, 1.45f, -0.5f);
            seal.AddComponent<MemoryAbilityUnlock>().Configure(MemoryAbility.Forget, subtitles, sealLabel);

            var memoryObjectRoot = Group("ArchiveMemoryObjects", memoryRoot);
            var wall = BuildForgettableWall(memoryObjectRoot, stone, gold, new Vector3(0f, 1.6f, 127f));
            BuildArchiveRecallDoor(memoryObjectRoot, wall, memory, ghost, gold, stone);

            if (fragment == null)
                Debug.LogWarning($"Memory fragment is missing at {FragmentPath}; the final echo will still play.");
            var collectorTrigger = Trigger("ArchiveMemoryEchoTrigger", environment, new Vector3(0f, 1f, 141f),
                new Vector3(15f, 3f, 5f));
            var echoFigure = BuildMemoryFigure(collectorTrigger.transform, "GrandfatherMemoryEcho", memory, ghost);
            echoFigure.SetActive(false);
            collectorTrigger.AddComponent<ForgottenArchiveEcho>().Configure(subtitles, echoFigure, player.transform,
                fragment, collector);

            var endTrigger = Trigger("JourneyEndTrigger", environment, new Vector3(0f, 1f, 148.5f),
                new Vector3(18f, 3f, 3f));
            endTrigger.AddComponent<PresentationObjectiveTrigger>().Configure(flow, 5, "THE LIGHT REMEMBERS",
                "The lighthouse has kept the memories you brought back. The path ahead is yours now.");
            var finish = endTrigger.AddComponent<PresentationLevelFinishTrigger>();
            finish.Configure(flow);
        }

        private static void BuildArchiveRoom(Transform parent, Material stone, Material gold)
        {
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var position = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 6f, 2.2f,
                    121f + Mathf.Cos(angle * Mathf.Deg2Rad) * 6f);
                var wall = Block($"ArchivePillar_{i + 1}", parent, position,
                    new Vector3(0.8f, 4.4f, 0.8f), stone);
                Block($"ArchivePillarCap_{i + 1}", parent, position + Vector3.up * 2.2f,
                    new Vector3(1.2f, 0.25f, 1.2f), gold);
                _ = wall;
            }
            Block("ArchiveLintel", parent, new Vector3(0f, 4.2f, 121f), new Vector3(12.5f, 0.55f, 0.65f), stone);
            var doorframe = SpawnModel(parent, Village + "DoorFrame_Flat_Brick.fbx",
                new Vector3(0f, 0f, 116.5f), Quaternion.identity, Vector3.one * 1.6f);
            if (doorframe == null)
                Block("ArchiveEntryFrame", parent, new Vector3(0f, 1.8f, 116.5f), new Vector3(5.6f, 3.6f, 0.5f), gold);
        }

        private static MemoryForgettable BuildForgettableWall(Transform parent, Material stone, Material gold, Vector3 position)
        {
            var target = Block("ForgettableArchiveGate", parent, position, new Vector3(9f, 3.2f, 0.8f), stone);
            var cue = LocalBlock("ArchiveGate_PurpleMemoryMark", target.transform,
                new Vector3(0f, 0.2f, -0.52f), new Vector3(0.14f, 1.3f, 0.08f), gold);
            UnityEngine.Object.DestroyImmediate(cue.GetComponent<Collider>());
            var hint = WorldLabel("ForgetHint", target.transform, "HOLD RMB  ·  F TO FORGET", 0.09f);
            hint.transform.localPosition = new Vector3(0f, 0.65f, -0.56f);
            var forgettable = target.AddComponent<MemoryForgettable>();
            forgettable.Configure(ForgetCategory.Barrier, target.GetComponentsInChildren<Renderer>(true),
                target.GetComponentsInChildren<Collider>(true), 12f, 0.65f);
            forgettable.ConfigureHint(hint);
            return forgettable;
        }

        private static void BuildArchiveRecallDoor(Transform parent, MemoryForgettable prerequisite,
            Material memory, Material ghost, Material gold, Material stone)
        {
            var root = Group("ArchiveDoor_RecallAndEcho", parent);
            var present = Group("PresentState_Ruins", root);
            var target = Block("DoorMemoryTarget", present, new Vector3(0f, 1.1f, 131f),
                new Vector3(0.6f, 2.2f, 0.5f), stone);
            CreateCrack(target.transform, gold);
            var memoryState = Group("MemoryState", root);
            var preview = Group("GhostPreview", memoryState);
            var ghostDoor = Block("RememberedDoor_Ghost", preview, new Vector3(0f, 2f, 131f),
                new Vector3(5.5f, 4f, 0.45f), ghost);
            UnityEngine.Object.DestroyImmediate(ghostDoor.GetComponent<Collider>());
            var materialized = Group("MaterializedMemory", memoryState);
            var door = Block("RememberedDoor", materialized, new Vector3(0f, 2f, 131f),
                new Vector3(5.5f, 4f, 0.45f), memory);
            var recall = root.gameObject.AddComponent<MemoryRecallable>();
            recall.Configure(present.gameObject, memoryState.gameObject, preview.gameObject, materialized.gameObject, 30f);
            recall.RefreshStateObjects();

            var pathRoot = Group("Door_EchoPath", parent);
            var low = Group("Waypoint_Closed", pathRoot);
            low.position = door.transform.position;
            var high = Group("Waypoint_Raised", pathRoot);
            high.position = door.transform.position + Vector3.up * 5.2f;
            var path = pathRoot.gameObject.AddComponent<EchoPath>();
            path.Configure(new[] { low, high }, 4f, 0f, false);
            var echoPreview = Group("DoorMotionPreview", parent);
            echoPreview.gameObject.SetActive(false);
            var line = echoPreview.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPositions(new[] { low.position, high.position });
            line.startWidth = line.endWidth = 0.1f;
            line.sharedMaterial = ghost;
            var echo = root.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path, door.transform, echoPreview.gameObject, null, door.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureRecallRequirement(recall);
            echo.ConfigureForgottenBlockers(prerequisite);
            root.gameObject.AddComponent<MemoryComposite>().Configure(recall, echo);
        }

        private static void BuildObjectiveTriggers(Transform parent, PresentationLevelFlow flow)
        {
            MakeObjective(parent, flow, 1, "01  ·  THE SHATTERED BRIDGE",
                "Hold RMB to reveal the intact bridge, then left-click to Recall it. Cross while its memory is solid.",
                new Vector3(0f, 1f, 13.4f), new Vector3(11f, 3f, 3f));
            MakeObjective(parent, flow, 2, "02  ·  A FERRY OUT OF TIME",
                "Recall the ferry first. Then aim Memory Light at it and press Q to Echo its remembered movement.",
                new Vector3(0f, 1f, 48f), new Vector3(10f, 3f, 3f));
            MakeObjective(parent, flow, 3, "03  ·  THE HOLLOW",
                "Keep Memory Light on the Hollow to stop, destabilize and repel it. The sanctuary is safe; revealed remnants restore lost memories with E.",
                new Vector3(0f, 1f, 93f), new Vector3(10f, 3f, 3f));
            MakeObjective(parent, flow, 4, "04  ·  THE FORGOTTEN ARCHIVE",
                "Touch the seal with E. Reveal the marked gate with RMB and press F to Forget it; restore the moving door with Recall, then press Q to Echo it upward.",
                new Vector3(0f, 1f, 112f), new Vector3(12f, 3f, 3f));
        }

        private static void MakeObjective(Transform parent, PresentationLevelFlow flow, int index, string title,
            string description, Vector3 position, Vector3 size)
        {
            var trigger = Trigger($"Objective_{index:00}", parent, position, size);
            trigger.AddComponent<PresentationObjectiveTrigger>().Configure(flow, index, title, description);
        }

        private static Vector3[] BuildCheckpoints()
        {
            return new[]
            {
                new Vector3(0f, 0.1f, -4.5f), new Vector3(0f, 0.1f, 31f), new Vector3(0f, 0.1f, 74f),
                new Vector3(-4.8f, 0.1f, 95f), new Vector3(0f, 0.1f, 118f), new Vector3(0f, 0.1f, 137f)
            };
        }

        private static void BuildResetVolumes(Transform parent)
        {
            for (var i = 0; i < 3; i++)
            {
                var reset = Trigger($"SeaFallReset_{i + 1:00}", parent,
                    new Vector3(0f, -5f, 25f + i * 25f), new Vector3(80f, 2f, 22f));
                reset.AddComponent<ResetVolume>();
            }
        }

        private static SanctuaryLight BuildSanctuary(Transform parent, Material lantern)
        {
            var root = Group("Sanctuary", parent);
            root.position = new Vector3(0f, 0f, 91f);
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "SanctuaryStoneRing";
            visual.transform.SetParent(root, false);
            visual.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            visual.transform.localScale = new Vector3(3.5f, 0.12f, 3.5f);
            visual.GetComponent<Renderer>().sharedMaterial = lantern;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            var glow = root.gameObject.AddComponent<SphereCollider>();
            glow.isTrigger = true;
            glow.radius = 4.8f;
            var light = root.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.32f);
            light.range = 9f;
            light.intensity = 1.65f;
            return root.gameObject.AddComponent<SanctuaryLight>();
        }

        private static MemoryRemnant BuildRemnant(Transform parent, string name, MemoryAbility ability,
            Vector3 position, Material memory, Material gold)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = name;
            orb.transform.SetParent(parent, false);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one * 0.62f;
            orb.GetComponent<Renderer>().sharedMaterial = memory;
            var light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.39f, 0.83f, 1f);
            light.range = 4.5f;
            light.intensity = 0f;
            var halo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            halo.name = "MemoryHalo";
            halo.transform.SetParent(orb.transform, false);
            halo.transform.localScale = Vector3.one * 1.8f;
            halo.GetComponent<Renderer>().sharedMaterial = gold;
            UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());
            var label = WorldLabel("AbilityLabel", orb.transform,
                ability == MemoryAbility.Recall ? "E  ·  RESTORE RECALL" : "E  ·  RESTORE ECHO", 0.1f);
            label.transform.localPosition = Vector3.up * 1.1f;
            var component = orb.AddComponent<MemoryRemnant>();
            component.Configure(ability, null, orb.GetComponentsInChildren<Renderer>(true), light, label);
            return component;
        }

        private static void BuildDecorativeVillage(Transform parent, Material gold)
        {
            var village = Group("AbandonedShoreVillage", parent);
            var gableMesh = GetHouseGableMesh();
            var housePositions = new[]
            {
                new Vector3(-5.8f, 0f, 5.4f), new Vector3(5.8f, 0f, 9f),
                new Vector3(-5.8f, 0f, 121.5f), new Vector3(5.8f, 0f, 126.5f)
            };
            for (var i = 0; i < housePositions.Length; i++)
            {
                var house = Group($"ShoreHouse_{i + 1:00}", village);
                house.position = housePositions[i];
                house.localScale = Vector3.one;
                var frontLeft = SpawnModel(house, Village + "Wall_Plaster_Window_Wide_Flat.fbx",
                    house.position + new Vector3(-1.8f, 0f, -2f), Quaternion.identity, Vector3.one);
                var frontDoor = SpawnModel(house, Village + "Wall_Plaster_Door_Flat.fbx",
                    house.position + new Vector3(0f, 0f, -2f), Quaternion.identity, Vector3.one);
                var wallMaterial = frontLeft != null
                    ? frontLeft.GetComponentInChildren<Renderer>()?.sharedMaterial
                    : null;
                CreateHouseGable(house, "FrontGable", gableMesh, wallMaterial, new Vector3(0f, 3.08f, -1.97f));
                CreateHouseGable(house, "RearGable", gableMesh, wallMaterial, new Vector3(0f, 3.08f, 1.97f));
                var frontRight = SpawnModel(house, Village + "Wall_Plaster_Window_Wide_Flat2.fbx",
                    house.position + new Vector3(1.8f, 0f, -2f), Quaternion.identity, Vector3.one);
                AddHouseWallCollider(house, frontLeft);
                AddHouseWallCollider(house, frontRight);
                if (frontDoor != null)
                    AddHouseDoorColliders(house, frontDoor.transform);
                for (var segment = 0; segment < 3; segment++)
                {
                    var back = SpawnModel(house, Village + "Wall_Plaster_Straight.fbx",
                        house.position + new Vector3((segment - 1) * 1.8f, 0f, 2f), Quaternion.Euler(0f, 180f, 0f), Vector3.one);
                    AddHouseWallCollider(house, back);
                }
                for (var side = -1; side <= 1; side += 2)
                for (var segment = 0; segment < 2; segment++)
                {
                    var sideWall = SpawnModel(house, Village + "Wall_Plaster_Straight.fbx",
                        house.position + new Vector3(side * 2.8f, 0f, (segment == 0 ? -0.95f : 0.95f)),
                        Quaternion.Euler(0f, side < 0 ? -90f : 90f, 0f), Vector3.one);
                    AddHouseWallCollider(house, sideWall);
                }

                SpawnModel(house, Village + "Roof_RoundTiles_4x4.fbx",
                    house.position + new Vector3(0f, 3.1f, 0f), Quaternion.identity, Vector3.one * 1.05f);
                SpawnModel(house, Village + "Floor_WoodDark.fbx", house.position + new Vector3(0f, 0.04f, 0f),
                    Quaternion.identity, Vector3.one * 1.15f);
                SpawnModel(house, Village + (i % 2 == 0 ? "Prop_Chimney.fbx" : "Prop_Chimney2.fbx"),
                    house.position + new Vector3(1.55f, 5.2f, 0.4f), Quaternion.identity, Vector3.one * 0.75f);
                SpawnModel(house, Village + "Prop_Crate.fbx", house.position + new Vector3(-1.5f, 0f, -2.8f),
                    Quaternion.identity, Vector3.one * 0.8f);
                SpawnModel(house, Village + "WindowShutters_Wide_Flat_Open.fbx",
                    house.position + new Vector3(-1.8f, 0f, -2.15f), Quaternion.identity, Vector3.one);
                SpawnModel(house, Village + "WindowShutters_Wide_Flat_Closed.fbx",
                    house.position + new Vector3(1.8f, 0f, -2.15f), Quaternion.identity, Vector3.one);
                CreateLantern(house, house.position + new Vector3(0f, 2.6f, -2.5f), gold);
                CreatePavedBranch(village, house.position + new Vector3(0f, 0f, -2.7f));
            }
            SpawnModel(village, Village + "Prop_WoodenFence_Single.fbx", new Vector3(-8f, 0f, 11f),
                Quaternion.Euler(0f, 90f, 0f), Vector3.one * 1.8f);
            SpawnModel(village, Village + "Prop_MetalFence_Simple.fbx", new Vector3(8f, 0f, 11f),
                Quaternion.Euler(0f, -90f, 0f), Vector3.one * 1.8f);
        }

        private static Mesh GetHouseGableMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(HouseGableMeshPath);
            if (mesh != null) return mesh;
            const string folder = "Assets/_Game/Art/Showcase";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/_Game/Art", "Showcase");

            mesh = new Mesh { name = "LR_HouseGable" };
            mesh.vertices = new[]
            {
                new Vector3(-2.8f, 0f, -0.12f), new Vector3(2.8f, 0f, -0.12f), new Vector3(0f, 4.22f, -0.12f),
                new Vector3(-2.8f, 0f, 0.12f), new Vector3(2.8f, 0f, 0.12f), new Vector3(0f, 4.22f, 0.12f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f)
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 3, 4, 5,
                0, 3, 5, 0, 5, 2,
                1, 2, 5, 1, 5, 4,
                0, 1, 4, 0, 4, 3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, HouseGableMeshPath);
            return mesh;
        }

        private static void CreateHouseGable(Transform parent, string name, Mesh mesh, Material material, Vector3 position)
        {
            var gable = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            gable.transform.SetParent(parent, false);
            gable.transform.localPosition = position;
            gable.GetComponent<MeshFilter>().sharedMesh = mesh;
            gable.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddHouseWallCollider(Transform house, GameObject wall)
        {
            if (wall == null) return;
            var collision = new GameObject(wall.name + "_Collision");
            collision.transform.SetParent(house, false);
            collision.transform.SetPositionAndRotation(wall.transform.position, wall.transform.rotation);
            var collider = collision.AddComponent<BoxCollider>();
            collider.size = new Vector3(2.02f, 3.12f, 0.16f);
            collider.center = new Vector3(0f, 1.56f, 0f);
        }

        private static void AddHouseDoorColliders(Transform house, Transform door)
        {
            const float openingWidth = 1.2f;
            const float openingHeight = 2.5f;
            const float totalWidth = 2.02f;
            const float wallHeight = 3.12f;
            const float thickness = 0.16f;
            var sideWidth = (totalWidth - openingWidth) * 0.5f;

            for (var side = -1; side <= 1; side += 2)
            {
                var jamb = new GameObject("DoorJamb_Collision");
                jamb.transform.SetParent(house, false);
                jamb.transform.SetPositionAndRotation(
                    door.position + door.right * (side * (openingWidth + sideWidth) * 0.5f) + Vector3.up * (wallHeight * 0.5f),
                    door.rotation);
                jamb.AddComponent<BoxCollider>().size = new Vector3(sideWidth, wallHeight, thickness);
            }

            var lintel = new GameObject("DoorLintel_Collision");
            lintel.transform.SetParent(house, false);
            lintel.transform.SetPositionAndRotation(
                door.position + Vector3.up * (openingHeight + (wallHeight - openingHeight) * 0.5f), door.rotation);
            lintel.AddComponent<BoxCollider>().size = new Vector3(openingWidth, wallHeight - openingHeight, thickness);
        }

        private static void CreatePavedBranch(Transform parent, Vector3 doorway)
        {
            var direction = doorway.x < 0f ? 1f : -1f;
            var z = Mathf.Round(doorway.z);
            for (var distance = 1f; distance <= 5f; distance += 1.9f)
                SpawnModel(parent, Village + "Floor_UnevenBrick.fbx",
                    new Vector3(doorway.x + direction * distance, 0.025f, z), Quaternion.Euler(0f, 90f, 0f),
                    Vector3.one);
        }

        private static void CreateForestDressing(Transform parent)
        {
            var forest = Group("StylizedForest", parent);
            var treePaths = new[]
            {
                Nature + "Pine_1.fbx", Nature + "Pine_2.fbx",
                KayKit + "Tree_1_A_Color1.fbx", KayKit + "Tree_2_A_Color1.fbx",
                KayKit + "Tree_3_A_Color1.fbx", KayKit + "Tree_4_A_Color1.fbx"
            };
            var banks = new[]
            {
                new GroundPatch("", new Vector3(0f, 0f, 5f), new Vector3(23f, 0f, 26f)),
                new GroundPatch("", new Vector3(0f, 0f, 37f), new Vector3(18f, 0f, 22f)),
                new GroundPatch("", new Vector3(0f, 0f, 52f), new Vector3(15f, 0f, 12f)),
                new GroundPatch("", new Vector3(0f, 0f, 77f), new Vector3(17f, 0f, 22f)),
                new GroundPatch("", new Vector3(0f, 0f, 99f), new Vector3(12f, 0f, 24f)),
                new GroundPatch("", new Vector3(0f, 0f, 122f), new Vector3(20f, 0f, 27f)),
                new GroundPatch("", new Vector3(0f, 0f, 147f), new Vector3(20f, 0f, 23f))
            };
            var random = new System.Random(80531);
            var treeIndex = 0;
            foreach (var bank in banks)
            {
                var halfWidth = bank.Size.x * 0.5f - 0.7f;
                var zMin = bank.Center.z - bank.Size.z * 0.5f + 1.2f;
                var zMax = bank.Center.z + bank.Size.z * 0.5f - 1.2f;
                var treesPerSide = Mathf.Max(1, Mathf.RoundToInt(bank.Size.z / 5.2f));
                for (var i = 0; i < treesPerSide; i++)
                for (var side = -1; side <= 1; side += 2)
                {
                    var z = Mathf.Lerp(zMin, zMax, (i + 0.35f + (float)random.NextDouble() * 0.3f) / treesPerSide);
                    var x = side * RandomRange(random, Mathf.Max(3.9f, halfWidth - 1.7f), halfWidth);
                    if (IsNearHouse(x, z))
                        continue;
                    var path = treePaths[treeIndex % treePaths.Length];
                    var scale = RandomRange(random, 1.35f, 2.1f);
                    SpawnModel(forest, path, new Vector3(x, 0f, z),
                        Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Vector3.one * scale);
                    treeIndex++;
                }
            }

            var rockPaths = new[] { Nature + "Rock_Medium_1.fbx", Nature + "Rock_Medium_2.fbx",
                KayKit + "Rock_1_A_Color1.fbx", KayKit + "Rock_2_A_Color1.fbx" };
            for (var i = 0; i < 24; i++)
            {
                var z = RandomRange(random, 18f, 158f);
                var side = i % 2 == 0 ? -1f : 1f;
                var x = side * RandomRange(random, 5.4f, 9.4f);
                if (IsNearHouse(x, z))
                    continue;
                SpawnModel(forest, rockPaths[i % rockPaths.Length], new Vector3(x, 0f, z),
                    Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f),
                    Vector3.one * RandomRange(random, 0.65f, 1.25f));
            }
        }

        private static void BuildLighthouseLandmark(Transform parent, Material stone, Material gold, Material beacon)
        {
            var tower = Group("OldLightLighthouse", parent);
            tower.position = new Vector3(0f, 0f, 151f);
            Block("LighthouseFoundation", tower, tower.position + new Vector3(0f, 0.6f, 0f), new Vector3(9f, 1.2f, 9f), stone);
            Block("LighthouseTower_Lower", tower, tower.position + new Vector3(0f, 7f, 0f), new Vector3(6f, 13f, 6f), stone);
            Block("LighthouseTower_Upper", tower, tower.position + new Vector3(0f, 16f, 0f), new Vector3(7.2f, 5f, 7.2f), gold);
            Block("BeaconGlass", tower, tower.position + new Vector3(0f, 19.2f, 0f), new Vector3(4f, 1.3f, 4f), beacon);
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "LighthouseRoof";
            cap.transform.SetParent(tower, false);
            cap.transform.SetPositionAndRotation(tower.position + new Vector3(0f, 20.4f, 0f), Quaternion.identity);
            cap.transform.localScale = new Vector3(4.8f, 0.55f, 4.8f);
            cap.GetComponent<Renderer>().sharedMaterial = stone;
            var light = tower.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.58f, 0.86f, 1f);
            light.range = 28f;
            light.intensity = 4f;
            light.transform.localPosition = new Vector3(0f, 19f, 0f);
        }

        private static void CreateGuideFigure(Transform parent, Material memory, Material gold)
        {
            var figure = BuildMemoryFigure(parent, "TheGrandfatherMemory", memory, gold);
            figure.transform.position = new Vector3(-9f, 0f, 13.5f);
            var caption = WorldLabel("GrandfatherWhisper", figure.transform,
                "THE LIGHT KEEPS WHAT WE CANNOT", 0.035f);
            caption.transform.localPosition = new Vector3(0f, 2.7f, 0f);
        }

        private static GameObject BuildMemoryFigure(Transform parent, string name, Material memory, Material gold)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "MemorySilhouette";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = Vector3.up * 0.95f;
            body.transform.localScale = new Vector3(0.54f, 0.86f, 0.44f);
            body.GetComponent<Renderer>().sharedMaterial = memory;
            UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "MemoryFaceGlow";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.92f, 0.01f);
            head.transform.localScale = Vector3.one * 0.36f;
            head.GetComponent<Renderer>().sharedMaterial = gold;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
            return root;
        }

        private static void BuildBrokenArch(Transform parent, Material memory, Material gold)
        {
            Block("HollowRuinedArch_Left", parent, new Vector3(-4.8f, 1.8f, 103f), new Vector3(0.55f, 3.6f, 0.55f), memory);
            Block("HollowRuinedArch_Right", parent, new Vector3(4.8f, 1.8f, 103f), new Vector3(0.55f, 3.6f, 0.55f), memory);
            Block("HollowRuinedArch_Crown", parent, new Vector3(0f, 3.55f, 103f), new Vector3(10f, 0.5f, 0.6f), gold);
        }

        private static HudParts BuildHud(Transform root, GameObject player, PlayerInputReader reader,
            PlayerMovement movement, PlayerInteractor interactor, PlayerMemoryLight memoryLight)
        {
            var canvasObject = new GameObject("JourneyUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var gameplayHud = new GameObject("GameplayHUD", typeof(RectTransform), typeof(CanvasGroup));
            gameplayHud.transform.SetParent(canvasObject.transform, false);
            var hudRect = gameplayHud.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = hudRect.offsetMax = Vector2.zero;
            var hudGroup = gameplayHud.GetComponent<CanvasGroup>();
            hudGroup.alpha = 0f;
            hudGroup.interactable = false;
            hudGroup.blocksRaycasts = false;

            var objectiveCard = Panel(gameplayHud.transform, "ObjectiveCard", sprite,
                new Vector2(0.035f, 0.78f), new Vector2(0.42f, 0.96f), new Color(0.025f, 0.065f, 0.095f, 0.86f));
            var objectiveGroup = objectiveCard.AddComponent<CanvasGroup>();
            objectiveGroup.alpha = 0f;
            objectiveGroup.blocksRaycasts = false;
            var objectiveTitle = TextElement(objectiveCard.transform, "ObjectiveTitle", "", 23,
                new Color(0.92f, 0.77f, 0.44f), TextAnchor.UpperLeft, new Vector2(24f, 58f), new Vector2(-24f, -15f),
                fontStyle: FontStyle.Bold);
            var objectiveDescription = TextElement(objectiveCard.transform, "ObjectiveDescription", "", 19,
                new Color(0.91f, 0.94f, 0.96f), TextAnchor.UpperLeft, new Vector2(24f, 10f), new Vector2(-24f, -68f));

            var tracker = FixedTextElement(gameplayHud.transform, "ObjectiveTracker", "THE SHORE REMEMBERS", 17,
                new Color(0.95f, 0.82f, 0.55f), TextAnchor.UpperLeft,
                new Vector2(55f, -52f), new Vector2(470f, 38f), fontStyle: FontStyle.Bold);
            tracker.rectTransform.anchorMin = tracker.rectTransform.anchorMax = Vector2.one;
            tracker.rectTransform.pivot = Vector2.one;
            tracker.rectTransform.anchoredPosition = new Vector2(-38f, -30f);
            tracker.rectTransform.sizeDelta = new Vector2(420f, 38f);
            tracker.alignment = TextAnchor.UpperRight;
            var controls = Panel(gameplayHud.transform, "ControlsPanel", sprite,
                new Vector2(0.025f, 0.025f), new Vector2(0.43f, 0.24f), new Color(0.018f, 0.04f, 0.062f, 0.92f));
            TextElement(controls.transform, "ControlsHeading", "TRAVEL  ·  MEMORY", 14,
                new Color(0.95f, 0.82f, 0.55f), TextAnchor.MiddleLeft,
                new Vector2(18f, 112f), new Vector2(-18f, -10f), fontStyle: FontStyle.Bold);
            TextElement(controls.transform, "Controls", "MOVE  WASD / LEFT STICK   ·   RUN  SHIFT / L3\nJUMP  SPACE / A   ·   LIGHT  RMB / LT\nRECALL  LMB / RT   ·   ECHO  Q / Y   ·   INTERACT  E / X   ·   FORGET  F / B",
                14, new Color(0.86f, 0.91f, 0.94f), TextAnchor.MiddleLeft,
                new Vector2(18f, 10f), new Vector2(-18f, -45f));

            var abilities = Panel(gameplayHud.transform, "AbilityReadout", sprite,
                new Vector2(0.68f, 0.025f), new Vector2(0.975f, 0.22f), new Color(0.018f, 0.04f, 0.062f, 0.92f));
            TextElement(abilities.transform, "AbilitiesHeading", "ABILITIES", 14,
                new Color(0.95f, 0.82f, 0.55f), TextAnchor.MiddleLeft,
                new Vector2(16f, 111f), new Vector2(-12f, -8f), fontStyle: FontStyle.Bold);
            var recall = TextElement(abilities.transform, "RecallStatus", "", 15, Color.white,
                TextAnchor.MiddleLeft, new Vector2(16f, 77f), new Vector2(-12f, -35f));
            var echo = TextElement(abilities.transform, "EchoStatus", "", 15, Color.white,
                TextAnchor.MiddleLeft, new Vector2(16f, 45f), new Vector2(-12f, -67f));
            var forget = TextElement(abilities.transform, "ForgetStatus", "", 15, Color.white,
                TextAnchor.MiddleLeft, new Vector2(16f, 13f), new Vector2(-12f, -99f));

            var title = Panel(canvasObject.transform, "StartScreen", sprite, Vector2.zero, Vector2.one,
                new Color(0.015f, 0.035f, 0.06f, 0.62f));
            var titleGroup = title.AddComponent<CanvasGroup>();
            var titleCard = Panel(title.transform, "TitleCard", sprite,
                new Vector2(0.24f, 0.13f), new Vector2(0.76f, 0.87f), new Color(0.012f, 0.028f, 0.046f, 0.86f));
            titleCard.transform.SetAsFirstSibling();
            FixedTextElement(title.transform, "TitleEyebrow", "A LIGHTHOUSE STORY  ·  A PLAYABLE JOURNEY", 18,
                new Color(0.91f, 0.74f, 0.4f), TextAnchor.MiddleCenter,
                new Vector2(0f, 235f), new Vector2(880f, 44f), fontStyle: FontStyle.Bold);
            var titleText = FixedTextElement(title.transform, "GameTitle", "LIGHT\nREMEMBERS", 78,
                new Color(0.94f, 0.97f, 0.98f), TextAnchor.MiddleCenter,
                new Vector2(0f, 78f), new Vector2(900f, 210f), fontStyle: FontStyle.Bold);
            titleText.lineSpacing = 0.83f;
            FixedTextElement(title.transform, "TitleSubtitle", "When the shore forgets, the light remembers.", 25,
                new Color(0.68f, 0.82f, 0.87f), TextAnchor.MiddleCenter,
                new Vector2(0f, -76f), new Vector2(760f, 48f));
            var rule = Panel(title.transform, "TitleGoldRule", sprite,
                new Vector2(0.445f, 0.405f), new Vector2(0.555f, 0.41f), new Color(0.91f, 0.68f, 0.32f, 0.95f));
            _ = rule;
            var buttonObject = Panel(title.transform, "BeginJourneyButton", sprite,
                new Vector2(0.39f, 0.315f), new Vector2(0.61f, 0.39f), new Color(0.18f, 0.56f, 0.62f, 1f));
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            buttonObject.GetComponent<Image>().raycastTarget = true;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.14f, 1.14f, 1.08f, 1f),
                pressedColor = new Color(0.82f, 0.92f, 0.94f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f),
                colorMultiplier = 1f,
                fadeDuration = 0.15f
            };
            TextElement(buttonObject.transform, "BeginLabel", "BEGIN JOURNEY   →", 21,
                new Color(0.98f, 0.97f, 0.9f), TextAnchor.MiddleCenter,
                new Vector2(10f, 6f), new Vector2(-10f, -6f), fontStyle: FontStyle.Bold);
            FixedTextElement(title.transform, "StartHint", "PRESS ENTER / A  ·  OR SELECT BEGIN", 15,
                new Color(0.63f, 0.74f, 0.79f), TextAnchor.MiddleCenter,
                new Vector2(0f, -236f), new Vector2(700f, 34f));
            FixedTextElement(title.transform, "PrologueCopy", "A quiet village. A broken path. A light that can bring the past within reach.", 18,
                new Color(0.73f, 0.8f, 0.83f), TextAnchor.MiddleCenter,
                new Vector2(0f, -307f), new Vector2(1120f, 52f));

            var ending = Panel(canvasObject.transform, "EndingScreen", sprite, Vector2.zero, Vector2.one,
                new Color(0.01f, 0.025f, 0.045f, 0.95f));
            var endingGroup = ending.AddComponent<CanvasGroup>();
            endingGroup.alpha = 0f;
            endingGroup.blocksRaycasts = false;
            endingGroup.interactable = false;
            var endingTitle = FixedTextElement(ending.transform, "EndingTitle", "THE LIGHT REMEMBERS", 58,
                new Color(0.94f, 0.97f, 0.98f), TextAnchor.MiddleCenter,
                new Vector2(0f, 72f), new Vector2(1200f, 120f), fontStyle: FontStyle.Bold);
            FixedTextElement(ending.transform, "EndingCaption", "A journey through memory · Recall · Echo · Forget", 22,
                new Color(0.72f, 0.85f, 0.89f), TextAnchor.MiddleCenter,
                new Vector2(0f, -24f), new Vector2(1000f, 56f));

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
            return new HudParts(hudGroup, titleGroup, button, objectiveGroup, objectiveTitle, objectiveDescription,
                tracker, recall, echo, forget, endingGroup, endingTitle, canvasObject.transform);
        }

        private static SubtitlePresenter BuildSubtitleCanvas(Transform parent)
        {
            var canvasObject = new GameObject("StorySubtitles", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var panel = Panel(canvasObject.transform, "SubtitlePanel", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                new Vector2(0.16f, 0.04f), new Vector2(0.84f, 0.16f), new Color(0.02f, 0.045f, 0.07f, 0.88f));
            var text = TextElement(panel.transform, "SubtitleText", "", 23, new Color(0.94f, 0.96f, 0.97f),
                TextAnchor.MiddleCenter, new Vector2(22f, 10f), new Vector2(-22f, -10f));
            var presenter = canvasObject.AddComponent<SubtitlePresenter>();
            presenter.Configure(group, text);
            return presenter;
        }

        private static CanvasGroup BuildCollapseOverlay(Transform parent)
        {
            var canvasObject = new GameObject("CollapseFade", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<Canvas>().sortingOrder = 200;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var image = Panel(canvasObject.transform, "Blackout", sprite, Vector2.zero, Vector2.one,
                new Color(0.004f, 0.008f, 0.016f, 1f));
            image.GetComponent<Image>().raycastTarget = false;
            return group;
        }

        private static void CreateLantern(Transform parent, Vector3 position, Material material)
        {
            var post = Block("LanternPost", parent, position + Vector3.down * 0.35f,
                new Vector3(0.14f, 1.15f, 0.14f), material);
            UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "MemoryLanternGlow";
            glow.transform.SetParent(parent, false);
            glow.transform.position = position + Vector3.up * 0.38f;
            glow.transform.localScale = Vector3.one * 0.28f;
            glow.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
        }

        private static void CreatePathSign(Transform parent, string label, Vector3 position, Material gold)
        {
            var plank = Block(label + "Sign", parent, position, new Vector3(3.4f, 0.5f, 0.18f), gold);
            UnityEngine.Object.DestroyImmediate(plank.GetComponent<Collider>());
            var text = WorldLabel("SignText", parent, label, 0.07f);
            text.transform.position = position + new Vector3(0f, 0f, -0.12f);
        }

        private static void CreateCrack(Transform parent, Material gold)
        {
            var crack = LocalBlock("MemorySignature_GoldenCrack", parent,
                new Vector3(0f, 0.02f, -0.53f), new Vector3(0.09f, 0.72f, 0.08f), gold);
            crack.transform.localRotation = Quaternion.Euler(0f, 0f, -13f);
        }

        private static void BuildRuinMarker(Transform parent, string name, Vector3 position,
            Material stone, Material gold, Vector3 size)
        {
            var marker = Block(name, parent, position, size, stone);
            CreateCrack(marker.transform, gold);
        }

        private static void CreatePathMarker(Transform parent, string name, Vector3 position, Material material)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * 0.3f;
            marker.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        private static GameObject Trigger(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var trigger = new GameObject(name, typeof(BoxCollider));
            trigger.transform.SetParent(parent, false);
            trigger.transform.position = position;
            var collider = trigger.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            return trigger;
        }

        private static void CreateCheckpoint(Transform parent, string name, Vector3 position)
        {
            var spawn = new GameObject(name + "_Spawn").transform;
            spawn.SetParent(parent, false);
            spawn.position = position;
            var checkpoint = Trigger(name, parent, position + Vector3.up * 0.3f, new Vector3(4f, 2f, 4f));
            checkpoint.AddComponent<Checkpoint>().Configure(spawn);
        }

        private static void CreateArchiveProps(Transform parent, Material stone)
        {
            SpawnModel(parent, Village + "Prop_Crate.fbx", new Vector3(-4.8f, 0f, 121f), Quaternion.identity, Vector3.one);
            SpawnModel(parent, Village + "Stairs_Exterior_Straight.fbx", new Vector3(4.6f, 0f, 121f),
                Quaternion.Euler(0f, 180f, 0f), Vector3.one * 1.2f);
            _ = stone;
        }

        private static void BuildArchivePropsIfNeeded(Transform parent, Material stone) => CreateArchiveProps(parent, stone);

        private static void AddToBuildSettingsFirst(string scenePath)
        {
            var existing = EditorBuildSettings.scenes;
            var result = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };
            foreach (var item in existing)
                if (!string.Equals(item.path, scenePath, StringComparison.OrdinalIgnoreCase))
                    result.Add(item);
            EditorBuildSettings.scenes = result.ToArray();
        }

        private static GameObject SpawnModel(Transform parent, string path, Vector3 position,
            Quaternion rotation, Vector3 scale)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null)
            {
                Debug.LogWarning($"Showcase model not found: {path}");
                return null;
            }
            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = Path.GetFileNameWithoutExtension(path);
            instance.transform.SetParent(parent, false);
            instance.transform.SetPositionAndRotation(position, rotation * model.transform.localRotation);
            instance.transform.localScale = Vector3.Scale(model.transform.localScale, scale);
            return instance;
        }

        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.SetPositionAndRotation(position, Quaternion.identity);
            block.transform.localScale = size;
            if (material != null)
                block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject LocalBlock(string name, Transform parent, Vector3 localPosition,
            Vector3 worldSize, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            var parentScale = parent.lossyScale;
            block.transform.localScale = new Vector3(
                worldSize.x / Mathf.Max(0.001f, parentScale.x),
                worldSize.y / Mathf.Max(0.001f, parentScale.y),
                worldSize.z / Mathf.Max(0.001f, parentScale.z));
            if (material != null)
                block.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            return block;
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            if (parent != null)
                group.SetParent(parent, false);
            return group;
        }

        private static GameObject FindInRoots(GameObject[] roots, string name)
        {
            foreach (var root in roots)
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child.gameObject;
            return null;
        }

        private static TextMesh WorldLabel(string name, Transform parent, string value, float characterSize)
        {
            var label = new GameObject(name, typeof(TextMesh));
            label.transform.SetParent(parent, false);
            var text = label.GetComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = characterSize;
            text.fontSize = 48;
            text.color = new Color(0.95f, 0.85f, 0.66f);
            text.richText = false;
            if (parent != null)
            {
                var parentScale = parent.lossyScale;
                label.transform.localScale = new Vector3(
                    1f / Mathf.Max(0.001f, parentScale.x),
                    1f / Mathf.Max(0.001f, parentScale.y),
                    1f / Mathf.Max(0.001f, parentScale.z));
            }
            return text;
        }

        private static GameObject Panel(Transform parent, string name, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return panel;
        }

        private static Text TextElement(Transform parent, string name, string value, int fontSize,
            Color color, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax,
            FontStyle fontStyle = FontStyle.Normal)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.01f, 0.025f, 0.92f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Text FixedTextElement(Transform parent, string name, string value, int fontSize,
            Color color, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size,
            FontStyle fontStyle = FontStyle.Normal)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            var rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.01f, 0.025f, 0.92f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                throw new FileNotFoundException($"Required material is missing: {path}");
            return material;
        }

        private static Material CreateSceneMaterial(string name, Color baseColor, Color emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = name, color = baseColor };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            const string folder = "Assets/_Game/Art/Materials";
            var path = $"{folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(material);
                return existing;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetShowcaseMaterial(Material material, Color baseColor, Color emission)
        {
            if (material == null)
                return;
            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(material);
        }

        private static void ConfigureWorldLighting(Transform parent)
        {
            var sunObject = new GameObject("StormBreak_DirectionalLight", typeof(Light));
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(32f, -28f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.63f, 0.78f, 0.92f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
        }
    }

    internal readonly struct RecallPuzzle
    {
        public readonly Transform Root;
        public readonly MemoryRecallable Recall;
        public RecallPuzzle(Transform root, MemoryRecallable recall) { Root = root; Recall = recall; }
    }

    internal readonly struct GroundPatch
    {
        public readonly string Name;
        public readonly Vector3 Center;
        public readonly Vector3 Size;

        public GroundPatch(string name, Vector3 center, Vector3 size)
        {
            Name = name;
            Center = center;
            Size = size;
        }
    }

    internal readonly struct FerryPuzzle
    {
        public readonly Transform Root;
        public readonly MemoryRecallable Recall;
        public readonly MemoryEchoable Echo;
        public FerryPuzzle(Transform root, MemoryRecallable recall, MemoryEchoable echo)
        { Root = root; Recall = recall; Echo = echo; }
    }

    internal readonly struct HudParts
    {
        public readonly CanvasGroup Gameplay;
        public readonly CanvasGroup Title;
        public readonly Button StartButton;
        public readonly CanvasGroup ObjectiveCard;
        public readonly Text ObjectiveTitle;
        public readonly Text ObjectiveDescription;
        public readonly Text Tracker;
        public readonly Text Recall;
        public readonly Text Echo;
        public readonly Text Forget;
        public readonly CanvasGroup Ending;
        public readonly Text EndingTitle;
        public readonly Transform Canvas;

        public HudParts(CanvasGroup gameplay, CanvasGroup title, Button startButton, CanvasGroup objectiveCard, Text objectiveTitle,
            Text objectiveDescription, Text tracker, Text recall, Text echo, Text forget,
            CanvasGroup ending, Text endingTitle, Transform canvas)
        {
            Gameplay = gameplay;
            Title = title;
            StartButton = startButton;
            ObjectiveCard = objectiveCard;
            ObjectiveTitle = objectiveTitle;
            ObjectiveDescription = objectiveDescription;
            Tracker = tracker;
            Recall = recall;
            Echo = echo;
            Forget = forget;
            Ending = ending;
            EndingTitle = endingTitle;
            Canvas = canvas;
        }
    }
}
