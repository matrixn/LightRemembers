using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>One Memory Light target composed from any supported memory abilities.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryComposite : MonoBehaviour, IMemoryLightTarget, IMemoryForgetActionTarget
    {
        [SerializeField] private MemoryRecallable recall;
        [SerializeField] private MemoryEchoable echo;
        [SerializeField] private MemoryForgettable forget;

        public MemoryRecallable Recall => recall;
        public MemoryEchoable Echo => echo;
        public MemoryForgettable Forget => forget;
        public string LastFailureFeedback => forget != null && !string.IsNullOrEmpty(forget.LastFailureFeedback)
            ? forget.LastFailureFeedback
            : echo != null ? echo.LastFailureFeedback : null;
        public bool IsRevealed => (recall != null && recall.IsRevealed) || (echo != null && echo.IsRevealed) ||
            (forget != null && forget.IsRevealed);

        public void Configure(MemoryRecallable recallable, MemoryEchoable echoable, MemoryForgettable forgettable = null)
        {
            recall = recallable;
            echo = echoable;
            forget = forgettable;
        }

        public void SetRevealed(bool revealed)
        {
            recall?.SetRevealed(revealed);
            echo?.SetRevealed(revealed);
            forget?.SetRevealed(revealed);
        }

        public bool TryRecall() => recall != null && recall.TryRecall();
        public bool TryEcho() => echo != null && echo.TryEcho();
        public bool TryForget() => forget != null && forget.TryForget();
    }
}
