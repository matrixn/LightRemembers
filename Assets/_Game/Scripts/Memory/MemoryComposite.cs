using UnityEngine;

namespace LightRemembers.Memory
{
    /// <summary>One Memory Light target composed from any supported memory abilities.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryComposite : MonoBehaviour, IMemoryLightTarget
    {
        [SerializeField] private MemoryRecallable recall;
        [SerializeField] private MemoryEchoable echo;

        public MemoryRecallable Recall => recall;
        public MemoryEchoable Echo => echo;
        public bool IsRevealed => (recall != null && recall.IsRevealed) || (echo != null && echo.IsRevealed);

        public void Configure(MemoryRecallable recallable, MemoryEchoable echoable)
        {
            recall = recallable;
            echo = echoable;
        }

        public void SetRevealed(bool revealed)
        {
            recall?.SetRevealed(revealed);
            echo?.SetRevealed(revealed);
        }

        public bool TryRecall() => recall != null && recall.TryRecall();
        public bool TryEcho() => echo != null && echo.TryEcho();
    }
}
