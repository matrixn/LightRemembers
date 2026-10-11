using System.Collections;
using LightRemembers.Environment;
using LightRemembers.Memory;
using LightRemembers.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LightRemembers.Tests.PlayMode
{
    public sealed class MemoryLightBeamPlayModeTests
    {
        [UnityTest]
        public IEnumerator BeamFollowsConfiguredLengthAndShowsOnlyItsCurrentContact()
        {
            var origin = new GameObject("MemoryLightOrigin");
            var visualObject = new GameObject("MemoryLightVisual");
            var visual = visualObject.AddComponent<MemoryLightBeamVisual>();
            visual.Configure(origin.transform, 12f, 28f);
            yield return null;

            var contact = new Vector3(2f, 1f, 6f);
            visual.SetBeamState(true, 6f, contact, Vector3.back, true);
            yield return null;

            Assert.That(visual.BeamVisible, Is.True);
            Assert.That(visual.ContactVisible, Is.True);
            Assert.That(visual.BeamLength, Is.EqualTo(6f).Within(0.01f));
            Assert.That(visual.ContactPoint, Is.EqualTo(contact));
            var cone = visualObject.transform.Find("MemoryLight_VisibleCone");
            Assert.That(cone, Is.Not.Null);
            Assert.That(cone.gameObject.activeSelf, Is.True);
            Assert.That(cone.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(32));
            var coneVertices = cone.GetComponent<MeshFilter>().sharedMesh.vertices;
            var maximumRadius = 0f;
            foreach (var vertex in coneVertices)
                maximumRadius = Mathf.Max(maximumRadius, new Vector2(vertex.x, vertex.y).magnitude);
            Assert.That(maximumRadius, Is.EqualTo(Mathf.Tan(14f * Mathf.Deg2Rad) * 0.3f).Within(0.001f),
                "The light should taper to a restrained endpoint instead of an oversized splash.");
            Assert.That(Vector3.Distance(cone.lossyScale, Vector3.one * 6f), Is.LessThan(0.01f));
            var contactSpot = visualObject.transform.Find("MemoryLight_ContactGlow");
            Assert.That(contactSpot.gameObject.activeSelf, Is.True);
            Assert.That(Vector3.Distance(contactSpot.position, contact), Is.LessThan(0.1f));

            visual.SetBeamState(true, 12f, Vector3.zero, Vector3.up, false);
            Assert.That(visual.BeamVisible, Is.True);
            Assert.That(visual.ContactVisible, Is.False);
            Assert.That(contactSpot.gameObject.activeSelf, Is.False);
            visual.SetBeamState(false, 0f, Vector3.zero, Vector3.up, false);
            Assert.That(cone.gameObject.activeSelf, Is.False);

            Object.Destroy(visualObject);
            Object.Destroy(origin);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeadowGroundCoverBendsNearbyInstancedGrassWhenPlayerSteps()
        {
            var player = new GameObject("GrassStepPlayer");
            player.transform.position = new Vector3(4f, 0f, 0f);
            var coverObject = new GameObject("MeadowGroundCoverTest");
            var cover = coverObject.AddComponent<MeadowGroundCover>();
            var grassMesh = CreateGrassMesh("GrassMeshA");
            var wispyMesh = CreateGrassMesh("GrassMeshB");
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            var grassMaterial = new Material(shader) { enableInstancing = true };
            var wispyMaterial = new Material(shader) { enableInstancing = true };
            cover.Configure(new[] { grassMesh, wispyMesh }, new[] { grassMaterial, wispyMaterial },
                new[] { new MeadowGroundCover.Patch(new Vector3(4f, 0f, 0f), new Vector2(4f, 4f)) },
                player.transform, null, null);

            yield return null;
            player.transform.position += Vector3.right * 0.1f;
            yield return null;

            Assert.That(cover.BladeCount, Is.GreaterThan(20));
            Assert.That(cover.LastBendingCount, Is.GreaterThan(0),
                "Ground cover within the player's footstep radius should bend away from the moving player.");

            Object.Destroy(coverObject);
            Object.Destroy(player);
            Object.Destroy(grassMaterial);
            Object.Destroy(wispyMaterial);
            Object.Destroy(grassMesh);
            Object.Destroy(wispyMesh);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoldingMemoryLightRaisesArmAndKeepsBeamOriginAtThePalm()
        {
            var player = new GameObject("ArmPosePlayer");
            var model = new GameObject("CharacterRig");
            model.transform.SetParent(player.transform, false);
            var upper = new GameObject("upperarm_r").transform;
            upper.SetParent(model.transform, false);
            upper.localPosition = Vector3.up;
            var forearm = new GameObject("lowerarm_r").transform;
            forearm.SetParent(upper, false);
            forearm.localPosition = Vector3.right;
            var hand = new GameObject("hand_r").transform;
            hand.SetParent(forearm, false);
            hand.localPosition = Vector3.right * 0.5f;
            var origin = new GameObject("MemoryLightOrigin").transform;
            origin.SetParent(player.transform, false);
            var pose = player.AddComponent<MemoryLightArmPose>();
            pose.Configure(null, model.transform, origin);
            pose.SetLightHeld(true);

            yield return new WaitForSeconds(0.25f);

            var raisedDirection = (forearm.position - upper.position).normalized;
            Assert.That(pose.PoseWeight, Is.GreaterThan(0.9f));
            Assert.That(Vector3.Dot(raisedDirection, Vector3.up), Is.GreaterThan(0.8f),
                "The arm should lift toward the face/sky, not remain extended sideways.");
            Assert.That(Vector3.Distance(origin.position, hand.position), Is.LessThan(0.01f));

            pose.SetLightHeld(false);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MemoryLightMotesAreEmittedAcrossBeamVolumeWithinTaperedRadius()
        {
            var origin = new GameObject("DustBeamOrigin");
            var visualObject = new GameObject("DustBeamVisual");
            var visual = visualObject.AddComponent<MemoryLightBeamVisual>();
            visual.Configure(origin.transform, 10f, 28f);
            yield return null;
            visual.SetBeamState(true, 8f, Vector3.zero, Vector3.back, false);

            var moteObject = new GameObject("MemoryLight_Motes", typeof(ParticleSystem), typeof(ParticleSystemRenderer));
            moteObject.transform.SetParent(origin.transform, false);
            var system = moteObject.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 240;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.7f);
            var emission = system.emission;
            emission.rateOverTime = 0f;
            var shape = system.shape;
            shape.enabled = false;
            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.025f;
            noise.frequency = 0.5f;
            var drift = moteObject.AddComponent<MemoryLightMoteDrift>();
            drift.Configure(system, visual, 10f, 28f);
            system.Play();

            yield return new WaitForSeconds(1.2f);

            var particles = new ParticleSystem.Particle[system.main.maxParticles];
            var count = system.GetParticles(particles);
            var maximumDepth = 0f;
            var outsideTaper = 0;
            for (var i = 0; i < count; i++)
            {
                maximumDepth = Mathf.Max(maximumDepth, particles[i].position.z);
                var depth = Mathf.Max(0f, particles[i].position.z);
                var allowedRadius = Mathf.Tan(14f * Mathf.Deg2Rad) * 0.3f * depth + 0.08f;
                if (new Vector2(particles[i].position.x, particles[i].position.y).magnitude > allowedRadius)
                    outsideTaper++;
            }

            Assert.That(count, Is.GreaterThan(25), "Motes should remain continuously visible while the beam is active.");
            Assert.That(maximumDepth, Is.GreaterThan(5f), "Dust should populate the length of the beam, not just the palm.");
            Assert.That(outsideTaper, Is.LessThanOrEqualTo(2), "Drift should stay subtle enough to remain inside the light cone.");

            Object.Destroy(visualObject);
            Object.Destroy(origin);
            yield return null;
        }

        private static Mesh CreateGrassMesh(string meshName)
        {
            var mesh = new Mesh { name = meshName };
            mesh.vertices = new[]
            {
                new Vector3(-0.2f, 0f, 0f), new Vector3(0.2f, 0f, 0f), new Vector3(0f, 0.7f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
