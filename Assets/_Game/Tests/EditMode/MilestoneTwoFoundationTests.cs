using LightRemembers.Interaction;
using LightRemembers.Editor;
using LightRemembers.Memory;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LightRemembers.Tests.EditMode
{
    public sealed class MilestoneTwoFoundationTests
    {
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PlayerPrototype.prefab";
        private const string InputActionsPath = "Assets/_Game/Settings/LightRemembers.inputactions";
        private const string PrototypeScenePath = "Assets/_Game/Scenes/Prototype/BoathousePrototype.unity";

        [Test]
        public void PlayerPrototypePrefabHasRequiredComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerController>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerInputReader>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerMovement>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerMemoryLight>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerInteractor>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("CameraTarget"), Is.Not.Null);
            Assert.That(prefab.transform.Find("MemoryLightOrigin"), Is.Not.Null);
            Assert.That(prefab.transform.Find("GroundCheck"), Is.Not.Null);
        }

        [Test]
        public void MemoryRevealableTransitionsBetweenTargetedAndUntargeted()
        {
            var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = "RevealableTest";
            try
            {
                var target = targetObject.AddComponent<MemoryRevealable>();
                var contract = (IMemoryLightTarget)target;
                Assert.That(contract.IsRevealed, Is.False);

                contract.SetRevealed(true);
                Assert.That(contract.IsRevealed, Is.True);
                var propertyBlock = new MaterialPropertyBlock();
                targetObject.GetComponent<Renderer>().GetPropertyBlock(propertyBlock);
                Assert.That(propertyBlock.GetColor("_EmissionColor"), Is.Not.EqualTo(Color.black));

                contract.SetRevealed(false);
                Assert.That(contract.IsRevealed, Is.False);
                targetObject.GetComponent<Renderer>().GetPropertyBlock(propertyBlock);
                Assert.That(propertyBlock.GetColor("_EmissionColor"), Is.EqualTo(Color.black));
            }
            finally
            {
                Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void RequiredPlayerInputActionsAreAvailable()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            Assert.That(actions, Is.Not.Null);
            var playerMap = actions.FindActionMap("Player", true);
            var requiredActions = new[]
            {
                "Move", "Look", "Jump", "Sprint", "Interact", "MemoryLight", "SecondaryAbility",
                "PrimaryAbility", "SecondaryAbility", "Pause"
            };
            foreach (var actionName in requiredActions)
                Assert.That(playerMap.FindAction(actionName, false), Is.Not.Null, $"Missing action: {actionName}");
        }

        [Test]
        public void PrototypeSceneContainsPlayerMemoryTargetAndLantern()
        {
            var scene = EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True);
            var player = EditorAutomation.FindGameObject(scene, "PlayerPrototype");
            var memoryObject = EditorAutomation.FindGameObject(scene, "MemoryTestObject");
            var lantern = EditorAutomation.FindGameObject(scene, "OldLantern");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.GetComponent<CharacterController>(), Is.Not.Null);
            Assert.That(memoryObject, Is.Not.Null);
            Assert.That(memoryObject.GetComponent<MemoryRevealable>(), Is.Not.Null);
            Assert.That(lantern, Is.Not.Null);
            Assert.That(lantern.GetComponent<InteractableBehaviour>(), Is.Not.Null);
        }
    }
}
