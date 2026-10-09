using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Opens a passage only while two remembered structures coexist.</summary>
    [DisallowMultipleComponent]
    public sealed class DualRecallGate : MonoBehaviour
    {
        [SerializeField] private MemoryRecallable first;
        [SerializeField] private MemoryRecallable second;
        [SerializeField] private GameObject barrier;

        public bool IsOpen { get; private set; }

        public void Configure(MemoryRecallable firstRecall, MemoryRecallable secondRecall, GameObject gateBarrier)
        {
            first = firstRecall;
            second = secondRecall;
            barrier = gateBarrier;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            IsOpen = first != null && second != null &&
                first.State == RecallState.Recalled && second.State == RecallState.Recalled;
            if (barrier != null)
                barrier.SetActive(!IsOpen);
        }
    }
}
