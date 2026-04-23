namespace FortuneValley.Managers.Notifications
{
    /// <summary>
    /// Produces a (title, message) pair from a typed event context and the
    /// title/message templates authored on a GuidanceTipSO. One
    /// implementation per trigger kind; each builder knows the positional
    /// argument order for its own context type, so templates stay
    /// compile-time safe (no reflection, no runtime placeholder parsing).
    /// </summary>
    public interface IBannerMessageBuilder<TContext>
    {
        (string title, string message) Build(string titleTemplate, string messageTemplate, TContext context);
    }
}
