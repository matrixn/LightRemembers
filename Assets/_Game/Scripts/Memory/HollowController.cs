using LightRemembers.Player;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class HollowController : MonoBehaviour, IMemoryLightTarget, IMemoryThreat, IMemoryForgetActionTarget
    {
        private const float AttackWindupSeconds = 0.72f;
        private const float AttackCooldownSeconds = 2.5f;
        private const float UnstableExposureSeconds = 1f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private PlayerMemoryLight playerMemoryLight;
        [SerializeField] private MemoryCorruptionController corruptionTarget;
        [SerializeField] private SanctuaryLight sanctuaryLight;
        [SerializeField] private Transform returnToDarkness;
        [SerializeField] private Transform[] observationPositions;
        [SerializeField] private CharacterController bodyCollider;
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private GameObject[] revealedMemoryFragments;
        [SerializeField, Min(0f)] private float detectionRange = 18f;
        [SerializeField, Min(0f)] private float attackRange = 1.85f;
        [SerializeField, Min(0f)] private float approachDistance = 6f;
        [SerializeField, Min(0f)] private float stalkSpeed = 0.8f;
        [SerializeField, Min(0f)] private float approachSpeed = 1.45f;
        [SerializeField, Min(0f)] private float repelSpeed = 1.8f;
        [SerializeField, Min(0f)] private float repelExposureSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float forgetPreparationSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float perceptionForgetDuration = 4f;
        [SerializeField, Min(0f)] private float observationDuration = 2.1f;
        [SerializeField, Min(0f)] private float gravity = 18f;
        [SerializeField] private Color voidEmission = new Color(0.12f, 0.035f, 0.2f, 1f);
        [SerializeField] private Color unstableEmission = new Color(0.55f, 0.19f, 0.85f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private HollowState _state;
        private HollowState _stateBeforeExposure;
        private float _verticalSpeed;
        private float _exposureSeconds;
        private float _continuousLightExposure;
        private float _attackWindupRemaining;
        private float _nextAttackTime;
        private float _stateTimer;
        private float _perceptionForgetRemaining;
        private Vector3 _lastKnownPlayerPosition;
        private bool _woken;
        private bool _narrativeSuppressed;
        private bool _isRevealed;
        private bool _hasRelocated;

        public HollowState State => _state;
        public bool IsRevealed => _isRevealed;
        public float ExposureSeconds => _exposureSeconds;
        public bool HasRelocatedOffscreen => _hasRelocated;
        public float RepelExposureSeconds => MemoryAbilityState.SteadyLightUnlocked ? 1.8f : repelExposureSeconds;
        public float AttackCooldownRemaining => Mathf.Max(0f, _nextAttackTime - Time.time);
        public float ContinuousLightExposure => _continuousLightExposure;
        public float PerceptionForgetRemaining => _perceptionForgetRemaining;
        public bool HasPerceptionForgetConfiguration => player != null && playerMemoryLight != null && bodyCollider != null;
        public string LastFailureFeedback { get; private set; }

        public void Configure(Transform target, Camera targetCamera, PlayerMemoryLight memoryLight,
            MemoryCorruptionController targetCorruption, SanctuaryLight safeLight, Transform returnPoint,
            Transform[] lookoutPositions, Renderer[] renderers, GameObject[] memoryFragments)
        {
            player = target;
            playerCamera = targetCamera;
            playerMemoryLight = memoryLight;
            corruptionTarget = targetCorruption;
            sanctuaryLight = safeLight;
            returnToDarkness = returnPoint;
            observationPositions = lookoutPositions;
            bodyRenderers = renderers;
            revealedMemoryFragments = memoryFragments;
            bodyCollider = GetComponent<CharacterController>();
        }

        public void Wake()
        {
            if (_woken)
                return;
            _woken = true;
            _stateTimer = observationDuration;
            SetState(HollowState.Observe);
        }

        public void SetNarrativeSuppressed(bool suppressed)
        {
            _narrativeSuppressed = suppressed;
            if (suppressed)
            {
                _attackWindupRemaining = 0f;
                SetState(HollowState.Observe);
            }
        }

        public void SetRevealed(bool revealed)
        {
            _isRevealed = revealed;
            if (revealedMemoryFragments != null)
                foreach (var fragment in revealedMemoryFragments)
                    if (fragment != null)
                        fragment.SetActive(revealed);
            ApplyVoidVisual(_exposureSeconds >= UnstableExposureSeconds);
        }

        public void TickLightExposure(bool illuminated, float deltaTime)
        {
            if (illuminated)
            {
                _continuousLightExposure += Mathf.Max(0f, deltaTime);
                if (_exposureSeconds <= 0f)
                    _stateBeforeExposure = _state;
                _exposureSeconds = Mathf.Min(RepelExposureSeconds, _exposureSeconds + Mathf.Max(0f, deltaTime));
                _attackWindupRemaining = 0f;
                if (_exposureSeconds >= RepelExposureSeconds)
                    SetState(HollowState.Repelled);
                else if (_state == HollowState.Search)
                {
                    ApplyVoidVisual(true);
                    return;
                }
                else if (_exposureSeconds >= UnstableExposureSeconds)
                    SetState(HollowState.Unstable);
                ApplyVoidVisual(_exposureSeconds >= UnstableExposureSeconds);
                return;
            }

            _continuousLightExposure = 0f;
            if (_exposureSeconds > 0f)
            {
                _exposureSeconds = Mathf.Max(0f, _exposureSeconds - deltaTime * 0.55f);
                if (_exposureSeconds < UnstableExposureSeconds)
                    ApplyVoidVisual(false);
                if (_state == HollowState.Unstable && _exposureSeconds < UnstableExposureSeconds)
                    SetState(_stateBeforeExposure == HollowState.Attack ? HollowState.Approach : _stateBeforeExposure);
                if (_state == HollowState.Repelled)
                    SetState(HollowState.Return);
            }
        }

        public bool TryForget()
        {
            LastFailureFeedback = null;
            if (!MemoryAbilityState.ForgetAvailable || !_isRevealed ||
                _continuousLightExposure < forgetPreparationSeconds ||
                _state == HollowState.Dormant || _state == HollowState.Repelled || _state == HollowState.Return)
            {
                LastFailureFeedback = _continuousLightExposure < forgetPreparationSeconds
                    ? "Hold the light steady; it hasn't forgotten you yet."
                    : "This memory won't let go yet.";
                return false;
            }

            _lastKnownPlayerPosition = player != null ? player.position : transform.position;
            _perceptionForgetRemaining = perceptionForgetDuration;
            _attackWindupRemaining = 0f;
            SetState(HollowState.Search);
            return true;
        }

        public void ConfigurePerceptionForget(float preparationSeconds, float durationSeconds)
        {
            forgetPreparationSeconds = Mathf.Max(0.1f, preparationSeconds);
            perceptionForgetDuration = Mathf.Max(0.1f, durationSeconds);
        }

        public bool CanEnter(Vector3 worldPosition) => sanctuaryLight == null || sanctuaryLight.CanEnter(worldPosition);

        public void NotifySanctuaryBoundary() => SetState(HollowState.Return);

        private void Awake()
        {
            if (bodyCollider == null)
                bodyCollider = GetComponent<CharacterController>();
            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main;
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                bodyRenderers = GetComponentsInChildren<Renderer>(true);
            _propertyBlock = new MaterialPropertyBlock();
            SetState(HollowState.Dormant);
            ApplyVoidVisual(false);
        }

        private void OnEnable()
        {
            MemoryAbilityState.SteadyLightUnlockedChanged += OnSteadyLightUnlocked;
            if (revealedMemoryFragments != null)
                foreach (var fragment in revealedMemoryFragments)
                    if (fragment != null)
                        fragment.SetActive(_isRevealed);
        }

        private void OnDisable() => MemoryAbilityState.SteadyLightUnlockedChanged -= OnSteadyLightUnlocked;

        private void Update()
        {
            if (player == null || bodyCollider == null || _narrativeSuppressed)
                return;

            var deltaTime = Time.deltaTime;
            var toPlayer = player.position - transform.position;
            var distance = toPlayer.magnitude;
            if (!_woken && distance <= detectionRange)
                Wake();
            if (!_woken)
                return;

            var illuminated = playerMemoryLight != null && ReferenceEquals(playerMemoryLight.CurrentTarget, this);
            TickLightExposure(illuminated, deltaTime);

            if (sanctuaryLight != null && sanctuaryLight.Contains(player.position))
            {
                SetState(HollowState.Return);
                MoveToward(returnToDarkness != null ? returnToDarkness.position : transform.position, repelSpeed, deltaTime);
                return;
            }

            if (illuminated)
            {
                if (_state == HollowState.Repelled)
                    MoveAwayFrom(player.position, repelSpeed, deltaTime);
                return;
            }

            if (_state == HollowState.Search)
            {
                UpdateSearch(deltaTime);
                return;
            }

            if (!_hasRelocated && (_state == HollowState.Observe || _state == HollowState.Stalk) &&
                !IsVisibleFromPlayer(transform.position) && TryRelocateOffscreen())
            {
                SetState(HollowState.Observe);
                return;
            }

            if (_state == HollowState.Repelled)
                SetState(HollowState.Return);

            switch (_state)
            {
                case HollowState.Observe:
                    UpdateObservation(deltaTime);
                    break;
                case HollowState.Unstable:
                    break;
                case HollowState.Stalk:
                    if (distance <= approachDistance)
                        SetState(HollowState.Approach);
                    MoveToward(player.position, stalkSpeed, deltaTime);
                    break;
                case HollowState.Approach:
                    if (distance > approachDistance + 1.25f)
                    {
                        SetState(HollowState.Stalk);
                        MoveToward(player.position, stalkSpeed, deltaTime);
                    }
                    else
                    {
                        MoveToward(player.position, approachSpeed, deltaTime);
                        UpdateAttack(distance, deltaTime);
                    }
                    break;
                case HollowState.Attack:
                    UpdateAttack(distance, deltaTime);
                    break;
                case HollowState.Return:
                    UpdateReturn(deltaTime);
                    break;
                case HollowState.Search:
                    UpdateSearch(deltaTime);
                    break;
            }
        }

        private void UpdateSearch(float deltaTime)
        {
            _perceptionForgetRemaining -= deltaTime;
            var offset = new Vector3(Mathf.Sin(Time.time * 1.15f) * 1.6f, 0f,
                Mathf.Cos(Time.time * 0.85f) * 1.2f);
            var searchPoint = _perceptionForgetRemaining > 1.8f
                ? _lastKnownPlayerPosition
                : _lastKnownPlayerPosition + offset;
            MoveToward(searchPoint, stalkSpeed * 0.72f, deltaTime);

            if (_perceptionForgetRemaining > 0f || player == null)
                return;

            var playerDistance = Vector3.Distance(transform.position, player.position);
            SetState(playerDistance <= detectionRange
                ? playerDistance <= approachDistance ? HollowState.Approach : HollowState.Stalk
                : HollowState.Observe);
        }

        private void UpdateObservation(float deltaTime)
        {
            if (!_hasRelocated && !IsVisibleFromPlayer(transform.position))
                TryRelocateOffscreen();
            _stateTimer -= deltaTime;
            if (_stateTimer <= 0f)
                SetState(HollowState.Stalk);
        }

        private void UpdateAttack(float distance, float deltaTime)
        {
            if (distance > attackRange + 0.2f || (sanctuaryLight != null && sanctuaryLight.Contains(player.position)))
            {
                _attackWindupRemaining = 0f;
                if (_state == HollowState.Attack)
                    SetState(HollowState.Approach);
                return;
            }
            if (_state != HollowState.Attack)
            {
                if (Time.time < _nextAttackTime)
                    return;
                _attackWindupRemaining = AttackWindupSeconds;
                SetState(HollowState.Attack);
                return;
            }
            _attackWindupRemaining -= deltaTime;
            if (_attackWindupRemaining > 0f)
                return;

            _nextAttackTime = Time.time + AttackCooldownSeconds;
            corruptionTarget?.TryReceiveHollowHit();
            _attackWindupRemaining = 0f;
            SetState(HollowState.Approach);
        }

        private void UpdateReturn(float deltaTime)
        {
            if (returnToDarkness == null)
            {
                SetState(HollowState.Stalk);
                return;
            }
            MoveToward(returnToDarkness.position, repelSpeed, deltaTime);
            if ((transform.position - returnToDarkness.position).sqrMagnitude <= 0.36f)
                SetState(HollowState.Stalk);
        }

        private void MoveToward(Vector3 destination, float speed, float deltaTime)
        {
            var offset = destination - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.04f)
            {
                var direction = offset.normalized;
                var candidate = transform.position + direction * (speed * deltaTime);
                if (!CanEnter(candidate))
                {
                    SetState(HollowState.Return);
                    return;
                }
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up), Mathf.Clamp01(deltaTime * 2.2f));
                bodyCollider.Move(direction * (speed * deltaTime) + Vector3.down * (gravity * deltaTime * deltaTime));
            }
            else
                bodyCollider.Move(Vector3.down * (gravity * deltaTime * deltaTime));
        }

        private void MoveAwayFrom(Vector3 source, float speed, float deltaTime)
        {
            var direction = transform.position - source;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = -transform.forward;
            direction.Normalize();
            var candidate = transform.position + direction * speed * deltaTime;
            if (CanEnter(candidate))
                bodyCollider.Move(direction * speed * deltaTime + Vector3.down * (gravity * deltaTime * deltaTime));
        }

        private bool TryRelocateOffscreen()
        {
            if (observationPositions == null || observationPositions.Length == 0 || IsVisibleFromPlayer(transform.position))
                return false;
            foreach (var point in observationPositions)
            {
                if (point == null || IsVisibleFromPlayer(point.position))
                    continue;
                transform.SetPositionAndRotation(point.position, point.rotation);
                _hasRelocated = true;
                return true;
            }
            return false;
        }

        private bool IsVisibleFromPlayer(Vector3 worldPosition)
        {
            if (playerCamera == null)
                return Vector3.Dot(player.forward, worldPosition - player.position) > 0.25f;
            var viewport = playerCamera.WorldToViewportPoint(worldPosition + Vector3.up * 1.1f);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f)
                return false;
            var origin = playerCamera.transform.position;
            var direction = worldPosition + Vector3.up * 1.1f - origin;
            if (!Physics.Raycast(origin, direction.normalized, out var hit, direction.magnitude))
                return true;
            return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
        }

        private void SetState(HollowState next)
        {
            _state = next;
            if (next == HollowState.Observe)
                _stateTimer = observationDuration;
            ApplyVoidVisual(next == HollowState.Unstable || _exposureSeconds >= UnstableExposureSeconds);
        }

        private void ApplyVoidVisual(bool unstable)
        {
            if (bodyRenderers == null)
                return;
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            foreach (var targetRenderer in bodyRenderers)
            {
                if (targetRenderer == null)
                    continue;
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, unstable ? unstableEmission : voidEmission);
                _propertyBlock.SetColor(BaseColorId, unstable
                    ? new Color(0.18f, 0.055f, 0.25f, 1f)
                    : new Color(0.025f, 0.02f, 0.04f, 1f));
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void OnSteadyLightUnlocked() => repelExposureSeconds = Mathf.Min(repelExposureSeconds, 1.8f);
    }
}
