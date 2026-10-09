using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Carries CharacterController passengers along with a translating/rotating Echo object.</summary>
    [DisallowMultipleComponent]
    public sealed class EchoRideSurface : MonoBehaviour
    {
        private const int RiderCapacity = 8;
        [SerializeField] private Collider occupancyVolume;
        private readonly CharacterController[] _riders = new CharacterController[RiderCapacity];

        public int RiderCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < _riders.Length; i++)
                    if (_riders[i] != null && _riders[i].enabled && _riders[i].gameObject.activeInHierarchy)
                        count++;
                return count;
            }
        }

        public void Configure(Collider triggerVolume) => occupancyVolume = triggerVolume;

        private void OnTriggerEnter(Collider other) => AddRider(other.GetComponentInParent<CharacterController>());
        private void OnTriggerStay(Collider other) => AddRider(other.GetComponentInParent<CharacterController>());

        private void OnTriggerExit(Collider other)
        {
            var controller = other.GetComponentInParent<CharacterController>();
            for (var i = 0; i < _riders.Length; i++)
                if (_riders[i] == controller)
                    _riders[i] = null;
        }

        public void CarryRiders(Vector3 previousPosition, Quaternion previousRotation, Vector3 nextPosition, Quaternion nextRotation)
        {
            for (var i = 0; i < _riders.Length; i++)
            {
                var rider = _riders[i];
                if (rider == null || !rider.enabled || !rider.gameObject.activeInHierarchy)
                {
                    _riders[i] = null;
                    continue;
                }

                var relativePosition = rider.transform.position - previousPosition;
                var desiredPosition = nextPosition + nextRotation * (Quaternion.Inverse(previousRotation) * relativePosition);
                rider.Move(desiredPosition - rider.transform.position);
                rider.transform.rotation = nextRotation * Quaternion.Inverse(previousRotation) * rider.transform.rotation;
            }
        }

        private void AddRider(CharacterController rider)
        {
            if (rider == null)
                return;
            for (var i = 0; i < _riders.Length; i++)
            {
                if (_riders[i] == rider)
                    return;
                if (_riders[i] == null)
                {
                    _riders[i] = rider;
                    return;
                }
            }
        }
    }
}
