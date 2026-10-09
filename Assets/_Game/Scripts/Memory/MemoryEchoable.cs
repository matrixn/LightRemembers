using System.Collections;
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

        public EchoState State => _state;
        public bool IsRevealed => _isRevealed;
        public EchoPath Path => path;
        public Transform MovingObject => movingObject;
        public EchoRideSurface RideSurface => rideSurface;
        public MemoryRecallable RecallRequirement => recallRequirement;
        public bool RequiresRecall => recallRequirement != null;
        public string LastFailureFeedback { get; private set; }

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
            if (pathPreview != null)
                pathPreview.SetActive(_state == EchoState.Revealed);
        }

        private void OnDisable()
        {
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
                pathPreview.SetActive(revealed);
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

            if (_state != EchoState.Revealed || !MemoryAbilityState.EchoUnlocked ||
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
                pathPreview.SetActive(_isRevealed);
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
