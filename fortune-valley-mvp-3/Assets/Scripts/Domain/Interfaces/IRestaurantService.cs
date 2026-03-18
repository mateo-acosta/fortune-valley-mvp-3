namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Service contract for the restaurant income system.
    /// </summary>
    public interface IRestaurantService
    {
        float TotalEarned { get; }
        int CurrentLevel { get; }
        string GetPerformanceSummary();
    }
}
