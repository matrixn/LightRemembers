using System.Linq;
using LightRemembers.Editor;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightRemembers.Tests.EditMode
{
    public sealed class MemoryConvergenceTests
    {
        [Test]
        public void CompositeCanContainRecallAndEchoAndRevealsBothTogether()
        {
            var root = new GameObject("CompositeMemoryTest");
            var recall = root.AddComponent<MemoryRecallable>();
            var echo = root.AddComponent<MemoryEchoable>();
            var composite = root.AddComponent<MemoryComposite>();
            composite.Configure(recall, echo);

            composite.SetRevealed(true);

            Assert.That(composite.Recall, Is.SameAs(recall));
            Assert.That(composite.Echo, Is.SameAs(echo));
            Assert.That(recall.State, Is.EqualTo(RecallState.Revealed));
            Assert.That(echo.State, Is.EqualTo(EchoState.Revealed));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void OldLight02HasAllRoomsAndConfiguredRecallEchoCompositions()
        {
            EditorSceneManager.OpenScene(OldLight02Setup.ScenePath, OpenSceneMode.Single);
            var all = EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject).ToArray();
            var names = all.Select(item => item.name).ToArray();
            foreach (var room in new[] { "Entrance", "TeachingRoom", "CraneRoom", "MechanismRoom", "FinalChamber" })
                Assert.That(names, Does.Contain(room), $"Missing Old Light room: {room}");

            var teaching = Find<MemoryComposite>(all, "TeachingPlatform");
            var crane = Find<MemoryComposite>(all, "BrokenWorkshopCrane");
            var wheel = Find<MemoryComposite>(all, "LighthouseDriveWheel");
            Assert.That(teaching, Is.Not.Null);
            Assert.That(teaching.Recall, Is.Not.Null);
            Assert.That(teaching.Echo, Is.Not.Null);
            Assert.That(teaching.Echo.RequiresRecall, Is.True);
            Assert.That(teaching.Echo.Path.IsValid, Is.True);
            Assert.That(crane, Is.Not.Null);
            Assert.That(crane.Echo.RequiresRecall, Is.True);
            Assert.That(crane.Echo.RideSurface, Is.Not.Null);
            Assert.That(crane.Echo.Path.IsValid, Is.True);
            var craneApproach = Find<Collider>(all, "CraneApproach");
            var craneLanding = Find<Collider>(all, "CraneLanding");
            Assert.That(craneApproach, Is.Not.Null);
            Assert.That(craneLanding, Is.Not.Null);
            Assert.That(craneApproach.bounds.min.z, Is.LessThan(39f));
            Assert.That(craneApproach.bounds.max.z, Is.GreaterThan(39f));
            Assert.That(craneLanding.bounds.min.z, Is.LessThan(39f));
            Assert.That(craneLanding.bounds.max.z, Is.GreaterThan(39f));
            Assert.That(craneApproach.bounds.max.x, Is.LessThan(0f), "The approach bank should be on the near side of the moving crane.");
            Assert.That(craneLanding.bounds.min.x, Is.GreaterThan(0f), "The far bank should be across the crane gap.");
            Assert.That(wheel, Is.Not.Null);
            Assert.That(wheel.Echo.RequiresRecall, Is.True);
            Assert.That(Find<MemoryComposite>(all, "CounterweightLift").Recall, Is.Null);
            Assert.That(Find<MemoryComposite>(all, "CounterweightLift").Echo.Path.IsValid, Is.True);

            foreach (var recallable in all.Select(item => item.GetComponent<MemoryRecallable>()).Where(item => item != null))
            {
                var colliders = recallable.transform.Find("MemoryState/MaterializedMemory").GetComponentsInChildren<Collider>(true);
                Assert.That(colliders.All(collider => !collider.enabled), Is.True, recallable.name + " collider should start off");
                Assert.That(recallable.transform.Find("MemoryState/GhostPreview").GetComponentsInChildren<Collider>(true).Length,
                    Is.EqualTo(0), recallable.name + " ghost must not have gameplay colliders");
            }

            var fragment = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(
                "Assets/_Game/Art/MemoryFragments/memory_workshop_02.asset");
            Assert.That(fragment, Is.Not.Null);
            Assert.That(fragment.FragmentId, Is.EqualTo("memory_workshop_02"));
            Assert.That(fragment.Title, Is.EqualTo("Her Turn"));
            Assert.That(fragment.Description, Is.EqualTo("She was always there. Why can't I remember her face?"));
            var gate = Find<DualRecallGate>(all, "DualRecallGate");
            Assert.That(gate, Is.Not.Null);
            Assert.That(gate.IsOpen, Is.False);
            Assert.That(Find<MemoryEchoSequence>(all, "MemoryEchoSequence"), Is.Not.Null);
            Assert.That(Find<MemoryAnchorReward>(all, "MemoryAnchorReward"), Is.Not.Null);
        }

        private static T Find<T>(GameObject[] all, string name) where T : Component =>
            all.FirstOrDefault(item => item.name == name)?.GetComponent<T>();
    }
}
