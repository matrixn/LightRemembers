using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class HollowWakeTrigger : MonoBehaviour
    {
        [SerializeField] private HollowController hollow;

        public void Configure(HollowController controller)
        {
            hollow = controller;
            var trigger = GetComponent<Collider>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<LightRemembers.Player.CheckpointRespawner>() != null)
                hollow?.Wake();
        }
    }
}
