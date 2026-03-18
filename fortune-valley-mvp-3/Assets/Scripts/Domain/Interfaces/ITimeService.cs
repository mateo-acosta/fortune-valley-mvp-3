namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Service contract for game time management.
    /// </summary>
    public interface ITimeService
    {
        int CurrentTick { get; }
        void StartTime();
        void StopTime();
    }
}
