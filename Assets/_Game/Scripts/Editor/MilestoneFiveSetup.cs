using System.IO;
using LightRemembers.Memory;
using LightRemembers.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Editor
{
    /// <summary>Appends a compact boat-and-lift Echo teaching route to BoathousePrototype.</summary>
    public static class MilestoneFiveSetup
    {
        private const string PresentMaterialPath = "Assets/_Game/Art/Materials/PrototypeBridgeRuins.mat";
        private const string HintMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemoryHintYellow.mat";
        private const string SolidMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemorySolid.mat";
        private const string GhostMaterialPath = "Assets/_Game/Art/Materials/PrototypeMemoryGhost.mat";

        [MenuItem("Light Remembers/Prototype/Build Milestone 5 Echo Route")]
        public static void BuildEchoRoute()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != EditorAutomation.PrototypeScenePath)
                scene = EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, OpenSceneMode.Single);

            var environment = EditorAutomation.FindGameObject(scene, "Environment");
            var gameplay = EditorAutomation.FindGameObject(scene, "Gameplay");
            var player = EditorAutomation.FindGameObject(scene, "PlayerPrototype");
            if (environment == null || gameplay == null || player == null)
                throw new IOException("BoathousePrototype must contain the existing Environment, Gameplay, and PlayerPrototype before building Echo.");

            var existing = EditorAutomation.FindGameObject(scene, "MilestoneFive_EchoRoute");
            if (existing != null)
                Object.DestroyImmediate(existing);
            var oldWaterZone = EditorAutomation.FindGameObject(scene, "EchoWaterRecoveryZone");
            if (oldWaterZone != null)
                Object.DestroyImmediate(oldWaterZone);
            var oldCheckpoint = EditorAutomation.FindGameObject(scene, "EchoDockCheckpoint");
            if (oldCheckpoint != null)
                Object.DestroyImmediate(oldCheckpoint);
            RemoveGeneratedAreas(environment.transform);

            var present = LoadOrCreateMaterial(PresentMaterialPath, new Color(0.27f, 0.30f, 0.32f), Color.black, false);
            var hint = LoadOrCreateMaterial(HintMaterialPath, new Color(1f, 0.74f, 0.1f), new Color(0.28f, 0.16f, 0.01f), false);
            var solid = LoadOrCreateMaterial(SolidMaterialPath, new Color(0.48f, 0.85f, 0.9f), new Color(0.2f, 0.75f, 1.2f), false);
            var ghost = LoadOrCreateMaterial(GhostMaterialPath, new Color(0.35f, 0.9f, 1f, 0.28f), new Color(0.25f, 1.1f, 1.5f), true);

            var route = EditorAutomation.CreateGameObject("MilestoneFive_EchoRoute", gameplay.transform, Vector3.zero, Vector3.zero, Vector3.one);
            if (gameplay.GetComponent<MemoryAbilityBootstrap>() == null)
                gameplay.AddComponent<MemoryAbilityBootstrap>();
            var respawner = player.GetComponent<CheckpointRespawner>();
            if (respawner == null)
                player.AddComponent<CheckpointRespawner>();
            BuildBoat(environment.transform, route.transform, present, hint, solid, ghost);
            BuildLift(environment.transform, route.transform, present, hint, solid, ghost);
            BuildSafetyZone(environment.transform, gameplay.transform, route.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildBoat(Transform environment, Transform route, Material present, Material hint, Material solid, Material ghost)
        {
            var dock = Child("BoatDock", environment);
            Cube("DockApproach", dock, new Vector3(0f, 0.25f, 13.25f), new Vector3(8f, 0.5f, 7.5f), present);
            Cube("DockEnd", dock, new Vector3(0f, 0.25f, 17.5f), new Vector3(5f, 0.5f, 3f), present);
            Cube("FarLanding", dock, new Vector3(0f, 0.25f, 30f), new Vector3(7f, 0.5f, 7f), present);
            Cube("ToLiftWalkway", dock, new Vector3(3.6f, 0.25f, 33.5f), new Vector3(4.5f, 0.5f, 3f), present);
            Cube("LiftApproach", dock, new Vector3(6f, 0.25f, 35.25f), new Vector3(5f, 0.5f, 2.5f), present);

            var water = Cube("Waterway", dock, new Vector3(0f, -1.7f, 23.75f), new Vector3(10f, 0.35f, 11.5f), present);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            var boat = Child("OldBoat", route);
            boat.localPosition = new Vector3(0f, 0.5f, 18.5f);
            Cube("BoatHull", boat, new Vector3(0f, -0.05f, 0f), new Vector3(2.5f, 0.45f, 4.2f), present);
            Cube("BoatDeck", boat, new Vector3(0f, 0.22f, 0f), new Vector3(2.15f, 0.14f, 3.7f), present);
            Cube("BoatMemoryMark", boat, new Vector3(0f, 0.305f, -0.8f), new Vector3(0.42f, 0.025f, 0.12f), hint);
            var surfaceRoot = Child("RideTrigger", boat);
            var trigger = surfaceRoot.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.9f, 0f);
            trigger.size = new Vector3(2.65f, 2.1f, 4.4f);
            var rideSurface = surfaceRoot.gameObject.AddComponent<EchoRideSurface>();
            rideSurface.Configure(trigger);

            var path = BuildPath(route, "OldBoatEchoPath", new[]
            {
                new Vector3(0f, 0.5f, 18.5f), new Vector3(0f, 0.5f, 23.75f), new Vector3(0f, 0.5f, 29f)
            }, 7f, 2.5f, ghost);
            var echo = boat.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path.Path, boat, path.Preview, rideSurface, boat.GetComponentsInChildren<Renderer>(true));
        }

        private static void BuildLift(Transform environment, Transform route, Material present, Material hint, Material solid, Material ghost)
        {
            var balcony = Child("LiftBalcony", environment);
            Cube("UpperLanding", balcony, new Vector3(6f, 5.45f, 40f), new Vector3(7f, 0.5f, 8f), present);
            var arrivalMarker = Cube("UpperEndMarker", balcony, new Vector3(6f, 5.8f, 43.1f), new Vector3(1f, 0.18f, 1f), hint);
            Object.DestroyImmediate(arrivalMarker.GetComponent<Collider>());

            var lift = Child("OldLift", route);
            lift.localPosition = new Vector3(6f, 0.5f, 36.25f);
            Cube("LiftPlatform", lift, new Vector3(0f, 0.12f, 0f), new Vector3(3.2f, 0.24f, 3f), present);
            Cube("LiftMemoryMark", lift, new Vector3(0f, 0.255f, -0.85f), new Vector3(0.4f, 0.02f, 0.1f), hint);
            var triggerObject = Child("RideTrigger", lift);
            var trigger = triggerObject.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.95f, 0f);
            trigger.size = new Vector3(3.3f, 2f, 3.1f);
            var rideSurface = triggerObject.gameObject.AddComponent<EchoRideSurface>();
            rideSurface.Configure(trigger);

            var path = BuildPath(route, "OldLiftEchoPath", new[]
            {
                new Vector3(6f, 0.5f, 36.25f), new Vector3(6f, 3f, 36.25f), new Vector3(6f, 5.35f, 36.25f)
            }, 5f, 3f, ghost);
            var echo = lift.gameObject.AddComponent<MemoryEchoable>();
            echo.Configure(path.Path, lift, path.Preview, rideSurface, lift.GetComponentsInChildren<Renderer>(true));
        }

        private static (EchoPath Path, GameObject Preview) BuildPath(Transform parent, string name, Vector3[] positions,
            float duration, float pause, Material ghost)
        {
            var pathObject = Child(name, parent);
            var points = new Transform[positions.Length];
            for (var i = 0; i < positions.Length; i++)
            {
                var point = Child($"Waypoint_{i + 1:00}", pathObject);
                point.position = positions[i];
                points[i] = point;
            }

            var path = pathObject.gameObject.AddComponent<EchoPath>();
            path.Configure(points, duration, pause, true);
            var preview = Child($"{name}_VisiblePath", parent);
            preview.gameObject.SetActive(false);
            var lineObject = Child("PathLine", preview);
            var line = lineObject.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = points.Length;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.sharedMaterial = ghost;
            line.numCapVertices = 3;
            for (var i = 0; i < points.Length; i++)
            {
                var marker = EditorAutomation.CreatePrimitive(PrimitiveType.Sphere, $"WaypointGlow_{i + 1:00}", preview,
                    positions[i], Vector3.zero, Vector3.one * 0.28f);
                marker.GetComponent<Renderer>().sharedMaterial = ghost;
                Object.DestroyImmediate(marker.GetComponent<Collider>());
                line.SetPosition(i, positions[i] + Vector3.up * 0.4f);
            }
            return (path, preview.gameObject);
        }

        private static void BuildSafetyZone(Transform environment, Transform gameplay, Transform route)
        {
            var zone = Child("EchoWaterRecoveryZone", environment);
            zone.position = new Vector3(0f, -2f, 23.5f);
            var box = zone.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(18f, 1.5f, 20f);
            zone.gameObject.AddComponent<ResetVolume>();

            var spawn = Child("EchoDockRespawn", route);
            spawn.position = new Vector3(0f, 0.1f, 10.75f);
            var checkpoint = Child("EchoDockCheckpoint", gameplay);
            checkpoint.position = new Vector3(0f, 0.8f, 12.4f);
            var checkpointTrigger = checkpoint.gameObject.AddComponent<BoxCollider>();
            checkpointTrigger.isTrigger = true;
            checkpointTrigger.size = new Vector3(8f, 2f, 2f);
            checkpoint.gameObject.AddComponent<Checkpoint>().Configure(spawn);
        }

        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = EditorAutomation.CreatePrimitive(PrimitiveType.Cube, name, parent, position, Vector3.zero, scale);
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static void RemoveGeneratedAreas(Transform environment)
        {
            var descendants = environment.GetComponentsInChildren<Transform>(true);
            for (var i = descendants.Length - 1; i >= 0; i--)
            {
                var item = descendants[i];
                if (item.parent == environment && (item.name == "BoatDock" || item.name == "LiftBalcony"))
                    Object.DestroyImmediate(item.gameObject);
            }
        }

        private static Transform Child(string name, Transform parent) =>
            EditorAutomation.CreateGameObject(name, parent, Vector3.zero, Vector3.zero, Vector3.one).transform;

        private static Material LoadOrCreateMaterial(string path, Color baseColor, Color emission, bool transparent)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new IOException("Universal Render Pipeline/Lit shader is unavailable.");
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

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
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
