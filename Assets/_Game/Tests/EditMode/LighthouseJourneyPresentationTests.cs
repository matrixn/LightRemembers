using System.Collections.Generic;
using System.Linq;
using LightRemembers.Memory;
using LightRemembers.Narrative;
using LightRemembers.Player;
using LightRemembers.UI;
using LightRemembers.Environment;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
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
                var visualRoot = player.transform.Find("PlayerCharacterVisual");
                Assert.That(visualRoot, Is.Not.Null);
                Assert.That(Vector3.Dot(visualRoot.forward, Vector3.back), Is.GreaterThan(0.99f),
                    "The opening pose should face along the village route, not back toward the camera.");
                var animationController = animator.runtimeAnimatorController as AnimatorController;
                Assert.That(animationController, Is.Not.Null);
                Assert.That(animationController.parameters.Any(parameter => parameter.name == "Jump" &&
                    parameter.type == AnimatorControllerParameterType.Trigger), Is.True,
                    "A single press should request one jump animation, independent of how long Space is held.");
                Assert.That(animationController.layers[0].stateMachine.states
                    .Any(state => state.state.name == "JumpLand"), Is.True);
                var importedClips = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/ThirdParty/Quaternius/UniversalAnimationLibrary/UAL1_Standard.fbx")
                    .OfType<AnimationClip>().ToArray();
                foreach (var loopName in new[] { "Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Sprint_Loop", "Jump_Loop" })
                    Assert.That(importedClips.Single(clip => clip.name == "Armature|" + loopName).isLooping, Is.True,
                        $"The {loopName} clip needs looping enabled in its import settings.");
                Assert.That(player.GetComponent<MemoryLightBeamVisual>(), Is.Not.Null,
                    "Memory Light needs a visible cone and contact highlight, not just a surface splash.");
                var lightOrigin = player.GetComponentsInChildren<Transform>(true)
                    .Single(transform => transform.name == "MemoryLightOrigin");
                var armPose = player.GetComponent<MemoryLightArmPose>();
                Assert.That(armPose, Is.Not.Null);
                Assert.That(armPose.HasArmRig, Is.True, "Memory Light should have a usable right-arm pose rig.");
                Assert.That(armPose.TrackedHand.name, Is.EqualTo("hand_r"));
                Assert.That(armPose.LightOrigin, Is.SameAs(lightOrigin),
                    "The beam origin should follow the palm without reparenting an object inside the player prefab.");
                Assert.That(Vector3.Distance(lightOrigin.position, armPose.TrackedHand.position), Is.LessThan(0.1f));
                var motes = lightOrigin.Find("MemoryLight_Motes");
                Assert.That(motes.GetComponent<ParticleSystem>(), Is.Not.Null);
                var particleSystem = motes.GetComponent<ParticleSystem>();
                Assert.That(particleSystem.main.startSize.constantMin, Is.GreaterThan(0f));
                Assert.That(particleSystem.main.startSize.constantMax, Is.LessThanOrEqualTo(0.0025f),
                    "Motes should read as very fine dust, not large glowing squares.");
                Assert.That(particleSystem.velocityOverLifetime.enabled, Is.True);
                Assert.That(particleSystem.noise.enabled, Is.True,
                    "The dust needs slow drift instead of simply spraying straight out.");
                Assert.That(particleSystem.emission.rateOverTime.constant, Is.Zero,
                    "Motes are placed directly throughout the beam instead of spraying from the palm.");
                var moteDrift = motes.GetComponent<MemoryLightMoteDrift>();
                Assert.That(moteDrift, Is.Not.Null);
                Assert.That(moteDrift.EmissionRate, Is.GreaterThan(30f));
                Assert.That(moteDrift.BeamVisual, Is.SameAs(player.GetComponent<MemoryLightBeamVisual>()));
                Assert.That(motes.GetComponent<ParticleSystemRenderer>().sharedMaterial.shader.name,
                    Is.EqualTo("Universal Render Pipeline/Particles/Unlit"),
                    "The Memory Light motes need a URP particle material instead of the magenta missing-material fallback.");
                Assert.That(player.GetComponent<PlayerMemoryLight>().MaximumRange, Is.GreaterThanOrEqualTo(18f));

                Assert.That(FindInScene<GrassWindSway>(roots), Has.Length.EqualTo(1));
                var groundCover = FindInScene<MeadowGroundCover>(roots).Single();
                Assert.That(groundCover.SourceMeshCount, Is.EqualTo(2));
                Assert.That(groundCover.PatchCount, Is.EqualTo(7));
                Assert.That(groundCover.DensityPerSquareMeter, Is.GreaterThan(3f));
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
