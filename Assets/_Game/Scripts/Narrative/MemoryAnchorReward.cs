using LightRemembers.Interaction;
using LightRemembers.Memory;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Narrative
{
    /// <summary>One-time Old Light reward: awards its fragment and a second Recall slot.</summary>
    public sealed class MemoryAnchorReward : InteractableBehaviour
    {
        [SerializeField] private MemoryFragmentDefinition fragment;
        [SerializeField] private MemoryFragmentCollector collector;
        [SerializeField] private SubtitlePresenter subtitles;
        private bool _collected;
        private bool _available;

        public bool IsCollected => _collected || MemoryAbilityState.MemoryAnchorUnlocked;

        private void Start()
        {
            if (IsCollected)
                gameObject.SetActive(false);
        }

        public void Configure(MemoryFragmentDefinition definition, MemoryFragmentCollector memoryCollector,
            SubtitlePresenter presenter = null)
        {
            fragment = definition;
            collector = memoryCollector;
            subtitles = presenter;
        }

        public void SetAvailable(bool available) => _available = available;

        public override bool CanInteract(Transform interactor) => _available && !IsCollected;

        public override void Interact(Transform interactor)
        {
            Award();
        }

        public bool Award()
        {
            if (IsCollected)
                return false;
            _collected = true;
            if (fragment != null)
                collector?.TryCollect(fragment);
            MemoryAbilityState.UnlockMemoryAnchor();
            subtitles?.Show(string.Empty, "Memory Anchor\nYou can hold more of the past.");
            gameObject.SetActive(false);
            return true;
        }
    }
}
