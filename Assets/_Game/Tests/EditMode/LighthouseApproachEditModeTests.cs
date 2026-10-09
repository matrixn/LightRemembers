using System.Linq;
using LightRemembers.Editor;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightRemembers.Tests.EditMode
{
    public sealed class LighthouseApproachEditModeTests
    {
        [Test]
        public void LighthouseApproachHasRecoverableRecallThenEchoPontoon()
        {
            var scene = EditorSceneManager.OpenScene(LighthouseApproachSetup.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject).ToArray();

            var composite = Find<MemoryComposite>(all, "LighthousePontoonMemory");
            Assert.That(composite, Is.Not.Null);
            Assert.That(composite.Recall, Is.Not.Null);
            Assert.That(composite.Echo, Is.Not.Null);
            Assert.That(composite.Echo.RequiresRecall, Is.True);
            Assert.That(composite.Echo.RecallRequirement, Is.SameAs(composite.Recall));
            Assert.That(composite.Echo.Path.IsValid, Is.True);
            Assert.That(composite.Echo.RideSurface, Is.Not.Null);

            var remembered = all.First(item => item.name == "RememberedPontoon").GetComponent<BoxCollider>();
            Assert.That(remembered, Is.Not.Null);
            Assert.That(remembered.enabled, Is.False, "The pontoon must require Recall before it is physical.");
            Assert.That(Find<Collider>(all, "RememberedPontoon_Ghost"), Is.Null,
                "The revealed motion preview must not be physical.");
            Assert.That(composite.Echo.RideSurface.GetComponent<Collider>().isTrigger, Is.True);

            var nearBank = Find<Collider>(all, "LighthouseApproach_NearBank");
            var farBank = Find<Collider>(all, "LighthouseApproach_FarBank");
            Assert.That(farBank.bounds.min.z - nearBank.bounds.max.z, Is.GreaterThan(7f),
                "The gap should be too large to casually jump across.");
            var halfPontoonLength = remembered.size.z * remembered.transform.lossyScale.z * 0.5f;
            Assert.That(composite.Echo.Path.Waypoints[0].position.z - halfPontoonLength,
                Is.LessThan(nearBank.bounds.max.z + 0.5f),
                "The pontoon should be reachable from the approach bank after Recall.");
            Assert.That(composite.Echo.Path.Waypoints[1].position.z + halfPontoonLength,
                Is.GreaterThan(farBank.bounds.min.z - 0.5f),
                "The Echo destination should overlap the far bank for a safe dismount.");
            Assert.That(Find<Checkpoint>(all, "Checkpoint_ApproachStart"), Is.Not.Null);
            Assert.That(Find<Checkpoint>(all, "Checkpoint_ApproachFarBank"), Is.Not.Null);
            Assert.That(Find<ResetVolume>(all, "ApproachFallReset"), Is.Not.Null);
            Assert.That(Find<CheckpointRespawner>(all, "PlayerPrototype"), Is.Not.Null);

            var oldLightScene = EditorSceneManager.OpenScene(OldLight02Setup.ScenePath, OpenSceneMode.Additive);
            var exit = Find<ExitUnlocker>(oldLightScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject).ToArray(), "OldLight02Exit");
            var serializedExit = new SerializedObject(exit);
            Assert.That(serializedExit.FindProperty("returnScene").stringValue,
                Is.EqualTo(LighthouseApproachSetup.SceneName));
        }

        private static T Find<T>(GameObject[] all, string name) where T : Component =>
            all.FirstOrDefault(item => item.name == name)?.GetComponent<T>();
    }
}
