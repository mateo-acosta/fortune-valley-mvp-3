namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Service contract for player currency state and mutations.
    /// Reads used by GameSummaryBuilder etc.; write methods used by purchase/reward flows.
    /// </summary>
    public interface ICurrencyService
    {
        float CheckingBalance { get; }
        float InvestingBalance { get; }
        float TotalLiquidBalance { get; }

        bool TrySpendChecking(float amount, string reason = "Unknown");
        void AddToChecking(float amount, string source = "Unknown");
    }
}
