using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryEncounterResetter : MonoBehaviour
    {
        [SerializeField] private MemoryRecallable[] recallables;
        [SerializeField] private MemoryEchoable[] echoables;
        [SerializeField] private MemoryFocusGroup[] focusGroups;
        [SerializeField] private MemoryRemnant[] remnants;

        public void Configure(MemoryRecallable[] recalls, MemoryEchoable[] echoes,
            MemoryFocusGroup[] groups, MemoryRemnant[] memoryRemnants)
        {
            recallables = recalls;
            echoables = echoes;
            focusGroups = groups;
            remnants = memoryRemnants;
        }

        public void ResetTransientMemory()
        {
            if (recallables != null)
                foreach (var recallable in recallables)
                    if (recallable != null)
                        recallable.ForceResetToPresent();
            if (echoables != null)
                foreach (var echoable in echoables)
                    if (echoable != null)
                        echoable.ResetToStart();
            if (focusGroups != null)
                foreach (var group in focusGroups)
                    if (group != null)
                        group.ResetGroup();
            ClearRemnants();
        }

        public void ClearRemnants()
        {
            if (remnants != null)
                foreach (var remnant in remnants)
                    if (remnant != null)
                        remnant.Hide();
        }
    }
}
