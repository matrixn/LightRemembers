using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Emits slow, microscopic memory dust throughout the currently visible beam volume.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryLightMoteDrift : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private MemoryLightBeamVisual beamVisual;
        [SerializeField, Min(1f)] private float maximumRange = 19f;
        [SerializeField, Range(1f, 89f)] private float spotAngle = 28f;
        [SerializeField, Range(0.05f, 1f)] private float radiusScale = 0.3f;
        [SerializeField, Min(1f)] private float emissionRate = 42f;
        [SerializeField, Min(1)] private int maxParticles = 240;
        [SerializeField] private Vector2 particleSize = new Vector2(0.00045f, 0.0024f);
        [SerializeField] private Vector2 lifetime = new Vector2(1.5f, 2.7f);

        private float _emissionRemainder;
        private ParticleSystem.EmitParams _emitParams;

        public ParticleSystem ParticleSystem => particleSystem;
        public MemoryLightBeamVisual BeamVisual => beamVisual;
        public float EmissionRate => emissionRate;
        public float MaximumMoteSize => particleSize.y;

        public void Configure(ParticleSystem system, MemoryLightBeamVisual visual,
            float range = 19f, float angle = 28f)
        {
            particleSystem = system;
            beamVisual = visual;
            maximumRange = Mathf.Max(1f, range);
            spotAngle = Mathf.Clamp(angle, 1f, 89f);
            _emissionRemainder = 0f;
        }

        private void Awake()
        {
            if (particleSystem == null) particleSystem = GetComponent<ParticleSystem>();
            if (beamVisual == null) beamVisual = GetComponentInParent<MemoryLightBeamVisual>();
        }

        private void LateUpdate()
        {
            if (particleSystem == null || !particleSystem.isPlaying || beamVisual == null || !beamVisual.BeamVisible)
            {
                _emissionRemainder = 0f;
                return;
            }

            var beamLength = Mathf.Clamp(beamVisual.BeamLength, 0.2f, maximumRange);
            _emissionRemainder += emissionRate * Time.deltaTime;
            var capacity = Mathf.Max(0, maxParticles - particleSystem.particleCount);
            var emitCount = Mathf.Min(Mathf.FloorToInt(_emissionRemainder), capacity);
            _emissionRemainder -= emitCount;
            if (capacity == 0) _emissionRemainder = 0f;
            for (var i = 0; i < emitCount; i++)
                EmitMote(beamLength);
        }

        private void EmitMote(float beamLength)
        {
            var depth = Random.value * beamLength;
            var endRadius = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * radiusScale * beamLength;
            var radius = Mathf.Sqrt(Random.value) * endRadius * (depth / beamLength);
            var angle = Random.value * Mathf.PI * 2f;
            _emitParams.position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, depth);
            _emitParams.velocity = new Vector3(Random.Range(-0.018f, 0.018f),
                Random.Range(-0.014f, 0.014f), Random.Range(-0.01f, 0.01f));
            _emitParams.startLifetime = Random.Range(lifetime.x, lifetime.y);
            _emitParams.startSize = Random.Range(particleSize.x, particleSize.y);
            _emitParams.startColor = new Color(0.68f, 0.94f, 1f, 0.55f);
            particleSystem.Emit(_emitParams, 1);
        }
    }
}
