using System.Collections;
using System.Linq;
using LightRemembers.Memory;
using LightRemembers.Player;
using LightRemembers.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class LighthouseJourneyPlayModeTests
    {
        [UnityTest]
        public IEnumerator JourneyLoadsSinglePlayerAndBridgeChangesRealColliderState()
        {
            var load = SceneManager.LoadSceneAsync("LighthouseJourney", LoadSceneMode.Additive);
            Assert.That(load, Is.Not.Null, "LighthouseJourney must be included in Build Settings.");
            yield return load;

            var scene = SceneManager.GetSceneByName("LighthouseJourney");
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            var previous = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);

            var playerCount = 0;
            PlayerController player = null;
            PresentationLevelFlow flow = null;
            MemoryRecallable bridge = null;
            MemoryComposite ferry = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var scenePlayers = root.GetComponentsInChildren<PlayerController>(true);
                playerCount += scenePlayers.Length;
                if (scenePlayers.Length > 0)
                    player = scenePlayers[0];
                flow = root.GetComponentInChildren<PresentationLevelFlow>(true) ?? flow;
                var recallables = root.GetComponentsInChildren<MemoryRecallable>(true);
                foreach (var candidate in recallables)
                    if (candidate.name == "BrokenBridge_RecallPuzzle")
                        bridge = candidate;
                var composites = root.GetComponentsInChildren<MemoryComposite>(true);
                foreach (var candidate in composites)
                    if (candidate.Echo != null && candidate.Echo.RequiresRecall)
                        ferry = candidate;
            }

            Assert.That(playerCount, Is.EqualTo(1));
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.HasStarted, Is.False, "The title screen should hold gameplay until the player begins.");
            Assert.That(player.GetComponent<PlayerMovement>().enabled, Is.False);
            Assert.That(player.GetComponent<PlayerAnimationDriver>(), Is.Not.Null);
            Assert.That(player.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.Not.Null);
            var journeyUi = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(transform => transform.name == "JourneyUI");
            Assert.That(journeyUi.Find("GameplayHUD").GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(journeyUi.Find("StartScreen").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            flow.BeginLevel();
            Assert.That(flow.HasStarted, Is.True);
            Assert.That(player.GetComponent<PlayerMovement>().enabled, Is.True);
            Assert.That(flow.CurrentObjectiveIndex, Is.Zero);
            Assert.That(journeyUi.Find("GameplayHUD").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
            Assert.That(bridge, Is.Not.Null);
            Assert.That(ferry, Is.Not.Null, "The ferry should compose Recall shape with Echo movement.");
            Assert.That(ferry.Echo.RecallRequirement, Is.SameAs(ferry.Recall));

            var memoryCollider = bridge.transform.Find("MemoryState/MaterializedMemory/RememberedBridge")
                .GetComponent<Collider>();
            Assert.That(bridge.State, Is.EqualTo(RecallState.Normal));
            Assert.That(memoryCollider.enabled, Is.False);
            bridge.SetRevealed(true);
            Assert.That(bridge.transform.Find("MemoryState/GhostPreview").gameObject.activeSelf, Is.True);
            Assert.That(memoryCollider.enabled, Is.False, "A preview must remain non-physical.");
            Assert.That(bridge.TryRecall(), Is.True);
            Assert.That(memoryCollider.enabled, Is.True, "Recall must materialize working collision.");
            bridge.ForceResetToPresent();
            Assert.That(bridge.State, Is.EqualTo(RecallState.Normal));
            Assert.That(memoryCollider.enabled, Is.False);

            SceneManager.SetActiveScene(previous);
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}
