using LightRemembers.Player;
using System.Collections.Generic;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class PlayerMemoryLight : MonoBehaviour
    {
        private const int HitCapacity = 16;

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform lightOrigin;
        [SerializeField] private Transform aimTransform;
        [SerializeField] private Light focusedLight;
        [SerializeField, Min(0f)] private float maximumRange = 12f;
        [SerializeField] private LayerMask targetLayers = ~0;

        private readonly RaycastHit[] _hits = new RaycastHit[HitCapacity];
        private readonly List<MonoBehaviour> _componentBuffer = new List<MonoBehaviour>(8);
        private IMemoryLightTarget _currentTarget;
        private Vector3 _lastHitPoint;

        public IMemoryLightTarget CurrentTarget => _currentTarget;
        public IMemoryRecallable CurrentRecallableTarget => _currentTarget is MemoryComposite composite
            ? composite.Recall
            : _currentTarget as IMemoryRecallable;
        public float MaximumRange { get => maximumRange; set => maximumRange = Mathf.Max(0f, value); }

        public void Configure(PlayerInputReader reader, Transform origin, Transform aim, Light beam)
        {
            if (inputReader != null)
            {
                inputReader.PrimaryAbilityPressed -= TryRecallCurrentTarget;
                inputReader.SecondaryAbilityPressed -= TryEchoCurrentTarget;
            }
            inputReader = reader;
            lightOrigin = origin;
            aimTransform = aim;
            focusedLight = beam;
            BindPrimaryAbility();
        }

        private void Awake()
        {
            if (aimTransform == null && Camera.main != null)
                aimTransform = Camera.main.transform;
            if (focusedLight != null)
                focusedLight.enabled = false;
        }

        private void OnEnable()
        {
            BindPrimaryAbility();
        }

        private void Update()
        {
            if (inputReader == null || lightOrigin == null || aimTransform == null)
            {
                SetTarget(null);
                return;
            }

            var active = inputReader.MemoryLightHeld;
            if (focusedLight != null)
                focusedLight.enabled = active;
            if (!active)
            {
                SetTarget(null);
                return;
            }

            var origin = lightOrigin.position;
            var direction = aimTransform.forward;
            lightOrigin.rotation = Quaternion.LookRotation(direction, Vector3.up);
            var count = Physics.RaycastNonAlloc(
                origin, direction, _hits, maximumRange, targetLayers, QueryTriggerInteraction.Ignore);

            var nearestDistance = float.PositiveInfinity;
            var nearestCollider = (Collider)null;
            var nearestPoint = origin + direction * maximumRange;
            for (var i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance)
                    continue;
                nearestDistance = _hits[i].distance;
                nearestCollider = _hits[i].collider;
                nearestPoint = _hits[i].point;
            }

            _lastHitPoint = nearestPoint;
            if (nearestCollider == null)
            {
                SetTarget(null);
                return;
            }

            if (_currentTarget is Component currentComponent && currentComponent != null &&
                (nearestCollider.transform == currentComponent.transform || nearestCollider.transform.IsChildOf(currentComponent.transform)))
                return;

            SetTarget(GetMemoryTarget(nearestCollider));
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.PrimaryAbilityPressed -= TryRecallCurrentTarget;
                inputReader.SecondaryAbilityPressed -= TryEchoCurrentTarget;
            }
            SetTarget(null);
        }

        private void TryRecallCurrentTarget()
        {
            if (_currentTarget is MemoryComposite composite)
                composite.TryRecall();
            else if (_currentTarget is IMemoryRecallable recallable)
                recallable.TryRecall();
        }

        private void TryEchoCurrentTarget()
        {
            if (_currentTarget is MemoryComposite composite)
                composite.TryEcho();
            else if (_currentTarget is IMemoryEchoable echoable)
                echoable.TryEcho();
        }

        private void BindPrimaryAbility()
        {
            if (!isActiveAndEnabled || inputReader == null)
                return;
            inputReader.PrimaryAbilityPressed -= TryRecallCurrentTarget;
            inputReader.PrimaryAbilityPressed += TryRecallCurrentTarget;
            inputReader.SecondaryAbilityPressed -= TryEchoCurrentTarget;
            inputReader.SecondaryAbilityPressed += TryEchoCurrentTarget;
        }

        private void SetTarget(IMemoryLightTarget target)
        {
            if (ReferenceEquals(_currentTarget, target))
                return;
            if (_currentTarget != null)
                _currentTarget.SetRevealed(false);
            _currentTarget = target;
            if (_currentTarget != null)
                _currentTarget.SetRevealed(true);
        }

        private IMemoryLightTarget GetMemoryTarget(Collider targetCollider)
        {
            _componentBuffer.Clear();
            targetCollider.GetComponentsInParent(true, _componentBuffer);
            foreach (var behaviour in _componentBuffer)
                if (behaviour is MemoryComposite composite)
                    return composite;
            foreach (var behaviour in _componentBuffer)
            {
                if (behaviour is IMemoryLightTarget target)
                    return target;
            }

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            if (lightOrigin == null)
                return;
            var direction = aimTransform != null ? aimTransform.forward : lightOrigin.forward;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lightOrigin.position, 0.08f);
            Gizmos.DrawLine(lightOrigin.position, lightOrigin.position + direction * maximumRange);
            Gizmos.DrawWireSphere(lightOrigin.position + direction * maximumRange, 0.12f);
            if (_currentTarget != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_lastHitPoint, 0.09f);
            }
        }
    }
}
