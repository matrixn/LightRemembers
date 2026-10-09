using System.Collections;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class HollowMemoryPlayModeTests
    {
        private GameObject _player;
        private GameObject _hollowObject;
        private MemoryIntegrity _integrity;
        private MemoryCorruptionController _corruption;
        private MemoryRemnant _recallRemnant;
        private MemoryRemnant _echoRemnant;

        [SetUp]
        public void SetUp()
        {
            MemoryAbilityState.RestoreAllMemories();
            MemoryAbilityState.SetEchoUnlocked(true);
            MemoryAbilityState.SetSteadyLightUnlocked(false);
            _player = new GameObject("HollowTestPlayer");
            _integrity = _player.AddComponent<MemoryIntegrity>();
            _integrity.Configure(3);
            _recallRemnant = CreateRemnant("RecallRemnantTest");
            _echoRemnant = CreateRemnant("EchoRemnantTest");
            _corruption = _player.AddComponent<MemoryCorruptionController>();
            _corruption.Configure(_integrity, _recallRemnant, _echoRemnant,
                null, null, null, null, 0.05f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_player != null) Object.Destroy(_player);
            if (_hollowObject != null) Object.Destroy(_hollowObject);
            if (_recallRemnant != null) Object.Destroy(_recallRemnant.gameObject);
            if (_echoRemnant != null) Object.Destroy(_echoRemnant.gameObject);
            MemoryAbilityState.RestoreAllMemories();
            MemoryAbilityState.SetEchoUnlocked(false);
            MemoryAbilityState.SetSteadyLightUnlocked(false);
            yield return null;
        }

        [Test]
        public void MemoryIntegrityStartsFullAndHasNoNumericHealthContract()
        {
            Assert.That(_integrity.MaximumSegments, Is.EqualTo(3));
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(3));
            Assert.That(MemoryAbilityState.MemoryLightAvailable, Is.True);
        }

        [UnityTest]
        public IEnumerator HollowHitCorruptsRecallThenEchoAndRemnantsRestoreTheMatchingMemory()
        {
            Assert.That(_corruption.TryReceiveHollowHit(), Is.True);
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(2));
            Assert.That(MemoryAbilityState.RecallCorrupted, Is.True);
            Assert.That(MemoryAbilityState.RecallAvailable, Is.False);
            Assert.That(MemoryAbilityState.EchoAvailable, Is.True);
            Assert.That(_recallRemnant.gameObject.activeSelf, Is.True);

            Assert.That(_corruption.TryReceiveHollowHit(), Is.False, "A duplicate hit inside the immunity window must be ignored.");
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(2));
            _recallRemnant.SetRevealed(true);
            _recallRemnant.Interact(_player.transform);
            Assert.That(MemoryAbilityState.RecallAvailable, Is.True);
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(3));
            Assert.That(_recallRemnant.gameObject.activeSelf, Is.False);

            yield return new WaitForSeconds(0.06f);
            Assert.That(_corruption.TryReceiveHollowHit(), Is.True);
            Assert.That(MemoryAbilityState.EchoCorrupted, Is.True);
            Assert.That(MemoryAbilityState.EchoAvailable, Is.False);
            Assert.That(MemoryAbilityState.MemoryLightAvailable, Is.True);
            _echoRemnant.SetRevealed(true);
            _echoRemnant.Interact(_player.transform);
            Assert.That(MemoryAbilityState.EchoAvailable, Is.True);
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(3));
            Assert.That(_echoRemnant.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator ThirdMemoryHitCollapsesToCheckpointAndRestoresBothAbilities()
        {
            var character = _player.AddComponent<CharacterController>();
            character.height = 1.8f;
            character.center = Vector3.up * 0.9f;
            var respawner = _player.AddComponent<LightRemembers.Player.CheckpointRespawner>();
            var checkpoint = new GameObject("HollowTestCheckpoint").transform;
            checkpoint.position = new Vector3(7f, 0.1f, 11f);
            respawner.SetCheckpoint(checkpoint);
            var handler = _player.AddComponent<MemoryCollapseHandler>();
            handler.Configure(null, null, null, null, respawner, _integrity, _corruption,
                null, null, 0f, 0f, 0f);
            _corruption.Configure(_integrity, _recallRemnant, _echoRemnant,
                handler, null, null, null, 0.01f);
            _player.transform.position = new Vector3(20f, 0.1f, 20f);

            Assert.That(_corruption.TryReceiveHollowHit(), Is.True);
            yield return new WaitForSeconds(0.02f);
            Assert.That(_corruption.TryReceiveHollowHit(), Is.True);
            yield return new WaitForSeconds(0.02f);
            Assert.That(_corruption.TryReceiveHollowHit(), Is.True);
            while (handler.IsCollapsing)
                yield return null;

            Assert.That(_player.transform.position, Is.EqualTo(checkpoint.position));
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(3));
            Assert.That(MemoryAbilityState.RecallAvailable, Is.True);
            Assert.That(MemoryAbilityState.EchoAvailable, Is.True);
            Assert.That(_corruption.CorruptionStage, Is.EqualTo(0));
            Object.Destroy(checkpoint.gameObject);
        }

        [Test]
        public void MemoryLightExposureStopsInstabilityThenRepelsTheHollow()
        {
            var player = new GameObject("ExposureTestPlayer");
            _hollowObject = new GameObject("ExposureTestHollow");
            _hollowObject.AddComponent<CharacterController>();
            var hollow = _hollowObject.AddComponent<HollowController>();
            hollow.Configure(player.transform, null, null, _corruption, null, null,
                null, _hollowObject.GetComponentsInChildren<Renderer>(true), null);
            hollow.Wake();

            hollow.TickLightExposure(true, 0.9f);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Observe));
            hollow.TickLightExposure(true, 0.2f);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Unstable));
            hollow.TickLightExposure(true, 1.4f);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Repelled));
            hollow.TickLightExposure(false, 0.2f);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Return));
            Assert.That(hollow.RepelExposureSeconds, Is.EqualTo(2.5f).Within(0.01f));
            Object.Destroy(player);
        }

        [Test]
        public void SteadyLightShortensRepelTimeAndSanctuaryBlocksMemoryLoss()
        {
            var safeObject = new GameObject("TestSanctuary");
            var safeCollider = safeObject.AddComponent<SphereCollider>();
            var safeLight = safeObject.AddComponent<SanctuaryLight>();
            safeLight.Configure(4f, null, _corruption);
            _corruption.Configure(_integrity, _recallRemnant, _echoRemnant,
                null, null, safeLight, null, 0.05f);
            Assert.That(_corruption.TryReceiveHollowHit(), Is.False);
            Assert.That(_integrity.CurrentSegments, Is.EqualTo(3));
            Assert.That(safeLight.CanEnter(safeObject.transform.position), Is.False);

            _hollowObject = new GameObject("SteadyLightTestHollow");
            _hollowObject.AddComponent<CharacterController>();
            var hollow = _hollowObject.AddComponent<HollowController>();
            hollow.Configure(_player.transform, null, null, _corruption, safeLight, null,
                null, _hollowObject.GetComponentsInChildren<Renderer>(true), null);
            MemoryAbilityState.SetSteadyLightUnlocked(true);
            Assert.That(hollow.RepelExposureSeconds, Is.EqualTo(1.8f).Within(0.01f));
            hollow.Wake();
            hollow.TickLightExposure(true, 1.8f);
            Assert.That(hollow.State, Is.EqualTo(HollowState.Repelled));
            Assert.That(hollow.CanEnter(safeObject.transform.position), Is.False);
            Object.Destroy(safeObject);
        }

        [UnityTest]
        public IEnumerator HollowTelegraphsItsAttackAndDoesNotChainHits()
        {
            var player = new GameObject("TelegraphTestPlayer");
            player.transform.position = new Vector3(0f, 0.1f, 1.1f);
            var playerCollider = player.AddComponent<CharacterController>();
            playerCollider.height = 1.8f;
            playerCollider.center = Vector3.up * 0.9f;
            var integrity = player.AddComponent<MemoryIntegrity>();
            integrity.Configure(3);
            var corruption = player.AddComponent<MemoryCorruptionController>();
            corruption.Configure(integrity, null, null, null, null, null, null, 0.1f);

            _hollowObject = new GameObject("TelegraphTestHollow");
            _hollowObject.transform.position = new Vector3(0f, 0.1f, 0f);
            _hollowObject.AddComponent<CharacterController>();
            var hollow = _hollowObject.AddComponent<HollowController>();
            hollow.Configure(player.transform, null, null, corruption, null, null,
                null, _hollowObject.GetComponentsInChildren<Renderer>(true), null);
            hollow.Wake();
            yield return new WaitForSeconds(4f);

            Assert.That(integrity.CurrentSegments, Is.EqualTo(2), "The close-range wind-up should produce one memory hit.");
            Assert.That(hollow.AttackCooldownRemaining, Is.GreaterThan(0f));
            yield return new WaitForSeconds(1.1f);
            Assert.That(integrity.CurrentSegments, Is.EqualTo(2), "The Hollow must not chain an immediate second hit.");
            Object.Destroy(player);
        }

        [Test]
        public void MemoryWorldReactorsRespondToEventAndFragmentCannotBeAwardedTwice()
        {
            var root = new GameObject("MemoryReactionTest");
            var fragments = new GameObject("RecallShards");
            fragments.transform.SetParent(root.transform);
            fragments.SetActive(false);
            var reactor = root.AddComponent<MemoryStateVisualReactor>();
            reactor.Configure(MemoryAbility.Recall, null, fragments);
            MemoryAbilityState.CorruptRecall();
            Assert.That(fragments.activeSelf, Is.True);
            MemoryAbilityState.RestoreMemory(MemoryAbility.Recall);
            Assert.That(fragments.activeSelf, Is.False);

            var fragment = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            fragment.Configure("test-memory-fear-03", "Look at Me", "A test fragment.");
            var collectorObject = new GameObject("FragmentCollectorTest");
            var collector = collectorObject.AddComponent<MemoryFragmentCollector>();
            Assert.That(collector.TryCollect(fragment), Is.True);
            Assert.That(collector.TryCollect(fragment), Is.False);
            Assert.That(collector.Count, Is.EqualTo(1));
            Object.Destroy(root);
            Object.Destroy(collectorObject);
            Object.Destroy(fragment);
        }

        [UnityTest]
        public IEnumerator HollowEncounterNarrativeAwardsFragmentAndSteadyLightOnce()
        {
            var fragment = ScriptableObject.CreateInstance<MemoryFragmentDefinition>();
            fragment.Configure("memory_fear_03", "Look at Me", "I knew her voice. I still can't remember her face.");
            var collectorObject = new GameObject("NarrativeCollectorTest");
            var collector = collectorObject.AddComponent<MemoryFragmentCollector>();
            var presenterObject = new GameObject("NarrativeSubtitleTest");
            var canvasGroup = presenterObject.AddComponent<CanvasGroup>();
            var textObject = new GameObject("NarrativeTextTest");
            textObject.transform.SetParent(presenterObject.transform);
            var text = textObject.AddComponent<UnityEngine.UI.Text>();
            var presenter = presenterObject.AddComponent<LightRemembers.UI.SubtitlePresenter>();
            presenter.Configure(canvasGroup, text);
            var dialogue = ScriptableObject.CreateInstance<DialogueSequence>();
            dialogue.Configure(new[] { new DialogueLine("UnknownChild", "Then look at me.", 0.1f) });
            var child = new GameObject("NarrativeChild");
            var unknown = new GameObject("NarrativeUnknownChild");
            var face = new GameObject("NarrativeFace");
            var distortion = new GameObject("NarrativeDistortion");
            _hollowObject = new GameObject("NarrativeHollow");
            _hollowObject.AddComponent<CharacterController>();
            var hollow = _hollowObject.AddComponent<HollowController>();
            var echoObject = new GameObject("HollowMemoryEchoTest");
            var echo = echoObject.AddComponent<HollowMemoryEcho>();
            echo.Configure(dialogue, presenter, child, unknown, face, distortion, fragment,
                collector, hollow, null, null, null, 0.01f);
            echo.Begin();
            while (!echo.IsCompleted)
                yield return null;

            Assert.That(collector.HasCollected("memory_fear_03"), Is.True);
            Assert.That(MemoryAbilityState.SteadyLightUnlocked, Is.True);
            echo.Begin();
            Assert.That(collector.Count, Is.EqualTo(1));

            Object.Destroy(echoObject);
            Object.Destroy(collectorObject);
            Object.Destroy(presenterObject);
            Object.Destroy(dialogue);
            Object.Destroy(fragment);
            Object.Destroy(child);
            Object.Destroy(unknown);
            Object.Destroy(face);
            Object.Destroy(distortion);
        }

        private MemoryRemnant CreateRemnant(string name)
        {
            var root = new GameObject(name);
            root.AddComponent<SphereCollider>();
            var remnant = root.AddComponent<MemoryRemnant>();
            remnant.Configure(MemoryAbility.Recall, _corruption, root.GetComponentsInChildren<Renderer>(true), null);
            return remnant;
        }
    }
}
