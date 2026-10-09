using UnityEngine;
using LightRemembers.Narrative;

namespace LightRemembers.Interaction
{
    public sealed class AncientLever : InteractableBehaviour
    {
        [SerializeField] private MemoryEchoSequence echoSequence;
        [SerializeField] private Transform handle;
        [SerializeField] private float activatedAngle = -35f;
        private bool _activated;
        private Quaternion _restRotation;

        private void Awake()
        {
            if (handle != null) _restRotation = handle.localRotation;
        }

        public override bool CanInteract(Transform interactor) => !_activated;

        public override void Interact(Transform interactor)
        {
            if (_activated) return;
            _activated = true;
            if (handle != null) handle.localRotation = _restRotation * Quaternion.Euler(activatedAngle, 0f, 0f);
            if (echoSequence != null) echoSequence.Begin();
        }

        public void Configure(MemoryEchoSequence sequence, Transform leverHandle)
        {
            echoSequence = sequence;
            handle = leverHandle;
            if (handle != null) _restRotation = handle.localRotation;
        }
    }
}
