using System.Collections;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class ForgetMechanicPlayModeTests
    {
        private string _savedProgress;
        private bool _forgetUnlockedBefore;
        private bool _forgetCorruptedBefore;
        private bool _echoUnlockedBefore;
        private readonly System.Collections.Generic.List<GameObject> _objects = new System.Collections.Generic.List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _savedProgress = MemoryProgressionSave.CaptureRawSave();
            _forgetUnlockedBefore = MemoryAbilityState.ForgetUnlocked;
            _forgetCorruptedBefore = MemoryAbilityState.ForgetCorrupted;
            _echoUnlockedBefore = MemoryAbilityState.EchoUnlocked;
            MemoryProgressionSave.DeleteSave();
            MemoryAbilityState.SetForgetUnlocked(true);
            MemoryAbilityState.SetEchoUnlocked(true);
            if (MemoryAbilityState.ForgetCorrupted)
                MemoryAbilityState.RestoreMemory(MemoryAbility.Forget);
        }

        [TearDown]
        public void TearDown()
        {
            if (MemoryForgetCapacity.ActiveTarget != null)
                MemoryForgetCapacity.ActiveTarget.RestorePresentImmediately();
            foreach (var item in _objects)
                if (item != null)
                    Object.Destroy(item);
            _objects.Clear();

            if (MemoryAbilityState.ForgetCorrupted)
                MemoryAbilityState.RestoreMemory(MemoryAbility.Forget);
            MemoryAbilityState.SetForgetUnlocked(_forgetUnlockedBefore);
            if (_forgetCorruptedBefore)
                MemoryAbilityState.CorruptForget();
            MemoryAbilityState.SetEchoUnlocked(_echoUnlockedBefore);
            MemoryProgressionSave.RestoreRawSave(_savedProgress);
        }

        [UnityTest]
        public IEnumerator ForgetRequiresRevealDisablesPhysicalStateAndKeepsReferencesAlive()
        {
            var target = CreateForgettable("ForgetRevealTest", 0.15f, 0.05f);
            var renderer = target.GetComponent<Renderer>();
            var collider = target.GetComponent<Collider>();
            Assert.That(target.State, Is.EqualTo(ForgetState.Present));
            Assert.That(target.TryForget(), Is.False);

            target.SetRevealed(true);
            Assert.That(target.State, Is.EqualTo(ForgetState.Revealed));
            Assert.That(target.TryForget(), Is.True);
            target.SetRevealed(false);
            Assert.That(collider.enabled, Is.False, "A Forgotten obstacle must immediately stop blocking traversal.");
            yield return new WaitForSeconds(0.12f);
            Assert.That(target.State, Is.EqualTo(ForgetState.Forgotten));
            Assert.That(renderer.enabled, Is.False);
            Assert.That(target.gameObject.activeSelf, Is.True);
            Assert.That(target.GetComponent<MemoryForgettable>(), Is.SameAs(target), "The target and its component references stay alive.");

            yield return new WaitForSeconds(0.35f);
            Assert.That(target.State, Is.EqualTo(ForgetState.Present));
            Assert.That(renderer.enabled, Is.True);
            Assert.That(collider.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ExpirationWaitsUntilPlayerLeavesRestorationVolume()
        {
            var target = CreateForgettable("ForgetOccupancyTest", 0.1f, 0.05f);
            var player = new GameObject("ForgetOccupant", typeof(CharacterController));
            _objects.Add(player);
            player.transform.position = target.transform.position;
            target.SetRevealed(true);
            Assert.That(target.TryForget(), Is.True);

            yield return new WaitForSeconds(0.42f);
            Assert.That(target.State, Is.EqualTo(ForgetState.Forgotten),
                "An obstacle must not rematerialize around a character still inside its restoration bounds.");
            Assert.That(target.GetComponent<Collider>().enabled, Is.False);

            player.transform.position = target.transform.position + Vector3.right * 5f;
            Physics.SyncTransforms();
            target.SetRevealed(false);
            yield return new WaitForSeconds(0.3f);
            Assert.That(target.State, Is.EqualTo(ForgetState.Present));
            Assert.That(target.GetComponent<Collider>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator ForgetCapacityIsOneAndASecondTargetWaitsForSafeRestoration()
        {
            var first = CreateForgettable("ForgetCapacityFirst", 5f, 0.05f);
            var second = CreateForgettable("ForgetCapacitySecond", 5f, 0.05f);
            second.transform.position = Vector3.right * 5f;
            first.SetRevealed(true);
            Assert.That(first.TryForget(), Is.True);
            first.SetRevealed(false);
            yield return new WaitForSeconds(0.12f);
            Assert.That(first.State, Is.EqualTo(ForgetState.Forgotten));

            second.SetRevealed(true);
            Assert.That(second.TryForget(), Is.False);
            Assert.That(first.State, Is.EqualTo(ForgetState.Restoring));
            Assert.That(second.State, Is.EqualTo(ForgetState.Revealed));
            yield return new WaitForSeconds(0.15f);
            Assert.That(first.State, Is.EqualTo(ForgetState.Present));
            Assert.That(second.TryForget(), Is.True);
        }

        [UnityTest]
        public IEnumerator ForgetConsequenceIsReversibleWhenItsSupportReturns()
        {
            var supportObject = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            supportObject.name = "ConsequenceSupport";
            var support = supportObject.AddComponent<MemoryForgettable>();
            support.Configure(ForgetCategory.Mechanism,
                new[] { supportObject.GetComponent<Renderer>() }, new[] { supportObject.GetComponent<Collider>() },
                0.12f, 0.05f);
            var beam = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            beam.name = "ReversibleDroppedBeam";
            var consequenceHost = Track(new GameObject("ConsequenceHost"));
            var consequence = consequenceHost.AddComponent<ForgetConsequence>();
            consequence.Configure(support, new[] { beam.GetComponent<Renderer>() },
                new[] { beam.GetComponent<Collider>() });

            Assert.That(beam.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(beam.GetComponent<Collider>().enabled, Is.False);
            support.SetRevealed(true);
            Assert.That(support.TryForget(), Is.True);
            support.SetRevealed(false);
            yield return new WaitForSeconds(0.1f);
            Assert.That(support.State, Is.EqualTo(ForgetState.Forgotten));
            Assert.That(beam.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(beam.GetComponent<Collider>().enabled, Is.True);
            support.SetRevealed(false);

            yield return new WaitForSeconds(0.3f);
            Assert.That(support.State, Is.EqualTo(ForgetState.Present).Or.EqualTo(ForgetState.Revealed));
            Assert.That(beam.GetComponent<Renderer>().enabled, Is.False);
            Assert.That(beam.GetComponent<Collider>().enabled, Is.False);
        }

        [Test]
        public void CameraAndPlayerCriticalObjectsAreProtectedEvenIfMisconfigured()
        {
            var cameraObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _objects.Add(cameraObject);
            cameraObject.AddComponent<Camera>();
            var forgettable = cameraObject.AddComponent<MemoryForgettable>();
            forgettable.Configure(ForgetCategory.EnvironmentalProp,
                new[] { cameraObject.GetComponent<Renderer>() }, new[] { cameraObject.GetComponent<Collider>() });
            forgettable.SetRevealed(true);
            Assert.That(forgettable.TryForget(), Is.False);
            Assert.That(cameraObject.GetComponent<Collider>().enabled, Is.True);
            Assert.That(cameraObject.GetComponent<Camera>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ForgettingARecalledEntityReleasesRecallBeforeItCanBecomeForgotten()
        {
            var root = Track(new GameObject("RecallForgetEntity"));
            var present = CreateChildCube(root.transform, "PresentBlock", true);
            var memoryState = Track(new GameObject("MemoryState"));
            memoryState.transform.SetParent(root.transform, false);
            var preview = Track(new GameObject("Preview"));
            preview.transform.SetParent(memoryState.transform, false);
            var remembered = CreateChildCube(memoryState.transform, "RememberedBlock", false);
            var materialized = Track(new GameObject("Materialized"));
            materialized.transform.SetParent(memoryState.transform, false);
            remembered.transform.SetParent(materialized.transform, false);

            var recall = root.AddComponent<MemoryRecallable>();
            recall.Configure(present, memoryState, preview, materialized, 10f);
            var forget = root.AddComponent<MemoryForgettable>();
            forget.Configure(ForgetCategory.Barrier, new[] { present.GetComponent<Renderer>(), remembered.GetComponent<Renderer>() },
                new[] { present.GetComponent<Collider>(), remembered.GetComponent<Collider>() }, 5f, 0.05f,
                null, recall);
            var composite = root.AddComponent<MemoryComposite>();
            composite.Configure(recall, null, forget);

            composite.SetRevealed(true);
            Assert.That(composite.TryRecall(), Is.True);
            Assert.That(recall.State, Is.EqualTo(RecallState.Recalled));
            Assert.That(composite.TryForget(), Is.True);
            yield return new WaitForSeconds(0.3f);
            Assert.That(recall.State, Is.Not.EqualTo(RecallState.Recalled));
            Assert.That(forget.State, Is.EqualTo(ForgetState.Forgetting).Or.EqualTo(ForgetState.Forgotten));

            composite.SetRevealed(true);
            Assert.That(composite.TryRecall(), Is.False, "A forgotten entity cannot be recalled into contradictory existence.");
        }

        [UnityTest]
        public IEnumerator EchoCanMoveOnlyAfterItsAuthoredStopperIsForgotten()
        {
            var stopper = CreateForgettable("EchoStopper", 1f, 0.05f);
            var root = Track(new GameObject("GatedEcho"));
            var path = root.AddComponent<EchoPath>();
            var start = Track(new GameObject("Start"));
            var end = Track(new GameObject("End"));
            end.transform.position = Vector3.right;
            path.Configure(new[] { start.transform, end.transform }, 0.1f, 0f, false);
            var moving = CreateChildCube(root.transform, "MovingBlock", true);
            var preview = Track(new GameObject("EchoPreview"));
            preview.transform.SetParent(root.transform, false);
            var echo = root.AddComponent<MemoryEchoable>();
            echo.Configure(path, moving.transform, preview, null, moving.GetComponentsInChildren<Renderer>(true));
            echo.ConfigureForgottenBlockers(stopper);
            echo.SetRevealed(true);
            Assert.That(echo.TryEcho(), Is.False);

            stopper.SetRevealed(true);
            Assert.That(stopper.TryForget(), Is.True);
            stopper.SetRevealed(false);
            yield return new WaitForSeconds(0.2f);
            echo.SetRevealed(true);
            Assert.That(echo.TryEcho(), Is.True);
            yield return new WaitForSeconds(0.2f);
            Assert.That(echo.State, Is.EqualTo(EchoState.Revealed));
            Assert.That(Vector3.Distance(moving.transform.position, end.transform.position), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator HollowNeedsContinuousLightThenSearchesAndReacquiresWithoutBeingRemoved()
        {
            var player = Track(new GameObject("PerceptionForgetPlayer", typeof(CharacterController)));
            var light = player.AddComponent<PlayerMemoryLight>();
            var hollowObject = Track(new GameObject("PerceptionForgetHollow", typeof(CharacterController)));
            var hollow = hollowObject.AddComponent<HollowController>();
            hollow.Configure(player.transform, null, light, null, null, null, null,
                new[] { hollowObject.GetComponent<Renderer>() }, null);
            hollow.ConfigurePerceptionForget(0.15f, 0.25f);
            hollow.Wake();
            hollow.SetRevealed(true);

            hollow.TickLightExposure(true, 0.1f);
            Assert.That(hollow.TryForget(), Is.False, "The Hollow must not lose perception before the exposure threshold.");
            hollow.TickLightExposure(false, 0.02f);
            hollow.TickLightExposure(true, 0.2f);
            Assert.That(hollow.TryForget(), Is.True);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Search));
            Assert.That(hollowObject.activeSelf, Is.True);
            Assert.That(hollowObject.GetComponent<HollowController>(), Is.SameAs(hollow));

            yield return new WaitForSeconds(0.35f);
            Assert.That(hollow.State, Is.Not.EqualTo(HollowState.Search), "It should resume normal detection after its grace period.");
            Assert.That(hollowObject.activeSelf, Is.True, "Perception Forget never removes The Hollow.");
        }

        [Test]
        public void ForgetUnlockAndArchiveFragmentPersistWithoutSavingTemporaryObjectState()
        {
            MemoryAbilityState.SetForgetUnlocked(false);
            Assert.That(MemoryAbilityState.UnlockForget(), Is.True);
            MemoryProgressionSave.Load();
            Assert.That(MemoryProgressionSave.IsForgetUnlocked, Is.True);
            Assert.That(MemoryAbilityState.ForgetAvailable, Is.True);
            var definition = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            definition.Configure("test_forget_" + System.Guid.NewGuid().ToString("N"), "Not Here", "Test memory.");
            var collector = Track(new GameObject("PersistentFragmentCollector")).AddComponent<MemoryFragmentCollector>();
            Assert.That(collector.TryCollect(definition), Is.True);
            Assert.That(MemoryProgressionSave.HasFragment(definition.FragmentId), Is.True);
            Assert.That(collector.TryCollect(definition), Is.False);
            var secondCollector = Track(new GameObject("SecondPersistentCollector")).AddComponent<MemoryFragmentCollector>();
            Assert.That(secondCollector.HasCollected(definition.FragmentId), Is.True);
            Object.Destroy(definition);

            var newWorldObject = CreateForgettable("UnserializedForgetState", 1f, 0.05f);
            newWorldObject.SetRevealed(true);
            Assert.That(newWorldObject.TryForget(), Is.True);
            var reloadedWorldObject = CreateForgettable("ReloadedObject", 1f, 0.05f);
            Assert.That(reloadedWorldObject.State, Is.EqualTo(ForgetState.Present),
                "Forget timers and temporary absence are not part of the progression save.");
        }

        private MemoryForgettable CreateForgettable(string name, float duration, float transition)
        {
            var gameObject = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            gameObject.name = name;
            var forgettable = gameObject.AddComponent<MemoryForgettable>();
            forgettable.Configure(ForgetCategory.Obstacle, new[] { gameObject.GetComponent<Renderer>() },
                new[] { gameObject.GetComponent<Collider>() }, duration, transition);
            return forgettable;
        }

        private GameObject CreateChildCube(Transform parent, string name, bool present)
        {
            var item = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            item.name = name;
            item.transform.SetParent(parent, false);
            item.SetActive(present);
            return item;
        }

        private GameObject Track(GameObject item)
        {
            _objects.Add(item);
            return item;
        }
    }
}
