using UnityEngine;

namespace LightRemembers.Player
{
    /// <summary>Validates the player composition; individual behaviours own input and motion.</summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader), typeof(PlayerMovement))]
    public sealed class PlayerController : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<PlayerMovement>().InputReader == null)
                Debug.LogError("PlayerMovement requires a PlayerInputReader reference.", this);
        }
    }
}
