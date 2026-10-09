using System.Collections;
using LightRemembers.Memory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class MemoryRecallablePlayModeTests
    {
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
