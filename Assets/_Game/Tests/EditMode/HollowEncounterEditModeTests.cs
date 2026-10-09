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
    public sealed class HollowEncounterEditModeTests
    {
        [Test]
        public void EncounterSceneHasThreatMemoryGateSanctuaryAndCheckpointSystems()
        {
            var scene = EditorSceneManager.OpenScene(HollowEncounterSetup.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject).ToArray();
            var names = all.Select(gameObject => gameObject.name).ToArray();

            foreach (var required in new[]
            {
                "HollowEncounter01", "Environment", "ForestPath", "Ruins", "SafeArea", "FinalGate",
                "MemoryObjects", "Hollow", "Lighting", "Gameplay", "PlayerSpawn", "Checkpoints", "ResetVolumes", "Exit"
            })
                Assert.That(names, Does.Contain(required), $"Missing encounter object: {required}");

            var player = all.FirstOrDefault(gameObject => gameObject.name == "PlayerPrototype");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<MemoryIntegrity>().MaximumSegments, Is.EqualTo(3));
            Assert.That(player.GetComponent<MemoryCorruptionController>(), Is.Not.Null);
            Assert.That(player.GetComponent<MemoryCollapseHandler>(), Is.Not.Null);
            Assert.That(Find<HollowController>(all, "Hollow"), Is.Not.Null);
            Assert.That(Find<HollowWakeTrigger>(all, "HollowWakeTrigger"), Is.Not.Null);
            Assert.That(Find<MemoryRemnant>(all, "RecallMemoryRemnant"), Is.Not.Null);
            Assert.That(Find<MemoryRemnant>(all, "EchoMemoryRemnant"), Is.Not.Null);
            Assert.That(Find<HollowMemoryEcho>(all, "HollowMemoryEcho"), Is.Not.Null);
            Assert.That(Find<SanctuaryLight>(all, "SanctuaryLight"), Is.Not.Null);

            var bridge = Find<MemoryRecallable>(all, "RecallBridge_AfterAttack");
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.State, Is.EqualTo(RecallState.Normal));
            Assert.That(bridge.transform.Find("MemoryState/MaterializedMemory")
                .GetComponentsInChildren<Collider>(true).All(collider => !collider.enabled), Is.True);

            var finalRecall = Find<MemoryRecallable>(all, "FinalGateMemoryMechanism");
            var finalEcho = Find<MemoryEchoable>(all, "FinalGateMemoryMechanism");
            var gate = Find<RecallEchoGate>(all, "FinalGateRecallEchoController");
            Assert.That(finalRecall, Is.Not.Null);
            Assert.That(finalEcho, Is.Not.Null);
            Assert.That(finalEcho.RequiresRecall, Is.True);
            Assert.That(finalEcho.Path.IsValid, Is.True);
            Assert.That(gate, Is.Not.Null);
            Assert.That(gate.IsOpen, Is.False);

            var sanctuaryCollider = Find<SanctuaryLight>(all, "SanctuaryLight").GetComponent<SphereCollider>();
            Assert.That(sanctuaryCollider.isTrigger, Is.True);
            Assert.That(sanctuaryCollider.radius, Is.GreaterThanOrEqualTo(5f));

            var fragment = AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(
                "Assets/_Game/Art/MemoryFragments/memory_fear_03.asset");
            Assert.That(fragment, Is.Not.Null);
            Assert.That(fragment.FragmentId, Is.EqualTo("memory_fear_03"));
            Assert.That(fragment.Title, Is.EqualTo("Look at Me"));
            Assert.That(fragment.Description, Is.EqualTo("I knew her voice. I still can't remember her face."));

            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueSequence>(
                "Assets/_Game/Art/Dialogue/HollowEncounter01.asset");
            Assert.That(dialogue, Is.Not.Null);
            Assert.That(dialogue.Lines.Select(line => line.text), Is.EqualTo(new[]
            {
                "You're scared again.", "I'm not.", "Then look at me."
            }));
        }

        private static T Find<T>(GameObject[] all, string name) where T : Component =>
            all.FirstOrDefault(gameObject => gameObject.name == name)?.GetComponent<T>();
    }
}
