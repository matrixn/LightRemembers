using UnityEngine;

namespace LightRemembers.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Transform respawnPoint;
        private Collider _trigger;

        private void Awake()
        {
            _trigger = GetComponent<Collider>();
            _trigger.isTrigger = true;
            if (respawnPoint == null) respawnPoint = transform;
        }

        public void Configure(Transform point) => respawnPoint = point;

        private void OnTriggerEnter(Collider other)
        {
            var respawner = other.GetComponentInParent<CheckpointRespawner>();
            if (respawner != null) respawner.SetCheckpoint(respawnPoint);
        }
    }
}
