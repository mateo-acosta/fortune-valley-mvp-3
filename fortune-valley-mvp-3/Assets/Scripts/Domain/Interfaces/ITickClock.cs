namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Narrow abstraction over the tick-driven game clock.
    /// Exposes only what coin-collection needs so tests can stub it
    /// without instantiating TimeManager (a MonoBehaviour).
    /// </summary>
    public interface ITickClock
    {
        int TicksPerDay { get; }
    }
}
