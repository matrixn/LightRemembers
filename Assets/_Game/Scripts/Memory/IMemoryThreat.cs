namespace LightRemembers.Memory
{
    public enum HollowState
    {
        Dormant,
        Observe,
        Stalk,
        Approach,
        Attack,
        Unstable,
        Repelled,
        Return
    }

    public interface IMemoryThreat
    {
        HollowState State { get; }
        void Wake();
        void SetNarrativeSuppressed(bool suppressed);
    }
}
