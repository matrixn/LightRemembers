using LightRemembers.Memory;
using LightRemembers.Player;
using LightRemembers.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LightRemembers.UI
{
    /// <summary>Owns the presentation-level start, objective cards, ability readout and ending.</summary>
    public sealed class PresentationLevelFlow : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerMemoryLight memoryLight;
        [SerializeField] private CanvasGroup gameplayHud;
        [SerializeField] private CanvasGroup titleScreen;
        [SerializeField] private Button startButton;
        [SerializeField] private CanvasGroup objectiveCard;
        [SerializeField] private Text objectiveTitle;
        [SerializeField] private Text objectiveDescription;
        [SerializeField] private Text objectiveTracker;
        [SerializeField] private Text recallStatus;
        [SerializeField] private Text echoStatus;
        [SerializeField] private Text forgetStatus;
        [SerializeField] private CanvasGroup endingScreen;
        [SerializeField] private Text endingTitle;
        [SerializeField, Min(0.5f)] private float cardDuration = 7f;
        private float _cardRemaining;
        private int _objectiveIndex = -1;
        private bool _started;
        private bool _finished;
        private bool _lastEcho;
        private bool _lastForget;
        private bool _lastRecallCorrupted;
        private bool _lastEchoCorrupted;
        private bool _lastForgetCorrupted;

        public bool HasStarted => _started;
        public bool IsFinished => _finished;
        public int CurrentObjectiveIndex => _objectiveIndex;

        public void Configure(PlayerInputReader reader, PlayerMovement playerMovement,
            PlayerInteractor playerInteractor, PlayerMemoryLight playerMemoryLight,
            CanvasGroup hud, CanvasGroup title, Button beginButton, CanvasGroup card, Text cardTitle,
            Text cardDescription, Text tracker, Text recall, Text echo, Text forget,
            CanvasGroup ending, Text finalTitle)
        {
            inputReader = reader;
            movement = playerMovement;
            interactor = playerInteractor;
            memoryLight = playerMemoryLight;
            gameplayHud = hud;
            titleScreen = title;
            startButton = beginButton;
            objectiveCard = card;
            objectiveTitle = cardTitle;
            objectiveDescription = cardDescription;
            objectiveTracker = tracker;
            recallStatus = recall;
            echoStatus = echo;
            forgetStatus = forget;
            endingScreen = ending;
            endingTitle = finalTitle;
        }

        private void Awake()
        {
            SetGameplayEnabled(false);
            SetCanvas(gameplayHud, 0f, false);
            SetCanvas(titleScreen, 1f, true);
            SetCanvas(objectiveCard, 0f, false);
            SetCanvas(endingScreen, 0f, false);
            RefreshAbilityReadout(true);
        }

        private void OnEnable()
        {
            MemoryAbilityState.MemoryStateChanged += OnMemoryStateChanged;
            MemoryAbilityState.EchoUnlockedChanged += OnAbilityAvailabilityChanged;
            MemoryAbilityState.ForgetUnlockedChanged += OnAbilityAvailabilityChanged;
        }

        private void OnDisable()
        {
            MemoryAbilityState.MemoryStateChanged -= OnMemoryStateChanged;
            MemoryAbilityState.EchoUnlockedChanged -= OnAbilityAvailabilityChanged;
            MemoryAbilityState.ForgetUnlockedChanged -= OnAbilityAvailabilityChanged;
            if (startButton != null)
                startButton.onClick.RemoveListener(BeginLevel);
        }

        private void Start()
        {
            if (startButton != null)
                startButton.onClick.AddListener(BeginLevel);
        }

        private void Update()
        {
            if (!_started && WasStartPressed())
                BeginLevel();

            if (_cardRemaining > 0f)
            {
                _cardRemaining -= Time.unscaledDeltaTime;
                var targetAlpha = _cardRemaining < 0.55f
                    ? Mathf.Clamp01(_cardRemaining / 0.55f)
                    : 1f;
                SetCanvas(objectiveCard, targetAlpha, targetAlpha > 0.01f);
            }

            RefreshAbilityReadout(false);
        }

        public void BeginLevel()
        {
            if (_started)
                return;

            _started = true;
            SetCanvas(titleScreen, 0f, false);
            SetCanvas(gameplayHud, 1f, true);
            SetGameplayEnabled(true);
            ShowObjective(0, "THE SHORE REMEMBERS", "Follow the lanterns toward the old lighthouse.");
        }

        public void ShowObjective(int index, string title, string description)
        {
            if (!_started || _finished || index < _objectiveIndex)
                return;

            _objectiveIndex = index;
            if (objectiveTitle != null)
                objectiveTitle.text = title;
            if (objectiveDescription != null)
                objectiveDescription.text = description;
            if (objectiveTracker != null)
                objectiveTracker.text = title;
            _cardRemaining = cardDuration;
            SetCanvas(objectiveCard, 1f, true);
        }

        public void CompleteLevel()
        {
            if (!_started || _finished)
                return;

            _finished = true;
            SetGameplayEnabled(false);
            if (endingTitle != null)
                endingTitle.text = "THE LIGHT REMEMBERS";
            SetCanvas(endingScreen, 1f, true);
        }

        private bool WasStartPressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                   (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
                   (gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.buttonSouth.wasPressedThisFrame));
        }

        private void SetGameplayEnabled(bool enabled)
        {
            if (!enabled)
            {
                if (memoryLight != null) memoryLight.enabled = false;
                if (interactor != null) interactor.enabled = false;
                if (movement != null) movement.enabled = false;
                if (inputReader != null) inputReader.enabled = false;
                return;
            }

            if (inputReader != null) inputReader.enabled = true;
            if (movement != null) movement.enabled = true;
            if (interactor != null) interactor.enabled = true;
            if (memoryLight != null) memoryLight.enabled = true;
        }

        private void OnMemoryStateChanged(MemoryStateChange _) => RefreshAbilityReadout(true);
        private void OnAbilityAvailabilityChanged() => RefreshAbilityReadout(true);

        private void RefreshAbilityReadout(bool force)
        {
            var recallCorrupted = MemoryAbilityState.RecallCorrupted;
            var echoUnlocked = MemoryAbilityState.EchoUnlocked;
            var echoCorrupted = MemoryAbilityState.EchoCorrupted;
            var forgetUnlocked = MemoryAbilityState.ForgetUnlocked;
            var forgetCorrupted = MemoryAbilityState.ForgetCorrupted;
            if (!force && recallCorrupted == _lastRecallCorrupted && echoUnlocked == _lastEcho &&
                echoCorrupted == _lastEchoCorrupted && forgetUnlocked == _lastForget &&
                forgetCorrupted == _lastForgetCorrupted)
                return;

            _lastRecallCorrupted = recallCorrupted;
            _lastEcho = echoUnlocked;
            _lastEchoCorrupted = echoCorrupted;
            _lastForget = forgetUnlocked;
            _lastForgetCorrupted = forgetCorrupted;
            SetAbilityText(recallStatus, "LMB  RECALL", !recallCorrupted, recallCorrupted ? "RESTORE AT A MEMORY REMNANT" : "READY");
            SetAbilityText(echoStatus, "Q  ECHO", echoUnlocked && !echoCorrupted,
                echoCorrupted ? "MEMORY LOST" : echoUnlocked ? "READY" : "NOT YET REMEMBERED");
            SetAbilityText(forgetStatus, "F  FORGET", forgetUnlocked && !forgetCorrupted,
                forgetCorrupted ? "MEMORY LOST" : forgetUnlocked ? "READY" : "SEALED IN THE ARCHIVE");
        }

        private static void SetAbilityText(Text label, string action, bool available, string status)
        {
            if (label == null)
                return;
            label.text = $"{action}   ·   {status}";
            label.color = available ? new Color(0.72f, 0.94f, 0.9f) : new Color(0.65f, 0.68f, 0.73f);
        }

        private static void SetCanvas(CanvasGroup group, float alpha, bool visible)
        {
            if (group == null)
                return;
            group.alpha = alpha;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
