using System;
using LightRemembers.Player;
using UnityEngine;

namespace LightRemembers.Memory
{
    public enum ForgetCategory
    {
        Obstacle,
        Barrier,
        Mechanism,
        EnvironmentalProp,
        Perception
    }

    /// <summary>
    /// Explicitly-authored temporary absence. It disables only its configured renderers,
    /// colliders, and behaviours; the GameObject and all references remain alive.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MemoryForgettable : MonoBehaviour, IMemoryForgettable
    {
        private const float OccupancyPollInterval = 0.15f;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private ForgetCategory category = ForgetCategory.Obstacle;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] physicalColliders;
        [SerializeField] private Behaviour[] suppressedBehaviours;
        [SerializeField] private MemoryRecallable recallable;
        [SerializeField] private TextMesh contextualHint;
        [SerializeField, Min(0.1f)] private float duration = 10f;
        [SerializeField, Min(0.05f)] private float transitionDuration = 0.55f;
        [SerializeField] private LayerMask restorationOccupancyLayers = ~0;
        [SerializeField] private Color forgottenTint = new Color(0.14f, 0.1f, 0.2f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color forgetEmission = new Color(0.65f, 0.12f, 1.1f, 1f);

        private readonly Collider[] _overlapBuffer = new Collider[24];
        private bool[] _rendererWasEnabled;
        private bool[] _colliderWasEnabled;
        private bool[] _behaviourWasEnabled;
        private Color[] _baseColors;
        private Bounds _restorationBounds;
        private MaterialPropertyBlock _propertyBlock;
        private ForgetState _state;
        private bool _isRevealed;
        private bool _awaitingRecallRelease;
        private float _timer;
        private float _occupancyTimer;

        public ForgetCategory Category => category;
        public ForgetState State => _state;
        public MemoryRecallable Recallable => recallable;
        public Renderer[] RendererTargets => renderers;
        public Collider[] ColliderTargets => physicalColliders;
        public bool IsRevealed => _isRevealed;
        public float Duration { get => duration; set => duration = Mathf.Max(0.1f, value); }
        public string LastFailureFeedback { get; private set; }
        public event Action<ForgetState> StateChanged;

        public void Configure(ForgetCategory targetCategory, Renderer[] targetRenderers, Collider[] targetColliders,
            float forgetDuration = 10f, float transitionSeconds = 0.55f, Behaviour[] suppressed = null,
            MemoryRecallable recall = null)
        {
            category = targetCategory;
            renderers = targetRenderers;
            physicalColliders = targetColliders;
            duration = Mathf.Max(0.1f, forgetDuration);
            transitionDuration = Mathf.Max(0.05f, transitionSeconds);
            suppressedBehaviours = suppressed;
            recallable = recall;
            if (contextualHint != null)
                contextualHint.text = "F / RS CLICK - FORGET";
            CacheTargets();
            RestorePresentImmediately();
        }

        public void SetRevealed(bool revealed)
        {
            _isRevealed = revealed;
            if (contextualHint != null)
                contextualHint.gameObject.SetActive(revealed && gameObject.activeInHierarchy);
            if (_state == ForgetState.Present || _state == ForgetState.Revealed)
            {
                SetState(revealed ? ForgetState.Revealed : ForgetState.Present);
                ApplyCue(revealed);
            }
            else if (_state == ForgetState.Forgotten && revealed)
                ApplyCue(true);
        }

        public void ConfigureHint(TextMesh hint)
        {
            contextualHint = hint;
            RefreshHint();
        }

        public bool TryForget()
        {
            LastFailureFeedback = null;
            if (IsProtectedObject())
            {
                LastFailureFeedback = "This memory is part of the world that must remain.";
                return false;
            }
            if (!MemoryAbilityState.ForgetAvailable)
            {
                LastFailureFeedback = "The memory slips away before it can be released.";
                return false;
            }
            if (!_isRevealed || (_state != ForgetState.Revealed && _state != ForgetState.Present))
            {
                LastFailureFeedback = "There's nothing left to hold onto.";
                return false;
            }

            if (recallable != null && recallable.State == RecallState.Recalled)
            {
                _awaitingRecallRelease = true;
                recallable.RequestSafeRelease();
                return true;
            }

            return TryBeginForgetting();
        }

        public void RequestSafeRestore()
        {
            if (_state == ForgetState.Forgotten)
                BeginRestorationWhenSafe();
        }

        /// <summary>Used by scene reset and load recovery; temporary absence is never saved.</summary>
        public void RestorePresentImmediately()
        {
            _timer = 0f;
            _awaitingRecallRelease = false;
            SetRenderersEnabled(true);
            RestoreConfiguredColliders(true);
            RestoreSuppressedBehaviours();
            ApplyTint(0f, false);
            SetState(_isRevealed ? ForgetState.Revealed : ForgetState.Present);
            MemoryForgetCapacity.Release(this);
        }

        private void Awake()
        {
            CacheTargets();
            RestorePresentImmediately();
        }

        private void OnValidate()
        {
            duration = Mathf.Max(0.1f, duration);
            transitionDuration = Mathf.Max(0.05f, transitionDuration);
            if (category == ForgetCategory.Perception)
                Debug.LogWarning($"{name}: Perception targets should use a dedicated perception-forget interaction, not physical MemoryForgettable.", this);
        }

        private void OnDestroy() => MemoryForgetCapacity.Release(this);

        private void OnEnable() => MemoryAbilityState.MemoryStateChanged += OnMemoryStateChanged;

        private void OnDisable() => MemoryAbilityState.MemoryStateChanged -= OnMemoryStateChanged;

        private void Start() => RefreshHint();

        private void OnMemoryStateChanged(MemoryStateChange change)
        {
            if (change.Ability == MemoryAbility.Forget)
                RefreshHint();
        }

        private void Update()
        {
            if (_awaitingRecallRelease)
            {
                if (recallable == null || recallable.State != RecallState.Recalled)
                {
                    _awaitingRecallRelease = false;
                    TryBeginForgetting();
                }
                return;
            }

            switch (_state)
            {
                case ForgetState.Forgetting:
                    _timer += Time.deltaTime;
                    ApplyTint(Mathf.Clamp01(_timer / transitionDuration), true);
                    if (_timer >= transitionDuration)
                    {
                        SetRenderersEnabled(false);
                        SetSuppressedBehaviours(false);
                        _timer = 0f;
                        SetState(ForgetState.Forgotten);
                    }
                    break;
                case ForgetState.Forgotten:
                    _timer += Time.deltaTime;
                    if (_timer >= duration)
                        BeginRestorationPoll();
                    break;
                case ForgetState.Restoring:
                    if (IsRestorationOccupied())
                    {
                        SetRenderersEnabled(false);
                        ApplyTint(1f, false);
                        SetState(ForgetState.Forgotten);
                        return;
                    }
                    _timer += Time.deltaTime;
                    ApplyTint(1f - Mathf.Clamp01(_timer / transitionDuration), false);
                    if (_timer >= transitionDuration)
                    {
                        if (IsRestorationOccupied())
                        {
                            SetRenderersEnabled(false);
                            SetState(ForgetState.Forgotten);
                            return;
                        }
                        RestoreConfiguredColliders(true);
                        RestoreSuppressedBehaviours();
                        ApplyTint(0f, false);
                        _timer = 0f;
                        SetState(_isRevealed ? ForgetState.Revealed : ForgetState.Present);
                        MemoryForgetCapacity.Release(this);
                    }
                    break;
            }
        }

        private bool TryBeginForgetting()
        {
            if (!MemoryForgetCapacity.TryClaim(this))
            {
                LastFailureFeedback = "This memory won't let go yet.";
                return false;
            }

            recallable ??= GetComponent<MemoryRecallable>();
            RefreshRestorationBounds();
            _timer = 0f;
            RestoreConfiguredColliders(false);
            SetState(ForgetState.Forgetting);
            return true;
        }

        private void BeginRestorationWhenSafe()
        {
            if (_state != ForgetState.Forgotten || IsRestorationOccupied())
            {
                _occupancyTimer = OccupancyPollInterval;
                return;
            }
            _timer = 0f;
            SetRenderersEnabled(true);
            SetSuppressedBehaviours(true);
            RestoreConfiguredColliders(false);
            SetState(ForgetState.Restoring);
        }

        private void CacheTargets()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);
            if (physicalColliders == null || physicalColliders.Length == 0)
                physicalColliders = GetComponentsInChildren<Collider>(true);
            if (recallable == null)
                recallable = GetComponent<MemoryRecallable>();

