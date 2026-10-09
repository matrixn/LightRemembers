using UnityEngine;

namespace LightRemembers.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CheckpointRespawner : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerMovement movement;
        private Vector3 _checkpointPosition;
        private Quaternion _checkpointRotation;

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (movement == null) movement = GetComponent<PlayerMovement>();
            _checkpointPosition = transform.position;
            _checkpointRotation = transform.rotation;
        }

        public void SetCheckpoint(Transform point)
        {
            if (point == null) return;
            _checkpointPosition = point.position;
            _checkpointRotation = point.rotation;
        }

        public void Respawn()
        {
            if (characterController != null) characterController.enabled = false;
            transform.SetPositionAndRotation(_checkpointPosition, _checkpointRotation);
            movement?.ResetMotion();
            if (characterController != null) characterController.enabled = true;
        }
    }
}
