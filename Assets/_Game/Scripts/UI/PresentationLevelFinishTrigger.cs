using UnityEngine;

namespace LightRemembers.UI
{
    [RequireComponent(typeof(Collider))]
    public sealed class PresentationLevelFinishTrigger : MonoBehaviour
    {
        [SerializeField] private PresentationLevelFlow levelFlow;

        public void Configure(PresentationLevelFlow flow)
        {
            levelFlow = flow;
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<LightRemembers.Player.PlayerController>() != null)
                levelFlow?.CompleteLevel();
        }
    }
}
