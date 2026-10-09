using System;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class RecallEchoGate : MonoBehaviour
    {
        [SerializeField] private MemoryRecallable recallRequirement;
        [SerializeField] private MemoryEchoable echoRequirement;
        [SerializeField] private GameObject physicalBarrier;
        private bool _isOpen;

        public bool IsOpen => _isOpen;
        public event Action Opened;

        public void Configure(MemoryRecallable recall, MemoryEchoable echo, GameObject barrier)
        {
            Unsubscribe();
            recallRequirement = recall;
            echoRequirement = echo;
            physicalBarrier = barrier;
            Subscribe();
            _isOpen = false;
            if (physicalBarrier != null)
                physicalBarrier.SetActive(true);
        }

        private void OnEnable()
        {
            Subscribe();
            TryOpen();
        }

        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (recallRequirement != null)
                recallRequirement.StateChanged += OnRecallStateChanged;
            if (echoRequirement != null)
                echoRequirement.EchoCompleted += OnEchoCompleted;
        }

        private void Unsubscribe()
        {
            if (recallRequirement != null)
                recallRequirement.StateChanged -= OnRecallStateChanged;
            if (echoRequirement != null)
                echoRequirement.EchoCompleted -= OnEchoCompleted;
        }

        private void OnRecallStateChanged(RecallState state)
        {
            if (state != RecallState.Recalled)
                return;
            TryOpen();
        }

        private void OnEchoCompleted() => TryOpen();

        private void TryOpen()
        {
            if (_isOpen || recallRequirement == null || echoRequirement == null ||
                recallRequirement.State != RecallState.Recalled || echoRequirement.State != EchoState.Revealed && echoRequirement.State != EchoState.Idle)
                return;
            if (echoRequirement.MovingObject == null || echoRequirement.Path == null ||
                !echoRequirement.Path.TryEvaluate(1f, out var destination, out _))
                return;
            if (Vector3.Distance(echoRequirement.MovingObject.position, destination) > 0.08f)
                return;

            _isOpen = true;
            if (physicalBarrier != null)
                physicalBarrier.SetActive(false);
            Opened?.Invoke();
        }
    }
}
