using LightRemembers.Player;
using UnityEngine;

namespace LightRemembers.UI
{
    [RequireComponent(typeof(Collider))]
    public sealed class PresentationObjectiveTrigger : MonoBehaviour
    {
        [SerializeField] private PresentationLevelFlow levelFlow;
        [SerializeField, Min(0)] private int objectiveIndex;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;

        public void Configure(PresentationLevelFlow flow, int index, string objectiveTitle, string helperText)
        {
            levelFlow = flow;
            objectiveIndex = index;
            title = objectiveTitle;
            description = helperText;
            var trigger = GetComponent<Collider>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null || levelFlow == null)
                return;

            levelFlow.ShowObjective(objectiveIndex, title, description);
            gameObject.SetActive(false);
        }
    }
}
