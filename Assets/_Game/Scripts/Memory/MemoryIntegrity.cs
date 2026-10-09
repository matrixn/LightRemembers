using System;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryIntegrity : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumSegments = 3;
        [SerializeField, Min(0)] private int currentSegments = 3;

        public int MaximumSegments => maximumSegments;
        public int CurrentSegments => currentSegments;
        public bool IsFull => currentSegments >= maximumSegments;
        public bool IsEmpty => currentSegments <= 0;
        public event Action<int, int> Changed;

        public void Configure(int segments)
        {
            maximumSegments = Mathf.Max(1, segments);
            SetCurrent(maximumSegments);
        }

        public bool LoseOne()
        {
            if (IsEmpty)
                return false;
            SetCurrent(currentSegments - 1);
            return true;
        }

        public bool RestoreOne()
        {
            if (IsFull)
                return false;
            SetCurrent(currentSegments + 1);
            return true;
        }

        public void RestoreFull() => SetCurrent(maximumSegments);

        private void Awake()
        {
            maximumSegments = Mathf.Max(1, maximumSegments);
            currentSegments = Mathf.Clamp(currentSegments, 0, maximumSegments);
        }

        private void SetCurrent(int value)
        {
            var next = Mathf.Clamp(value, 0, maximumSegments);
            if (currentSegments == next)
                return;
            currentSegments = next;
            Changed?.Invoke(currentSegments, maximumSegments);
        }
    }
}
