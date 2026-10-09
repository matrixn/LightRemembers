using System;
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Non-persistent ability availability for the current play session.</summary>
    public static class MemoryAbilityState
    {
        private static bool _echoUnlocked;
        private static bool _memoryAnchorUnlocked;
        private static bool _workshopShortcutUnlocked;

        public static bool EchoUnlocked => _echoUnlocked;
        public static bool MemoryAnchorUnlocked => _memoryAnchorUnlocked;
        public static bool WorkshopShortcutUnlocked => _workshopShortcutUnlocked;
        public static int RecallCapacity => _memoryAnchorUnlocked ? 2 : 1;
        public static event Action EchoUnlockedChanged;
        public static event Action RecallCapacityChanged;

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

        public static bool UnlockMemoryAnchor()
        {
            if (_memoryAnchorUnlocked)
                return false;
            _memoryAnchorUnlocked = true;
            RecallCapacityChanged?.Invoke();
            return true;
        }

        /// <summary>Allows deterministic session setup in tests and isolated prototypes.</summary>
        public static void SetMemoryAnchorUnlocked(bool unlocked)
        {
            if (_memoryAnchorUnlocked == unlocked)
                return;
            _memoryAnchorUnlocked = unlocked;
            RecallCapacityChanged?.Invoke();
        }

        public static bool UnlockWorkshopShortcut()
        {
            if (_workshopShortcutUnlocked)
                return false;
            _workshopShortcutUnlocked = true;
            return true;
        }

        public static void SetWorkshopShortcutUnlocked(bool unlocked) => _workshopShortcutUnlocked = unlocked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForNewSession()
        {
            _echoUnlocked = false;
            _memoryAnchorUnlocked = false;
            _workshopShortcutUnlocked = false;
            EchoUnlockedChanged = null;
            RecallCapacityChanged = null;
        }
    }
}
