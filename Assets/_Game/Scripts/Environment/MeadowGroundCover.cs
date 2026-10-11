using System;
using UnityEngine;

namespace LightRemembers.Environment
{
    /// <summary>Instanced, low grass carpet with inexpensive wind and player-footstep bending.</summary>
    [DisallowMultipleComponent]
    public sealed class MeadowGroundCover : MonoBehaviour
    {
        [Serializable]
        public struct Patch
        {
            public Vector3 center;
            public Vector2 size;

            public Patch(Vector3 center, Vector2 size)
            {
                this.center = center;
                this.size = size;
            }
        }

        private struct Blade
        {
            public Vector3 position;
            public float yaw;
            public float scale;
            public float phase;
        }

        private const int MaxInstancesPerDraw = 1023;
        [SerializeField] private Mesh[] meshes = Array.Empty<Mesh>();
        [SerializeField] private Material[] materials = Array.Empty<Material>();
        [SerializeField] private Patch[] patches = Array.Empty<Patch>();
        [SerializeField] private Transform player;
        [SerializeField] private Player.PlayerInputReader inputReader;
        [SerializeField] private CharacterController characterController;
        [SerializeField, Min(0.1f)] private float densityPerSquareMeter = 12f;
        [SerializeField, Min(0f)] private float windAngle = 3.2f;
        [SerializeField, Min(0f)] private float windFrequency = 0.85f;
        [SerializeField, Min(0.1f)] private float footstepRadius = 0.9f;
        [SerializeField, Min(0f)] private float footstepBend = 34f;

        private Blade[][][] _blades;
        private Matrix4x4[] _drawMatrices;
        private int _lastBendingCount;
        private Vector3 _previousPlayerPosition;
        private bool _hasPreviousPlayerPosition;

        public int BladeCount { get; private set; }
        public int LastBendingCount => _lastBendingCount;
        public int SourceMeshCount => meshes != null ? meshes.Length : 0;
        public int PatchCount => patches != null ? patches.Length : 0;
        public float DensityPerSquareMeter => densityPerSquareMeter;

        public void Configure(Mesh[] sourceMeshes, Material[] sourceMaterials, Patch[] coverPatches,
            Transform playerTransform, Player.PlayerInputReader reader, CharacterController controller)
        {
            meshes = sourceMeshes ?? Array.Empty<Mesh>();
            materials = sourceMaterials ?? Array.Empty<Material>();
            patches = coverPatches ?? Array.Empty<Patch>();
            player = playerTransform;
            inputReader = reader;
            characterController = controller;
            BuildInstances();
        }

        public void BindPlayer(Transform playerTransform, Player.PlayerInputReader reader, CharacterController controller)
        {
            player = playerTransform;
            inputReader = reader;
            characterController = controller;
        }

        private void Awake()
        {
            if (player != null && inputReader == null) inputReader = player.GetComponent<Player.PlayerInputReader>();
            if (player != null && characterController == null) characterController = player.GetComponent<CharacterController>();
            if (_blades == null) BuildInstances();
        }

        private void OnEnable()
        {
            if (_blades == null) BuildInstances();
        }

