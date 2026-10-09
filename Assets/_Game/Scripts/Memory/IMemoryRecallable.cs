namespace LightRemembers.Memory
{
    /// <summary>A revealable memory that can temporarily become physically present.</summary>
    public interface IMemoryRecallable : IMemoryLightTarget
    {
        RecallState State { get; }
        bool TryRecall();
    }
}
