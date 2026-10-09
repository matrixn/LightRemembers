using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class SanctuaryLight : MonoBehaviour
    {
        [SerializeField] private SphereCollider sanctuaryVolume;
        [SerializeField] private Light warmLight;
        [SerializeField] private MemoryCorruptionController playerCorruption;
        [SerializeField, Min(0f)] private float radius = 5f;

        public float Radius => radius;
        public Vector3 Center => transform.TransformPoint(sanctuaryVolume != null ? sanctuaryVolume.center : Vector3.zero);

        public void Configure(float lightRadius, Light lightSource, MemoryCorruptionController corruption)
        {
            radius = Mathf.Max(0.1f, lightRadius);
            warmLight = lightSource;
            playerCorruption = corruption;
            sanctuaryVolume = GetComponent<SphereCollider>();
            sanctuaryVolume.isTrigger = true;
            sanctuaryVolume.radius = radius;
        }

        public bool Contains(Vector3 worldPosition)
        {
            var center = Center;
            var scale = transform.lossyScale;
            var worldRadius = radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return (worldPosition - center).sqrMagnitude <= worldRadius * worldRadius;
        }

        public bool CanEnter(Vector3 worldPosition) => !Contains(worldPosition);

        public void StabilizePlayer()
        {
            if (playerCorruption != null)
                playerCorruption.StabilizeAtSanctuary();
        }

        private void Awake()
        {
            if (sanctuaryVolume == null)
                sanctuaryVolume = GetComponent<SphereCollider>();
            if (sanctuaryVolume != null)
            {
                sanctuaryVolume.isTrigger = true;
                sanctuaryVolume.radius = radius;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var corruption = other.GetComponentInParent<MemoryCorruptionController>();
            if (corruption == null)
                return;
            playerCorruption = corruption;
            corruption.StabilizeAtSanctuary();
        }
    }
}
