using System.Linq;
using LightRemembers.Editor;
using LightRemembers.Memory;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LightRemembers.Tests.EditMode
{
    public sealed class MemoryEchoTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            MemoryAbilityState.SetEchoUnlocked(false);
            _root = new GameObject("EchoTestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
            MemoryAbilityState.SetEchoUnlocked(false);
        }

        [Test]
        public void MemoryEchoableStartsIdleAndRevealTransitionsToRevealed()
        {
            var setup = CreateEchoable();
            Assert.That(setup.Echo.State, Is.EqualTo(EchoState.Idle));
            setup.Echo.SetRevealed(true);
            Assert.That(setup.Echo.State, Is.EqualTo(EchoState.Revealed));
            Assert.That(setup.Preview.activeSelf, Is.True);
        }

        [Test]
        public void EchoRequiresRevealAndUnlockedAbility()
        {
            var setup = CreateEchoable();
            MemoryAbilityState.UnlockEcho();
            Assert.That(setup.Echo.TryEcho(), Is.False);
            setup.Echo.SetRevealed(true);
            MemoryAbilityState.SetEchoUnlocked(false);
            Assert.That(setup.Echo.TryEcho(), Is.False);
        }

        [Test]
        public void EchoPathValidatesPointsAndInterpolatesPositionAndRotation()
        {
            var pathObject = new GameObject("Path");
            pathObject.transform.SetParent(_root.transform, false);
            var first = new GameObject("First").transform;
            first.SetParent(_root.transform, false);
            first.position = Vector3.zero;
            var last = new GameObject("Last").transform;
            last.SetParent(_root.transform, false);
            last.position = new Vector3(8f, 4f, -2f);
            last.rotation = Quaternion.Euler(0f, 90f, 0f);
            var path = pathObject.AddComponent<EchoPath>();
            path.Configure(new[] { first, last }, 2f, 1f, true);

            Assert.That(path.IsValid, Is.True);
            Assert.That(path.TryEvaluate(1f, out var position, out var rotation), Is.True);
            Assert.That(Vector3.Distance(position, last.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(rotation, last.rotation), Is.LessThan(0.01f));
            path.Configure(new Transform[] { first, null }, 2f, 0f, false);
            Assert.That(path.IsValid, Is.False);
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(last.gameObject);
        }

        [Test]
        public void BoathouseHasConfiguredBoatAndLiftEchoPaths()
        {
            EditorSceneManager.OpenScene(EditorAutomation.PrototypeScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
            var boat = Find("OldBoat")?.GetComponent<MemoryEchoable>();
            var lift = Find("OldLift")?.GetComponent<MemoryEchoable>();
            Assert.That(boat, Is.Not.Null);
            Assert.That(lift, Is.Not.Null);
            Assert.That(boat.Path.IsValid, Is.True);
            Assert.That(lift.Path.IsValid, Is.True);
            Assert.That(boat.Path.ReturnToStart, Is.True);
            Assert.That(lift.Path.ReturnToStart, Is.True);
            Assert.That(boat.RideSurface, Is.Not.Null);
            Assert.That(lift.RideSurface, Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<MemoryAbilityBootstrap>(), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<CheckpointRespawner>(), Is.Not.Null);
            Assert.That(Find("EchoDockCheckpoint")?.GetComponent<Checkpoint>(), Is.Not.Null);
            Assert.That(FindAll("BoatDock").Count, Is.EqualTo(1));
            Assert.That(FindAll("LiftBalcony").Count, Is.EqualTo(1));
            Assert.That(FindAll("UpperEndMarker").Count, Is.EqualTo(1));
            Assert.That(Find("UpperEndMarker")?.GetComponent<Collider>(), Is.Null,
                "The arrival marker is visual guidance, not a gameplay target or raycast blocker.");
        }

        private EchoSetup CreateEchoable()
        {
            var moving = GameObject.CreatePrimitive(PrimitiveType.Cube);
            moving.name = "MovingObject";
            moving.transform.SetParent(_root.transform, false);
            var pathRoot = new GameObject("Path");
            pathRoot.transform.SetParent(_root.transform, false);
            var start = new GameObject("Start").transform;
            start.SetParent(_root.transform, false);
            var end = new GameObject("End").transform;
            end.SetParent(_root.transform, false);
            end.position = Vector3.right * 3f;
            var path = pathRoot.AddComponent<EchoPath>();
            path.Configure(new[] { start, end }, 1f, 0f, false);
            var preview = new GameObject("Preview");
            preview.transform.SetParent(_root.transform, false);
            var echo = _root.AddComponent<MemoryEchoable>();
            echo.Configure(path, moving.transform, preview, null, moving.GetComponentsInChildren<Renderer>());
            return new EchoSetup(echo, preview);
        }

        private static GameObject Find(string name) => EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(transform => transform.name == name)?.gameObject;

        private static System.Collections.Generic.List<GameObject> FindAll(string name) =>
            EditorSceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name)
                .Select(transform => transform.gameObject)
                .ToList();

        private readonly struct EchoSetup
        {
            public readonly MemoryEchoable Echo;
            public readonly GameObject Preview;
            public EchoSetup(MemoryEchoable echo, GameObject preview) { Echo = echo; Preview = preview; }
        }
    }
}
