using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Authored reversible world response to a forgettable support/mechanism.</summary>
    [DisallowMultipleComponent]
    public sealed class ForgetConsequence : MonoBehaviour
    {
        [SerializeField] private MemoryForgettable source;
        [SerializeField] private Renderer[] consequenceRenderers;
        [SerializeField] private Collider[] consequenceColliders;
        [SerializeField] private bool activeWhenSourceForgotten = true;
        [SerializeField] private bool[] _rendererDefaults;
        [SerializeField] private bool[] _colliderDefaults;

        public void Configure(MemoryForgettable memory, Renderer[] renderers, Collider[] colliders,
            bool activeWhenForgotten = true)
        {
            if (source != null)
                source.StateChanged -= OnSourceStateChanged;
            source = memory;
            consequenceRenderers = renderers;
            consequenceColliders = colliders;
            activeWhenSourceForgotten = activeWhenForgotten;
            CacheDefaults(true);
            Apply(source != null && source.State == ForgetState.Forgotten);
            if (isActiveAndEnabled && source != null)
                source.StateChanged += OnSourceStateChanged;
        }

        private void Awake() => CacheDefaults();

        private void OnEnable()
        {
            if (source != null)
            {
                source.StateChanged += OnSourceStateChanged;
                Apply(source.State == ForgetState.Forgotten);
            }
        }

        private void OnDisable()
        {
            if (source != null)
                source.StateChanged -= OnSourceStateChanged;
        }

        private void OnSourceStateChanged(ForgetState state) => Apply(state == ForgetState.Forgotten);

        private void Apply(bool forgotten)
        {
            var show = forgotten == activeWhenSourceForgotten;
            if (consequenceRenderers != null)
                for (var i = 0; i < consequenceRenderers.Length; i++)
                    if (consequenceRenderers[i] != null)
                        consequenceRenderers[i].enabled = show && _rendererDefaults[i];
            if (consequenceColliders != null)
                for (var i = 0; i < consequenceColliders.Length; i++)
                    if (consequenceColliders[i] != null)
                        consequenceColliders[i].enabled = show && _colliderDefaults[i];
        }

        private void CacheDefaults(bool force = false)
        {
            var rendererCount = consequenceRenderers != null ? consequenceRenderers.Length : 0;
            if (force || _rendererDefaults == null || _rendererDefaults.Length != rendererCount)
            {
                _rendererDefaults = new bool[rendererCount];
                for (var i = 0; i < rendererCount; i++)
                    _rendererDefaults[i] = consequenceRenderers[i] != null && consequenceRenderers[i].enabled;
            }
            var colliderCount = consequenceColliders != null ? consequenceColliders.Length : 0;
            if (force || _colliderDefaults == null || _colliderDefaults.Length != colliderCount)
            {
                _colliderDefaults = new bool[colliderCount];
                for (var i = 0; i < colliderCount; i++)
                    _colliderDefaults[i] = consequenceColliders[i] != null && consequenceColliders[i].enabled;
            }
        }
    }
}
