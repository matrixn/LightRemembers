using UnityEngine;

namespace LightRemembers.Environment
{
    /// <summary>Low-cost, deterministic breeze motion for grouped meadow plants.</summary>
    [DisallowMultipleComponent]
    public sealed class GrassWindSway : MonoBehaviour
    {
        [SerializeField] private Transform[] plants = System.Array.Empty<Transform>();
        [SerializeField, Min(0f)] private float angle = 3.5f;
        [SerializeField, Min(0f)] private float frequency = 1.15f;

        private Quaternion[] _restRotations = System.Array.Empty<Quaternion>();
        private float[] _phaseOffsets = System.Array.Empty<float>();

        public void Configure(Transform[] targets, float swayAngle = 3.5f, float swayFrequency = 1.15f)
        {
            plants = targets ?? System.Array.Empty<Transform>();
            angle = Mathf.Max(0f, swayAngle);
            frequency = Mathf.Max(0f, swayFrequency);
            CacheRestPose();
        }

        private void Awake() => CacheRestPose();

        private void CacheRestPose()
        {
            _restRotations = new Quaternion[plants.Length];
            _phaseOffsets = new float[plants.Length];
            for (var i = 0; i < plants.Length; i++)
            {
                if (plants[i] != null)
                {
                    _restRotations[i] = plants[i].localRotation;
                    _phaseOffsets[i] = (i * 2.3999632f) % (Mathf.PI * 2f);
                }
            }
        }

        private void Update()
        {
            var time = Time.time * frequency;
            for (var i = 0; i < plants.Length; i++)
            {
                var plant = plants[i];
                if (plant == null) continue;
                var sway = Mathf.Sin(time + _phaseOffsets[i]) * angle;
                plant.localRotation = Quaternion.Euler(sway * 0.65f, 0f, sway) * _restRotations[i];
            }
        }
    }
}
