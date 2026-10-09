namespace LightRemembers.Memory
{
    /// <summary>A memory target whose remembered state is motion rather than restored form.</summary>
    public interface IMemoryEchoable : IMemoryLightTarget
    {
        EchoState State { get; }
        bool TryEcho();
    }
}
