using UnityEngine;

namespace LightRemembers.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 4f;
        [SerializeField, Min(0f)] private float sprintSpeed = 6.5f;
        [SerializeField, Min(0f)] private float rotationSmoothTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0f)] private float groundedDownForce = 2f;

        private float _verticalSpeed;
        private float _rotationVelocity;
        private bool _jumpRequested;

        public CharacterController Controller => characterController;
        public PlayerInputReader InputReader => inputReader;

        public void ResetMotion()
        {
            _verticalSpeed = 0f;
            _rotationVelocity = 0f;
            _jumpRequested = false;
        }

        public void Configure(CharacterController controller, PlayerInputReader reader, Transform view)
        {
            characterController = controller;
            inputReader = reader;
            cameraTransform = view;
        }

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void OnEnable()
        {
            if (inputReader != null)
                inputReader.JumpPressed += RequestJump;
        }

        private void OnDisable()
        {
            if (inputReader != null)
                inputReader.JumpPressed -= RequestJump;
            _jumpRequested = false;
        }

        private void Update()
        {
            if (characterController == null || inputReader == null || cameraTransform == null)
                return;

            var input = Vector2.ClampMagnitude(inputReader.Move, 1f);
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            var moveDirection = forward * input.y + right * input.x;
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();

            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                var targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                var angle = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (characterController.isGrounded)
            {
                if (_verticalSpeed < 0f)
                    _verticalSpeed = -groundedDownForce;
                if (_jumpRequested)
                    _verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _jumpRequested = false;
            _verticalSpeed += gravity * Time.deltaTime;
            var speed = inputReader.SprintHeld ? sprintSpeed : walkSpeed;
            var velocity = moveDirection * speed + Vector3.up * _verticalSpeed;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void RequestJump() => _jumpRequested = true;
    }
}
