using System.Collections;
using LightRemembers.Memory;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class LighthouseApproachPlayModeTests
    {
        [UnityTest]
        public IEnumerator PontoonNeedsRecallThenEchoAndItsColliderMovesWithIt()
        {
            var echoWasUnlocked = MemoryAbilityState.EchoUnlocked;
            var recallWasCorrupted = MemoryAbilityState.RecallCorrupted;
            var echoWasCorrupted = MemoryAbilityState.EchoCorrupted;
            var load = SceneManager.LoadSceneAsync("LighthouseApproach01", LoadSceneMode.Additive);
            Assert.That(load, Is.Not.Null);
            yield return load;

            var scene = SceneManager.GetSceneByName("LighthouseApproach01");
            Assert.That(scene.IsValid() && scene.isLoaded, Is.True);
            SceneManager.SetActiveScene(scene);
            if (!MemoryAbilityState.RecallAvailable)
                MemoryAbilityState.RestoreMemory(MemoryAbility.Recall);
            if (!MemoryAbilityState.EchoUnlocked)
                MemoryAbilityState.UnlockEcho();
            if (!MemoryAbilityState.EchoAvailable)
                MemoryAbilityState.RestoreMemory(MemoryAbility.Echo);

            MemoryComposite composite = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                composite = root.GetComponentInChildren<MemoryComposite>(true);
                if (composite != null)
                    break;
            }

            Assert.That(composite, Is.Not.Null);
            var recall = composite.Recall;
            var echo = composite.Echo;
            var pontoonCollider = composite.transform.Find("MemoryState/MaterializedMemory/RememberedPontoon")
                .GetComponent<Collider>();
            var ghostPreview = composite.transform.Find("MemoryState/GhostPreview").gameObject;
            var motionPreview = echo.Path.transform.parent.Find("LighthousePontoon_MotionPreview").gameObject;
            Assert.That(recall.State, Is.EqualTo(RecallState.Normal));
            Assert.That(pontoonCollider.enabled, Is.False);
            Assert.That(ghostPreview.activeSelf, Is.False);
            Assert.That(motionPreview.activeSelf, Is.False);
            composite.SetRevealed(true);
            Assert.That(ghostPreview.activeSelf, Is.True, "Memory Light should reveal the remembered form.");
            Assert.That(motionPreview.activeSelf, Is.True, "Memory Light should reveal the remembered movement path.");
            Assert.That(echo.TryEcho(), Is.False, "Echo must not move a pontoon that has not been recalled.");
            Assert.That(echo.LastFailureFeedback, Does.Contain("shape is gone"));

            Assert.That(composite.TryRecall(), Is.True);
            Assert.That(recall.State, Is.EqualTo(RecallState.Recalled));
            Assert.That(pontoonCollider.enabled, Is.True);
            Assert.That(ghostPreview.activeSelf, Is.False);
            var destination = echo.Path.Waypoints[1].position;
            echo.Path.Configure(echo.Path.Waypoints, 0.2f, 0f, false);
            Assert.That(composite.TryEcho(), Is.True);
            Assert.That(echo.State, Is.EqualTo(EchoState.Echoing));
            yield return new WaitForSeconds(0.35f);
            Assert.That(echo.State, Is.EqualTo(EchoState.Revealed));
            Assert.That(Vector3.Distance(echo.MovingObject.position, destination), Is.LessThan(0.05f));
            Assert.That(pontoonCollider.enabled, Is.True, "Echo should move the recalled physical pontoon, not its preview.");

            CheckpointRespawner respawner = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                respawner = root.GetComponentInChildren<CheckpointRespawner>(true);
                if (respawner != null)
                    break;
            }
            Assert.That(respawner, Is.Not.Null);
            Transform sceneRoot = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "LighthouseApproach01")
                    sceneRoot = root.transform;
            var startRespawn = sceneRoot != null
                ? sceneRoot.Find("Gameplay/Checkpoints/Checkpoint_ApproachStart_Spawn")
                : null;
            Assert.That(startRespawn, Is.Not.Null);
            respawner.SetCheckpoint(startRespawn);
            respawner.transform.position = new Vector3(0f, -10f, 11f);
            respawner.Respawn();
            Assert.That(Vector3.Distance(respawner.transform.position, startRespawn.position), Is.LessThan(0.01f),
                "A fall must safely return the player to the approach checkpoint.");
            Assert.That(respawner.GetComponent<CharacterController>().enabled, Is.True);

            var unload = SceneManager.UnloadSceneAsync(scene);
            if (unload != null)
                yield return unload;
            MemoryAbilityState.SetEchoUnlocked(echoWasUnlocked);
            if (recallWasCorrupted)
                MemoryAbilityState.CorruptRecall();
            if (echoWasCorrupted)
                MemoryAbilityState.CorruptEcho();
        }
    }
}
