using UnityEngine;

namespace LightRemembers.Interaction
{
    public sealed class OldLanternInteractable : InteractableBehaviour
    {
        public override bool CanInteract(Transform interactor) => interactor != null;

        public override void Interact(Transform interactor)
        {
            Debug.Log("Prototype interaction: the old lantern flickers to life.", this);
        }
    }
}
