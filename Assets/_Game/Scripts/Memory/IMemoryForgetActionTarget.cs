namespace LightRemembers.Memory
{
    /// <summary>Optional Forget action on the current shared Memory Light target.</summary>
    public interface IMemoryForgetActionTarget
    {
        string LastFailureFeedback { get; }
        bool TryForget();
    }

    public interface IMemoryForgettable : IMemoryLightTarget, IMemoryForgetActionTarget
    {
        ForgetCategory Category { get; }
        ForgetState State { get; }
    }
}