            _rendererWasEnabled = new bool[renderers.Length];
            _baseColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                var target = renderers[i];
                _rendererWasEnabled[i] = target != null && target.enabled;
                var material = target != null ? target.sharedMaterial : null;
                _baseColors[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }

            _colliderWasEnabled = new bool[physicalColliders.Length];
            var hasBounds = false;
            for (var i = 0; i < physicalColliders.Length; i++)
            {
                var target = physicalColliders[i];
                _colliderWasEnabled[i] = target != null && target.enabled;
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                    continue;
                if (!hasBounds)
                {
                    _restorationBounds = target.bounds;
                    hasBounds = true;
                }
                else
                    _restorationBounds.Encapsulate(target.bounds);
            }

            _behaviourWasEnabled = new bool[suppressedBehaviours != null ? suppressedBehaviours.Length : 0];
            for (var i = 0; i < _behaviourWasEnabled.Length; i++)
                _behaviourWasEnabled[i] = suppressedBehaviours[i] != null && suppressedBehaviours[i].enabled;

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
        }

        private void RefreshRestorationBounds()
        {
            var hasBounds = false;
            foreach (var target in physicalColliders)
            {
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy)
                    continue;
                if (!hasBounds)
                {
                    _restorationBounds = target.bounds;
                    hasBounds = true;
                }
                else
                    _restorationBounds.Encapsulate(target.bounds);
            }
        }

        private bool IsProtectedObject()
        {
            return GetComponentInParent<PlayerController>() != null || GetComponentInParent<CheckpointRespawner>() != null ||
                   GetComponentInParent<HollowController>() != null || GetComponentInParent<MemoryCorruptionController>() != null ||
                   GetComponentInParent<MemoryIntegrity>() != null || GetComponent<Camera>() != null ||
                   GetComponent<AudioListener>() != null || CompareTag("MainCamera");
        }

        private void BeginRestorationPoll()
        {
            _occupancyTimer -= Time.deltaTime;
            if (_occupancyTimer > 0f)
                return;
            _occupancyTimer = OccupancyPollInterval;
            BeginRestorationWhenSafe();
        }

        private bool IsRestorationOccupied()
        {
            if (physicalColliders == null || physicalColliders.Length == 0 || _restorationBounds.size.sqrMagnitude <= 0f)
                return false;
            var count = Physics.OverlapBoxNonAlloc(_restorationBounds.center, _restorationBounds.extents + Vector3.one * 0.08f,
                _overlapBuffer, Quaternion.identity, restorationOccupancyLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var candidate = _overlapBuffer[i];
                _overlapBuffer[i] = null;
                if (candidate == null || candidate.transform == transform || candidate.transform.IsChildOf(transform))
                    continue;
                if (candidate.GetComponentInParent<CharacterController>() != null ||
                    candidate.GetComponentInParent<HollowController>() != null ||
                    (candidate.attachedRigidbody != null && !candidate.attachedRigidbody.isKinematic))
                    return true;
            }
            return false;
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (renderers == null)
                return;
            for (var i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].enabled = enabled && _rendererWasEnabled[i];
        }

        private void RestoreConfiguredColliders(bool enabled)
        {
            if (physicalColliders == null)
                return;
            for (var i = 0; i < physicalColliders.Length; i++)
                if (physicalColliders[i] != null)
                    physicalColliders[i].enabled = enabled && _colliderWasEnabled[i];
        }

        private void SetSuppressedBehaviours(bool enabled)
        {
            if (suppressedBehaviours == null)
                return;
            for (var i = 0; i < suppressedBehaviours.Length; i++)
                if (suppressedBehaviours[i] != null)
                    suppressedBehaviours[i].enabled = enabled && _behaviourWasEnabled[i];
        }

        private void RestoreSuppressedBehaviours() => SetSuppressedBehaviours(true);

        private void ApplyCue(bool revealed) => ApplyTint(0f, revealed);

        private void RefreshHint()
        {
            if (contextualHint == null)
                return;
            contextualHint.text = MemoryAbilityState.ForgetAvailable
                ? "F / RS CLICK - FORGET"
                : MemoryAbilityState.ForgetCorrupted ? "F / RS CLICK - LOST" : "F / RS - SEALED";
            contextualHint.color = MemoryAbilityState.ForgetAvailable
                ? new Color(1f, 0.78f, 0.35f)
                : new Color(0.5f, 0.44f, 0.34f);
        }

        private void ApplyTint(float progress, bool revealed)
        {
            if (renderers == null)
                return;
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            var strength = Mathf.Clamp01(progress);
            var revealEmission = revealed && MemoryAbilityState.ForgetAvailable ? forgetEmission * 0.4f : Color.black;
            for (var i = 0; i < renderers.Length; i++)
            {
                var target = renderers[i];
                if (target == null)
                    continue;
                target.GetPropertyBlock(_propertyBlock);
                var baseColor = Color.Lerp(_baseColors[i], forgottenTint, strength);
                if (revealed)
                    baseColor = Color.Lerp(baseColor, new Color(0.95f, 0.77f, 0.36f, 1f), 0.22f);
                _propertyBlock.SetColor(BaseColorId, baseColor);
                _propertyBlock.SetColor(LegacyColorId, baseColor);
                _propertyBlock.SetColor(ColorId, baseColor);
                _propertyBlock.SetColor(EmissionColorId, revealEmission + forgetEmission * strength);
                target.SetPropertyBlock(_propertyBlock);
            }
        }

        private void SetState(ForgetState state)
        {
            _state = state;
            StateChanged?.Invoke(state);
        }
    }

    /// <summary>One-slot coordinator for active physical absence.</summary>
    public static class MemoryForgetCapacity
    {
        private static MemoryForgettable _active;
        public static MemoryForgettable ActiveTarget => _active;

        public static bool TryClaim(MemoryForgettable requested)
        {
            if (_active == null || _active == requested)
            {
                _active = requested;
                return true;
            }

            _active.RequestSafeRestore();
            return false;
        }

        public static void Release(MemoryForgettable target)
        {
            if (_active == target)
                _active = null;
        }
    }
}
