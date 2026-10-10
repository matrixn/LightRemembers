using System;
using System.Collections.Generic;
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>
    /// Small prototype progression save. Only permanent progression is stored;
    /// temporary world states and Forget timers are deliberately omitted.
    /// </summary>
    public static class MemoryProgressionSave
    {
        private const string PlayerPrefsKey = "LightRemembers.Progression.v1";
        private static SaveData _data;

        public static bool IsForgetUnlocked => Data.forgetUnlocked;

        public static void SetForgetUnlocked(bool unlocked)
        {
            Data.forgetUnlocked = unlocked;
            Save();
        }

        public static bool HasFragment(string fragmentId)
        {
            if (string.IsNullOrWhiteSpace(fragmentId))
                return false;
            return Data.fragments.Contains(fragmentId);
        }

        public static bool TryAddFragment(string fragmentId)
        {
            if (string.IsNullOrWhiteSpace(fragmentId) || !Data.fragments.Add(fragmentId))
                return false;
            Save();
            return true;
        }

        public static void Save()
        {
            Data.fragmentsList = new List<string>(Data.fragments).ToArray();
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void Load()
        {
            _data = null;
            _ = Data;
        }

        public static void DeleteSave()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
            _data = new SaveData();
        }

        public static string CaptureRawSave() => PlayerPrefs.HasKey(PlayerPrefsKey)
            ? PlayerPrefs.GetString(PlayerPrefsKey)
            : null;

        public static void RestoreRawSave(string rawSave)
        {
            if (string.IsNullOrEmpty(rawSave))
                PlayerPrefs.DeleteKey(PlayerPrefsKey);
            else
                PlayerPrefs.SetString(PlayerPrefsKey, rawSave);
            PlayerPrefs.Save();
            Load();
        }

        private static SaveData Data
        {
            get
            {
                if (_data != null)
                    return _data;

                _data = new SaveData();
                if (!PlayerPrefs.HasKey(PlayerPrefsKey))
                    return _data;

                try
                {
                    var loaded = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(PlayerPrefsKey));
                    if (loaded != null)
                    {
                        _data = loaded;
                        _data.fragments ??= new HashSet<string>();
                        if (_data.fragmentsList != null)
                            foreach (var id in _data.fragmentsList)
                                if (!string.IsNullOrWhiteSpace(id))
                                    _data.fragments.Add(id);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Progression save could not be read; starting with safe defaults. {exception.Message}");
                    _data = new SaveData();
                }
                return _data;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public bool forgetUnlocked;
            public string[] fragmentsList = Array.Empty<string>();
            [NonSerialized] public HashSet<string> fragments = new HashSet<string>();
        }
    }
}
