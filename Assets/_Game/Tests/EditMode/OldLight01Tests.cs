using LightRemembers.Editor;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightRemembers.Tests.EditMode
{
    public sealed class OldLight01Tests
    {
        [Test]
        public void OldLightSceneHasThreePuzzlesAndFiveIndependentRecallables()
        {
            var scene = EditorSceneManager.OpenScene(OldLight01Setup.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);
            var recallables = Object.FindObjectsByType<MemoryRecallable>(FindObjectsInactive.Include);
            Assert.That(recallables, Has.Length.EqualTo(5));
            Assert.That(Find("BrokenBridgePuzzle"), Is.Not.Null);
            Assert.That(Find("LowerStairMemory"), Is.Not.Null);
            Assert.That(Find("UpperWalkwayMemory"), Is.Not.Null);
            Assert.That(Find("MemoryDoorThreshold"), Is.Not.Null);
            Assert.That(Find("MovableMemoryPlatform"), Is.Not.Null);
        }

        [Test]
        public void MemoryChoiceUsesOneFocusGroupAndAllMemoryCollidersStartDisabled()
        {
            EditorSceneManager.OpenScene(OldLight01Setup.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var group = Object.FindAnyObjectByType<MemoryFocusGroup>();
            Assert.That(group, Is.Not.Null);
            foreach (var recallable in Object.FindObjectsByType<MemoryRecallable>(FindObjectsInactive.Include))
            {
                foreach (var collider in recallable.transform.Find("MemoryState/MaterializedMemory").GetComponentsInChildren<Collider>(true))
                    Assert.That(collider.enabled, Is.False, $"{recallable.name} should begin with a non-physical remembered state.");
            }
            var choices = new[] { Find("MemoryDoorThreshold"), Find("MovableMemoryPlatform") };
            foreach (var choice in choices)
            {
                Assert.That(choice, Is.Not.Null);
                Assert.That(choice.GetComponent<MemoryRecallable>().State, Is.EqualTo(RecallState.Normal));
                var remembered = choice.transform.Find("MemoryState/MaterializedMemory");
                Assert.That(remembered, Is.Not.Null);
                foreach (var collider in remembered.GetComponentsInChildren<Collider>(true))
                    Assert.That(collider.enabled, Is.False);
                var rememberedDeck = remembered.Find("RememberedWalkway");
                Assert.That(rememberedDeck.position.x, Is.EqualTo(choice.transform.position.x).Within(0.01f));
            }

            var upperDeck = Find("UpperWalkwayMemory").transform.Find("MemoryState/MaterializedMemory/RememberedWalkway");
            Assert.That(upperDeck.position.y, Is.EqualTo(1.28f).Within(0.01f), "The second stair memory should meet the upper landing, not float above it.");
            Assert.That(Find("MemoryDoorThreshold").transform.Find("PresentState_Ruins/BrokenDoorLeaf"), Is.Not.Null);
            Assert.That(Find("MemoryDoorThreshold").transform.Find("MemoryState/MaterializedMemory/RememberedDoorLintel"), Is.Not.Null);
        }

        [Test]
        public void ChamberHasCheckpointResetLeverEchoFragmentAndLockedExit()
        {
            EditorSceneManager.OpenScene(OldLight01Setup.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            Assert.That(Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include).Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(Object.FindObjectsByType<ResetVolume>(FindObjectsInactive.Include).Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(Object.FindAnyObjectByType<AncientLever>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<MemoryEchoSequence>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<MemoryFragmentCollector>(), Is.Not.Null);
            var exit = Object.FindAnyObjectByType<ExitUnlocker>();
            Assert.That(exit, Is.Not.Null);
            Assert.That(exit.IsUnlocked, Is.False);
            Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>("Assets/_Game/Art/MemoryFragments/memory_boathouse_01.asset"), Is.Not.Null);
        }

        [Test]
        public void ChamberExitWaitsForInteractAndHasDoorAnimationConfigured()
        {
            EditorSceneManager.OpenScene(OldLight01Setup.ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var exit = Object.FindAnyObjectByType<ExitUnlocker>();
            Assert.That(exit, Is.Not.Null);
            Assert.That(exit.CanInteract(null), Is.False);
            Assert.That(Find("ExitBarrier"), Is.Not.Null);
            Assert.That(Find("SubtitlePanel"), Is.Not.Null);
        }

        private static GameObject Find(string name) => UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == name)?.gameObject;
    }
}
