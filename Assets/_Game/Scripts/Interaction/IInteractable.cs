using UnityEngine;

namespace LightRemembers.Interaction
{
    public interface IInteractable
    {
        bool CanInteract(Transform interactor);
        void Interact(Transform interactor);
    }

    public abstract class InteractableBehaviour : MonoBehaviour, IInteractable
    {
        public abstract bool CanInteract(Transform interactor);
        public abstract void Interact(Transform interactor);
    }
}
