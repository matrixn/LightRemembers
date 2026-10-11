using UnityEngine;
using LightRemembers.Player;

namespace LightRemembers.Memory
{
    /// <summary>Drives a small palm glow from the same Memory Light input as the beam.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryLightPalmFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Renderer palmRenderer;
        [SerializeField] private Light palmLight;

        public bool IsActive => palmRenderer != null && palmRenderer.enabled;

        public void Configure(PlayerInputReader reader, Renderer visual, Light light)
        {
            Unbind();
            inputReader = reader;
            palmRenderer = visual;
            palmLight = light;
            Bind();
            SetActive(inputReader != null && inputReader.MemoryLightHeld);
        }

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            Bind();
            SetActive(inputReader != null && inputReader.MemoryLightHeld);
        }

        private void OnDisable() => Unbind();

        private void Bind()
        {
            if (!isActiveAndEnabled || inputReader == null) return;
            inputReader.MemoryLightPressed -= OnPressed;
            inputReader.MemoryLightPressed += OnPressed;
            inputReader.MemoryLightReleased -= OnReleased;
            inputReader.MemoryLightReleased += OnReleased;
        }

        private void Unbind()
        {
            if (inputReader == null) return;
            inputReader.MemoryLightPressed -= OnPressed;
            inputReader.MemoryLightReleased -= OnReleased;
        }

        private void OnPressed() => SetActive(true);
        private void OnReleased() => SetActive(false);

        private void SetActive(bool active)
        {
            if (palmRenderer != null) palmRenderer.enabled = active;
            if (palmLight != null) palmLight.enabled = active;
        }
    }
}
