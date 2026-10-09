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
        private static bool _recallCorrupted;
        private static bool _echoCorrupted;
        private static bool _steadyLightUnlocked;

        public static bool EchoUnlocked => _echoUnlocked;
        public static bool RecallAvailable => !_recallCorrupted;
        public static bool EchoAvailable => _echoUnlocked && !_echoCorrupted;
        public static bool RecallCorrupted => _recallCorrupted;
        public static bool EchoCorrupted => _echoCorrupted;
        public static bool MemoryLightAvailable => true;
        public static bool SteadyLightUnlocked => _steadyLightUnlocked;
        public static float HollowRepelExposureSeconds => _steadyLightUnlocked ? 1.8f : 2.5f;
        public static bool MemoryAnchorUnlocked => _memoryAnchorUnlocked;
        public static bool WorkshopShortcutUnlocked => _workshopShortcutUnlocked;
        public static int RecallCapacity => _memoryAnchorUnlocked ? 2 : 1;
        public static event Action EchoUnlockedChanged;
        public static event Action RecallCapacityChanged;
        public static event Action<MemoryStateChange> MemoryStateChanged;
        public static event Action SteadyLightUnlockedChanged;

        public static void UnlockEcho()
        {
            if (_echoUnlocked)
                return;
            _echoUnlocked = true;
            EchoUnlockedChanged?.Invoke();
            MemoryStateChanged?.Invoke(new MemoryStateChange(MemoryAbility.Echo, EchoAvailable));
        }

        public static void SetEchoUnlocked(bool unlocked)
        {
            if (_echoUnlocked == unlocked)
                return;
            _echoUnlocked = unlocked;
            EchoUnlockedChanged?.Invoke();
            MemoryStateChanged?.Invoke(new MemoryStateChange(MemoryAbility.Echo, EchoAvailable));
        }

        public static bool CorruptRecall()
        {
            if (_recallCorrupted)
                return false;
            _recallCorrupted = true;
            MemoryStateChanged?.Invoke(new MemoryStateChange(MemoryAbility.Recall, false));
            return true;
        }

        public static bool CorruptEcho()
        {
            if (_echoCorrupted)
                return false;
            _echoCorrupted = true;
            MemoryStateChanged?.Invoke(new MemoryStateChange(MemoryAbility.Echo, false));
            return true;
        }

        public static bool RestoreMemory(MemoryAbility ability)
        {
            switch (ability)
            {
                case MemoryAbility.Recall:
                    if (!_recallCorrupted) return false;
                    _recallCorrupted = false;
                    MemoryStateChanged?.Invoke(new MemoryStateChange(ability, true));
                    return true;
                case MemoryAbility.Echo:
                    if (!_echoCorrupted) return false;
                    _echoCorrupted = false;
                    MemoryStateChanged?.Invoke(new MemoryStateChange(ability, EchoAvailable));
                    return true;
                default:
                    return false;
            }
        }

        public static void RestoreAllMemories()
        {
            RestoreMemory(MemoryAbility.Recall);
            RestoreMemory(MemoryAbility.Echo);
        }

        public static bool UnlockSteadyLight()
        {
            if (_steadyLightUnlocked)
                return false;
            _steadyLightUnlocked = true;
            SteadyLightUnlockedChanged?.Invoke();
            return true;
        }

        /// <summary>Allows deterministic setup and cleanup for isolated prototype scenes and tests.</summary>
        public static void SetSteadyLightUnlocked(bool unlocked)
        {
            if (_steadyLightUnlocked == unlocked)
                return;
            _steadyLightUnlocked = unlocked;
            SteadyLightUnlockedChanged?.Invoke();
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
            _recallCorrupted = false;
            _echoCorrupted = false;
            _steadyLightUnlocked = false;
            EchoUnlockedChanged = null;
            RecallCapacityChanged = null;
            MemoryStateChanged = null;
            SteadyLightUnlockedChanged = null;
        }
    }
}
