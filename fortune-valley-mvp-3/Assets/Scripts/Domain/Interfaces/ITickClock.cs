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

        // Stage 0a alias: same value, new naming. Defaults to TicksPerDay so
        // existing implementers don't have to add the property explicitly.
        int EnginePulsesPerTick => TicksPerDay;
    }
}
