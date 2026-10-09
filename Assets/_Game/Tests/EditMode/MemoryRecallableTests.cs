using LightRemembers.Editor;
using LightRemembers.Memory;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Tests.EditMode
{
    public sealed class MemoryRecallableTests
    {
        private GameObject _root;
        private GameObject _present;
        private GameObject _memory;
        private GameObject _preview;
        private GameObject _materialized;
        private Collider _memoryCollider;
        private MemoryRecallable _recallable;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RecallableTest");
            _present = new GameObject("PresentState");
            _present.transform.SetParent(_root.transform, false);
            _memory = new GameObject("MemoryState");
            _memory.transform.SetParent(_root.transform, false);
            _preview = new GameObject("Preview");
            _preview.transform.SetParent(_memory.transform, false);
            _materialized = new GameObject("Materialized");
            _materialized.transform.SetParent(_memory.transform, false);
            var bridgePiece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridgePiece.name = "RememberedCollider";
            bridgePiece.transform.SetParent(_materialized.transform, false);
            _memoryCollider = bridgePiece.GetComponent<Collider>();
            _recallable = _root.AddComponent<MemoryRecallable>();
            _recallable.Configure(_present, _memory, _preview, _materialized);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [Test]
        public void BeginsInNormalStateWithRememberedColliderDisabled()
        {
            Assert.That(_recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(_present.activeSelf, Is.True);
            Assert.That(_memory.activeSelf, Is.False);
            Assert.That(_memoryCollider.enabled, Is.False);
        }

        [Test]
        public void RevealShowsGhostWithoutEnablingGameplayCollider()
        {
            _recallable.SetRevealed(true);

            Assert.That(_recallable.State, Is.EqualTo(RecallState.Revealed));
            Assert.That(_preview.activeSelf, Is.True);
            Assert.That(_materialized.activeSelf, Is.False);
            Assert.That(_memoryCollider.enabled, Is.False);
        }

        [Test]
        public void UnrevealingWithoutRecallReturnsToNormal()
        {
            _recallable.SetRevealed(true);
            _recallable.SetRevealed(false);

            Assert.That(_recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(_present.activeSelf, Is.True);
            Assert.That(_memory.activeSelf, Is.False);
            Assert.That(_memoryCollider.enabled, Is.False);
        }

        [Test]
        public void RecallCannotActivateBeforeReveal()
        {
            Assert.That(_recallable.TryRecall(), Is.False);
            Assert.That(_recallable.State, Is.EqualTo(RecallState.Normal));
            Assert.That(_memoryCollider.enabled, Is.False);
        }

        [Test]
        public void ValidRecallMaterializesRememberedGeometryAndCollider()
        {
            _recallable.SetRevealed(true);

            Assert.That(_recallable.TryRecall(), Is.True);
            Assert.That(_recallable.State, Is.EqualTo(RecallState.Recalled));
            Assert.That(_present.activeSelf, Is.False);
            Assert.That(_preview.activeSelf, Is.False);
            Assert.That(_materialized.activeSelf, Is.True);
            Assert.That(_memoryCollider.enabled, Is.True);
        }

        [Test]
        public void BrokenBridgePuzzleHasRecallableMemoryAndDisabledInitialCollider()
        {
            var scene = EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, OpenSceneMode.Single);
            var puzzleObject = EditorAutomation.FindGameObject(scene, "BrokenBridgePuzzle");
            Assert.That(puzzleObject, Is.Not.Null);
            var puzzle = puzzleObject.GetComponent<BrokenBridgePuzzle>();
            Assert.That(puzzle, Is.Not.Null);
            Assert.That(puzzle.Bridge, Is.Not.Null);
            Assert.That(puzzle.Bridge.State, Is.EqualTo(RecallState.Normal));
            Assert.That(puzzle.StartPlatform, Is.Not.Null);
            Assert.That(puzzle.DestinationPlatform, Is.Not.Null);

            var yellowCrack = puzzleObject.transform.Find("PresentState_RuinedBridge/MemoryHint_YellowCrack");
            Assert.That(yellowCrack, Is.Not.Null, "The ruined bridge entrance should be marked with a yellow memory hint.");
            var hintRenderer = yellowCrack.GetComponentInChildren<Renderer>(true);
            Assert.That(hintRenderer, Is.Not.Null);
            var hintColor = hintRenderer.sharedMaterial.GetColor("_BaseColor");
            Assert.That(hintColor.r, Is.GreaterThan(0.8f));
            Assert.That(hintColor.g, Is.GreaterThan(0.5f));
            Assert.That(hintColor.b, Is.LessThan(0.3f));
            Assert.That(yellowCrack.GetComponentsInChildren<Collider>(true), Is.Empty,
                "The visual hint must not interfere with Memory Light targeting or bridge physics.");

            var materialized = puzzleObject.transform.Find("MemoryState_IntactBridge/MaterializedBridge");
            Assert.That(materialized, Is.Not.Null);
            foreach (var rememberedCollider in materialized.GetComponentsInChildren<Collider>(true))
                Assert.That(rememberedCollider.enabled, Is.False);
        }
    }
}
