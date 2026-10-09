using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightRemembers.Interaction
{
    [DisallowMultipleComponent]
    public sealed class ExitUnlocker : MonoBehaviour
    {
        [SerializeField] private GameObject barrier;
        [SerializeField] private string returnScene = "BoathousePrototype";
        [SerializeField] private bool isUnlocked;

        public bool IsUnlocked => isUnlocked;

        public void Configure(GameObject doorBarrier, string destinationScene)
        {
            barrier = doorBarrier;
            returnScene = destinationScene;
            SetUnlocked(false);
        }

        public void Unlock() => SetUnlocked(true);

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            if (barrier != null) barrier.SetActive(!unlocked);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isUnlocked || other.GetComponentInParent<CharacterController>() == null) return;
            if (!string.IsNullOrWhiteSpace(returnScene)) SceneManager.LoadScene(returnScene);
        }
    }
}
