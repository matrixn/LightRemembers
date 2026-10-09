using LightRemembers.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace LightRemembers.CameraSystem
{
    /// <summary>Routes the existing Look action into Cinemachine 3 orbital axes.</summary>
    public sealed class CinemachineOrbitInput : MonoBehaviour
    {
        [SerializeField] private CinemachineOrbitalFollow orbitalFollow;
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Min(0f)] private float stickDegreesPerSecond = 120f;

        public void Configure(CinemachineOrbitalFollow orbital, PlayerInputReader reader)
        {
            orbitalFollow = orbital;
            inputReader = reader;
        }

        private void LateUpdate()
        {
            if (orbitalFollow == null || inputReader == null)
                return;

            var multiplier = inputReader.LookIsMouseDelta
                ? mouseSensitivity
                : stickDegreesPerSecond * Time.deltaTime;
            var look = inputReader.Look * multiplier;

            var horizontal = orbitalFollow.HorizontalAxis;
            horizontal.Value += look.x;
            orbitalFollow.HorizontalAxis = horizontal;

            var vertical = orbitalFollow.VerticalAxis;
            vertical.Value = Mathf.Clamp(vertical.Value - look.y, vertical.Range.x, vertical.Range.y);
            orbitalFollow.VerticalAxis = vertical;
        }
    }
}
