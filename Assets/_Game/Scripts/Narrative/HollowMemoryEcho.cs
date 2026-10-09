using System.Collections;
using LightRemembers.Memory;
using LightRemembers.Player;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Narrative
{
    [DisallowMultipleComponent]
    public sealed class HollowMemoryEcho : MonoBehaviour
    {
        [SerializeField] private DialogueSequence dialogue;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField] private GameObject childEcho;
        [SerializeField] private GameObject unknownChildEcho;
        [SerializeField] private GameObject resolvingFace;
        [SerializeField] private GameObject hollowDistortion;
        [SerializeField] private MemoryFragmentDefinition fragment;
        [SerializeField] private MemoryFragmentCollector collector;
        [SerializeField] private HollowController hollow;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private PlayerMemoryLight memoryLight;
        [SerializeField, Min(0f)] private float distortionDuration = 1.1f;
        private bool _started;
        private bool _completed;

        public bool HasStarted => _started;
        public bool IsCompleted => _completed;

        public void Configure(DialogueSequence lines, SubtitlePresenter presenter, GameObject child,
            GameObject unknownChild, GameObject face, GameObject distortion, MemoryFragmentDefinition memoryFragment,
            MemoryFragmentCollector memoryCollector, HollowController threat, PlayerInputReader reader,
            PlayerMovement movement, PlayerMemoryLight playerLight, float distortionTime = 1.1f)
        {
            dialogue = lines;
            subtitles = presenter;
            childEcho = child;
            unknownChildEcho = unknownChild;
            resolvingFace = face;
            hollowDistortion = distortion;
            fragment = memoryFragment;
            collector = memoryCollector;
            hollow = threat;
            inputReader = reader;
            playerMovement = movement;
            memoryLight = playerLight;
            distortionDuration = Mathf.Max(0f, distortionTime);
        }

        public void Begin()
        {
            if (_started)
                return;
            _started = true;
            StartCoroutine(PlayEcho());
        }

        private IEnumerator PlayEcho()
        {
            SetPlayerControl(false);
            hollow?.SetNarrativeSuppressed(true);
            if (childEcho != null) childEcho.SetActive(true);
            if (unknownChildEcho != null) unknownChildEcho.SetActive(true);
            if (resolvingFace != null) resolvingFace.SetActive(true);
            if (hollowDistortion != null) hollowDistortion.SetActive(false);

            if (dialogue != null && dialogue.Lines != null)
            {
                foreach (var line in dialogue.Lines)
                {
                    subtitles?.Show(line.speaker, line.text);
                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, line.duration));
                }
            }

            // The remembered face resolves for a breath, then the dark shape takes its place.
            if (resolvingFace != null) resolvingFace.SetActive(false);
            if (hollowDistortion != null) hollowDistortion.SetActive(true);
            if (distortionDuration > 0f)
                yield return new WaitForSecondsRealtime(distortionDuration);
            if (childEcho != null) childEcho.SetActive(false);
            if (unknownChildEcho != null) unknownChildEcho.SetActive(false);
            if (hollowDistortion != null) hollowDistortion.SetActive(false);

            if (fragment != null)
                collector?.TryCollect(fragment);
            var rewardGranted = MemoryAbilityState.UnlockSteadyLight();
            subtitles?.Show(string.Empty, rewardGranted ? "The light holds steady in your hands." : "The quiet light waits ahead.");
            yield return new WaitForSecondsRealtime(2f);
            subtitles?.Hide();
            hollow?.SetNarrativeSuppressed(false);
            SetPlayerControl(true);
            _completed = true;
        }

        private void SetPlayerControl(bool enabled)
        {
            if (playerMovement != null) playerMovement.enabled = enabled;
            if (memoryLight != null) memoryLight.enabled = enabled;
            if (inputReader != null) inputReader.enabled = enabled;
        }
    }
}
