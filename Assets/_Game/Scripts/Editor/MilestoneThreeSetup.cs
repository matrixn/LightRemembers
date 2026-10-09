using System.IO;
using LightRemembers.Memory;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Editor
{
    /// <summary>Builds the first Recall traversal puzzle with Unity Editor APIs.</summary>
    public static class MilestoneThreeSetup
    {
        private const string PresentMaterialPath = "Assets/_Game/Art/Materials/PrototypeBridgeRuins.mat";
        private const string MemoryHintMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemoryHintYellow.mat";
        private const string SolidMemoryMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemorySolid.mat";
        private const string GhostMemoryMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemoryGhost.mat";

        [MenuItem("Light Remembers/Prototype/Build Milestone 3 Broken Bridge")]
        public static void BuildBrokenBridgePuzzle()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != EditorAutomation.PrototypeScenePath)
                scene = EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, OpenSceneMode.Single);

            var gameplay = EditorAutomation.FindGameObject(scene, "Gameplay");
            if (gameplay == null)
                throw new InvalidDataException("BoathousePrototype must contain a Gameplay object.");

            var existing = EditorAutomation.FindGameObject(scene, "MilestoneThree");
            if (existing != null)
                Object.DestroyImmediate(existing);

            var presentMaterial = LoadOrCreateMaterial(
                PresentMaterialPath, new Color(0.28f, 0.31f, 0.34f), new Color(0.02f, 0.03f, 0.04f), false);
            var memoryHintMaterial = LoadOrCreateMaterial(
                MemoryHintMaterialPath, new Color(1f, 0.76f, 0.12f), new Color(0.28f, 0.16f, 0.015f), false);
            var solidMemoryMaterial = LoadOrCreateMaterial(
                SolidMemoryMaterialPath, new Color(0.48f, 0.85f, 0.9f), new Color(0.2f, 0.75f, 1.2f), false);
            var ghostMemoryMaterial = LoadOrCreateMaterial(
                GhostMemoryMaterialPath, new Color(0.35f, 0.9f, 1f, 0.28f), new Color(0.25f, 1.1f, 1.5f), true);

            BuildBridge(gameplay.transform, presentMaterial, memoryHintMaterial, solidMemoryMaterial, ghostMemoryMaterial);
            EditorAutomation.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildBridge(
            Transform gameplay,
            Material presentMaterial,
            Material memoryHintMaterial,
            Material solidMaterial,
            Material ghostMaterial)
        {
            var milestone = EditorAutomation.CreateGameObject(
                "MilestoneThree", gameplay, Vector3.zero, Vector3.zero, Vector3.one);
            var puzzle = EditorAutomation.CreateGameObject(
                "BrokenBridgePuzzle", milestone.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var puzzleCollider = puzzle.AddComponent<BoxCollider>();
            puzzleCollider.isTrigger = true;
            puzzleCollider.center = new Vector3(4f, 0.25f, -1f);
            puzzleCollider.size = new Vector3(3.3f, 0.5f, 7f);
            var puzzleLogic = puzzle.AddComponent<BrokenBridgePuzzle>();

            // Permanent banks and stairs provide a clear, walkable approach at both ends.
            var startPlatform = CreateCube(
                "StartSidePlatform", puzzle.transform, new Vector3(4f, 0.55f, -6.5f),
                new Vector3(3.2f, 0.3f, 4f), presentMaterial).GetComponent<Collider>();
            var destinationPlatform = CreateCube(
                "DestinationSidePlatform", puzzle.transform, new Vector3(4f, 0.55f, 4.5f),
                new Vector3(3.2f, 0.3f, 4f), presentMaterial).GetComponent<Collider>();
            CreateStairs(puzzle.transform, presentMaterial, new[] { -9.5f, -9f, -8.5f }, new[] { 0.2f, 0.4f, 0.6f });
            CreateStairs(puzzle.transform, presentMaterial, new[] { 6.5f, 7f, 7.5f }, new[] { 0.6f, 0.4f, 0.2f });

            var present = EditorAutomation.CreateGameObject(
                "PresentState_RuinedBridge", puzzle.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreateCube("BrokenFragment_Start", present.transform, new Vector3(4f, 0.8f, -3.95f),
                new Vector3(2.8f, 0.2f, 1.1f), presentMaterial);
            CreateCube("BrokenFragment_Destination", present.transform, new Vector3(4f, 0.8f, 1.95f),
                new Vector3(2.8f, 0.2f, 1.1f), presentMaterial);
            CreateCube("FallenDebris_A", present.transform, new Vector3(3.1f, -0.65f, -1.2f),
                new Vector3(0.8f, 0.35f, 1.1f), presentMaterial);
            CreateCube("FallenDebris_B", present.transform, new Vector3(4.8f, -0.8f, 0.35f),
                new Vector3(0.7f, 0.3f, 0.8f), presentMaterial);
            CreateYellowMemoryHint(present.transform, memoryHintMaterial);

            var memory = EditorAutomation.CreateGameObject(
                "MemoryState_IntactBridge", puzzle.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var preview = EditorAutomation.CreateGameObject(
                "GhostPreview", memory.transform, Vector3.zero, Vector3.zero, Vector3.one);
            var ghostDeck = CreateCube("GhostBridgeDeck", preview.transform, new Vector3(4f, 0.6f, -1f),
                new Vector3(3f, 0.2f, 7f), ghostMaterial);
            Object.DestroyImmediate(ghostDeck.GetComponent<Collider>());
            for (var i = 0; i < 5; i++)
            {
                var rail = CreateCube($"GhostRail_{i + 1}", preview.transform,
                    new Vector3(i % 2 == 0 ? 2.65f : 5.35f, 0.95f, -3.4f + i * 1.2f),
                    new Vector3(0.12f, 0.45f, 0.8f), ghostMaterial);
                Object.DestroyImmediate(rail.GetComponent<Collider>());
            }

            var materialized = EditorAutomation.CreateGameObject(
                "MaterializedBridge", memory.transform, Vector3.zero, Vector3.zero, Vector3.one);
            CreateCube("BridgeDeck", materialized.transform, new Vector3(4f, 0.6f, -1f),
                new Vector3(3f, 0.2f, 7f), solidMaterial);
            for (var i = 0; i < 5; i++)
            {
                CreateCube($"BridgeRail_{i + 1}", materialized.transform,
                    new Vector3(i % 2 == 0 ? 2.65f : 5.35f, 0.95f, -3.4f + i * 1.2f),
                    new Vector3(0.12f, 0.45f, 0.8f), solidMaterial);
            }

            var resetPoint = EditorAutomation.CreateGameObject(
                "PitResetPoint", puzzle.transform, new Vector3(4f, 0.7f, -6.5f), Vector3.zero, Vector3.one);
            var recallable = puzzle.AddComponent<MemoryRecallable>();
            recallable.Configure(present, memory, preview, materialized, 8f);
            puzzleLogic.Configure(recallable, startPlatform, destinationPlatform, resetPoint.transform);
        }

        private static void CreateStairs(Transform parent, Material material, float[] zPositions, float[] heights)
        {
            for (var i = 0; i < zPositions.Length; i++)
            {
                var height = heights[i];
                CreateCube($"ApproachStep_{(zPositions[0] < 0f ? "Start" : "Destination")}_{i + 1}",
                    parent, new Vector3(4f, height * 0.5f, zPositions[i]), new Vector3(2.2f, height, 0.55f), material);
            }
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(
                PrimitiveType.Cube, name, parent, position, Vector3.zero, scale);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void CreateYellowMemoryHint(Transform present, Material material)
        {
            var crack = EditorAutomation.CreateGameObject(
                "MemoryHint_YellowCrack", present, Vector3.zero, Vector3.zero, Vector3.one);
            var points = new[]
            {
                new Vector3(3.78f, 0.913f, -4.46f),
                new Vector3(4.05f, 0.913f, -4.24f),
                new Vector3(3.91f, 0.913f, -4.05f),
                new Vector3(4.24f, 0.913f, -3.87f),
                new Vector3(4.09f, 0.913f, -3.58f)
            };

            for (var i = 0; i < points.Length - 1; i++)
                CreateCrackSegment(crack.transform, $"CrackSegment_{i + 1}", points[i], points[i + 1], 0.045f, material);

            CreateCrackSegment(crack.transform, "CrackBranch_Left", points[1],
                new Vector3(3.68f, 0.913f, -4.13f), 0.032f, material);
            CreateCrackSegment(crack.transform, "CrackBranch_Right", points[3],
                new Vector3(4.52f, 0.913f, -3.98f), 0.032f, material);
        }

        private static void CreateCrackSegment(
            Transform parent, string name, Vector3 start, Vector3 end, float width, Material material)
        {
            var direction = end - start;
            var segment = EditorAutomation.CreatePrimitive(
                PrimitiveType.Cube,
                name,
                parent,
                (start + end) * 0.5f,
                Quaternion.LookRotation(direction, Vector3.up).eulerAngles,
                new Vector3(width, 0.018f, direction.magnitude));
            segment.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(segment.GetComponent<Collider>());
        }

        private static Material LoadOrCreateMaterial(string path, Color baseColor, Color emission, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidDataException("Universal Render Pipeline/Lit shader is unavailable.");

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
    }
}
