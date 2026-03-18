using FortuneValley.Domain.Enums;

namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Service contract for the city lot ownership system.
    /// </summary>
    public interface ICityService
    {
        int PlayerLotCount { get; }
        int RivalLotCount { get; }
        int TotalLots { get; }
        Owner GetOwner(string lotId);
        string GetCitySummary();
    }
}
