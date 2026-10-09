using LightRemembers.Player;
using UnityEngine;

namespace LightRemembers.Interaction
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private const int ColliderCapacity = 24;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform interactionOrigin;
        [SerializeField, Min(0f)] private float interactionRange = 2.25f;
        [SerializeField] private LayerMask interactionLayers = ~0;

        private readonly Collider[] _overlaps = new Collider[ColliderCapacity];

        public void Configure(PlayerInputReader reader, Transform origin)
        {
            inputReader = reader;
            interactionOrigin = origin;
        }

        private void OnEnable()
        {
            if (inputReader != null)
                inputReader.InteractPressed += TryInteract;
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.InteractPressed -= TryInteract;
        }

        private void TryInteract()
        {
            if (interactionOrigin == null)
                return;

            var count = Physics.OverlapSphereNonAlloc(
                interactionOrigin.position, interactionRange, _overlaps, interactionLayers, QueryTriggerInteraction.Collide);
            InteractableBehaviour nearest = null;
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var interactable = _overlaps[i].GetComponentInParent<InteractableBehaviour>();
                if (interactable == null || !interactable.CanInteract(transform))
                    continue;
                var distance = (interactable.transform.position - interactionOrigin.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;
                nearest = interactable;
                nearestDistance = distance;
            }

            if (nearest != null)
                nearest.Interact(transform);
        }
    }
}
