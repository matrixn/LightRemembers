using UnityEngine;

namespace LightRemembers.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ResetVolume : MonoBehaviour
    {
        private void Awake() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            var respawner = other.GetComponentInParent<CheckpointRespawner>();
            if (respawner != null) respawner.Respawn();
        }
    }
}
