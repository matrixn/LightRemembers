using System;
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Non-persistent ability availability for the current play session.</summary>
    public static class MemoryAbilityState
    {
        private static bool _echoUnlocked;

        public static bool EchoUnlocked => _echoUnlocked;
        public static event Action EchoUnlockedChanged;

        public static void UnlockEcho()
        {
            if (_echoUnlocked)
                return;
            _echoUnlocked = true;
            EchoUnlockedChanged?.Invoke();
        }

        public static void SetEchoUnlocked(bool unlocked)
        {
            if (_echoUnlocked == unlocked)
                return;
            _echoUnlocked = unlocked;
            EchoUnlockedChanged?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            _echoUnlocked = false;
            EchoUnlockedChanged = null;
        }
    }
}
