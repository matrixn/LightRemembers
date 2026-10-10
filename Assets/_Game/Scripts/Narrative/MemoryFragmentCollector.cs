using System.Collections.Generic;
using UnityEngine;

namespace LightRemembers.Narrative
{
    /// <summary>Scene-local collection state; fragment definitions remain immutable assets.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryFragmentCollector : MonoBehaviour
    {
        private readonly HashSet<string> _collected = new HashSet<string>();
        public int Count => _collected.Count;

        public bool HasCollected(string fragmentId) => !string.IsNullOrEmpty(fragmentId) &&
            (_collected.Contains(fragmentId) || Memory.MemoryProgressionSave.HasFragment(fragmentId));

        public bool TryCollect(MemoryFragmentDefinition fragment)
        {
            if (fragment == null || string.IsNullOrEmpty(fragment.FragmentId) ||
                HasCollected(fragment.FragmentId) || !Memory.MemoryProgressionSave.TryAddFragment(fragment.FragmentId))
                return false;

            _collected.Add(fragment.FragmentId);

            Debug.Log($"Memory Fragment collected: {fragment.Title} ({fragment.FragmentId})", this);
            return true;
        }
    }
}
