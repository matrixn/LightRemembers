using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>Authored waypoint route for one forward Echo and its optional safe return.</summary>
    [DisallowMultipleComponent]
    public sealed class EchoPath : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField, Min(0.1f)] private float legDuration = 6f;
        [SerializeField, Min(0f)] private float destinationPause = 2f;
        [SerializeField] private bool returnToStart = true;

        public Transform[] Waypoints => waypoints;
        public float LegDuration => legDuration;
        public float DestinationPause => destinationPause;
        public bool ReturnToStart => returnToStart;
        public bool IsValid => waypoints != null && waypoints.Length >= 2 &&
            System.Array.TrueForAll(waypoints, point => point != null);

        public void Configure(Transform[] points, float duration, float pause, bool returnAfterEcho)
        {
            waypoints = points;
            legDuration = Mathf.Max(0.1f, duration);
            destinationPause = Mathf.Max(0f, pause);
            returnToStart = returnAfterEcho;
        }

        public bool TryEvaluate(float normalizedTime, out Vector3 position, out Quaternion rotation)
        {
            position = transform.position;
            rotation = transform.rotation;
            if (!IsValid)
                return false;

            var lastSegment = waypoints.Length - 2;
            var scaled = Mathf.Clamp01(normalizedTime) * (lastSegment + 1);
            var segment = Mathf.Min(Mathf.FloorToInt(scaled), lastSegment);
            var t = segment == lastSegment && normalizedTime >= 1f ? 1f : scaled - segment;
            position = Vector3.Lerp(waypoints[segment].position, waypoints[segment + 1].position, t);
            rotation = Quaternion.Slerp(waypoints[segment].rotation, waypoints[segment + 1].rotation, t);
            return true;
        }

        private void OnValidate()
        {
            legDuration = Mathf.Max(0.1f, legDuration);
            destinationPause = Mathf.Max(0f, destinationPause);
        }
    }
}