        private void LateUpdate()
        {
            if (_blades == null || meshes.Length == 0 || materials.Length != meshes.Length) return;

            var playerPosition = player != null ? player.position : Vector3.one * 100000f;
            var movedByTransform = player != null && _hasPreviousPlayerPosition &&
                                   (playerPosition - _previousPlayerPosition).sqrMagnitude > 0.0004f;
            var moving = (inputReader != null && inputReader.Move.sqrMagnitude > 0.03f || movedByTransform) &&
                         (characterController == null || characterController.isGrounded);
            if (player != null)
            {
                _previousPlayerPosition = playerPosition;
                _hasPreviousPlayerPosition = true;
            }
            var stepPulse = moving ? 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(Time.time * 8.5f)) : 0f;
            _lastBendingCount = 0;
            for (var patchIndex = 0; patchIndex < _blades.Length; patchIndex++)
            for (var meshIndex = 0; meshIndex < meshes.Length; meshIndex++)
            {
                var group = _blades[patchIndex][meshIndex];
                var count = group.Length;
                for (var start = 0; start < count; start += MaxInstancesPerDraw)
                {
                    var batchCount = Mathf.Min(MaxInstancesPerDraw, count - start);
                    for (var i = 0; i < batchCount; i++)
                    {
                        var blade = group[start + i];
                        var wind = Mathf.Sin(Time.time * windFrequency + blade.phase) * windAngle;
                        var bend = Vector3.zero;
                        if (moving)
                        {
                            var offset = blade.position - playerPosition;
                            offset.y = 0f;
                            var distance = offset.magnitude;
                            if (distance < footstepRadius)
                            {
                                var amount = (1f - distance / footstepRadius) * stepPulse;
                                bend = new Vector3(offset.z, 0f, -offset.x).normalized * (footstepBend * amount);
                                _lastBendingCount++;
                            }
                        }

                        var rotation = Quaternion.Euler(wind * 0.45f + bend.x, blade.yaw,
                            wind + bend.z);
                        _drawMatrices[i] = Matrix4x4.TRS(blade.position, rotation, Vector3.one * blade.scale);
                    }

                    Graphics.DrawMeshInstanced(meshes[meshIndex], 0, materials[meshIndex],
                        _drawMatrices, batchCount, null, UnityEngine.Rendering.ShadowCastingMode.Off,
                        false, gameObject.layer);
                }
            }
        }

        private void BuildInstances()
        {
            if (meshes == null || materials == null || patches == null || meshes.Length == 0 ||
                materials.Length != meshes.Length || patches.Length == 0)
            {
                _blades = null;
                BladeCount = 0;
                return;
            }

            var random = new System.Random(241011);
            _blades = new Blade[patches.Length][][];
            BladeCount = 0;
            var areaRatio = patches.Length > 0 ? 0.61f : 0f; // Leave the paved trail and house clearings open.
            for (var p = 0; p < patches.Length; p++)
            {
                _blades[p] = new Blade[meshes.Length][];
                var patch = patches[p];
                var targetCount = Mathf.RoundToInt(Mathf.Max(0f, patch.size.x * patch.size.y * areaRatio) * densityPerSquareMeter);
                var lists = new System.Collections.Generic.List<Blade>[meshes.Length];
                for (var m = 0; m < lists.Length; m++) lists[m] = new System.Collections.Generic.List<Blade>(targetCount / meshes.Length + 8);
                var accepted = 0;
                var attempts = Mathf.Max(100, targetCount * 8);
                for (var attempt = 0; attempt < attempts && accepted < targetCount; attempt++)
                {
                    var x = patch.center.x + ((float)random.NextDouble() - 0.5f) * patch.size.x;
                    var z = patch.center.z + ((float)random.NextDouble() - 0.5f) * patch.size.y;
                    if (Mathf.Abs(x) < 2.25f || IsHouseClearing(x, z)) continue;
                    var meshIndex = random.Next(meshes.Length);
                    var scale = meshIndex == 0 ? Range(random, 0.18f, 0.25f) : Range(random, 0.13f, 0.19f);
                    lists[meshIndex].Add(new Blade
                    {
                        position = new Vector3(x, 0.035f, z),
                        yaw = (float)random.NextDouble() * 360f,
                        scale = scale,
                        phase = (float)random.NextDouble() * Mathf.PI * 2f
                    });
                    accepted++;
                }

                for (var m = 0; m < lists.Length; m++)
                {
                    _blades[p][m] = lists[m].ToArray();
                    BladeCount += _blades[p][m].Length;
                }
            }
            _drawMatrices = new Matrix4x4[MaxInstancesPerDraw];
        }

        private static bool IsHouseClearing(float x, float z)
        {
            return (Mathf.Abs(x + 5.8f) < 3.1f && Mathf.Abs(z - 5.4f) < 3.1f) ||
                   (Mathf.Abs(x - 5.8f) < 3.1f && Mathf.Abs(z - 9f) < 3.1f) ||
                   (Mathf.Abs(x + 5.8f) < 3.1f && Mathf.Abs(z - 121.5f) < 3.1f) ||
                   (Mathf.Abs(x - 5.8f) < 3.1f && Mathf.Abs(z - 126.5f) < 3.1f);
        }

        private static float Range(System.Random random, float min, float max) =>
            min + (float)random.NextDouble() * (max - min);
    }
}
