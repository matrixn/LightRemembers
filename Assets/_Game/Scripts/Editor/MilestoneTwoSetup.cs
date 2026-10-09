using System.IO;
using LightRemembers.CameraSystem;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LightRemembers.Editor
{
    /// <summary>Editor API recipe for the Milestone 2 player and prototype test scene.</summary>
    public static class MilestoneTwoSetup
    {
        public const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PlayerPrototype.prefab";
        public const string InputActionsPath = "Assets/_Game/Settings/LightRemembers.inputactions";
        private const string NeutralMaterialPath = "Assets/_Game/Art/Materials/PrototypeNeutral.mat";
        private const string LanternMaterialPath = "Assets/_Game/Art/Materials/PrototypeLantern.mat";

        [MenuItem("Light Remembers/Prototype/Build Milestone 2 Player Scene")]
        public static void BuildPlayerScene()
        {
            EnsureAssetFolder("Assets/_Game/Art/Materials");
            EnsureAssetFolder("Assets/_Game/Prefabs/Characters");

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
                throw new FileNotFoundException($"Input Actions asset not found at {InputActionsPath}.");

            var neutralMaterial = CreateOrLoadMaterial(NeutralMaterialPath, new Color(0.42f, 0.42f, 0.42f, 1f));
            var lanternMaterial = CreateOrLoadMaterial(LanternMaterialPath, new Color(0.3f, 0.22f, 0.12f, 1f));
            var playerPrefab = CreatePlayerPrefab(inputActions, neutralMaterial);
            BuildPrototypeScene(playerPrefab, neutralMaterial, lanternMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreatePlayerPrefab(InputActionAsset inputActions, Material neutralMaterial)
        {
            var player = new GameObject("PlayerPrototype");
            var characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.32f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.slopeLimit = 50f;
            characterController.stepOffset = 0.3f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0f;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.64f, 0.9f, 0.64f);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "CapsuleMesh";
            capsule.transform.SetParent(visual.transform, false);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.GetComponent<Renderer>().sharedMaterial = neutralMaterial;

            var cameraTarget = CreateChild(player.transform, "CameraTarget", new Vector3(0f, 1.45f, 0f));
            var memoryLightOrigin = CreateChild(player.transform, "MemoryLightOrigin", new Vector3(0f, 1.3f, 0.28f));
            var light = memoryLightOrigin.gameObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 12f;
            light.spotAngle = 26f;
            light.intensity = 7f;
            light.color = new Color(1f, 0.86f, 0.52f);
            light.shadows = LightShadows.None;
            light.enabled = false;

            var groundCheck = CreateChild(player.transform, "GroundCheck", new Vector3(0f, 0.08f, 0f));
            groundCheck.gameObject.hideFlags = HideFlags.None;

            var inputReader = player.AddComponent<PlayerInputReader>();
            inputReader.Configure(inputActions);
            var movement = player.AddComponent<PlayerMovement>();
            movement.Configure(characterController, inputReader, null);
            player.AddComponent<PlayerController>();

            var memoryLight = player.AddComponent<PlayerMemoryLight>();
            memoryLight.Configure(inputReader, memoryLightOrigin, null, light);

            var interactor = player.AddComponent<PlayerInteractor>();
            interactor.Configure(inputReader, player.transform);

            var prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            if (prefab == null)
                throw new IOException($"Could not create player prefab at {PlayerPrefabPath}.");
            return prefab;
        }

        private static void BuildPrototypeScene(GameObject playerPrefab, Material neutralMaterial, Material lanternMaterial)
        {
            var scene = EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, OpenSceneMode.Single);
            var gameplay = EditorAutomation.FindGameObject(scene, "Gameplay");
            var spawn = EditorAutomation.FindGameObject(scene, "PlayerSpawn");
            if (gameplay == null || spawn == null)
                throw new InvalidDataException("BoathousePrototype must contain Gameplay and PlayerSpawn before Milestone 2 setup.");

            var previousMilestone = EditorAutomation.FindGameObject(scene, "MilestoneTwo");
            if (previousMilestone != null)
                Object.DestroyImmediate(previousMilestone);

            var milestone = EditorAutomation.CreateGameObject("MilestoneTwo", gameplay.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "PlayerPrototype";
            player.transform.SetParent(milestone.transform, true);
            player.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);

            var inputReader = player.GetComponent<PlayerInputReader>();
            var mainCamera = CreateCameraRig(milestone.transform, player.transform.Find("CameraTarget"), inputReader);
            player.GetComponent<PlayerMovement>().Configure(player.GetComponent<CharacterController>(), inputReader, mainCamera.transform);
            player.GetComponent<PlayerMemoryLight>().Configure(
                inputReader,
                player.transform.Find("MemoryLightOrigin"),
                mainCamera.transform,
                player.transform.Find("MemoryLightOrigin").GetComponent<Light>());

            CreateMemoryTestObject(milestone.transform, neutralMaterial);
            CreateOldLantern(milestone.transform, lanternMaterial);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Camera CreateCameraRig(Transform parent, Transform cameraTarget, PlayerInputReader inputReader)
        {
            var cameraRig = EditorAutomation.CreateGameObject("CameraRig", parent, Vector3.zero, Vector3.zero, Vector3.one);
            var outputObject = EditorAutomation.CreateGameObject("Main Camera", cameraRig.transform, new Vector3(0f, 2f, -4.5f), new Vector3(8f, 0f, 0f), Vector3.one);
            outputObject.tag = "MainCamera";
            var outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.fieldOfView = 60f;
            outputObject.AddComponent<AudioListener>();
            outputObject.AddComponent<CinemachineBrain>();

            var virtualCameraObject = EditorAutomation.CreateGameObject("ThirdPersonCamera", cameraRig.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = cameraTarget;
            virtualCamera.LookAt = cameraTarget;

            var orbital = virtualCameraObject.AddComponent<CinemachineOrbitalFollow>();
            orbital.Radius = 4.5f;
            orbital.TargetOffset = new Vector3(0f, 0.1f, 0f);
            var horizontal = orbital.HorizontalAxis;
            horizontal.Value = 0f;
            horizontal.Range = new Vector2(-180f, 180f);
            horizontal.Wrap = true;
            orbital.HorizontalAxis = horizontal;
            var vertical = orbital.VerticalAxis;
            vertical.Value = 17.5f;
            vertical.Range = new Vector2(-25f, 65f);
            vertical.Wrap = false;
            orbital.VerticalAxis = vertical;

            var composer = virtualCameraObject.AddComponent<CinemachineRotationComposer>();
            composer.Damping = new Vector2(0.35f, 0.35f);
            var deoccluder = virtualCameraObject.AddComponent<CinemachineDeoccluder>();
            deoccluder.CollideAgainst = ~0;
            deoccluder.AvoidObstacles.Enabled = true;
            deoccluder.AvoidObstacles.CameraRadius = 0.2f;
            deoccluder.AvoidObstacles.Damping = 0.25f;
            deoccluder.AvoidObstacles.DampingWhenOccluded = 0.1f;

            var orbitInput = virtualCameraObject.AddComponent<CinemachineOrbitInput>();
            orbitInput.Configure(orbital, inputReader);
            return outputCamera;
        }

        private static void CreateMemoryTestObject(Transform parent, Material material)
        {
            var memoryObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            memoryObject.name = "MemoryTestObject";
            memoryObject.transform.SetParent(parent, false);
            memoryObject.transform.localPosition = new Vector3(0f, 0.9f, -3f);
            memoryObject.transform.localScale = new Vector3(1.2f, 1.8f, 0.55f);
            memoryObject.GetComponent<Renderer>().sharedMaterial = material;
            memoryObject.AddComponent<MemoryRevealable>();
        }

        private static void CreateOldLantern(Transform parent, Material material)
        {
            var lantern = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lantern.name = "OldLantern";
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = new Vector3(-0.9f, 0.55f, -6.4f);
            lantern.transform.localScale = new Vector3(0.35f, 0.55f, 0.35f);
            lantern.GetComponent<Renderer>().sharedMaterial = material;
            lantern.GetComponent<Collider>().isTrigger = true;
            lantern.AddComponent<OldLanternInteractable>();
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        private static Material CreateOrLoadMaterial(string path, Color baseColor)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidDataException("Universal Render Pipeline/Lit shader is unavailable.");
            material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", Color.black);
            material.EnableKeyword("_EMISSION");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
                EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
