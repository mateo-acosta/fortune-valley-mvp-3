namespace FortuneValley.Core
{
    /// <summary>
    /// Service contract for the investment portfolio system.
    /// Lives in Core (not Domain) because the full implementation depends on
    /// InvestmentDefinition, a ScriptableObject that requires Unity engine APIs.
    /// </summary>
    public interface IInvestmentService
    {
        float TotalPortfolioValue { get; }
        float LifetimeTotalGain { get; }
        float PeakPortfolioValue { get; }
        float LifetimeTotalPrincipalInvested { get; }
        int LifetimeTotalInvestmentsMade { get; }
        string GetPortfolioSummary();
    }
}
