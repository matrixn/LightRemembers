using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Lets the standalone prototype scene start with Echo available during development.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryAbilityBootstrap : MonoBehaviour
    {
        [SerializeField] private bool unlockEchoInDevelopment = true;

        private void Awake()
        {
            if (unlockEchoInDevelopment && Debug.isDebugBuild)
                MemoryAbilityState.UnlockEcho();
        }
    }
}
