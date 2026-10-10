using System.Linq;
using LightRemembers.Editor;
using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightRemembers.Tests.EditMode
{
    public sealed class ForgetPrototypeEditModeTests
    {
        [Test]
        public void ForgetInputUsesSharedPlayerActionMapWithKeyboardAndGamepadBindings()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(MilestoneTwoSetup.InputActionsPath);
            Assert.That(actions, Is.Not.Null);
            var action = actions.FindAction("Player/Forget", false);
            Assert.That(action, Is.Not.Null);
            Assert.That(action.type, Is.EqualTo(InputActionType.Button));
            Assert.That(action.bindings.Any(binding => binding.action == "Forget" && binding.path == "<Keyboard>/f"), Is.True);
            Assert.That(action.bindings.Any(binding => binding.action == "Forget" && binding.path == "<Gamepad>/rightStickPress"), Is.True);
        }

        [Test]
        public void ArchiveSceneComposesExplicitForgetRecallEchoAndPerceptionTargets()
        {
            var scene = EditorSceneManager.OpenScene(MilestoneTenSetup.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.gameObject).ToArray();
            MemoryForgettable FindForget(string name) => all.First(item => item.name == name).GetComponent<MemoryForgettable>();

            var wall = FindForget("ForgottenWall");
            Assert.That(wall, Is.Not.Null);
            Assert.That(wall.State, Is.EqualTo(ForgetState.Present));
            Assert.That(wall.RendererTargets.Length, Is.GreaterThan(0));
            Assert.That(wall.ColliderTargets.Length, Is.GreaterThan(0));
            Assert.That(wall.ColliderTargets.Any(collider => collider.enabled), Is.True);

            var seal = all.First(item => item.name == "ForgetUnlock_ArchiveSeal").GetComponent<MemoryAbilityUnlock>();
            Assert.That(seal, Is.Not.Null);
            Assert.That(FindForget("SupportBlock"), Is.Not.Null);
            Assert.That(all.First(item => item.name == "ArchiveHall").GetComponent<ForgetConsequence>(), Is.Not.Null);
            var droppedBeam = all.First(item => item.name == "Consequence_DroppedBeam");
            Assert.That(droppedBeam.GetComponent<Collider>().enabled, Is.False,
                "The wrong-choice consequence beam must begin retracted.");
            Assert.That(droppedBeam.GetComponent<Renderer>().enabled, Is.False);

            var echoComposite = all.First(item => item.name == "EchoBridgeMechanism").GetComponent<MemoryComposite>();
            Assert.That(echoComposite.Echo.ForgottenBlockers.Length, Is.EqualTo(1));
            Assert.That(echoComposite.Echo.ForgottenBlockers[0], Is.SameAs(FindForget("EchoPathStopper")));
            Assert.That(echoComposite.Echo.Path.IsValid, Is.True);

            var door = all.First(item => item.name == "RecallDoorAfterRubble").GetComponent<MemoryComposite>();
            Assert.That(FindForget("DoorwayRubble"), Is.Not.Null);
            Assert.That(door.Recall, Is.Not.Null);
            Assert.That(door.Recall.State, Is.EqualTo(RecallState.Normal));
            var rememberedDoorCollider = all.Where(item => item.name.StartsWith("DoorPost_") || item.name == "DoorHeader")
                .SelectMany(item => item.GetComponents<Collider>()).ToArray();
            Assert.That(rememberedDoorCollider.All(collider => !collider.enabled), Is.True);

            var hollow = all.First(item => item.name == "TheHollow").GetComponent<HollowController>();
            Assert.That(hollow, Is.Not.Null);
            Assert.That(hollow.HasPerceptionForgetConfiguration, Is.True);
            Assert.That(all.First(item => item.name == "ArchiveMemoryEchoTrigger").GetComponent<ForgottenArchiveEcho>(), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<MemoryFragmentDefinition>(
                "Assets/_Game/Art/MemoryFragments/memory_archive_04.asset"), Is.Not.Null);
            Assert.That(all.OfType<GameObject>().Any(item => item.GetComponent<Checkpoint>() != null), Is.True);
            Assert.That(all.OfType<GameObject>().Any(item => item.GetComponent<ResetVolume>() != null), Is.True);
        }
    }
}
