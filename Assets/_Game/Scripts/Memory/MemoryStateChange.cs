namespace LightRemembers.Memory
{
    public enum MemoryAbility
    {
        Recall,
        Echo,
        Forget
    }

    public readonly struct MemoryStateChange
    {
        public readonly MemoryAbility Ability;
        public readonly bool IsAvailable;

        public MemoryStateChange(MemoryAbility ability, bool isAvailable)
        {
            Ability = ability;
            IsAvailable = isAvailable;
        }
    }
}
