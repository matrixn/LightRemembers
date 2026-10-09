using System.Collections;
using LightRemembers.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ExitUnlocker : InteractableBehaviour
    {
        [SerializeField] private GameObject barrier;
        [SerializeField] private string returnScene = "BoathousePrototype";
        [SerializeField] private SubtitlePresenter subtitles;
        [SerializeField, Min(0f)] private float openDistance = 3.5f;
        [SerializeField, Min(0.05f)] private float openDuration = 0.8f;
        [SerializeField] private bool isUnlocked;

        private Collider[] _doorColliders;
        private Vector3 _closedLocalPosition;
        private Coroutine _opening;

        public bool IsUnlocked => isUnlocked;
        public bool IsDoorOpen { get; private set; }

        public void Configure(GameObject doorBarrier, string destinationScene, SubtitlePresenter presenter = null)
        {
            barrier = doorBarrier;
            returnScene = destinationScene;
            subtitles = presenter;
            CacheDoor();
            SetUnlocked(false);
        }

        public void ConfigurePresenter(SubtitlePresenter presenter) => subtitles = presenter;
        public void Unlock() => SetUnlocked(true);

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            if (barrier == null)
            {
                IsDoorOpen = unlocked;
                return;
            }

            CacheDoor();
            if (!unlocked)
            {
                if (_opening != null)
                    StopCoroutine(_opening);
                _opening = null;
                IsDoorOpen = false;
                barrier.transform.localPosition = _closedLocalPosition;
                SetDoorColliders(true);
                return;
            }

            if (Application.isPlaying)
            {
                if (_opening != null)
                    StopCoroutine(_opening);
                _opening = StartCoroutine(OpenDoor());
            }
            else
            {
                OpenDoorImmediately();
            }
        }

        public override bool CanInteract(Transform interactor) => isUnlocked && IsDoorOpen;

        public override void Interact(Transform interactor)
        {
            if (!CanInteract(interactor))
                return;
            subtitles?.Hide();
            if (!string.IsNullOrWhiteSpace(returnScene))
                SceneManager.LoadScene(returnScene);
        }

        private void Awake()
        {
            CacheDoor();
            if (!isUnlocked)
                SetDoorColliders(true);
        }

        private void OnDestroy()
        {
            if (_opening != null)
                StopCoroutine(_opening);
        }

        private IEnumerator OpenDoor()
        {
            var start = barrier.transform.localPosition;
            var destination = _closedLocalPosition + Vector3.right * openDistance;
            var elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed = Mathf.Min(openDuration, elapsed + Time.deltaTime);
                var t = elapsed / openDuration;
                t = t * t * (3f - 2f * t);
                barrier.transform.localPosition = Vector3.Lerp(start, destination, t);
                yield return null;
            }
            barrier.transform.localPosition = destination;
            SetDoorColliders(false);
            IsDoorOpen = true;
            _opening = null;
        }

        private void OpenDoorImmediately()
        {
            barrier.transform.localPosition = _closedLocalPosition + Vector3.right * openDistance;
            SetDoorColliders(false);
            IsDoorOpen = true;
        }

        private void CacheDoor()
        {
            if (barrier == null)
                return;
            if (_doorColliders == null || _doorColliders.Length == 0)
                _doorColliders = barrier.GetComponentsInChildren<Collider>(true);
            if (!IsDoorOpen)
                _closedLocalPosition = barrier.transform.localPosition;
        }

        private void SetDoorColliders(bool enabled)
        {
            if (_doorColliders == null)
                return;
            foreach (var doorCollider in _doorColliders)
                if (doorCollider != null)
                    doorCollider.enabled = enabled;
        }
    }
}
