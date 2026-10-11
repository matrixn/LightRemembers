using UnityEngine;

namespace LightRemembers.Player
{
    /// <summary>Maps device-independent movement state to the showcase character animator.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator characterAnimator;
        [SerializeField, Min(0f)] private float dampTime = 0.12f;

        public void Configure(PlayerInputReader reader, CharacterController controller, Animator animator)
        {
            UnbindJump();
            inputReader = reader;
            characterController = controller;
            characterAnimator = animator;
            BindJump();
        }

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            BindJump();
        }

        private void OnDisable()
        {
            UnbindJump();
        }

        private void Update()
        {
            if (inputReader == null || characterController == null || characterAnimator == null)
                return;

            var input = Mathf.Clamp01(inputReader.Move.magnitude);
            var speed = input <= 0.02f ? 0f : input * (inputReader.SprintHeld ? 1f : 0.62f);
            characterAnimator.SetFloat(MoveSpeedId, speed, dampTime, Time.deltaTime);
            characterAnimator.SetBool(GroundedId, characterController.isGrounded);
        }

        private void BindJump()
        {
            if (isActiveAndEnabled && inputReader != null)
            {
                inputReader.JumpPressed -= OnJumpPressed;
                inputReader.JumpPressed += OnJumpPressed;
            }
        }

        private void UnbindJump()
        {
            if (inputReader != null)
                inputReader.JumpPressed -= OnJumpPressed;
        }

        private void OnJumpPressed()
        {
            if (characterController != null && characterController.isGrounded && characterAnimator != null)
                characterAnimator.SetTrigger(JumpId);
        }
    }
}
