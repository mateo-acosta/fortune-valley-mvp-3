namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Service contract for player currency management.
    /// </summary>
    public interface ICurrencyService
    {
        float Balance { get; }
        void Add(float amount, string source = "Unknown");
        bool TrySpend(float amount, string reason = "Unknown");
        bool CanAfford(float amount);
    }
}
