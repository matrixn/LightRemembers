using UnityEngine;

namespace LightRemembers.Narrative
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class HollowMemoryEchoTrigger : MonoBehaviour
    {
        [SerializeField] private HollowMemoryEcho memoryEcho;

        public void Configure(HollowMemoryEcho echo)
        {
            memoryEcho = echo;
            var trigger = GetComponent<Collider>();
            if (trigger != null)
                trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<LightRemembers.Player.CheckpointRespawner>() != null)
                memoryEcho?.Begin();
        }
    }
}
