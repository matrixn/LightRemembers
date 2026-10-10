using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryStateVisualReactor : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MemoryAbility ability = MemoryAbility.Recall;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private GameObject corruptionFragments;
        [SerializeField, Min(0f)] private float stableEmission = 1f;
        [SerializeField, Range(0f, 1f)] private float corruptedEmission = 0.18f;
        private MaterialPropertyBlock _propertyBlock;

        public void Configure(MemoryAbility affectedAbility, Renderer[] targets, GameObject fragments = null)
        {
            ability = affectedAbility;
            renderers = targets;
            corruptionFragments = fragments;
            Apply(GetAvailability());
        }

        private void OnEnable()
        {
            MemoryAbilityState.MemoryStateChanged += OnMemoryStateChanged;
            Apply(GetAvailability());
        }

        private void OnDisable() => MemoryAbilityState.MemoryStateChanged -= OnMemoryStateChanged;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnMemoryStateChanged(MemoryStateChange change)
        {
            if (change.Ability == ability)
                Apply(change.IsAvailable);
        }

        private bool GetAvailability()
        {
            switch (ability)
            {
                case MemoryAbility.Recall: return MemoryAbilityState.RecallAvailable;
                case MemoryAbility.Echo: return MemoryAbilityState.EchoAvailable;
                default: return MemoryAbilityState.ForgetAvailable;
            }
        }

        private void Apply(bool available)
        {
            if (corruptionFragments != null)
                corruptionFragments.SetActive(!available);
            if (renderers == null)
                return;
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null)
                    continue;
                targetRenderer.GetPropertyBlock(_propertyBlock);
                var shared = targetRenderer.sharedMaterial;
                var baselineEmission = shared != null && shared.HasProperty(EmissionColorId)
                    ? shared.GetColor(EmissionColorId)
                    : Color.white;
                var baselineColor = shared != null && shared.HasProperty(BaseColorId)
                    ? shared.GetColor(BaseColorId)
                    : Color.white;
                var factor = available ? stableEmission : corruptedEmission;
                _propertyBlock.SetColor(EmissionColorId, baselineEmission * factor);
                if (!available)
                    _propertyBlock.SetColor(BaseColorId, new Color(baselineColor.r, baselineColor.g, baselineColor.b, 0.58f));
                else
                    _propertyBlock.SetColor(BaseColorId, baselineColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
