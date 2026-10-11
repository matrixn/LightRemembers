using UnityEngine;

namespace LightRemembers.Player
{
    /// <summary>Raises the character's right arm while Memory Light is held.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryLightArmPose : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform upperArm;
        [SerializeField] private Transform forearm;
        [SerializeField] private Transform hand;
        [SerializeField] private Transform lightOrigin;
        [SerializeField] private Transform poseFrame;
        [SerializeField, Min(0.01f)] private float blendSpeed = 7f;
        [SerializeField] private Vector3 targetDirection = new Vector3(0.08f, 0.92f, 0.35f);
        [SerializeField] private Vector3 forearmRaise = new Vector3(0f, 0f, -12f);

        private float _weight;
        private bool _subscribed;
        private bool _lightHeld;

        public bool HasArmRig => upperArm != null && forearm != null;
        public float PoseWeight => _weight;
        public Transform TrackedHand => hand;
        public Transform LightOrigin => lightOrigin;

        public void SetLightHeld(bool held) => _lightHeld = held;

        public void Configure(PlayerInputReader reader, Transform modelRoot, Transform origin)
        {
            Unsubscribe();
            inputReader = reader;
            _lightHeld = reader != null && reader.MemoryLightHeld;
            lightOrigin = origin;
            poseFrame = modelRoot;
            targetDirection = new Vector3(0.08f, 0.92f, 0.35f);
            forearmRaise = new Vector3(0f, 0f, -12f);
            if (modelRoot != null)
            {
                upperArm = FindDescendant(modelRoot, "upperarm_r");
                forearm = FindDescendant(modelRoot, "lowerarm_r");
                hand = FindDescendant(modelRoot, "hand_r");
            }
            Subscribe();
        }

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (upperArm == null || forearm == null)
            {
                var visual = transform.Find("PlayerCharacterVisual");
                if (visual != null)
                {
                    poseFrame = visual;
                    upperArm = FindDescendant(visual, "upperarm_r");
                    forearm = FindDescendant(visual, "lowerarm_r");
                    hand = FindDescendant(visual, "hand_r");
                }
            }
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (!HasArmRig) return;

            var target = _lightHeld || (inputReader != null && inputReader.MemoryLightHeld) ? 1f : 0f;
            _weight = Mathf.MoveTowards(_weight, target, blendSpeed * Time.deltaTime);
            var upperBase = upperArm.localRotation;
            var forearmBase = forearm.localRotation;
            if (_weight > 0f && hand != null)
            {
                var currentDirection = forearm.position - upperArm.position;
                // Aim in the visual rig's frame: this model faces opposite the player
                // root, so root-relative aim rotates the raised hand behind its head.
                var frame = poseFrame != null ? poseFrame : transform;
                var targetWorldDirection = frame.up * targetDirection.y +
                                           frame.right * targetDirection.x +
                                           frame.forward * targetDirection.z;
                if (currentDirection.sqrMagnitude > 0.0001f && targetWorldDirection.sqrMagnitude > 0.0001f)
                {
                    var worldDelta = Quaternion.FromToRotation(currentDirection.normalized,
                        targetWorldDirection.normalized);
                    var targetWorldRotation = worldDelta * upperArm.rotation;
                    var targetLocalRotation = upperArm.parent != null
                        ? Quaternion.Inverse(upperArm.parent.rotation) * targetWorldRotation
                        : targetWorldRotation;
                    upperArm.localRotation = Quaternion.Slerp(upperBase, targetLocalRotation, _weight);
                }
            }
            forearm.localRotation = forearmBase * Quaternion.Euler(forearmRaise * _weight);
            if (lightOrigin != null && hand != null)
                lightOrigin.position = hand.position;
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || inputReader == null || _subscribed) return;
            inputReader.MemoryLightPressed += OnLightPressed;
            inputReader.MemoryLightReleased += OnLightReleased;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || inputReader == null) return;
            inputReader.MemoryLightPressed -= OnLightPressed;
            inputReader.MemoryLightReleased -= OnLightReleased;
            _subscribed = false;
        }

        private void OnLightPressed() => SetLightHeld(true);
        private void OnLightReleased() => SetLightHeld(false);

        private static Transform FindDescendant(Transform root, string targetName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName)
                    return child;
            return null;
        }
    }
}
