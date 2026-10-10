using UnityEngine;
using System;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryRecallable : MonoBehaviour, IMemoryRecallable
    {
        private const float ExpirationPollInterval = 0.15f;
        private const float ExpirationWarningDuration = 2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("States")]
        [SerializeField] private GameObject presentState;
        [SerializeField] private GameObject memoryState;
        [SerializeField] private GameObject previewState;
        [SerializeField] private GameObject materializedState;
        [SerializeField] private Collider[] memoryColliders;
        [SerializeField] private Renderer[] previewRenderers;
        [SerializeField] private Renderer[] materializedRenderers;
        [SerializeField] private MemoryFocusGroup focusGroup;

        [Header("Recall")]
        [SerializeField, Min(0.1f)] private float recallDuration = 8f;
        [SerializeField] private LayerMask occupancyLayers = ~0;

        [Header("Prototype Appearance")]
        [SerializeField] private Color previewColor = new Color(0.42f, 0.95f, 1f, 0.32f);
        [SerializeField, ColorUsage(false, true)] private Color previewEmission = new Color(0.25f, 1.4f, 1.8f, 1f);
        [SerializeField] private Color materializedColor = new Color(0.72f, 1f, 1f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color materializedEmission = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField, Min(0f)] private float expirationPulseFrequency = 5f;

        private MaterialPropertyBlock _propertyBlock;
        private CharacterController[] _occupants;
        private RecallState _state;
        private bool _isTargeted;
        private bool _releasePending;
        private float _remainingDuration;
        private float _occupancyPollTimer;

        public RecallState State => _state;
        public bool IsRevealed => _isTargeted;
        public event Action<RecallState> StateChanged;
        public float RecallDuration
        {
            get => recallDuration;
            set => recallDuration = Mathf.Max(0.1f, value);
        }

        public void ConfigureFocusGroup(MemoryFocusGroup group) => focusGroup = group;

        /// <summary>Refreshes cached renderers/colliders after an Editor-authored state is populated.</summary>
        public void RefreshStateObjects()
        {
            previewRenderers = null;
            materializedRenderers = null;
            memoryColliders = null;
            CacheReferences();
            EnterState(_state);
        }

        public void Configure(
            GameObject present,
            GameObject memory,
            GameObject preview,
            GameObject materialized,
            float duration = 8f)
        {
            presentState = present;
            memoryState = memory;
            previewState = preview;
            materializedState = materialized;
            recallDuration = Mathf.Max(0.1f, duration);
            CacheReferences();
            _isTargeted = false;
            EnterState(RecallState.Normal);
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CacheReferences();
            EnterState(RecallState.Normal);
        }

        private void OnValidate() => recallDuration = Mathf.Max(0.1f, recallDuration);

        private void Update()
        {
            if (_state != RecallState.Recalled)
                return;

            if (!_releasePending)
            {
                _remainingDuration -= Time.deltaTime;
                if (_remainingDuration <= 0f)
                {
                    _remainingDuration = 0f;
                    _releasePending = true;
                    _occupancyPollTimer = 0f;
                }
            }

            if (_releasePending)
            {
                _occupancyPollTimer -= Time.deltaTime;
                if (_occupancyPollTimer <= 0f)
                {
                    _occupancyPollTimer = ExpirationPollInterval;
                    if (!HasCharacterOnRecalledGeometry())
                    {
                        EnterState(_isTargeted ? RecallState.Revealed : RecallState.Normal);
                        return;
                    }
                }
            }

            if (_releasePending || _remainingDuration <= ExpirationWarningDuration)
                ApplyMaterializedAppearance(GetExpirationPulse());
        }

        public void SetRevealed(bool revealed)
        {
            _isTargeted = revealed;
            if (_state == RecallState.Recalled)
                return;

            EnterState(revealed ? RecallState.Revealed : RecallState.Normal);
        }

        public bool TryRecall()
        {
            var forgettable = GetComponent<MemoryForgettable>();
            if (forgettable != null && (forgettable.State == ForgetState.Forgotten ||
                                        forgettable.State == ForgetState.Forgetting ||
                                        forgettable.State == ForgetState.Restoring))
                return false;
            if (_state != RecallState.Revealed || !MemoryAbilityState.RecallAvailable)
                return false;

            focusGroup?.Claim(this);
            _remainingDuration = recallDuration;
            _releasePending = false;
            _occupancyPollTimer = 0f;
            _occupants = UnityEngine.Object.FindObjectsByType<CharacterController>(FindObjectsInactive.Exclude);
            EnterState(RecallState.Recalled);
            return true;
        }

        public void ForceResetToPresent()
        {
            _remainingDuration = 0f;
            _releasePending = false;
            _occupancyPollTimer = 0f;
            _isTargeted = false;
            EnterState(RecallState.Normal);
        }

        /// <summary>Requests release using the same occupancy-safe policy as timed expiry.</summary>
        public void RequestSafeRelease()
        {
            if (_state != RecallState.Recalled)
                return;

            _remainingDuration = 0f;
            _releasePending = true;
            _occupancyPollTimer = 0f;
        }

        private void CacheReferences()
        {
            if (previewState != null && (previewRenderers == null || previewRenderers.Length == 0))
                previewRenderers = previewState.GetComponentsInChildren<Renderer>(true);
            if (materializedState != null)
            {
                if (materializedRenderers == null || materializedRenderers.Length == 0)
                    materializedRenderers = materializedState.GetComponentsInChildren<Renderer>(true);
                if (memoryColliders == null || memoryColliders.Length == 0)
                    memoryColliders = materializedState.GetComponentsInChildren<Collider>(true);
            }
        }

        private void EnterState(RecallState nextState)
        {
            _state = nextState;
            if (presentState != null)
                presentState.SetActive(nextState != RecallState.Recalled);
            if (memoryState != null)
                memoryState.SetActive(nextState != RecallState.Normal);
            if (previewState != null)
                previewState.SetActive(nextState == RecallState.Revealed);
            if (materializedState != null)
                materializedState.SetActive(nextState == RecallState.Recalled);

            var collidersEnabled = nextState == RecallState.Recalled;
            if (memoryColliders != null)
            {
                foreach (var memoryCollider in memoryColliders)
                {
                    if (memoryCollider != null)
                        memoryCollider.enabled = collidersEnabled;
                }
            }

            if (nextState == RecallState.Revealed)
                ApplyPreviewAppearance();
            else if (nextState == RecallState.Recalled)
                ApplyMaterializedAppearance(1f);
            StateChanged?.Invoke(nextState);
        }

        private void ApplyPreviewAppearance()
        {
            ApplyAppearance(previewRenderers, previewColor, previewEmission);
        }

        private void ApplyMaterializedAppearance(float emissionMultiplier)
        {
            ApplyAppearance(materializedRenderers, materializedColor, materializedEmission * emissionMultiplier);
        }

        private void ApplyAppearance(Renderer[] renderers, Color baseColor, Color emissionColor)
        {
            if (renderers == null)
                return;
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer == null)
                    continue;

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, baseColor);
                _propertyBlock.SetColor(LegacyColorId, baseColor);
                _propertyBlock.SetColor(EmissionColorId, emissionColor);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private float GetExpirationPulse()
        {
            if (_releasePending)
                return 1f + Mathf.Sin(Time.time * expirationPulseFrequency) * 0.2f;

            var progress = 1f - Mathf.Clamp01(_remainingDuration / ExpirationWarningDuration);
            var pulse = 0.75f + (0.25f * (0.5f + 0.5f * Mathf.Sin(Time.time * expirationPulseFrequency)));
            return Mathf.Lerp(1f, pulse, progress);
        }

        private bool HasCharacterOnRecalledGeometry()
        {
            if (memoryColliders == null || _occupants == null)
                return false;

            foreach (var occupant in _occupants)
            {
                if (occupant == null || !occupant.enabled || !occupant.gameObject.activeInHierarchy ||
                    (occupancyLayers.value & (1 << occupant.gameObject.layer)) == 0 ||
                    occupant.transform == transform || occupant.transform.IsChildOf(transform))
                    continue;

                var characterBounds = occupant.bounds;
                foreach (var memoryCollider in memoryColliders)
                {
                    if (memoryCollider == null || !memoryCollider.enabled || !memoryCollider.gameObject.activeInHierarchy)
                        continue;

                    var geometryBounds = memoryCollider.bounds;
                    geometryBounds.Expand(new Vector3(0.12f, 0.24f, 0.12f));
                    if (geometryBounds.Intersects(characterBounds))
                        return true;
                }
            }

            return false;
        }
    }
}
