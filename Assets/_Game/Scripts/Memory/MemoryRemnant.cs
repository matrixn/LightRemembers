using LightRemembers.Interaction;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryRemnant : InteractableBehaviour, IMemoryLightTarget
    {
        [SerializeField] private MemoryAbility ability;
        [SerializeField] private MemoryCorruptionController corruptionController;
        [SerializeField] private Renderer[] glowRenderers;
        [SerializeField] private Light glowLight;
        [SerializeField, Min(0f)] private float revealedIntensity = 1.5f;

        public MemoryAbility Ability => ability;
        public bool IsRevealed { get; private set; }

        public void Configure(MemoryAbility restoredAbility, MemoryCorruptionController controller,
            Renderer[] renderers, Light lightSource)
        {
            ability = restoredAbility;
            corruptionController = controller;
            glowRenderers = renderers;
            glowLight = lightSource;
            Hide();
        }

        public void Appear(MemoryAbility restoredAbility, MemoryCorruptionController controller)
        {
            ability = restoredAbility;
            corruptionController = controller;
            IsRevealed = false;
            gameObject.SetActive(true);
            SetGlow(false);
        }

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            SetGlow(revealed);
        }

        public override bool CanInteract(Transform interactor) => IsRevealed && corruptionController != null &&
            ((ability == MemoryAbility.Recall && MemoryAbilityState.RecallCorrupted) ||
             (ability == MemoryAbility.Echo && MemoryAbilityState.EchoCorrupted));

        public override void Interact(Transform interactor)
        {
            if (CanInteract(interactor) && corruptionController.TryRestore(ability))
                Hide();
        }

        public void Hide()
        {
            IsRevealed = false;
            SetGlow(false);
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            if (glowRenderers == null || glowRenderers.Length == 0)
                glowRenderers = GetComponentsInChildren<Renderer>(true);
            SetGlow(false);
        }

        private void SetGlow(bool visible)
        {
            if (glowRenderers != null)
                foreach (var targetRenderer in glowRenderers)
                    if (targetRenderer != null)
                        targetRenderer.enabled = visible && gameObject.activeInHierarchy;
            if (glowLight != null)
            {
                glowLight.enabled = visible && gameObject.activeInHierarchy;
                glowLight.intensity = revealedIntensity;
            }
        }
    }
}
