using LightRemembers.Memory;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Interaction
{
    /// <summary>One-shot world interaction that grants and persists an authored memory ability.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryAbilityUnlock : InteractableBehaviour
    {
        [SerializeField] private MemoryAbility ability = MemoryAbility.Forget;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField] private TextMesh prompt;
        private bool _unlocked;

        public bool IsUnlocked => _unlocked || IsAbilityUnlocked();

        public void Configure(MemoryAbility grant, SubtitlePresenter presenter, TextMesh contextualPrompt)
        {
            ability = grant;
            subtitles = presenter;
            prompt = contextualPrompt;
            RefreshPrompt();
        }

        public override bool CanInteract(Transform interactor) => !IsUnlocked;

        private void Start() => RefreshPrompt();

        public override void Interact(Transform interactor)
        {
            if (IsUnlocked)
                return;

            if (ability == MemoryAbility.Forget)
                MemoryAbilityState.UnlockForget();
            else if (ability == MemoryAbility.Echo)
                MemoryAbilityState.UnlockEcho();
            else
                return;

            _unlocked = true;
            RefreshPrompt();
            subtitles?.Show(string.Empty, ability == MemoryAbility.Forget
                ? "The archive remembers how to let go."
                : "The movement returns to memory.");
        }

        private bool IsAbilityUnlocked() => ability == MemoryAbility.Forget
            ? MemoryAbilityState.ForgetUnlocked
            : ability == MemoryAbility.Echo && MemoryAbilityState.EchoUnlocked;

        private void RefreshPrompt()
        {
            if (prompt != null)
                prompt.text = IsUnlocked ? string.Empty : "E - TOUCH THE ARCHIVE SEAL";
        }
    }
}
