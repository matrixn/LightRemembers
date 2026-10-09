namespace LightRemembers.Memory
{
    public interface IMemoryLightTarget
    {
        bool IsRevealed { get; }
        void SetRevealed(bool revealed);
    }
}
