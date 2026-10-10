using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Lets the standalone prototype scene start with Echo available during development.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryAbilityBootstrap : MonoBehaviour
    {
        [SerializeField] private bool unlockEchoInDevelopment = true;
        [SerializeField] private bool unlockForgetInDevelopment;

        private void Awake()
        {
            if (unlockEchoInDevelopment && Debug.isDebugBuild)
                MemoryAbilityState.UnlockEcho();
            if (unlockForgetInDevelopment && Debug.isDebugBuild)
                MemoryAbilityState.SetForgetUnlocked(true);
        }

        public void ConfigureDevelopmentUnlocks(bool echo, bool forget)
        {
            unlockEchoInDevelopment = echo;
            unlockForgetInDevelopment = forget;
        }
    }
}
