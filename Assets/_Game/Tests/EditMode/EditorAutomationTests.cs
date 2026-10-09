using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LightRemembers.Editor;

namespace LightRemembers.Tests.EditMode
{
    public sealed class EditorAutomationTests
    {
        [Test]
        public void BoathousePrototypeContainsAutomationCubeAtExpectedWorldPosition()
        {
            var scene = EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, OpenSceneMode.Single);
            Assert.That(scene.IsValid(), Is.True, "The prototype scene should open successfully.");

            var testObject = EditorAutomation.FindGameObject(scene, "CodexAutomationTest");
            Assert.That(testObject, Is.Not.Null, "The automation test cube should exist in the prototype scene.");
            Assert.That(
                EditorAutomation.ValidateTransform(testObject, new Vector3(2f, 0.5f, 2f)),
                Is.True,
                "The automation test cube should be at (2, 0.5, 2).");
        }
    }
}
