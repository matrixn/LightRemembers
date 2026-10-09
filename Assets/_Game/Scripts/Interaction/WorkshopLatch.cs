using LightRemembers.Memory;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Interaction
{
    /// <summary>Opens a session-persistent shortcut in the Old Light workshop.</summary>
    public sealed class WorkshopLatch : InteractableBehaviour
    {
        [SerializeField] private GameObject barrier;
        [SerializeField] private SubtitlePresenter subtitles;

        public bool IsUnlocked => MemoryAbilityState.WorkshopShortcutUnlocked;

        public void Configure(GameObject doorBarrier, SubtitlePresenter presenter = null)
        {
            barrier = doorBarrier;
            subtitles = presenter;
            ApplyUnlockedState();
        }

        private void Start() => ApplyUnlockedState();

        public override bool CanInteract(Transform interactor) => !IsUnlocked;

        public override void Interact(Transform interactor)
        {
            if (!MemoryAbilityState.UnlockWorkshopShortcut())
                return;
            ApplyUnlockedState();
            subtitles?.Show(string.Empty, "A shortcut opens through the workshop.");
        }

        private void ApplyUnlockedState()
        {
            if (barrier != null && IsUnlocked)
                barrier.SetActive(false);
        }
    }
}
