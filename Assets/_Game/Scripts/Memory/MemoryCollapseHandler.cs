using System;
using System.Collections;
using LightRemembers.Interaction;
using LightRemembers.Player;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class MemoryCollapseHandler : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private PlayerMemoryLight memoryLight;
        [SerializeField] private CheckpointRespawner checkpointRespawner;
        [SerializeField] private MemoryIntegrity integrity;
        [SerializeField] private MemoryCorruptionController corruption;
        [SerializeField] private MemoryEncounterResetter encounterResetter;
        [SerializeField] private CanvasGroup darkFade;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
        [SerializeField, Min(0f)] private float blackoutDuration = 0.45f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.55f;

        private Coroutine _collapseRoutine;
        public bool IsCollapsing => _collapseRoutine != null;
        public event Action CollapseCompleted;

        public void Configure(PlayerInputReader reader, PlayerMovement movement, PlayerInteractor interactor,
            PlayerMemoryLight light, CheckpointRespawner respawner, MemoryIntegrity memoryIntegrity,
            MemoryCorruptionController memoryCorruption, MemoryEncounterResetter resetter, CanvasGroup fade,
            float fadeOut = 0.35f, float blackout = 0.45f, float fadeIn = 0.55f)
        {
            inputReader = reader;
            playerMovement = movement;
            playerInteractor = interactor;
            memoryLight = light;
            checkpointRespawner = respawner;
            integrity = memoryIntegrity;
            corruption = memoryCorruption;
            encounterResetter = resetter;
            darkFade = fade;
            fadeOutDuration = Mathf.Max(0f, fadeOut);
            blackoutDuration = Mathf.Max(0f, blackout);
            fadeInDuration = Mathf.Max(0f, fadeIn);
        }

        public void BeginCollapse()
        {
            if (_collapseRoutine == null)
                _collapseRoutine = StartCoroutine(CollapseRoutine());
        }

        private IEnumerator CollapseRoutine()
        {
            SetControlsEnabled(false);
            yield return null;
            if (darkFade != null)
                yield return FadeTo(1f, fadeOutDuration);
            if (blackoutDuration > 0f)
                yield return new WaitForSecondsRealtime(blackoutDuration);

            encounterResetter?.ResetTransientMemory();
            checkpointRespawner?.Respawn();
            MemoryAbilityState.RestoreAllMemories();
            integrity?.RestoreFull();
            corruption?.CompleteCollapse();

            if (darkFade != null)
                yield return FadeTo(0f, fadeInDuration);
            SetControlsEnabled(true);
            _collapseRoutine = null;
            CollapseCompleted?.Invoke();
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (darkFade == null)
                yield break;
            var start = darkFade.alpha;
            if (duration <= 0f)
            {
                darkFade.alpha = target;
                yield break;
            }
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                darkFade.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            darkFade.alpha = target;
        }

        private void SetControlsEnabled(bool enabled)
        {
            if (playerMovement != null) playerMovement.enabled = enabled;
            if (playerInteractor != null) playerInteractor.enabled = enabled;
            if (memoryLight != null) memoryLight.enabled = enabled;
            if (inputReader != null) inputReader.enabled = enabled;
        }
    }
}
