using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BrokenBridgePuzzle : MonoBehaviour
    {
        [SerializeField] private MemoryRecallable bridge;
        [SerializeField] private Collider startPlatform;
        [SerializeField] private Collider destinationPlatform;
        [SerializeField] private Transform resetPoint;
        [SerializeField] private BoxCollider pitResetZone;

        public MemoryRecallable Bridge => bridge;
        public Collider StartPlatform => startPlatform;
        public Collider DestinationPlatform => destinationPlatform;

        public void Configure(MemoryRecallable recallable, Collider start, Collider destination, Transform respawnPoint)
        {
            bridge = recallable;
            startPlatform = start;
            destinationPlatform = destination;
            resetPoint = respawnPoint;
            pitResetZone = GetComponent<BoxCollider>();
            pitResetZone.isTrigger = true;
        }

        private void Awake()
        {
            if (bridge == null)
                bridge = GetComponentInChildren<MemoryRecallable>(true);
            if (pitResetZone == null)
                pitResetZone = GetComponent<BoxCollider>();
            if (pitResetZone != null)
                pitResetZone.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (bridge == null || bridge.State == RecallState.Recalled || resetPoint == null)
                return;

            var characterController = other.GetComponentInParent<CharacterController>();
            if (characterController == null)
                return;

            characterController.enabled = false;
            characterController.transform.SetPositionAndRotation(resetPoint.position, resetPoint.rotation);
            characterController.enabled = true;
        }
    }
}
