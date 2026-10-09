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
        [SerializeField] private TextMesh abilityLabel;
        [SerializeField, Min(0f)] private float idleIntensity = 0.22f;
        [SerializeField, Min(0f)] private float revealedIntensity = 1.5f;

        private Renderer _beaconRenderer;

        public MemoryAbility Ability => ability;
        public bool IsRevealed { get; private set; }

        public void Configure(MemoryAbility restoredAbility, MemoryCorruptionController controller,
            Renderer[] renderers, Light lightSource, TextMesh label = null)
        {
            ability = restoredAbility;
            corruptionController = controller;
            glowRenderers = renderers;
            glowLight = lightSource;
            abilityLabel = label;
            _beaconRenderer = GetComponent<Renderer>();
            UpdateAbilityLabel();
            Hide();
        }

        public void Appear(MemoryAbility restoredAbility, MemoryCorruptionController controller)
        {
            ability = restoredAbility;
            corruptionController = controller;
            UpdateAbilityLabel();
            IsRevealed = false;
            gameObject.SetActive(true);
            SetGlow(false);
        }

        public void SetRevealed(bool revealed)
        {
            IsRevealed = revealed;
            SetGlow(revealed);
            if (abilityLabel != null)
                abilityLabel.gameObject.SetActive(revealed && gameObject.activeInHierarchy);
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
            _beaconRenderer = GetComponent<Renderer>();
            SetGlow(false);
        }

        private void SetGlow(bool visible)
        {
            if (glowRenderers != null)
                foreach (var targetRenderer in glowRenderers)
                    if (targetRenderer != null)
                        targetRenderer.enabled = gameObject.activeInHierarchy &&
                            (visible || targetRenderer == _beaconRenderer);
            if (glowLight != null)
            {
                glowLight.enabled = gameObject.activeInHierarchy;
                glowLight.intensity = visible ? revealedIntensity : idleIntensity;
            }
            if (abilityLabel != null)
                abilityLabel.gameObject.SetActive(visible && gameObject.activeInHierarchy);
        }

        private void UpdateAbilityLabel()
        {
            if (abilityLabel != null)
                abilityLabel.text = ability == MemoryAbility.Recall ? "E - RECALL" : "E - ECHO";
        }
    }
}
