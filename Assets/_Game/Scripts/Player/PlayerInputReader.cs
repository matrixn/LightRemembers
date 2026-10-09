using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LightRemembers.Player
{
    /// <summary>Reads the shared Player action map and exposes device-agnostic gameplay input.</summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;
        private InputAction _memoryLightAction;
        private InputAction _primaryAbilityAction;
        private InputAction _secondaryAbilityAction;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool LookIsMouseDelta { get; private set; }
        public bool SprintHeld => _sprintAction != null && _sprintAction.IsPressed();
        public bool MemoryLightHeld => _memoryLightAction != null && _memoryLightAction.IsPressed();

        public event Action JumpPressed;
        public event Action InteractPressed;
        public event Action PrimaryAbilityPressed;
        public event Action SecondaryAbilityPressed;

        public void Configure(InputActionAsset actions) => inputActions = actions;

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} on {name} has no Input Actions asset assigned.", this);
                enabled = false;
                return;
            }

            var sourceMap = inputActions.FindActionMap("Player", true);
            _playerMap = sourceMap.Clone();
            _moveAction = _playerMap.FindAction("Move", true);
            _lookAction = _playerMap.FindAction("Look", true);
            _jumpAction = _playerMap.FindAction("Jump", true);
            _sprintAction = _playerMap.FindAction("Sprint", true);
            _interactAction = _playerMap.FindAction("Interact", true);
            _memoryLightAction = _playerMap.FindAction("MemoryLight", true);
            _primaryAbilityAction = _playerMap.FindAction("PrimaryAbility", true);
            _secondaryAbilityAction = _playerMap.FindAction("SecondaryAbility", true);

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;
            _lookAction.performed += OnLook;
            _lookAction.canceled += OnLook;
            _jumpAction.performed += OnJump;
            _interactAction.performed += OnInteract;
            _primaryAbilityAction.performed += OnPrimaryAbility;
            _secondaryAbilityAction.performed += OnSecondaryAbility;
            _playerMap.Enable();
        }

        private void OnDisable()
        {
            if (_playerMap == null)
                return;

            _secondaryAbilityAction.performed -= OnSecondaryAbility;
            _playerMap.Disable();
            _playerMap.Dispose();
            _playerMap = null;
            _moveAction = null;
            _lookAction = null;
            _jumpAction = null;
            _sprintAction = null;
            _interactAction = null;
            _memoryLightAction = null;
            _primaryAbilityAction = null;
            _secondaryAbilityAction = null;
            Move = Vector2.zero;
            Look = Vector2.zero;
        }

        private void OnMove(InputAction.CallbackContext context) => Move = context.ReadValue<Vector2>();

        private void OnLook(InputAction.CallbackContext context)
        {
            Look = context.ReadValue<Vector2>();
            LookIsMouseDelta = context.control?.device is Mouse;
        }

        private void OnJump(InputAction.CallbackContext context) => JumpPressed?.Invoke();
        private void OnInteract(InputAction.CallbackContext context) => InteractPressed?.Invoke();
        private void OnPrimaryAbility(InputAction.CallbackContext context) => PrimaryAbilityPressed?.Invoke();
        private void OnSecondaryAbility(InputAction.CallbackContext context) => SecondaryAbilityPressed?.Invoke();
    }
}
