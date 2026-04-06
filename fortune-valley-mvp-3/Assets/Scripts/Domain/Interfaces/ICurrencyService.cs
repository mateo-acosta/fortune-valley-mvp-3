namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Read-only service contract for player currency state.
    /// Used by GameSummaryBuilder and other consumers that only need to read balances.
    /// </summary>
    public interface ICurrencyService
    {
        float CheckingBalance { get; }
        float InvestingBalance { get; }
        float TotalLiquidBalance { get; }
    }
}
