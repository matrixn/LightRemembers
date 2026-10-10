using System.Collections.Generic;
using System.Linq;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using LightRemembers.UI;
using LightRemembers.Environment;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Tests.EditMode
{
    public sealed class LighthouseJourneyPresentationTests
    {
        private const string ScenePath = "Assets/_Game/Scenes/Prototype/LighthouseJourney.unity";

        [Test]
        public void PresentationSceneContainsOnePlayerAndAllJourneySystems()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                var players = FindInScene<PlayerController>(roots);
                Assert.That(players, Has.Length.EqualTo(1), "The standalone level must never duplicate the player.");
                Assert.That(FindInScene<PresentationLevelFlow>(roots), Has.Length.EqualTo(1));
                Assert.That(FindInScene<MemoryRecallable>(roots).Length, Is.GreaterThanOrEqualTo(3));
                Assert.That(FindInScene<MemoryEchoable>(roots).Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(FindInScene<MemoryCorruptionController>(roots), Has.Length.EqualTo(1));
                Assert.That(FindInScene<MemoryFragmentCollector>(roots), Has.Length.EqualTo(1));
                Assert.That(FindInScene<PresentationObjectiveTrigger>(roots).Length, Is.GreaterThanOrEqualTo(5));
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ShowcaseHasReadableStartStateCharacterTownAndCameraSafety()
        {
            var scene = SceneManager.GetSceneByPath(ScenePath);
            var openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                var roots = scene.GetRootGameObjects();
                var player = FindInScene<PlayerController>(roots).Single();
                var animator = player.GetComponentInChildren<Animator>(true);
                Assert.That(player.GetComponent<PlayerAnimationDriver>(), Is.Not.Null);
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                var motes = player.transform.Find("MemoryLightOrigin/MemoryLight_Motes");
                Assert.That(motes.GetComponent<ParticleSystem>(), Is.Not.Null);
                Assert.That(motes.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name,
                    Is.EqualTo("Universal Render Pipeline/Particles/Unlit"),
                    "The Memory Light motes need a URP particle material instead of the magenta missing-material fallback.");
                Assert.That(player.GetComponent<PlayerMemoryLight>().MaximumRange, Is.GreaterThanOrEqualTo(18f));

                Assert.That(FindInScene<GrassWindSway>(roots), Has.Length.EqualTo(1));
                Assert.That(FindInScene<MonoBehaviour>(roots)
                    .Single(component => component.GetType().Name == "CinemachineDeoccluder").enabled, Is.True);
                Assert.That(FindInScene<Renderer>(roots).Count(renderer => renderer.name == "Floor_UnevenBrick"),
                    Is.GreaterThan(20), "The approach should have a visibly paved route.");

                var transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                var firstHouse = transforms.Single(transform => transform.name == "ShoreHouse_01");
                var houseColliders = firstHouse.GetComponentsInChildren<BoxCollider>(true)
                    .Where(collider => collider.name.EndsWith("_Collision") ||
                                       collider.name == "DoorJamb_Collision" ||
                                       collider.name == "DoorLintel_Collision")
                    .ToArray();
                Assert.That(houseColliders, Has.Length.EqualTo(12));
                Assert.That(houseColliders.All(collider =>
                    collider.bounds.size.x < 3.5f && collider.bounds.size.y < 3.5f && collider.bounds.size.z < 3.5f),
                    Is.True, "House collision must stay in metres; imported FBX scale must not inflate walls over the level.");
                Assert.That(houseColliders.Any(collider =>
                    collider.bounds.Contains(firstHouse.position + new Vector3(0f, 1.25f, -2f))), Is.False,
                    "The front doorway should remain open for the player and the adaptive camera.");

                var ui = transforms.Single(transform => transform.name == "JourneyUI");
                Assert.That(ui.Find("GameplayHUD").GetComponent<CanvasGroup>().alpha, Is.Zero,
                    "Gameplay HUD must not bleed through the title screen.");
                Assert.That(ui.Find("StartScreen").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(ui.Find("StartScreen/StartHint").GetComponent<RectTransform>().anchoredPosition.y,
                    Is.LessThan(-200f), "The start hint should sit below, not over, the button.");
            }
            finally
            {
                if (openedHere && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T[] FindInScene<T>(IEnumerable<GameObject> roots) where T : Component =>
            roots.SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }
}
