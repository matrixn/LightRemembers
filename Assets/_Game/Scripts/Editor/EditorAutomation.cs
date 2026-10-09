using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Editor
{
    /// <summary>
    /// Small, reusable Editor API surface for safe scene authoring and validation.
    /// Keep higher-level scene recipes separate from these primitives as the project grows.
    /// </summary>
    public static class EditorAutomation
    {
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap/Bootstrap.unity";
        public const string PrototypeScenePath = "Assets/_Game/Scenes/Prototype/BoathousePrototype.unity";

        [MenuItem("Light Remembers/Automation/Create Foundation Scenes")]
        public static void CreateFoundationScenes()
        {
            CreateBootstrapScene();
            CreateBoathousePrototypeScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static Scene OpenOrCreateScene(string scenePath)
        {
            EnsureAssetFolder(Path.GetDirectoryName(scenePath)?.Replace('\\', '/'));

            if (File.Exists(scenePath))
            {
                return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, scenePath);
            return scene;
        }

        public static GameObject CreateGameObject(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localEulerAngles = localEulerAngles;
            gameObject.transform.localScale = localScale;
            return gameObject;
        }

        public static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale)
        {
            var gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localEulerAngles = localEulerAngles;
            gameObject.transform.localScale = localScale;
            return gameObject;
        }

        public static GameObject FindGameObject(Scene scene, string name)
        {
            if (!scene.IsValid())
            {
                return null;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        public static bool ValidateTransform(GameObject gameObject, Vector3 expectedPosition, float tolerance = 0.001f)
        {
            return gameObject != null && Vector3.Distance(gameObject.transform.position, expectedPosition) <= tolerance;
        }

        public static void SaveScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                throw new ArgumentException("Cannot save an invalid scene.", nameof(scene));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void CreateBootstrapScene()
        {
            var scene = OpenOrCreateScene(BootstrapScenePath);
            ClearScene(scene);

            var bootstrap = CreateGameObject("Bootstrap", null, Vector3.zero, Vector3.zero, Vector3.one);
            CreateGameObject("GameRoot", bootstrap.transform, Vector3.zero, Vector3.zero, Vector3.one);

            SaveScene(scene);
            AddSceneToBuildSettings(BootstrapScenePath);
        }

        public static void CreateBoathousePrototypeScene()
        {
            var scene = OpenOrCreateScene(PrototypeScenePath);
            ClearScene(scene);

            var prototype = CreateGameObject("BoathousePrototype", null, Vector3.zero, Vector3.zero, Vector3.one);
            var environment = CreateGameObject("Environment", prototype.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var ground = CreateGameObject("Ground", environment.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreatePrimitive(PrimitiveType.Plane, "GroundPlane", ground.transform, Vector3.zero, Vector3.zero, new Vector3(3f, 1f, 3f));

            var boathouse = CreateGameObject("Boathouse", environment.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreatePrimitive(PrimitiveType.Cube, "BoathouseBody", boathouse.transform, new Vector3(0f, 1.5f, 6f), Vector3.zero, new Vector3(8f, 3f, 6f));
            CreatePrimitive(PrimitiveType.Cube, "BoathouseRoof", boathouse.transform, new Vector3(0f, 3.4f, 6f), Vector3.zero, new Vector3(8.6f, 0.5f, 6.6f));

            var obstacles = CreateGameObject("Obstacles", environment.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreatePrimitive(PrimitiveType.Cube, "Obstacle_A", obstacles.transform, new Vector3(-5f, 1f, -1f), Vector3.zero, new Vector3(2f, 2f, 2f));
            CreatePrimitive(PrimitiveType.Cube, "Obstacle_B", obstacles.transform, new Vector3(4f, 0.75f, 0f), Vector3.zero, new Vector3(1.5f, 1.5f, 3f));
            CreatePrimitive(PrimitiveType.Cube, "CodexAutomationTest", obstacles.transform, new Vector3(2f, 0.5f, 2f), Vector3.zero, Vector3.one);

            var lighting = CreateGameObject("Lighting", prototype.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var directionalLight = CreateGameObject("DirectionalLight", lighting.transform, Vector3.zero, new Vector3(50f, -30f, 0f), Vector3.one);
            var light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.96f, 0.88f);

            var gameplay = CreateGameObject("Gameplay", prototype.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreateGameObject("PlayerSpawn", gameplay.transform, new Vector3(0f, 0.1f, -8f), Vector3.zero, Vector3.one);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.2f, 0.24f);

            SaveScene(scene);
            AddSceneToBuildSettings(PrototypeScenePath);
        }

        private static void ClearScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var folderName = Path.GetFileName(folderPath);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
