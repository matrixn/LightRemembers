using System.Collections.Generic;
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Allows a set of memory objects to share one active Recall choice.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryFocusGroup : MonoBehaviour
    {
        private readonly List<MemoryRecallable> _activeRecalls = new List<MemoryRecallable>(2);

        public MemoryRecallable Active => _activeRecalls.Count == 0 ? null : _activeRecalls[_activeRecalls.Count - 1];
        public int ActiveCount => _activeRecalls.Count;

        internal void Claim(MemoryRecallable recallable)
        {
            if (recallable == null || _activeRecalls.Contains(recallable))
                return;

            for (var i = _activeRecalls.Count - 1; i >= 0; i--)
                if (_activeRecalls[i] == null || _activeRecalls[i].State != RecallState.Recalled)
                    _activeRecalls.RemoveAt(i);

            while (_activeRecalls.Count >= MemoryAbilityState.RecallCapacity)
            {
                var oldest = _activeRecalls[0];
                _activeRecalls.RemoveAt(0);
                if (oldest == null)
                    continue;
                oldest.SetRevealed(false);
                oldest.RequestSafeRelease();
            }
            _activeRecalls.Add(recallable);
        }

        public void ResetGroup()
        {
            for (var i = 0; i < _activeRecalls.Count; i++)
                if (_activeRecalls[i] != null)
                    _activeRecalls[i].RequestSafeRelease();
            _activeRecalls.Clear();
        }
    }
}
