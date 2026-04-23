namespace FortuneValley.Domain.Notifications.Contexts
{
    /// <summary>
    /// Context for a credit-score-changed banner. Only new score is
    /// surfaced here; direction can be expressed in copy via a second tip
    /// (up variant / down variant) routed by whichever event fires.
    /// </summary>
    public readonly struct CreditScoreChangedContext
    {
        public int NewScore { get; }

        public CreditScoreChangedContext(int newScore)
        {
            NewScore = newScore;
        }
    }
}
