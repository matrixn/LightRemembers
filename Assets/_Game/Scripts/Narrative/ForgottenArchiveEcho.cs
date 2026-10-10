using System.Collections;
using LightRemembers.Memory;
using LightRemembers.UI;
using UnityEngine;

namespace LightRemembers.Narrative
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ForgottenArchiveEcho : MonoBehaviour
    {
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField] private GameObject unknownChild;
        [SerializeField] private Transform adultPlayer;
        [SerializeField] private MemoryFragmentDefinition fragment;
        [SerializeField] private MemoryFragmentCollector collector;
        [SerializeField, Min(0f)] private float lineGap = 1.1f;
        private bool _played;

        public bool HasPlayed => _played;

        public void Configure(SubtitlePresenter presenter, GameObject child, Transform player,
            MemoryFragmentDefinition memoryFragment, MemoryFragmentCollector memoryCollector, float gap = 1.1f)
        {
            subtitles = presenter;
            unknownChild = child;
            adultPlayer = player;
            fragment = memoryFragment;
            collector = memoryCollector;
            lineGap = Mathf.Max(0f, gap);
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_played || other.GetComponentInParent<PlayerMemoryLight>() == null)
                return;
            _played = true;
            adultPlayer = other.transform.root;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            if (unknownChild != null)
                unknownChild.SetActive(true);
            yield return Say("Unknown Child", "Close your eyes.");
            yield return Say("Child", "Why?");
            yield return Say("Unknown Child", "If you can't see me, I'm not here.");
            yield return Say("Child", "That's not how it works.");
            yield return Say("Unknown Child", "It is if you really forget.");
            yield return Say("Grandfather", "Children? Is someone down there?");
            subtitles?.Hide();
            if (unknownChild != null)
                unknownChild.SetActive(false);
            yield return new WaitForSeconds(1.4f);

            if (adultPlayer != null)
                transform.rotation = Quaternion.LookRotation(adultPlayer.forward, Vector3.up);
            yield return Say("Unknown Child", "See?");
            subtitles?.Hide();
            if (collector != null && fragment != null)
                collector.TryCollect(fragment);
        }

        private IEnumerator Say(string speaker, string line)
        {
            subtitles?.Show(speaker, line);
            yield return new WaitForSeconds(lineGap + Mathf.Max(0.8f, line.Length * 0.035f));
        }
    }
}
