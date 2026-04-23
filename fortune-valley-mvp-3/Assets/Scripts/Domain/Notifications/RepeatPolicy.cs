namespace FortuneValley.Domain.Notifications
{
    /// <summary>
    /// Single source of truth for "should this tip fire?". Mutually exclusive
    /// per tip; the cooldownSeconds field on a GuidanceTipSO only applies when
    /// policy is OncePerCooldown.
    /// </summary>
    public enum RepeatPolicy
    {
        EveryTime = 0,
        OncePerSession,
        OncePerPlayer,
        OncePerCooldown
    }
}
