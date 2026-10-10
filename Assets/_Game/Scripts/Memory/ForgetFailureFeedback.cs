using System.Collections;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Memory
{
    [DisallowMultipleComponent]
    public sealed class ForgetFailureFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerMemoryLight memoryLight;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField, Min(0.2f)] private float displayDuration = 1.8f;
        private Coroutine _hideRoutine;

        public void Configure(PlayerMemoryLight light, SubtitlePresenter presenter)
        {
            if (memoryLight != null)
                memoryLight.ActionRejected -= OnActionRejected;
            memoryLight = light;
            subtitles = presenter;
            if (isActiveAndEnabled && memoryLight != null)
                memoryLight.ActionRejected += OnActionRejected;
        }

        private void OnEnable()
        {
            if (memoryLight != null)
                memoryLight.ActionRejected += OnActionRejected;
        }

        private void OnDisable()
        {
            if (memoryLight != null)
                memoryLight.ActionRejected -= OnActionRejected;
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
        }

        private void OnActionRejected(string message)
        {
            if (subtitles == null)
                return;
            subtitles.Show(string.Empty, message);
            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            subtitles?.Hide();
            _hideRoutine = null;
        }
    }
}
