#if UNITY_EDITOR
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Inspector-only/development controls for testing the Hollow encounter.</summary>
    public sealed class HollowDebugTools : MonoBehaviour
    {
        [SerializeField] private MemoryCorruptionController corruption;
        [SerializeField] private HollowController hollow;

        public void Configure(MemoryCorruptionController memoryCorruption, HollowController threat)
        {
            corruption = memoryCorruption;
            hollow = threat;
        }

        [ContextMenu("Hollow Debug/Wake Hollow")]
        public void WakeHollow() => hollow?.Wake();

        [ContextMenu("Hollow Debug/Corrupt Recall")]
        public void CorruptRecall() => corruption?.DebugForceCorruption(MemoryAbility.Recall);

        [ContextMenu("Hollow Debug/Corrupt Echo")]
        public void CorruptEcho() => corruption?.DebugForceCorruption(MemoryAbility.Echo);

        [ContextMenu("Hollow Debug/Restore All Memory")]
        public void RestoreAllMemory() => corruption?.StabilizeAtSanctuary();

        [ContextMenu("Hollow Debug/Trigger Memory Collapse")]
        public void TriggerCollapse() => corruption?.DebugForceCollapse();
    }
}
#endif
