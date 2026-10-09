using System.Collections;
using LightRemembers.Interaction;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Narrative
{
    [DisallowMultipleComponent]
    public sealed class MemoryEchoSequence : MonoBehaviour
    {
        [SerializeField] private DialogueSequence dialogue;
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField] private GameObject childEcho;
        [SerializeField] private GameObject grandfatherEcho;
        [SerializeField] private GameObject unknownChildSilhouette;
        [SerializeField] private Light oldLight;
        [SerializeField] private MemoryFragmentDefinition fragment;
        [SerializeField] private MemoryFragmentCollector collector;
        [SerializeField] private ExitUnlocker exitUnlocker;
        [SerializeField, Min(0f)] private float lingeringSilhouetteDuration = 1.6f;
        private bool _started;
        private bool _completed;

        public bool HasStarted => _started;
        public bool IsCompleted => _completed;

        public void Configure(DialogueSequence lines, SubtitlePresenter presenter, GameObject child, GameObject grandfather,
            GameObject silhouette, Light chamberLight, MemoryFragmentDefinition memoryFragment,
            MemoryFragmentCollector memoryCollector, ExitUnlocker exit, float lingeringDuration = 1.6f)
        {
            dialogue = lines;
            subtitles = presenter;
            childEcho = child;
            grandfatherEcho = grandfather;
            unknownChildSilhouette = silhouette;
            oldLight = chamberLight;
            fragment = memoryFragment;
            collector = memoryCollector;
            exitUnlocker = exit;
            lingeringSilhouetteDuration = lingeringDuration;
        }

        public void ConfigurePresenter(SubtitlePresenter presenter) => subtitles = presenter;

        public void Begin()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(PlaySequence());
        }

        private IEnumerator PlaySequence()
        {
            if (childEcho != null) childEcho.SetActive(true);
            if (grandfatherEcho != null) grandfatherEcho.SetActive(true);
            if (unknownChildSilhouette != null) unknownChildSilhouette.SetActive(true);
            if (oldLight != null)
            {
                oldLight.enabled = true;
                oldLight.intensity = 1.1f;
                oldLight.color = new Color(1f, 0.66f, 0.28f);
            }

            var lines = dialogue != null ? dialogue.Lines : null;
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    subtitles?.Show(line.speaker, line.text);
                    yield return new WaitForSeconds(Mathf.Max(0.1f, line.duration));
                }
            }

            subtitles?.Hide();
            if (childEcho != null) childEcho.SetActive(false);
            if (grandfatherEcho != null) grandfatherEcho.SetActive(false);
            // The silent third silhouette deliberately lingers after the remembered pair is gone.
            yield return new WaitForSeconds(lingeringSilhouetteDuration);
            if (unknownChildSilhouette != null) unknownChildSilhouette.SetActive(false);

            if (collector != null && fragment != null) collector.TryCollect(fragment);
            exitUnlocker?.Unlock();
            _completed = true;
        }
    }
}
