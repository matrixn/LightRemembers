using System.Collections;
using LightRemembers.Memory;
using LightRemembers.Interaction;
using LightRemembers.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class MemoryRecallablePlayModeTests
    {
        [UnityTest]
        public IEnumerator EchoTravelsReturnsAndCanBeReplayed()
        {
            var root = new GameObject("EchoRuntimeTest");
            var moving = GameObject.CreatePrimitive(PrimitiveType.Cube);
            moving.name = "EchoMovingObject";
            moving.transform.SetParent(root.transform, false);
            var pathObject = new GameObject("EchoPath");
            var start = new GameObject("Start").transform;
            start.position = Vector3.zero;
            var end = new GameObject("End").transform;
            end.position = Vector3.right * 2f;
            var path = pathObject.AddComponent<EchoPath>();
            path.Configure(new[] { start, end }, 0.25f, 0.25f, true);
            var preview = new GameObject("EchoPathPreview");
            var echo = root.AddComponent<MemoryEchoable>();
            echo.Configure(path, moving.transform, preview, null, moving.GetComponentsInChildren<Renderer>());
            MemoryAbilityState.UnlockEcho();
            echo.SetRevealed(true);

            Assert.That(echo.State, Is.EqualTo(EchoState.Revealed));
            Assert.That(echo.TryEcho(), Is.True);
            Assert.That(echo.State, Is.EqualTo(EchoState.Echoing));
            yield return new WaitForSeconds(0.35f);
            Assert.That(moving.transform.position.x, Is.GreaterThan(1.8f));
            echo.SetRevealed(false);
            yield return new WaitForSeconds(0.5f);
            Assert.That(echo.State, Is.EqualTo(EchoState.Idle));
            Assert.That(moving.transform.position.x, Is.LessThan(0.05f));

            echo.SetRevealed(true);
            Assert.That(echo.TryEcho(), Is.True);
            yield return new WaitForSeconds(0.9f);
            Assert.That(echo.State, Is.EqualTo(EchoState.Revealed));
            Object.Destroy(root);
            Object.Destroy(pathObject);
            Object.Destroy(start.gameObject);
            Object.Destroy(end.gameObject);
            Object.Destroy(preview);
            MemoryAbilityState.SetEchoUnlocked(false);
        }

        [UnityTest]
        public IEnumerator EchoRideSurfaceCarriesCharacterControllerWithPlatform()
        {
            var platform = new GameObject("RideVolume");
            var trigger = platform.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(4f, 4f, 4f);
            var surface = platform.AddComponent<EchoRideSurface>();
            surface.Configure(trigger);
            var player = new GameObject("EchoPassenger");
            var controller = player.AddComponent<CharacterController>();
            player.transform.position = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(surface.RiderCount, Is.EqualTo(1));

            platform.transform.position = Vector3.right;
            surface.CarryRiders(Vector3.zero, Quaternion.identity, Vector3.right, Quaternion.identity);
            Assert.That(controller.transform.position.x, Is.EqualTo(1f).Within(0.05f));
            Object.Destroy(platform);
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator ChamberExitOpensBeforeBecomingInteractable()
        {
            var exitObject = new GameObject("ExitInteractionTest");
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(exitObject.transform, false);
            var exit = exitObject.AddComponent<ExitUnlocker>();
            exit.Configure(door, string.Empty);
            Assert.That(exit.CanInteract(null), Is.False);
            exit.Unlock();
            Assert.That(exit.CanInteract(null), Is.False);

            yield return new WaitForSeconds(1f);

            Assert.That(exit.IsDoorOpen, Is.True);
            Assert.That(exit.CanInteract(null), Is.True);
            exit.Interact(null);
            Object.Destroy(exitObject);
        }

        [UnityTest]
        public IEnumerator RecallExpiresAndRestoresPresentState()
        {
            var setup = CreateRecallable("ExpiryTest", 0.15f);
            setup.Recallable.SetRevealed(true);
            Assert.That(setup.Recallable.TryRecall(), Is.True);
            setup.Recallable.SetRevealed(false);
            Assert.That(setup.Collider.enabled, Is.True);

            yield return new WaitForSeconds(0.3f);

            Assert.That(setup.Recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(setup.Present.activeSelf, Is.True);
            Assert.That(setup.Memory.activeSelf, Is.False);
            Assert.That(setup.Collider.enabled, Is.False);
            Object.Destroy(setup.Root);
        }

        [UnityTest]
        public IEnumerator ExpirationWaitsUntilCharacterLeavesRecalledGeometry()
        {
            var setup = CreateRecallable("SafeExpiryTest", 0.15f);
            var character = new GameObject("TestPlayer");
            character.AddComponent<CharacterController>();
            setup.Recallable.SetRevealed(true);
            Assert.That(setup.Recallable.TryRecall(), Is.True);
            setup.Recallable.SetRevealed(false);

            yield return new WaitForSeconds(0.35f);

            Assert.That(setup.Recallable.State, Is.EqualTo(RecallState.Recalled));
            Assert.That(setup.Collider.enabled, Is.True);

            character.transform.position = new Vector3(10f, 0f, 0f);
            yield return new WaitForSeconds(0.25f);

            Assert.That(setup.Recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(setup.Collider.enabled, Is.False);
            Object.Destroy(setup.Root);
            Object.Destroy(character);
        }

        [UnityTest]
        public IEnumerator FocusGroupSafelyReleasesPreviousChoiceWhenAnotherIsRecalled()
        {
            var groupObject = new GameObject("FocusGroup");
            var group = groupObject.AddComponent<MemoryFocusGroup>();
            var first = CreateRecallable("FirstChoice", 8f);
            var second = CreateRecallable("SecondChoice", 8f);
            first.Recallable.ConfigureFocusGroup(group);
            second.Recallable.ConfigureFocusGroup(group);

            first.Recallable.SetRevealed(true);
            Assert.That(first.Recallable.TryRecall(), Is.True);
            second.Recallable.SetRevealed(true);
            Assert.That(second.Recallable.TryRecall(), Is.True);
            Assert.That(group.Active, Is.SameAs(second.Recallable));

            yield return new WaitForSeconds(0.25f);

            Assert.That(first.Recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(first.Collider.enabled, Is.False);
            Assert.That(second.Recallable.State, Is.EqualTo(RecallState.Recalled));
            Object.Destroy(first.Root);
            Object.Destroy(second.Root);
            Object.Destroy(groupObject);
        }

        [UnityTest]
        public IEnumerator EchoSequenceCollectsFragmentAndUnlocksExitOnlyOnce()
        {
            MemoryAbilityState.SetEchoUnlocked(false);
            var echoObject = new GameObject("EchoSequenceTest");
            var child = new GameObject("ChildEchoTest");
            var grandfather = new GameObject("GrandfatherEchoTest");
            var silhouette = new GameObject("UnknownSilhouetteTest");
            child.SetActive(false);
            grandfather.SetActive(false);
            silhouette.SetActive(false);
            var collectorObject = new GameObject("FragmentCollectorTest");
            var collector = collectorObject.AddComponent<MemoryFragmentCollector>();
            var exitObject = new GameObject("ExitUnlockerTest");
            var exit = exitObject.AddComponent<ExitUnlocker>();
            exit.Configure(null, string.Empty);
            var fragment = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            fragment.Configure("test_fragment", "Test Fragment", "A test memory.");
            var sequence = echoObject.AddComponent<MemoryEchoSequence>();
            sequence.Configure(null, null, child, grandfather, silhouette, null, fragment, collector, exit, 0f);

            sequence.Begin();
            sequence.Begin();
            yield return new WaitForSeconds(0.1f);

            Assert.That(sequence.IsCompleted, Is.True);
            Assert.That(child.activeSelf, Is.False);
            Assert.That(grandfather.activeSelf, Is.False);
            Assert.That(silhouette.activeSelf, Is.False);
            Assert.That(collector.Count, Is.EqualTo(1));
            Assert.That(collector.HasCollected("test_fragment"), Is.True);
            Assert.That(exit.IsUnlocked, Is.True);
            Assert.That(exit.IsDoorOpen, Is.True);
            Assert.That(MemoryAbilityState.EchoUnlocked, Is.True);

            Object.Destroy(echoObject);
            Object.Destroy(child);
            Object.Destroy(grandfather);
            Object.Destroy(silhouette);
            Object.Destroy(collectorObject);
            Object.Destroy(exitObject);
            Object.Destroy(fragment);
            MemoryAbilityState.SetEchoUnlocked(false);
        }

        private static RecallableSetup CreateRecallable(string name, float duration)
        {
            var root = new GameObject(name);
            var present = new GameObject("PresentState");
            present.transform.SetParent(root.transform, false);
            var memory = new GameObject("MemoryState");
            memory.transform.SetParent(root.transform, false);
            var preview = new GameObject("Preview");
            preview.transform.SetParent(memory.transform, false);
            var materialized = new GameObject("Materialized");
            materialized.transform.SetParent(memory.transform, false);
            var remembered = GameObject.CreatePrimitive(PrimitiveType.Cube);
            remembered.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            remembered.transform.localScale = new Vector3(3f, 0.2f, 7f);
            remembered.transform.SetParent(materialized.transform, false);
            var recallable = root.AddComponent<MemoryRecallable>();
            recallable.Configure(present, memory, preview, materialized, duration);
            return new RecallableSetup(root, present, memory, remembered.GetComponent<Collider>(), recallable);
        }

        private sealed class RecallableSetup
        {
            public readonly GameObject Root;
            public readonly GameObject Present;
            public readonly GameObject Memory;
            public readonly Collider Collider;
            public readonly MemoryRecallable Recallable;

            public RecallableSetup(GameObject root, GameObject present, GameObject memory, Collider collider, MemoryRecallable recallable)
            {
                Root = root;
                Present = present;
                Memory = memory;
                Collider = collider;
                Recallable = recallable;
            }
        }
    }
}
