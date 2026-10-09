using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Allows a set of memory objects to share one active Recall choice.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryFocusGroup : MonoBehaviour
    {
        private MemoryRecallable _active;

        public MemoryRecallable Active => _active;

        internal void Claim(MemoryRecallable recallable)
        {
            if (_active == recallable)
                return;

            if (_active != null)
            {
                _active.SetRevealed(false);
                _active.RequestSafeRelease();
            }
            _active = recallable;
        }

        public void ResetGroup()
        {
            if (_active != null)
                _active.RequestSafeRelease();
            _active = null;
        }
    }
}
