using System.Collections;
using System;
using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Replays an authored EchoPath while keeping the moved object physically interactive.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryEchoable : MonoBehaviour, IMemoryEchoable
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Transform movingObject;
        [SerializeField] private EchoPath path;
        [SerializeField] private GameObject pathPreview;
        [SerializeField] private EchoRideSurface rideSurface;
        [SerializeField] private MemoryRecallable recallRequirement;
        [SerializeField] private Renderer[] movingRenderers;
        [SerializeField, ColorUsage(false, true)] private Color idleEmission = Color.black;
        [SerializeField, ColorUsage(false, true)] private Color echoEmission = new Color(0.35f, 1.2f, 1.6f, 1f);

        private MaterialPropertyBlock _propertyBlock;
        private Coroutine _playback;
        private bool _isRevealed;
        private EchoState _state;
        private float _rejectionPulseRemaining;
        private bool _memoryCorrupted;

        public EchoState State => _state;
        public bool IsRevealed => _isRevealed;
        public EchoPath Path => path;
        public Transform MovingObject => movingObject;
        public EchoRideSurface RideSurface => rideSurface;
        public MemoryRecallable RecallRequirement => recallRequirement;
        public bool RequiresRecall => recallRequirement != null;
        public string LastFailureFeedback { get; private set; }
        public event Action EchoCompleted;

        public void Configure(EchoPath echoPath, Transform target, GameObject preview, EchoRideSurface surface, Renderer[] renderers = null)
        {
            path = echoPath;
            movingObject = target;
            pathPreview = preview;
            rideSurface = surface;
            movingRenderers = renderers;
            CacheReferences();
            SetRevealed(false);
        }

        private void Awake() => CacheReferences();

        private void OnEnable()
        {
            MemoryAbilityState.MemoryStateChanged += OnMemoryStateChanged;
            _memoryCorrupted = MemoryAbilityState.EchoCorrupted;
            if (pathPreview != null)
                pathPreview.SetActive(_isRevealed && !_memoryCorrupted);
        }

        private void OnDisable()
        {
            MemoryAbilityState.MemoryStateChanged -= OnMemoryStateChanged;
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
        }

        public void SetRevealed(bool revealed)
        {
            _isRevealed = revealed;
            if (_state == EchoState.Echoing)
                return;
            _state = revealed ? EchoState.Revealed : EchoState.Idle;
            if (pathPreview != null)
                pathPreview.SetActive(revealed && !_memoryCorrupted);
        }

        public bool TryEcho()
        {
            if (recallRequirement != null && recallRequirement.State != RecallState.Recalled)
            {
                LastFailureFeedback = "The movement is there... but the shape is gone.";
                _rejectionPulseRemaining = 0.45f;
                SetEmission(echoEmission * 1.8f);
                return false;
            }

            if (_state != EchoState.Revealed || !MemoryAbilityState.EchoAvailable ||
                path == null || !path.IsValid || movingObject == null)
                return false;

            if (pathPreview != null)
                pathPreview.SetActive(false);
            _state = EchoState.Echoing;
            SetEmission(echoEmission);
            _playback = StartCoroutine(PlayPath());
            return true;
        }

        public void ConfigureRecallRequirement(MemoryRecallable requirement) => recallRequirement = requirement;

        public void ResetToStart()
        {
            if (_playback != null)
            {
                StopCoroutine(_playback);
                _playback = null;
            }
            if (path != null && path.IsValid && movingObject != null)
                movingObject.SetPositionAndRotation(path.Waypoints[0].position, path.Waypoints[0].rotation);
            _rejectionPulseRemaining = 0f;
            _isRevealed = false;
            _state = EchoState.Idle;
            if (pathPreview != null)
                pathPreview.SetActive(false);
            SetEmission(idleEmission);
        }

        private void Update()
        {
            if (_rejectionPulseRemaining <= 0f || _state == EchoState.Echoing)
                return;
            _rejectionPulseRemaining -= Time.deltaTime;
            if (_rejectionPulseRemaining <= 0f)
            {
                _rejectionPulseRemaining = 0f;
                SetEmission(idleEmission);
            }
        }

        private IEnumerator PlayPath()
        {
            yield return Traverse(0f, 1f, path.LegDuration);
            if (path.DestinationPause > 0f)
                yield return new WaitForSeconds(path.DestinationPause);
            if (path.ReturnToStart)
                yield return Traverse(1f, 0f, path.LegDuration);

            _playback = null;
            _state = _isRevealed ? EchoState.Revealed : EchoState.Idle;
            if (pathPreview != null)
                pathPreview.SetActive(_isRevealed && !_memoryCorrupted);
            SetEmission(idleEmission);
            EchoCompleted?.Invoke();
        }

        private void OnMemoryStateChanged(MemoryStateChange change)
        {
            if (change.Ability != MemoryAbility.Echo)
                return;
            _memoryCorrupted = !change.IsAvailable;
            if (pathPreview != null)
                pathPreview.SetActive(_isRevealed && !_memoryCorrupted && _state != EchoState.Echoing);
            if (_memoryCorrupted && _state != EchoState.Echoing)
                SetEmission(echoEmission * 0.12f);
            else if (_state != EchoState.Echoing)
                SetEmission(idleEmission);
        }

        private IEnumerator Traverse(float start, float end, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                ApplyPose(Mathf.Lerp(start, end, elapsed / duration));
                yield return null;
            }
            ApplyPose(end);
        }

        private void ApplyPose(float normalizedTime)
        {
            if (movingObject == null || !path.TryEvaluate(normalizedTime, out var nextPosition, out var nextRotation))
                return;

            var previousPosition = movingObject.position;
            var previousRotation = movingObject.rotation;
            movingObject.SetPositionAndRotation(nextPosition, nextRotation);
            rideSurface?.CarryRiders(previousPosition, previousRotation, nextPosition, nextRotation);
        }

        private void CacheReferences()
        {
            if (movingObject == null)
                movingObject = transform;
            if (movingRenderers == null || movingRenderers.Length == 0)
                movingRenderers = movingObject.GetComponentsInChildren<Renderer>(true);
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
        }

        private void SetEmission(Color emission)
        {
            if (movingRenderers == null)
                return;
            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
            foreach (var targetRenderer in movingRenderers)
            {
                if (targetRenderer == null)
                    continue;
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(EmissionColorId, emission);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
